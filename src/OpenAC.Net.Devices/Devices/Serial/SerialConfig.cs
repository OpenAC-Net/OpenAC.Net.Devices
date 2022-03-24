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
using System.IO.Ports;
using System.Linq;
using OpenAC.Net.Core.Extensions;

namespace OpenAC.Net.Devices
{
    public class SerialConfig : BaseConfig
    {
        #region Fields

        private string porta;
        private int baud;
        private int dataBits;
        private Parity parity;
        private StopBits stopBits;
        private Handshake handshake;
        private readonly string[] validPorts = { "COM", "LPT" };

        #endregion Fields

        #region Constructor

        public SerialConfig() : base("Serial")
        {
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
        /// Retorna/define o nome da porta serial para a conexão.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public string Porta
        {
            get => porta;
            set
            {
                if (!IsValidPort(value)) throw new ArgumentException("Porta ínvalida.");
                if (!SetProperty(ref porta, value)) return;
            }
        }

        public int Baud
        {
            get => baud;
            set => SetProperty(ref baud, value);
        }

        public int DataBits
        {
            get => dataBits;
            set => SetProperty(ref dataBits, value);
        }

        public Parity Parity
        {
            get => parity;
            set => SetProperty(ref parity, value);
        }

        public StopBits StopBits
        {
            get => stopBits;
            set => SetProperty(ref stopBits, value);
        }

        public Handshake Handshake
        {
            get => handshake;
            set => SetProperty(ref handshake, value);
        }

        #endregion Properties

        #region Methods

        /// <inheritdoc />
        private bool IsValidPort(string aPorta) => !aPorta.IsEmpty() && validPorts.Any(p => aPorta.ToUpper().StartsWith(p));

        #endregion Methods
    }
}