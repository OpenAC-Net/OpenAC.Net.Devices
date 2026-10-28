// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 28-10-2026
//
// Last Modified By : RFTD
// Last Modified On : 28-10-2026
// ***********************************************************************
// <copyright file="OpenUsbStream.cs" company="OpenAC .Net">
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
//	 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.Devices.Commom;

namespace OpenAC.Net.Devices;

/// <summary>
/// Implementa um stream de comunicação direta com dispositivos USB (Windows CreateFile / USBPRINT, Linux usblp e macOS IOKit / CUPS).
/// </summary>
internal sealed class OpenUsbStream : OpenDeviceStream<UsbConfig>
{
    #region Fields

    private Stream? deviceStream;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="OpenUsbStream"/>.
    /// </summary>
    /// <param name="config">Configuração do dispositivo USB.</param>
    public OpenUsbStream(UsbConfig config) : base(config)
    {
        if (config.DevicePath!.IsEmpty() && !config.VendorId.HasValue && !config.ProductId.HasValue)
            throw new ArgumentException("É necessário informar o DevicePath ou o VendorId/ProductId na configuração USB.", nameof(config));
    }

    #endregion Constructors

    #region Properties

    /// <inheritdoc />
    protected override int Available
    {
        get
        {
            if (deviceStream is not { CanRead: true, CanSeek: true })
                return 0;

            try
            {
                return (int)Math.Max(0, deviceStream.Length - deviceStream.Position);
            }
            catch
            {
                return 0;
            }
        }
    }

    #endregion Properties

    #region Methods

    /// <inheritdoc />
    protected override bool OpenInternal()
    {
        if (deviceStream != null) return false;

        var targetPath = ResolveDevicePath();

        if (targetPath.StartsWith("cups:", StringComparison.OrdinalIgnoreCase))
        {
            var queueName = targetPath.Substring("cups:".Length).Trim();
            deviceStream = new RawPrinterStream(queueName);
        }
        else if (targetPath.StartsWith("ioreg://", StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException(
                $"O dispositivo USB ({targetPath}) foi localizado no IOKit do macOS, mas não expõe um nó '/dev/cu.*' nem uma fila CUPS configurada. Configure a impressora no macOS ou informe um caminho '/dev/cu.*'.");
        }
        else if (OSPlatformHelper.IsLinux || OSPlatformHelper.IsMacOS)
        {
            deviceStream = OpenUnixDeviceStream(targetPath);
        }
        else
        {
            deviceStream = OpenWindowsDeviceStream(targetPath);
        }

        Writer = new BinaryWriter(deviceStream);
        if (deviceStream.CanRead)
            Reader = new BinaryReader(deviceStream);

        return true;
    }

    /// <inheritdoc />
    protected override bool CloseInternal()
    {
        if (deviceStream == null && Writer == null && Reader == null) return false;

        DisposeStream();
        return true;
    }

    private string ResolveDevicePath()
    {
        if (!Config.DevicePath!.IsEmpty())
            return Config.DevicePath!;

        var devices = UsbConfig.GetDevices(Config.InterfaceGuid);
        var match = devices.FirstOrDefault(d =>
            (!Config.VendorId.HasValue || d.VendorId == Config.VendorId.Value) &&
            (!Config.ProductId.HasValue || d.ProductId == Config.ProductId.Value));

        if (match == null)
        {
            var vidInfo = Config.VendorId.HasValue ? $"VID=0x{Config.VendorId.Value:X4}" : "VID=*";
            var pidInfo = Config.ProductId.HasValue ? $"PID=0x{Config.ProductId.Value:X4}" : "PID=*";
            throw new IOException($"Dispositivo USB ({vidInfo}, {pidInfo}) não localizado.");
        }

        return match.DevicePath;
    }

    private FileStream OpenUnixDeviceStream(string path)
    {
        var bufferSize = Math.Max(Math.Max(Config.ReadBufferSize, Config.WriteBufferSize), 1);
        try
        {
            return new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize);
        }
        catch (UnauthorizedAccessException)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, bufferSize);
        }
        catch (IOException)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, bufferSize);
        }
    }

    private FileStream OpenWindowsDeviceStream(string path)
    {
        var bufferSize = Math.Max(Math.Max(Config.ReadBufferSize, Config.WriteBufferSize), 1);

        var handle = CreateFile(
            path,
            GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            IntPtr.Zero);

        var access = FileAccess.ReadWrite;
        if (handle.IsInvalid)
        {
            handle.Dispose();
            handle = CreateFile(
                path,
                GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);
            access = FileAccess.Write;
        }

        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new IOException($"Não foi possível abrir o dispositivo USB '{path}'.", new Win32Exception(error));
        }

        return new FileStream(handle, access, bufferSize, isAsync: false);
    }

    private void DisposeStream()
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
            deviceStream?.Dispose();
        }
        catch
        {
            // Ignora erro no descarte
        }

        Reader = null;
        Writer = null;
        deviceStream = null;
    }

    #endregion Methods

    #region Dispose Methods

    /// <inheritdoc />
    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        DisposeStream();
    }

    #endregion Dispose Methods

    #region Win32 Native

    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    #endregion Win32 Native
}
