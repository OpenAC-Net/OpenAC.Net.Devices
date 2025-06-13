// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="SerialConfig.cs" company="OpenAC .Net">
//		        		   The MIT License (MIT)
//	     		    Copyright (c) 2014 - 2024 Projeto OpenAC .Net
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
using System.IO;
using System.IO.Ports;
using System.Linq;
using OpenAC.Net.Core.Extensions;

namespace OpenAC.Net.Devices;

/// <summary>
/// Representa a configuração de uma conexão serial.
/// </summary>
public class SerialConfig : BaseConfig
{
    #region Fields

    private string porta;
    private int baud;
    private int dataBits;
    private Parity parity;
    private StopBits stopBits;
    private Handshake handshake;
    private readonly string[] windowsPorts;
    private readonly string[] unixesPorts;

    #endregion Fields

    #region Constructor

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="SerialConfig"/>.
    /// Define valores padrão para as propriedades da configuração serial.
    /// </summary>
    public SerialConfig() : base("Serial")
    {
        windowsPorts = ["COM", "LPT"];
        if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
        {
            unixesPorts = Directory.GetFiles("/dev/", "tty*") // Linux / FreeBSD / macOS
                .Concat(Directory.GetFiles("/dev/", "rfcomm*")) // Linux BT
                .Concat(Directory.GetFiles("/dev/", "cu*")) // FreeBSD / macOS
                .Distinct()
                .ToArray();
        }
        else
        {
            unixesPorts = [];
        }

        Porta = "COM1";
        Baud = 9600;
        DataBits = 8;
        Parity = Parity.None;
        StopBits = StopBits.One;
        WriteBufferSize = 2048;
        ReadBufferSize = 4096;
        Handshake = Handshake.None;
    }

    #endregion Constructor

    #region Properties

    /// <summary>
    /// Obtém ou define o nome da porta serial para a conexão.
    /// </summary>
    /// <exception cref="ArgumentException">Lançada quando o nome da porta é inválido.</exception>
    public string Porta
    {
        get => porta;
        set
        {
            if (!IsValidPort(value)) throw new ArgumentException("Porta ínvalida.");
            if (!SetProperty(ref porta, value)) return;
        }
    }

    /// <summary>
    /// Obtém ou define a taxa de transmissão (baud rate) da conexão serial.
    /// </summary>
    public int Baud
    {
        get => baud;
        set => SetProperty(ref baud, value);
    }

    /// <summary>
    /// Obtém ou define o número de bits de dados na conexão serial.
    /// </summary>
    public int DataBits
    {
        get => dataBits;
        set => SetProperty(ref dataBits, value);
    }

    /// <summary>
    /// Obtém ou define o tipo de paridade usado na conexão serial.
    /// </summary>
    public Parity Parity
    {
        get => parity;
        set => SetProperty(ref parity, value);
    }

    /// <summary>
    /// Obtém ou define o número de bits de parada usados na conexão serial.
    /// </summary>
    public StopBits StopBits
    {
        get => stopBits;
        set => SetProperty(ref stopBits, value);
    }

    /// <summary>
    /// Obtém ou define o tipo de handshake usado na conexão serial.
    /// </summary>
    public Handshake Handshake
    {
        get => handshake;
        set => SetProperty(ref handshake, value);
    }

    #endregion Properties

    #region Methods
    
    /// <summary>
    /// Verifica se o nome da porta fornecido é válido.
    /// </summary>
    /// <param name="aPorta">Nome da porta a ser validada.</param>
    /// <returns>Retorna <c>true</c> se a porta for válida; caso contrário, <c>false</c>.</returns>
    private bool IsValidPort(string aPorta) => !aPorta.IsEmpty() && (windowsPorts.Any(p => aPorta.ToUpper().StartsWith(p)) || unixesPorts.Contains(aPorta));

    #endregion Methods
}