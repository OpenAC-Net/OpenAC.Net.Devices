// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 28-10-2026
//
// Last Modified By : RFTD
// Last Modified On : 28-10-2026
// ***********************************************************************
// <copyright file="UsbDeviceInfo.cs" company="OpenAC .Net">
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
/// Representa as informações de um dispositivo USB localizado no sistema.
/// </summary>
public sealed class UsbDeviceInfo
{
    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="UsbDeviceInfo"/>.
    /// </summary>
    /// <param name="devicePath">Caminho do dispositivo no sistema operacional.</param>
    /// <param name="vendorId">Identificador do fabricante (VID), quando disponível.</param>
    /// <param name="productId">Identificador do produto (PID), quando disponível.</param>
    /// <param name="description">Descrição ou nome amigável do dispositivo, quando disponível.</param>
    public UsbDeviceInfo(string devicePath, int? vendorId = null, int? productId = null, string? description = null)
    {
        DevicePath = devicePath;
        VendorId = vendorId;
        ProductId = productId;
        Description = description;
    }

    /// <summary>
    /// Obtém o caminho do dispositivo no sistema operacional (ex.: <c>\\?\usb#vid_...#...</c> no Windows ou <c>/dev/usb/lp0</c> no Linux).
    /// </summary>
    public string DevicePath { get; }

    /// <summary>
    /// Obtém o identificador do fabricante (Vendor ID - VID), quando identificado.
    /// </summary>
    public int? VendorId { get; }

    /// <summary>
    /// Obtém o identificador do produto (Product ID - PID), quando identificado.
    /// </summary>
    public int? ProductId { get; }

    /// <summary>
    /// Obtém a descrição ou nome do dispositivo, quando disponível.
    /// </summary>
    public string? Description { get; }

    /// <inheritdoc />
    public override string ToString() =>
        VendorId.HasValue && ProductId.HasValue
            ? $"{Description ?? "USB Device"} [VID:{VendorId.Value:X4} PID:{ProductId.Value:X4}] ({DevicePath})"
            : $"{Description ?? "USB Device"} ({DevicePath})";
}
