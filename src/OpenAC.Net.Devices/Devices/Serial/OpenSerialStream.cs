﻿// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="OpenSerialStream.cs" company="OpenAC .Net">
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

using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;

namespace OpenAC.Net.Devices;

/// <summary>
/// Implementa um stream de dispositivo serial baseado em <see cref="SerialPort"/>.
/// </summary>
internal sealed class OpenSerialStream(SerialConfig config) : OpenDeviceStream<SerialConfig>(config)
{
    #region Fields

    /// <summary>
    /// Instância do <see cref="SerialPort"/> utilizada para comunicação.
    /// </summary>
    private readonly SerialPort serialPort = new();

    #endregion Fields

    #region Properties

    /// <inheritdoc/>
    protected override int Available => serialPort?.BytesToRead ?? 0;

    #endregion Properties

    #region Methods

    /// <summary>
    /// Limpa o buffer de entrada da porta serial.
    /// </summary>
    public override void Limpar()
    {
        if (serialPort.IsOpen) serialPort.DiscardInBuffer();
    }

    /// <inheritdoc/>
    protected override bool OpenInternal()
    {
        if (serialPort.IsOpen) return false;

        ConfigSerial();
        serialPort.Open();

        Reader = new BinaryReader(serialPort.BaseStream);
        Writer = new BinaryWriter(serialPort.BaseStream);

        return serialPort.IsOpen;
    }

    /// <inheritdoc/>
    protected override bool CloseInternal()
    {
        if (!serialPort.IsOpen) return false;

        serialPort.Close();
        Reader?.Dispose();
        Writer?.Dispose();

        Reader = null;
        Writer = null;

        return !serialPort.IsOpen;
    }

    /// <summary>
    /// Configura os parâmetros da porta serial conforme o <see cref="SerialConfig"/>.
    /// </summary>
    private void ConfigSerial()
    {
        serialPort.PortName = Config.Porta;
        serialPort.BaudRate = Config.Baud;
        serialPort.DataBits = Config.DataBits;
        serialPort.Parity = Config.Parity;
        serialPort.StopBits = Config.StopBits;
        serialPort.Handshake = Config.Handshake;
        serialPort.ReadTimeout = Config.TimeOut;
        serialPort.WriteTimeout = Config.TimeOut;
        serialPort.ReadBufferSize = Config.ReadBufferSize;
        serialPort.WriteBufferSize = Config.WriteBufferSize;
    }

    #endregion Methods

    #region Dispose Methods

    /// <inheritdoc/>
    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        serialPort?.Close();
        serialPort?.Dispose();
        Task.Delay(250).Wait();
    }

    #endregion Dispose Methods
}