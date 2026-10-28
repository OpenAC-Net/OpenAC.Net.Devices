// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 28-10-2026
//
// Last Modified By : RFTD
// Last Modified On : 28-10-2026
// ***********************************************************************
// <copyright file="UsbConfig.cs" company="OpenAC .Net">
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using OpenAC.Net.Devices.Commom;

namespace OpenAC.Net.Devices;

/// <summary>
/// Configuração para comunicação direta com dispositivos USB (Windows SetupAPI / USBPRINT, Linux usblp e macOS IOKit / CUPS).
/// </summary>
public sealed class UsbConfig : BaseConfig
{
    #region Constants

    /// <summary>
    /// GUID padrão da classe de interface de impressoras USB no Windows (<c>GUID_DEVINTERFACE_USBPRINT</c>).
    /// </summary>
    public static readonly Guid UsbPrintInterfaceGuid = new("28d78fad-5a12-11d1-ae5b-0000f803a8c2");

    private static readonly Regex VidPidRegex = new(@"vid_([0-9a-fA-F]{4}).*?pid_([0-9a-fA-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex IoRegNodeRegex = new(@"\+-o\s+([^\s<@]+)", RegexOptions.Compiled);
    private static readonly Regex IoRegVendorRegex = new(@"""idVendor""\s*=\s*(0x[0-9a-fA-F]+|\d+)", RegexOptions.Compiled);
    private static readonly Regex IoRegProductRegex = new(@"""idProduct""\s*=\s*(0x[0-9a-fA-F]+|\d+)", RegexOptions.Compiled);
    private static readonly Regex IoRegProductNameRegex = new(@"""(?:USB Product Name|kUSBProductString)""\s*=\s*""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex IoRegCalloutRegex = new(@"""IOCalloutDevice""\s*=\s*""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex LpStatUsbRegex = new(@"^(?:device for|dispositivo para)\s+([^\s:]+):\s*(usb://[^\s]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    #endregion Constants

    #region Fields

    private string? devicePath;
    private int? vendorId;
    private int? productId;
    private Guid interfaceGuid = UsbPrintInterfaceGuid;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="UsbConfig"/> com valores padrão.
    /// </summary>
    public UsbConfig() : base("USB")
    {
    }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="UsbConfig"/> informando o caminho do dispositivo.
    /// </summary>
    /// <param name="devicePath">Caminho do dispositivo USB no sistema operacional.</param>
    public UsbConfig(string devicePath) : this()
    {
        this.devicePath = devicePath;
    }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="UsbConfig"/> informando o Vendor ID (VID) e Product ID (PID).
    /// </summary>
    /// <param name="vendorId">Identificador do fabricante (VID).</param>
    /// <param name="productId">Identificador do produto (PID).</param>
    public UsbConfig(int vendorId, int productId) : this()
    {
        this.vendorId = vendorId;
        this.productId = productId;
    }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="UsbConfig"/> a partir de um dispositivo localizado.
    /// </summary>
    /// <param name="deviceInfo">Informações do dispositivo, incluindo caminho e VID/PID, quando disponíveis.</param>
    /// <exception cref="ArgumentNullException">Lançada quando <paramref name="deviceInfo"/> é nulo.</exception>
    public UsbConfig(UsbDeviceInfo deviceInfo) : this()
    {
        if (deviceInfo == null) throw new ArgumentNullException(nameof(deviceInfo));

        DevicePath = deviceInfo.DevicePath;
        VendorId = deviceInfo.VendorId;
        ProductId = deviceInfo.ProductId;
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Obtém ou define o caminho direto do dispositivo USB (ex.: <c>\\?\usb#vid_...#...</c> no Windows, <c>/dev/usb/lp0</c> no Linux ou <c>/dev/cu.usb*</c> / <c>cups:Nome</c> no macOS).
    /// Quando informado, tem prioridade sobre <see cref="VendorId"/> e <see cref="ProductId"/>.
    /// </summary>
    public string? DevicePath
    {
        get => devicePath;
        set => SetProperty(ref devicePath, value);
    }

    /// <summary>
    /// Obtém ou define o identificador do fabricante (Vendor ID - VID).
    /// </summary>
    public int? VendorId
    {
        get => vendorId;
        set => SetProperty(ref vendorId, value);
    }

    /// <summary>
    /// Obtém ou define o identificador do produto (Product ID - PID).
    /// </summary>
    public int? ProductId
    {
        get => productId;
        set => SetProperty(ref productId, value);
    }

    /// <summary>
    /// Obtém ou define o GUID da interface de dispositivo no Windows.
    /// Por padrão utiliza <see cref="UsbPrintInterfaceGuid"/> (<c>GUID_DEVINTERFACE_USBPRINT</c>).
    /// </summary>
    public Guid InterfaceGuid
    {
        get => interfaceGuid;
        set => SetProperty(ref interfaceGuid, value);
    }

    #endregion Properties

    #region Methods

    /// <summary>
    /// Lista os dispositivos USB disponíveis no sistema operacional (Windows, Linux e macOS).
    /// </summary>
    /// <param name="interfaceGuid">GUID da interface de dispositivo no Windows (padrão: <see cref="UsbPrintInterfaceGuid"/>).</param>
    /// <returns>Lista de dispositivos USB encontrados.</returns>
    public static IReadOnlyList<UsbDeviceInfo> GetDevices(Guid? interfaceGuid = null)
    {
        if (OSPlatformHelper.IsMacOS)
            return GetMacOsUsbDevices();

        return OSPlatformHelper.IsLinux ? GetLinuxUsbDevices() : GetWindowsUsbDevices(interfaceGuid ?? UsbPrintInterfaceGuid);
    }

    /// <summary>
    /// Extrai o Vendor ID (VID) e Product ID (PID) a partir de um DevicePath no formato Windows (<c>vid_XXXX&amp;pid_YYYY</c>).
    /// </summary>
    /// <param name="path">Caminho de interface do dispositivo.</param>
    /// <param name="vid">Vendor ID extraído, se encontrado.</param>
    /// <param name="pid">Product ID extraído, se encontrado.</param>
    /// <returns><c>true</c> se o VID e o PID foram extraídos com sucesso; caso contrário, <c>false</c>.</returns>
    public static bool TryParseVidPidFromPath(string? path, out int vid, out int pid)
    {
        vid = 0;
        pid = 0;
        if (string.IsNullOrWhiteSpace(path)) return false;

        var match = VidPidRegex.Match(path);
        if (!match.Success) return false;

        return int.TryParse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out vid) &&
               int.TryParse(match.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out pid);
    }

    /// <summary>
    /// Analisa a saída do utilitário <c>ioreg -p IOUSB -l -w 0</c> e <c>lpstat -v</c> do macOS para extrair dispositivos USB.
    /// </summary>
    /// <param name="ioregOutput">Saída do comando <c>ioreg -p IOUSB -l -w 0</c>.</param>
    /// <param name="lpstatOutput">Saída opcional do comando <c>lpstat -v</c>.</param>
    /// <param name="devCuUsbFiles">Lista opcional de caminhos <c>/dev/cu.usb*</c>.</param>
    /// <returns>Lista de dispositivos USB identificados.</returns>
    public static IReadOnlyList<UsbDeviceInfo> ParseMacOsUsbDevices(
        string? ioregOutput,
        string? lpstatOutput = null,
        IEnumerable<string>? devCuUsbFiles = null)
    {
        var list = new List<UsbDeviceInfo>();
        var seenPaths = new HashSet<string>(StringComparer.Ordinal);

        var cupsQueues = new List<KeyValuePair<string, string>>();
        if (!string.IsNullOrWhiteSpace(lpstatOutput))
        {
            foreach (Match m in LpStatUsbRegex.Matches(lpstatOutput))
            {
                cupsQueues.Add(new KeyValuePair<string, string>(
                    m.Groups[1].Value.Trim(),
                    Uri.UnescapeDataString(m.Groups[2].Value.Trim())));
            }
        }

        if (!string.IsNullOrWhiteSpace(ioregOutput))
        {
            var blocks = ioregOutput!.Split(["+-o "], StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawBlock in blocks)
            {
                var block = "+-o " + rawBlock;
                var vidMatch = IoRegVendorRegex.Match(block);
                var pidMatch = IoRegProductRegex.Match(block);
                if (!vidMatch.Success || !pidMatch.Success) continue;

                var vid = ParseIntOrHex(vidMatch.Groups[1].Value);
                var pid = ParseIntOrHex(pidMatch.Groups[1].Value);
                if (!vid.HasValue || !pid.HasValue) continue;

                var nameMatch = IoRegProductNameRegex.Match(block);
                var nodeMatch = IoRegNodeRegex.Match(block);
                var description = nameMatch.Success
                    ? nameMatch.Groups[1].Value.Trim()
                    : nodeMatch.Success ? nodeMatch.Groups[1].Value.Trim() : "USB Device";

                var calloutMatch = IoRegCalloutRegex.Match(block);
                string? devicePath = null;

                if (calloutMatch.Success)
                {
                    devicePath = calloutMatch.Groups[1].Value.Trim();
                }
                else if (cupsQueues.Count > 0)
                {
                    var normalizedDesc = description.Replace(" ", string.Empty).Replace("-", string.Empty);
                    var matchingQueue = cupsQueues.FirstOrDefault(q =>
                        q.Value.IndexOf(description, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        q.Value.Replace(" ", string.Empty).Replace("-", string.Empty)
                            .IndexOf(normalizedDesc, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        q.Key.Replace("_", string.Empty)
                            .IndexOf(normalizedDesc, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (!string.IsNullOrEmpty(matchingQueue.Key))
                        devicePath = $"cups:{matchingQueue.Key}";
                }

                devicePath ??= $"ioreg://vid_{vid.Value:X4}&pid_{pid.Value:X4}";
                if (seenPaths.Add(devicePath))
                {
                    list.Add(new UsbDeviceInfo(devicePath, vid, pid, description));
                }
            }
        }

        foreach (var queue in cupsQueues)
        {
            var path = $"cups:{queue.Key}";
            if (seenPaths.Add(path))
                list.Add(new UsbDeviceInfo(path, null, null, queue.Key));
        }

        if (devCuUsbFiles != null)
        {
            foreach (var devFile in devCuUsbFiles)
            {
                if (seenPaths.Add(devFile))
                    list.Add(new UsbDeviceInfo(devFile, null, null, Path.GetFileName(devFile)));
            }
        }

        return list;
    }

    private static int? ParseIntOrHex(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex)
                ? hex
                : null;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dec) ? dec : null;
    }

    private static IReadOnlyList<UsbDeviceInfo> GetMacOsUsbDevices()
    {
        var ioregOutput = TryRunCommand("ioreg", "-p IOUSB -l -w 0");
        var lpstatOutput = TryRunCommand("lpstat", "-v");

        IEnumerable<string>? devFiles = null;
        if (Directory.Exists("/dev"))
        {
            try
            {
                devFiles = Directory.GetFiles("/dev", "cu.usb*")
                    .Concat(Directory.GetFiles("/dev", "tty.usb*"))
                    .OrderBy(f => f, StringComparer.Ordinal)
                    .ToArray();
            }
            catch
            {
                // Ignora erros ao listar /dev
            }
        }

        return ParseMacOsUsbDevices(ioregOutput, lpstatOutput, devFiles);
    }

    private static string? TryRunCommand(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            return output;
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<UsbDeviceInfo> GetLinuxUsbDevices()
    {
        var list = new List<UsbDeviceInfo>();
        var candidateDirs = new[] { "/dev/usb", "/dev" };
        var seenPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var dir in candidateDirs)
        {
            if (!Directory.Exists(dir)) continue;

            string[] files;
            try
            {
                files = Directory.GetFiles(dir, "lp*");
            }
            catch
            {
                continue;
            }

            foreach (var devPath in files.OrderBy(f => f, StringComparer.Ordinal))
            {
                if (!seenPaths.Add(devPath)) continue;

                var devName = Path.GetFileName(devPath);
                int? vid = null;
                int? pid = null;
                string? description = null;

                var sysfsCandidates = new[]
                {
                    $"/sys/class/usbmisc/{devName}/device/..",
                    $"/sys/class/usb/{devName}/device/..",
                    $"/sys/class/usbmisc/{devName}/device"
                };

                foreach (var sysPath in sysfsCandidates)
                {
                    if (!Directory.Exists(sysPath)) continue;

                    vid ??= TryReadHexFile(Path.Combine(sysPath, "idVendor"));
                    pid ??= TryReadHexFile(Path.Combine(sysPath, "idProduct"));
                    description ??= TryReadTextFile(Path.Combine(sysPath, "product"));

                    if (vid.HasValue && pid.HasValue) break;
                }

                list.Add(new UsbDeviceInfo(devPath, vid, pid, description));
            }
        }

        return list;
    }

    private static int? TryReadHexFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var content = File.ReadAllText(path).Trim();
            return int.TryParse(content, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var val) ? val : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadTextFile(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<UsbDeviceInfo> GetWindowsUsbDevices(Guid classGuid)
    {
        var list = new List<UsbDeviceInfo>();
        var hDevInfo = SetupApi.SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, IntPtr.Zero,
            SetupApi.DIGCF_PRESENT | SetupApi.DIGCF_DEVICEINTERFACE);

        if (hDevInfo == IntPtr.Zero || hDevInfo == new IntPtr(-1))
            return list;

        try
        {
            var interfaceData = new SetupApi.SP_DEVICE_INTERFACE_DATA
            {
                cbSize = Marshal.SizeOf<SetupApi.SP_DEVICE_INTERFACE_DATA>()
            };

            var index = 0;
            while (SetupApi.SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref classGuid, index, ref interfaceData))
            {
                index++;
                SetupApi.SetupDiGetDeviceInterfaceDetail(hDevInfo, ref interfaceData, IntPtr.Zero, 0, out var requiredSize, IntPtr.Zero);
                if (requiredSize <= 0) continue;

                var detailBuffer = Marshal.AllocHGlobal(requiredSize);
                try
                {
                    Marshal.WriteInt32(detailBuffer, IntPtr.Size == 8 ? 8 : 4 + Marshal.SystemDefaultCharSize);
                    if (SetupApi.SetupDiGetDeviceInterfaceDetail(hDevInfo, ref interfaceData, detailBuffer, requiredSize, out _, IntPtr.Zero))
                    {
                        var pDevicePath = IntPtr.Add(detailBuffer, 4);
                        var path = Marshal.PtrToStringAuto(pDevicePath);
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            int? vid = null;
                            int? pid = null;
                            if (TryParseVidPidFromPath(path, out var parsedVid, out var parsedPid))
                            {
                                vid = parsedVid;
                                pid = parsedPid;
                            }

                            list.Add(new UsbDeviceInfo(path!, vid, pid));
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(detailBuffer);
                }
            }
        }
        finally
        {
            SetupApi.SetupDiDestroyDeviceInfoList(hDevInfo);
        }

        return list;
    }

    #endregion Methods

    #region Native SetupAPI

    private static class SetupApi
    {
        public const int DIGCF_PRESENT = 0x00000002;
        public const int DIGCF_DEVICEINTERFACE = 0x00000010;

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(
            ref Guid classGuid,
            IntPtr enumerator,
            IntPtr hwndParent,
            int flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr hDevInfo,
            IntPtr devInfo,
            ref Guid interfaceClassGuid,
            int memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr hDevInfo,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            IntPtr deviceInterfaceDetailData,
            int deviceInterfaceDetailDataSize,
            out int requiredSize,
            IntPtr deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);
    }

    #endregion Native SetupAPI
}
