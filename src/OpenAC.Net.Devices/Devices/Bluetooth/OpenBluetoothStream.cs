// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 28-10-2026
//
// Last Modified By : RFTD
// Last Modified On : 28-10-2026
// ***********************************************************************
// <copyright file="OpenBluetoothStream.cs" company="OpenAC .Net">
//		        		   The MIT License (MIT)
//	     		    Copyright (c) 2014 - 2026 Projeto OpenAC .Net
//
//	 Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//	 The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//	 The SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.Devices.Commom;

namespace OpenAC.Net.Devices;

/// <summary>
/// Implementa um stream de comunicação Bluetooth RFCOMM / SPP nativo para Windows (Winsock AF_BTH), Linux (BlueZ AF_BLUETOOTH / rfcomm) e macOS (IOBluetooth / cu.*).
/// </summary>
internal sealed class OpenBluetoothStream : OpenDeviceStream<BluetoothConfig>
{
    #region Fields

    private Socket? windowsSocket;
    private Stream? activeStream;
    private int linuxSocketFd = -1;
    private readonly ulong btAddress;
    private readonly bool isDevicePath;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="OpenBluetoothStream"/>.
    /// </summary>
    /// <param name="config">Configuração Bluetooth a ser utilizada.</param>
    public OpenBluetoothStream(BluetoothConfig config) : base(config)
    {
        if (config.Endereco!.IsEmpty())
            throw new ArgumentException("O endereço Bluetooth não foi informado.", nameof(config));

        var trimmed = config.Endereco!.Trim();
        if (trimmed.StartsWith("/dev/", StringComparison.Ordinal))
        {
            isDevicePath = true;
        }
        else if (!BluetoothConfig.TryParseAddress(trimmed, out btAddress))
        {
            throw new ArgumentException($"Endereço Bluetooth inválido: '{config.Endereco}'. Use o formato XX:XX:XX:XX:XX:XX, /dev/rfcomm0 ou /dev/cu.*.", nameof(config));
        }
    }

    #endregion Constructors

    #region Properties

    /// <inheritdoc />
    protected override int Available
    {
        get
        {
            if (windowsSocket is { Connected: true })
                return windowsSocket.Available;

            if (linuxSocketFd >= 0)
            {
                return LinuxBlueZ.ioctl(linuxSocketFd, LinuxBlueZ.FIONREAD, out var available) == 0 && available > 0
                    ? available
                    : 0;
            }

            if (activeStream is { CanRead: true, CanSeek: true })
            {
                try
                {
                    return (int)Math.Max(0, activeStream.Length - activeStream.Position);
                }
                catch
                {
                    return 0;
                }
            }

            return 0;
        }
    }

    #endregion Properties

    #region Methods

    /// <inheritdoc />
    public override void Limpar()
    {
        if (activeStream == null) return;

        var bufferSize = Math.Max(Config.ReadBufferSize, 1);
        var buffer = new byte[bufferSize];

        while (Available > 0)
        {
            var toRead = Math.Min(Available, buffer.Length);
            var read = activeStream.Read(buffer, 0, toRead);
            if (read < 1) break;
        }
    }

    /// <inheritdoc />
    protected override bool OpenInternal()
    {
        if (activeStream != null) return false;

        DisposeResources();

        if (isDevicePath)
        {
            var bufferSize = Math.Max(Math.Max(Config.ReadBufferSize, Config.WriteBufferSize), 1);
            activeStream = new FileStream(Config.Endereco!.Trim(), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize);
        }
        else if (OSPlatformHelper.IsMacOS)
        {
            activeStream = OpenMacOsBluetoothStream();
        }
        else if (OSPlatformHelper.IsLinux)
        {
            activeStream = OpenLinuxRfcommStream();
        }
        else
        {
            activeStream = OpenWindowsRfcommStream();
        }

        Reader = new BinaryReader(activeStream);
        Writer = new BinaryWriter(activeStream);

        return true;
    }

    /// <inheritdoc />
    protected override bool CloseInternal()
    {
        if (activeStream == null && windowsSocket == null && linuxSocketFd < 0)
            return false;

        DisposeResources();
        return true;
    }

    private Stream OpenMacOsBluetoothStream()
    {
        var bufferSize = Math.Max(Math.Max(Config.ReadBufferSize, Config.WriteBufferSize), 1);

        // 1. Tenta resolver para uma porta serial virtual /dev/cu.<NomeDoDispositivo> criada pelo macOS
        var devices = BluetoothConfig.GetDevices();
        var matchedByMac = devices.FirstOrDefault(d => d.RawAddress == btAddress && !string.IsNullOrWhiteSpace(d.Name));
        if (matchedByMac != null && !string.IsNullOrWhiteSpace(matchedByMac.Name))
        {
            var normalizedName = matchedByMac.Name!.Replace(" ", "-");
            var cuMatch = devices.FirstOrDefault(d =>
                d.Address.StartsWith("/dev/cu.", StringComparison.Ordinal) &&
                (d.Address.IndexOf(matchedByMac.Name!, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 d.Address.IndexOf(normalizedName, StringComparison.OrdinalIgnoreCase) >= 0));

            if (cuMatch != null && File.Exists(cuMatch.Address))
            {
                return new FileStream(cuMatch.Address, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize);
            }
        }

        // 2. Caso não exista /dev/cu.* associado, abre canal RFCOMM direto via IOBluetooth.framework (Objective-C runtime)
        var channelId = (byte)(Config.Canal > 0 ? Config.Canal : 1);
        return new MacOsRfcommStream(BluetoothConfig.FormatAddress(btAddress).Replace(':', '-'), channelId);
    }

    private Stream OpenWindowsRfcommStream()
    {
        const AddressFamily AfBth = (AddressFamily)32;
        const ProtocolType BthProtoRfcomm = (ProtocolType)3;

        var socket = new Socket(AfBth, SocketType.Stream, BthProtoRfcomm)
        {
            ReceiveTimeout = Config.TimeOut * 1000,
            SendTimeout = Config.TimeOut * 1000,
            ReceiveBufferSize = Math.Max(Config.ReadBufferSize, 1),
            SendBufferSize = Math.Max(Config.WriteBufferSize, 1)
        };

        try
        {
            var endPoint = new WindowsBluetoothEndPoint(btAddress, Config.ServiceGuid, Config.Canal);
            socket.Connect(endPoint);
            windowsSocket = socket;
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private Stream OpenLinuxRfcommStream()
    {
        var fd = LinuxBlueZ.socket(LinuxBlueZ.AF_BLUETOOTH, LinuxBlueZ.SOCK_STREAM, LinuxBlueZ.BTPROTO_RFCOMM);
        if (fd < 0)
        {
            var errno = Marshal.GetLastWin32Error();
            throw new IOException($"Erro ao criar socket Bluetooth RFCOMM no Linux (errno={errno}).");
        }

        try
        {
            var timeoutSec = Math.Max(Config.TimeOut, 1);
            var tv = new LinuxBlueZ.TimeVal { tv_sec = timeoutSec, tv_usec = 0 };
            LinuxBlueZ.setsockopt(fd, LinuxBlueZ.SOL_SOCKET, LinuxBlueZ.SO_RCVTIMEO, ref tv, Marshal.SizeOf<LinuxBlueZ.TimeVal>());
            LinuxBlueZ.setsockopt(fd, LinuxBlueZ.SOL_SOCKET, LinuxBlueZ.SO_SNDTIMEO, ref tv, Marshal.SizeOf<LinuxBlueZ.TimeVal>());

            var addr = new LinuxBlueZ.SockAddrRc
            {
                rc_family = LinuxBlueZ.AF_BLUETOOTH,
                rc_bdaddr = new byte[6],
                rc_channel = (byte)(Config.Canal > 0 ? Config.Canal : 1)
            };

            // BlueZ armazena o endereço Bluetooth em ordem little-endian (bytes invertidos)
            for (var i = 0; i < 6; i++)
            {
                addr.rc_bdaddr[i] = (byte)((btAddress >> (i * 8)) & 0xFF);
            }

            var res = LinuxBlueZ.connect(fd, ref addr, Marshal.SizeOf<LinuxBlueZ.SockAddrRc>());
            if (res < 0)
            {
                var errno = Marshal.GetLastWin32Error();
                throw new IOException($"Não foi possível conectar ao dispositivo Bluetooth {BluetoothConfig.FormatAddress(btAddress)} no canal {addr.rc_channel} (errno={errno}).");
            }

            linuxSocketFd = fd;
            var safeHandle = new SafeFileHandle(new IntPtr(fd), ownsHandle: true);
            var bufferSize = Math.Max(Math.Max(Config.ReadBufferSize, Config.WriteBufferSize), 1);
            return new FileStream(safeHandle, FileAccess.ReadWrite, bufferSize, isAsync: false);
        }
        catch
        {
            LinuxBlueZ.close(fd);
            throw;
        }
    }

    private void DisposeResources()
    {
        try
        {
            Reader?.Dispose();
        }
        catch
        {
            // Ignora erro no descarte
        }

        try
        {
            Writer?.Dispose();
        }
        catch
        {
            // Ignora erro no descarte
        }

        try
        {
            activeStream?.Dispose();
        }
        catch
        {
            // Ignora erro no descarte
        }

        try
        {
            windowsSocket?.Dispose();
        }
        catch
        {
            // Ignora erro no descarte
        }

        Reader = null;
        Writer = null;
        activeStream = null;
        windowsSocket = null;
        linuxSocketFd = -1;
    }

    #endregion Methods

    #region Dispose Methods

    /// <inheritdoc />
    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        DisposeResources();
    }

    #endregion Dispose Methods

    #region InnerTypes

    /// <summary>
    /// Representa a estrutura <c>SOCKADDR_BTH</c> (30 bytes, alinhamento de 1 byte) usada pelo Winsock Bluetooth no Windows.
    /// </summary>
    internal sealed class WindowsBluetoothEndPoint : EndPoint
    {
        private const AddressFamily AfBth = (AddressFamily)32;
        private const int SockAddrBthSize = 30;

        private readonly ulong address;
        private readonly Guid serviceClassId;
        private readonly int port;

        public WindowsBluetoothEndPoint(ulong address, Guid serviceClassId, int port)
        {
            this.address = address;
            this.serviceClassId = serviceClassId;
            this.port = port;
        }

        public override AddressFamily AddressFamily => AfBth;

        public override SocketAddress Serialize()
        {
            var sa = new SocketAddress(AfBth, SockAddrBthSize);

            // Offset 0..1: addressFamily (AF_BTH = 32, little-endian)
            sa[0] = 32;
            sa[1] = 0;

            // Offset 2..9: btAddr (ulong, 8 bytes little-endian)
            for (var i = 0; i < 8; i++)
            {
                sa[2 + i] = (byte)((address >> (i * 8)) & 0xFF);
            }

            // Offset 10..25: serviceClassId (Guid, 16 bytes)
            var guidBytes = (port > 0 ? Guid.Empty : serviceClassId).ToByteArray();
            for (var i = 0; i < 16; i++)
            {
                sa[10 + i] = guidBytes[i];
            }

            // Offset 26..29: port (uint, 4 bytes little-endian; 0 = BT_PORT_ANY quando usa ServiceClassId)
            var portVal = (uint)(port > 0 ? port : 0);
            sa[26] = (byte)(portVal & 0xFF);
            sa[27] = (byte)((portVal >> 8) & 0xFF);
            sa[28] = (byte)((portVal >> 16) & 0xFF);
            sa[29] = (byte)((portVal >> 24) & 0xFF);

            return sa;
        }

        public override EndPoint Create(SocketAddress socketAddress)
        {
            if (socketAddress.Size < SockAddrBthSize)
                return new WindowsBluetoothEndPoint(0, Guid.Empty, 0);

            ulong addr = 0;
            for (var i = 0; i < 8; i++)
            {
                addr |= (ulong)socketAddress[2 + i] << (i * 8);
            }

            var guidBytes = new byte[16];
            for (var i = 0; i < 16; i++)
            {
                guidBytes[i] = socketAddress[10 + i];
            }

            var p = socketAddress[26]
                    | (socketAddress[27] << 8)
                    | (socketAddress[28] << 16)
                    | (socketAddress[29] << 24);

            return new WindowsBluetoothEndPoint(addr, new Guid(guidBytes), p);
        }
    }

    private static class LinuxBlueZ
    {
        public const int AF_BLUETOOTH = 31;
        public const int SOCK_STREAM = 1;
        public const int BTPROTO_RFCOMM = 3;
        public const int SOL_SOCKET = 1;
        public const int SO_RCVTIMEO = 20;
        public const int SO_SNDTIMEO = 21;
        public const uint FIONREAD = 0x541B;

        [StructLayout(LayoutKind.Sequential)]
        public struct SockAddrRc
        {
            public ushort rc_family;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] rc_bdaddr;
            public byte rc_channel;
            public byte padding;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TimeVal
        {
            public long tv_sec;
            public long tv_usec;
        }

        [DllImport("libc", SetLastError = true)]
        public static extern int socket(int domain, int type, int protocol);

        [DllImport("libc", SetLastError = true)]
        public static extern int connect(int sockfd, ref SockAddrRc addr, int addrlen);

        [DllImport("libc", SetLastError = true)]
        public static extern int setsockopt(int sockfd, int level, int optname, ref TimeVal optval, int optlen);

        [DllImport("libc", SetLastError = true)]
        public static extern int ioctl(int fd, uint request, out int argp);

        [DllImport("libc", SetLastError = true)]
        public static extern int close(int fd);
    }

    /// <summary>
    /// Stream Bluetooth RFCOMM nativo para macOS utilizando <c>IOBluetooth.framework</c> via Objective-C Runtime (<c>libobjc.dylib</c>).
    /// </summary>
    private sealed class MacOsRfcommStream : Stream
    {
        private IntPtr btDevice;
        private IntPtr rfcommChannel;

        public MacOsRfcommStream(string macAddressDashFormat, byte channelId)
        {
            var fw = dlopen("/System/Library/Frameworks/IOBluetooth.framework/IOBluetooth", RTLD_LAZY);
            if (fw == IntPtr.Zero)
                throw new IOException("Não foi possível carregar o framework IOBluetooth.framework no macOS.");

            var nsStringClass = objc_getClass("NSString");
            var selStringWithUtf8 = sel_registerName("stringWithUTF8String:");
            var nsAddress = objc_msgSend_IntPtr_String(nsStringClass, selStringWithUtf8, macAddressDashFormat);

            var ioBtDeviceClass = objc_getClass("IOBluetoothDevice");
            var selDeviceWithAddr = sel_registerName("deviceWithAddressString:");
            btDevice = objc_msgSend_IntPtr_IntPtr(ioBtDeviceClass, selDeviceWithAddr, nsAddress);
            if (btDevice == IntPtr.Zero)
                throw new IOException($"Dispositivo Bluetooth '{macAddressDashFormat}' não pôde ser instanciado via IOBluetooth.");

            var selOpenRfcomm = sel_registerName("openRFCOMMChannelSync:withChannelID:delegate:");
            var result = objc_msgSend_OpenRfcomm(btDevice, selOpenRfcomm, out rfcommChannel, channelId, IntPtr.Zero);
            if (result != 0 || rfcommChannel == IntPtr.Zero)
                throw new IOException($"Falha ao abrir canal RFCOMM {channelId} no macOS para '{macAddressDashFormat}' (IOReturn=0x{result:X8}).");

            objc_msgSend_Void(rfcommChannel, sel_registerName("retain"));
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => rfcommChannel != IntPtr.Zero;
        public override long Length => 0;
        public override long Position { get => 0; set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (rfcommChannel == IntPtr.Zero)
                throw new ObjectDisposedException(nameof(MacOsRfcommStream));

            if (count <= 0) return;

            var ptr = Marshal.AllocHGlobal(count);
            try
            {
                Marshal.Copy(buffer, offset, ptr, count);
                var selWriteSync = sel_registerName("writeSync:length:");
                var status = objc_msgSend_WriteSync(rfcommChannel, selWriteSync, ptr, (ushort)Math.Min(count, ushort.MaxValue));
                if (status != 0)
                    throw new IOException($"Erro ao enviar dados via IOBluetoothRFCOMMChannel no macOS (IOReturn=0x{status:X8}).");
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (rfcommChannel != IntPtr.Zero)
            {
                try
                {
                    objc_msgSend_Void(rfcommChannel, sel_registerName("closeChannel"));
                    objc_msgSend_Void(rfcommChannel, sel_registerName("release"));
                }
                catch
                {
                    // Ignora erro ao fechar canal
                }

                rfcommChannel = IntPtr.Zero;
            }

            if (btDevice != IntPtr.Zero)
            {
                try
                {
                    objc_msgSend_Void(btDevice, sel_registerName("closeConnection"));
                }
                catch
                {
                    // Ignora erro ao fechar conexão
                }

                btDevice = IntPtr.Zero;
            }

            base.Dispose(disposing);
        }

        private const int RTLD_LAZY = 0x1;

        [DllImport("/usr/lib/libSystem.dylib")]
        private static extern IntPtr dlopen(string path, int mode);

        [DllImport("/usr/lib/libobjc.dylib")]
        private static extern IntPtr objc_getClass(string name);

        [DllImport("/usr/lib/libobjc.dylib")]
        private static extern IntPtr sel_registerName(string name);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend_IntPtr_String(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.LPStr)] string arg1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern int objc_msgSend_OpenRfcomm(IntPtr receiver, IntPtr selector, out IntPtr rfcommChannel, byte channelId, IntPtr channelDelegate);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern int objc_msgSend_WriteSync(IntPtr receiver, IntPtr selector, IntPtr data, ushort length);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern void objc_msgSend_Void(IntPtr receiver, IntPtr selector);
    }

    #endregion InnerTypes
}
