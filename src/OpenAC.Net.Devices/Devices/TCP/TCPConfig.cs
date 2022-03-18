// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="TCPConfig.cs" company="OpenAC .Net">
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

using System.Text;
using OpenAC.Net.Core;

namespace OpenAC.Net.Devices
{
    public sealed class TCPConfig : NotifyPropertyChanges, IDeviceConfig
    {
        #region Fields

        private bool controlePorta = true;
        private Encoding encoding = Encoding.UTF8;
        private int timeOut;
        private int tentativas;
        private int intervaloTentativas;
        private int readBufferSize;
        private int writeBufferSize;
        private string ip;
        private int porta;

        #endregion Fields

        #region Constructors

        public TCPConfig()
        {
            Encoding = OpenEncoding.IBM860;
        }

        public TCPConfig(string ip, int porta) : this()
        {
            this.ip = ip;
            this.porta = porta;
        }

        #endregion Constructors

        #region Properties

        public string Name => "TCP";

        public string IP
        {
            get => ip;
            set => SetProperty(ref ip, value);
        }

        public int Porta
        {
            get => porta;
            set => SetProperty(ref porta, value);
        }

        public bool ControlePorta
        {
            get => controlePorta;
            set => SetProperty(ref controlePorta, value);
        }

        public Encoding Encoding
        {
            get => encoding;
            set => SetProperty(ref encoding, value);
        }

        public int TimeOut
        {
            get => timeOut;
            set => SetProperty(ref timeOut, value);
        }

        public int Tentativas
        {
            get => tentativas;
            set => SetProperty(ref tentativas, value);
        }

        public int IntervaloTentativas
        {
            get => intervaloTentativas;
            set => SetProperty(ref intervaloTentativas, value);
        }

        public int ReadBufferSize
        {
            get => readBufferSize;
            set => SetProperty(ref readBufferSize, value);
        }

        public int WriteBufferSize
        {
            get => writeBufferSize;
            set => SetProperty(ref writeBufferSize, value);
        }

        #endregion Properties
    }
}