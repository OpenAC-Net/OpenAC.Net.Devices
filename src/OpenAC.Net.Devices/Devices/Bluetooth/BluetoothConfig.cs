// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 28-10-2026
//
// Last Modified By : RFTD
// Last Modified On : 28-10-2026
// ***********************************************************************
// <copyright file="BluetoothConfig.cs" company="OpenAC .Net">
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
/// Configuração para comunicação Bluetooth RFCOMM / SPP (Serial Port Profile).
/// </summary>
public sealed class BluetoothConfig : BaseConfig
{
    #region Constants

    /// <summary>
    /// UUID padrão do perfil Bluetooth Serial Port Profile (SPP): <c>00001101-0000-1000-8000-00805F9B34FB</c>.
    /// </summary>
    public static readonly Guid SerialPortServiceGuid = new("00001101-0000-1000-8000-00805F9B34FB");

    private static readonly Regex MacOsBtAddressRegex = new(
        @"Address:\s*([0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2}[:\-][0-9A-Fa-f]{2})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    #endregion Constants

    #region Fields

    private string? endereco;
    private int canal;
    private Guid serviceGuid = SerialPortServiceGuid;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="BluetoothConfig"/> com valores padrão.
    /// </summary>
    public BluetoothConfig() : base("Bluetooth")
    {
    }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="BluetoothConfig"/> com o endereço e o canal especificados.
    /// </summary>
    /// <param name="endereco">Endereço MAC Bluetooth (<c>XX:XX:XX:XX:XX:XX</c>) ou caminho de dispositivo (<c>/dev/rfcomm0</c> ou <c>/dev/cu.*</c>).</param>
    /// <param name="canal">Canal RFCOMM (0 para automático/padrão, ou de 1 a 30).</param>
    public BluetoothConfig(string endereco, int canal = 0) : this()
    {
        this.endereco = endereco;
        this.canal = canal;
    }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="BluetoothConfig"/> a partir de um dispositivo localizado.
    /// </summary>
    /// <param name="deviceInfo">Informações do dispositivo, incluindo seu endereço MAC ou caminho de porta serial.</param>
    /// <param name="canal">Canal RFCOMM (0 para automático/padrão, ou de 1 a 30).</param>
    /// <exception cref="ArgumentNullException">Lançada quando <paramref name="deviceInfo"/> é nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Lançada quando <paramref name="canal"/> está fora do intervalo de 0 a 30.</exception>
    public BluetoothConfig(BluetoothDeviceInfo deviceInfo, int canal = 0) : this()
    {
        if (deviceInfo == null) throw new ArgumentNullException(nameof(deviceInfo));

        Endereco = deviceInfo.Address;
        Canal = canal;
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Obtém ou define o endereço MAC do dispositivo Bluetooth (ex.: <c>00:11:22:33:44:55</c>)
    /// ou o caminho do dispositivo serial Bluetooth no Linux/macOS (ex.: <c>/dev/rfcomm0</c> ou <c>/dev/cu.MTP-II</c>).
    /// </summary>
    public string? Endereco
    {
        get => endereco;
        set => SetProperty(ref endereco, value);
    }

    /// <summary>
    /// Obtém ou define o canal RFCOMM (0 para resolução via <see cref="ServiceGuid"/> no Windows ou canal 1 no Linux/macOS; ou 1..30).
    /// </summary>
    public int Canal
    {
        get => canal;
        set
        {
            if (value is < 0 or > 30)
                throw new ArgumentOutOfRangeException(nameof(Canal), "O canal RFCOMM deve estar entre 0 e 30.");

            SetProperty(ref canal, value);
        }
    }

    /// <summary>
    /// Obtém ou define o UUID do serviço Bluetooth (padrão: <see cref="SerialPortServiceGuid"/> - SPP).
    /// </summary>
    public Guid ServiceGuid
    {
        get => serviceGuid;
        set => SetProperty(ref serviceGuid, value);
    }

    #endregion Properties

    #region Methods

    /// <summary>
    /// Tenta converter uma string de endereço MAC Bluetooth (<c>00:11:22:33:44:55</c>, <c>00-11-22-33-44-55</c> ou <c>001122334455</c>)
    /// para seu valor numérico de 48 bits (<see cref="ulong"/>).
    /// </summary>
    /// <param name="address">String contendo o endereço MAC Bluetooth.</param>
    /// <param name="btAddress">Endereço de 48 bits convertido.</param>
    /// <returns><c>true</c> se o endereço for válido; caso contrário, <c>false</c>.</returns>
    public static bool TryParseAddress(string? address, out ulong btAddress)
    {
        btAddress = 0;
        if (string.IsNullOrWhiteSpace(address)) return false;

        var cleaned = address!.Trim()
            .Replace(":", string.Empty)
            .Replace("-", string.Empty)
            .Replace(".", string.Empty);

        if (cleaned.Length != 12) return false;

        return ulong.TryParse(cleaned, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out btAddress);
    }

    /// <summary>
    /// Formata um endereço numérico Bluetooth de 48 bits no padrão <c>XX:XX:XX:XX:XX:XX</c>.
    /// </summary>
    /// <param name="btAddress">Endereço numérico de 48 bits.</param>
    /// <returns>Endereço formatado como <c>XX:XX:XX:XX:XX:XX</c>.</returns>
    public static string FormatAddress(ulong btAddress) =>
        string.Join(":",
            ((btAddress >> 40) & 0xFF).ToString("X2", CultureInfo.InvariantCulture),
            ((btAddress >> 32) & 0xFF).ToString("X2", CultureInfo.InvariantCulture),
            ((btAddress >> 24) & 0xFF).ToString("X2", CultureInfo.InvariantCulture),
            ((btAddress >> 16) & 0xFF).ToString("X2", CultureInfo.InvariantCulture),
            ((btAddress >> 8) & 0xFF).ToString("X2", CultureInfo.InvariantCulture),
            (btAddress & 0xFF).ToString("X2", CultureInfo.InvariantCulture));

    /// <summary>
    /// Lista os dispositivos Bluetooth pareados/conhecidos no sistema operacional (Windows, Linux e macOS).
    /// </summary>
    /// <param name="issueInquiry">No Windows, indica se deve realizar uma nova busca ativa (inquiry) por dispositivos próximos.</param>
    /// <returns>Lista de dispositivos Bluetooth localizados.</returns>
    public static IReadOnlyList<BluetoothDeviceInfo> GetDevices(bool issueInquiry = false)
    {
        if (OSPlatformHelper.IsMacOS)
            return GetMacOsBluetoothDevices();

        if (OSPlatformHelper.IsLinux)
            return GetLinuxBluetoothDevices();

        return GetWindowsBluetoothDevices(issueInquiry);
    }

    /// <summary>
    /// Analisa a saída do utilitário <c>system_profiler SPBluetoothDataType</c> e portas <c>/dev/cu.*</c> no macOS.
    /// </summary>
    /// <param name="profilerOutput">Saída do comando <c>system_profiler SPBluetoothDataType</c>.</param>
    /// <param name="devCuPaths">Lista opcional de caminhos <c>/dev/cu.*</c>.</param>
    /// <returns>Lista de dispositivos Bluetooth identificados.</returns>
    public static IReadOnlyList<BluetoothDeviceInfo> ParseMacOsBluetoothDevices(
        string? profilerOutput,
        IEnumerable<string>? devCuPaths = null)
    {
        var list = new List<BluetoothDeviceInfo>();
        var seenAddresses = new HashSet<ulong>();

        if (!string.IsNullOrWhiteSpace(profilerOutput))
        {
            var lines = profilerOutput!.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var inConnectedSection = false;
            string? currentDeviceName = null;

            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed.Equals("Connected:", StringComparison.OrdinalIgnoreCase))
                {
                    inConnectedSection = true;
                    continue;
                }

                if (trimmed.Equals("Not Connected:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Paired:", StringComparison.OrdinalIgnoreCase))
                {
                    if (trimmed.Equals("Not Connected:", StringComparison.OrdinalIgnoreCase))
                        inConnectedSection = false;
                    continue;
                }

                var addrMatch = MacOsBtAddressRegex.Match(trimmed);
                if (addrMatch.Success && TryParseAddress(addrMatch.Groups[1].Value, out var rawAddr))
                {
                    // Evita capturar o endereço do próprio controlador Bluetooth local ("Bluetooth Controller:")
                    if (string.IsNullOrWhiteSpace(currentDeviceName) ||
                        string.Equals(currentDeviceName, "Bluetooth Controller", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(currentDeviceName, "Controller", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (seenAddresses.Add(rawAddr))
                    {
                        list.Add(new BluetoothDeviceInfo(
                            FormatAddress(rawAddr),
                            rawAddr,
                            currentDeviceName,
                            connected: inConnectedSection,
                            authenticated: true));
                    }
                }
                else if (trimmed.EndsWith(":", StringComparison.Ordinal) &&
                         !trimmed.Equals("Bluetooth:", StringComparison.OrdinalIgnoreCase) &&
                         !trimmed.StartsWith("Address:", StringComparison.OrdinalIgnoreCase))
                {
                    currentDeviceName = trimmed.Substring(0, trimmed.Length - 1).Trim();
                }
            }
        }

        if (devCuPaths != null)
        {
            foreach (var cuPath in devCuPaths)
            {
                var fileName = Path.GetFileName(cuPath);
                if (fileName.Equals("cu.Bluetooth-Incoming-Port", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("cu.wlan-debug", StringComparison.OrdinalIgnoreCase) ||
                    fileName.StartsWith("cu.usb", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var friendlyName = fileName.StartsWith("cu.", StringComparison.OrdinalIgnoreCase)
                    ? fileName.Substring(3)
                    : fileName;

                list.Add(new BluetoothDeviceInfo(cuPath, 0, friendlyName, connected: true, authenticated: true));
            }
        }

        return list;
    }

    private static IReadOnlyList<BluetoothDeviceInfo> GetMacOsBluetoothDevices()
    {
        string? profilerOutput = null;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "system_profiler",
                Arguments = "SPBluetoothDataType",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                profilerOutput = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);
            }
        }
        catch
        {
            // Ignora falha ao executar system_profiler
        }

        IEnumerable<string>? devCu = null;
        if (Directory.Exists("/dev"))
        {
            try
            {
                devCu = Directory.GetFiles("/dev", "cu.*")
                    .OrderBy(f => f, StringComparer.Ordinal)
                    .ToArray();
            }
            catch
            {
                // Ignora erros ao listar /dev
            }
        }

        return ParseMacOsBluetoothDevices(profilerOutput, devCu);
    }

    private static IReadOnlyList<BluetoothDeviceInfo> GetLinuxBluetoothDevices()
    {
        var list = new List<BluetoothDeviceInfo>();
        var seen = new HashSet<ulong>();

        const string btRoot = "/var/lib/bluetooth";
        if (Directory.Exists(btRoot))
        {
            try
            {
                foreach (var adapterDir in Directory.GetDirectories(btRoot))
                {
                    foreach (var deviceDir in Directory.GetDirectories(adapterDir))
                    {
                        var dirName = Path.GetFileName(deviceDir);
                        if (!TryParseAddress(dirName, out var rawAddr) || !seen.Add(rawAddr))
                            continue;

                        string? name = null;
                        var infoFile = Path.Combine(deviceDir, "info");
                        if (File.Exists(infoFile))
                        {
                            try
                            {
                                foreach (var line in File.ReadAllLines(infoFile))
                                {
                                    if (line.StartsWith("Name=", StringComparison.OrdinalIgnoreCase) ||
                                        line.StartsWith("Alias=", StringComparison.OrdinalIgnoreCase))
                                    {
                                        name = line.Substring(line.IndexOf('=') + 1).Trim();
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                // Sem permissão de leitura em /var/lib/bluetooth/<adapter>/<device>/info
                            }
                        }

                        list.Add(new BluetoothDeviceInfo(FormatAddress(rawAddr), rawAddr, name, connected: false, authenticated: true));
                    }
                }
            }
            catch
            {
                // Ignora caso /var/lib/bluetooth exija privilégios de root
            }
        }

        if (Directory.Exists("/dev"))
        {
            try
            {
                foreach (var rfcomm in Directory.GetFiles("/dev", "rfcomm*"))
                {
                    list.Add(new BluetoothDeviceInfo(rfcomm, 0, Path.GetFileName(rfcomm), connected: true, authenticated: true));
                }
            }
            catch
            {
                // Ignora erros ao enumerar /dev/rfcomm*
            }
        }

        return list;
    }

    private static IReadOnlyList<BluetoothDeviceInfo> GetWindowsBluetoothDevices(bool issueInquiry)
    {
        var list = new List<BluetoothDeviceInfo>();
        try
        {
            var searchParams = new Win32Bluetooth.BLUETOOTH_DEVICE_SEARCH_PARAMS
            {
                dwSize = Marshal.SizeOf<Win32Bluetooth.BLUETOOTH_DEVICE_SEARCH_PARAMS>(),
                fReturnAuthenticated = true,
                fReturnRemembered = true,
                fReturnUnknown = true,
                fReturnConnected = true,
                fIssueInquiry = issueInquiry,
                cTimeoutMultiplier = (byte)(issueInquiry ? 2 : 0),
                hRadio = IntPtr.Zero
            };

            var deviceInfo = new Win32Bluetooth.BLUETOOTH_DEVICE_INFO
            {
                dwSize = Marshal.SizeOf<Win32Bluetooth.BLUETOOTH_DEVICE_INFO>()
            };

            var hFind = Win32Bluetooth.BluetoothFindFirstDevice(ref searchParams, ref deviceInfo);
            if (hFind == IntPtr.Zero) return list;

            try
            {
                do
                {
                    list.Add(new BluetoothDeviceInfo(
                        FormatAddress(deviceInfo.Address),
                        deviceInfo.Address,
                        deviceInfo.szName,
                        deviceInfo.fConnected,
                        deviceInfo.fAuthenticated));
                } while (Win32Bluetooth.BluetoothFindNextDevice(hFind, ref deviceInfo));
            }
            finally
            {
                Win32Bluetooth.BluetoothFindDeviceClose(hFind);
            }
        }
        catch
        {
            // Caso o serviço Bluetooth do Windows não esteja disponível
        }

        return list;
    }

    #endregion Methods

    #region Win32 Bluetooth API

    private static class Win32Bluetooth
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct BLUETOOTH_DEVICE_SEARCH_PARAMS
        {
            public int dwSize;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnAuthenticated;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnRemembered;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnUnknown;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnConnected;
            [MarshalAs(UnmanagedType.Bool)] public bool fIssueInquiry;
            public byte cTimeoutMultiplier;
            public IntPtr hRadio;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct BLUETOOTH_DEVICE_INFO
        {
            public int dwSize;
            public ulong Address;
            public uint ulClassofDevice;
            [MarshalAs(UnmanagedType.Bool)] public bool fConnected;
            [MarshalAs(UnmanagedType.Bool)] public bool fRemembered;
            [MarshalAs(UnmanagedType.Bool)] public bool fAuthenticated;
            public SYSTEMTIME stLastSeen;
            public SYSTEMTIME stLastUsed;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
            public string szName;
        }

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern IntPtr BluetoothFindFirstDevice(
            ref BLUETOOTH_DEVICE_SEARCH_PARAMS pbtsp,
            ref BLUETOOTH_DEVICE_INFO pbtdi);

        [DllImport("bthprops.cpl", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BluetoothFindNextDevice(
            IntPtr hFind,
            ref BLUETOOTH_DEVICE_INFO pbtdi);

        [DllImport("bthprops.cpl", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BluetoothFindDeviceClose(IntPtr hFind);
    }

    #endregion Win32 Bluetooth API
}
