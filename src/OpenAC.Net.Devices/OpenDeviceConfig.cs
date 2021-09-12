// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="OpenDeviceConfig.cs" company="OpenAC .Net">
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
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;

namespace OpenAC.Net.Devices
{
    public class OpenDeviceConfig : INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        #region Fields

        private string porta;
        private bool controlePorta;
        private Encoding encoding;
        private int baud;
        private int dataBits;
        private Parity parity;
        private StopBits stopBits;
        private Handshake handshake;
        private int timeOut;
        private int tentativas;
        private int intervaloTentativas;
        private int readBufferSize;
        private int writeBufferSize;

        #endregion Fields

        #region Constructor

        protected OpenDeviceConfig()
        {
            ControlePorta = true;
            Encoding = OpenEncoding.IBM860;
            Porta = "COM1";
            Baud = 9600;
            DataBits = 8;
            Parity = Parity.None;
            StopBits = StopBits.One;
            WriteBufferSize = 2048;
            ReadBufferSize = 4096;
            Handshake = Handshake.None;
            TimeOut = 3;
            Tentativas = 3;
            IntervaloTentativas = 3000;
        }

        #endregion Constructor

        #region Properties

        public bool ControlePorta
        {
            get => controlePorta;
            set => SetProperty(ref controlePorta, value);
        }

        [Browsable(false)]
        public Encoding Encoding
        {
            get => encoding;
            set => SetProperty(ref encoding, value);
        }

        public string Porta
        {
            get => porta;
            set
            {
                if (!OpenDeviceManager.IsValidPort(value)) throw new ArgumentException("Porta ínvalida.");
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

        #region Methods

        /// <summary>
        /// Sets the value of the property to the specified value if it has changed.
        /// </summary>
        /// <typeparam name="TProp">The type of the property.</typeparam>
        /// <param name="currentValue">The current value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns><c>true</c> if the property was changed, otherwise <c>false</c>.</returns>
        private bool SetProperty<TProp>(
            ref TProp currentValue,
            TProp newValue,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<TProp>.Default.Equals(currentValue, newValue)) return false;

            currentValue = newValue;
            OnPropertyChanged(propertyName);

            return true;
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Debug.Assert(
                string.IsNullOrEmpty(propertyName) ||
                (GetType().GetRuntimeProperty(propertyName) != null),
                "Check that the property name exists for this instance.");

            PropertyChanged.Raise(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion Methods
    }
}