// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 28-10-2026
//
// Last Modified By : RFTD
// Last Modified On : 28-10-2026
// ***********************************************************************
// <copyright file="BluetoothDeviceInfo.cs" company="OpenAC .Net">
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

namespace OpenAC.Net.Devices;

/// <summary>
/// Representa as informações de um dispositivo Bluetooth pareado ou localizado no sistema.
/// </summary>
public sealed class BluetoothDeviceInfo
{
    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="BluetoothDeviceInfo"/>.
    /// </summary>
    /// <param name="address">Endereço MAC formatado (<c>XX:XX:XX:XX:XX:XX</c>) ou caminho do dispositivo.</param>
    /// <param name="rawAddress">Endereço numérico de 48 bits do dispositivo.</param>
    /// <param name="name">Nome amigável do dispositivo.</param>
    /// <param name="connected">Indica se o dispositivo está conectado.</param>
    /// <param name="authenticated">Indica se o dispositivo está pareado/autenticado.</param>
    public BluetoothDeviceInfo(string address, ulong rawAddress, string? name = null, bool connected = false, bool authenticated = false)
    {
        Address = address;
        RawAddress = rawAddress;
        Name = name;
        Connected = connected;
        Authenticated = authenticated;
    }

    /// <summary>
    /// Obtém o endereço MAC Bluetooth (<c>XX:XX:XX:XX:XX:XX</c>) ou caminho de dispositivo.
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// Obtém o endereço numérico de 48 bits do dispositivo Bluetooth.
    /// </summary>
    public ulong RawAddress { get; }

    /// <summary>
    /// Obtém o nome do dispositivo Bluetooth, quando disponível.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Obtém um valor que indica se o dispositivo está atualmente conectado.
    /// </summary>
    public bool Connected { get; }

    /// <summary>
    /// Obtém um valor que indica se o dispositivo está pareado/autenticado.
    /// </summary>
    public bool Authenticated { get; }

    /// <inheritdoc />
    public override string ToString() =>
        string.IsNullOrWhiteSpace(Name) ? Address : $"{Name} [{Address}]";
}
