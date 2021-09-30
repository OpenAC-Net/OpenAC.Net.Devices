// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="OpenSerialStream.cs" company="OpenAC .Net">
//		        		   The MIT License (MIT)
//	     		    Copyright (c) 2016 Projeto OpenAC .Net
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
using System.IO.Ports;

namespace OpenAC.Net.Devices
{
    internal sealed class OpenSerialStream : OpenDeviceStream
    {
        #region Fields

        private readonly SerialPort serialPort;

        #endregion Fields

        #region Constructor

        public OpenSerialStream(OpenDeviceConfig config) : base(config)
        {
            serialPort = new SerialPort();
        }

        #endregion Constructor

        #region Methods

        public override void Limpar()
        {
            if (serialPort.IsOpen) serialPort.DiscardInBuffer();
        }

        protected override bool OpenInternal()
        {
            if (serialPort.IsOpen) return false;

            ConfigSerial();
            serialPort.Open();

            return serialPort.IsOpen;
        }

        protected override bool CloseInternal()
        {
            if (!serialPort.IsOpen) return false;

            serialPort.Close();

            return !serialPort.IsOpen;
        }

        protected override void WriteInternal(byte[] dados)
        {
            if (dados.Length < 1) return;

            serialPort.Write(dados, 0, dados.Length);
        }

        protected override byte[] ReadInternal()
        {
            var ret = new List<byte>();
            while (serialPort.BytesToRead > 0)
            {
                var inbyte = new byte[1];
                serialPort.Read(inbyte, 0, 1);
                if (inbyte.Length < 1) continue;

                var value = (byte)inbyte.GetValue(0);
                ret.Add(value);
            }

            return ret.ToArray();
        }

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
            serialPort.Encoding = Config.Encoding;
        }

        #endregion Methods

        #region Dispose Methods

        protected override void Dispose(bool disposing)
        {
            if (disposing) GC.SuppressFinalize(this);

            serialPort?.Dispose();
        }

        #endregion Dispose Methods
    }
}