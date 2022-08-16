// ***********************************************************************
// Assembly         : OpenAC.Net.Devices.USB
// Author           : RFTD
// Created          : 11-06-2022
//
// Last Modified By : RFTD
// Last Modified On : 11-06-2022
// ***********************************************************************
// <copyright file="OpenUSBStream.cs" company="OpenAC .Net">
//		        		   The MIT License (MIT)
// 		    Copyright (c) 2016 - 2022 Projeto OpenAC .Net
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

namespace OpenAC.Net.Devices.USB
{
    public sealed class OpenUSBStream : OpenDeviceStream<USBConfig>
    {
        #region Fields

        private static readonly Guid GUID_DEVCLASS_NET = new Guid("{4D36E972-E325-11CE-BFC1-08002BE10318}");
        private static readonly Guid GUID_DEVCLASS_PORT = new Guid("{4D36E978-E325-11CE-BFC1-08002BE10318}");
        private static readonly Guid GUID_DEVCLASS_PRINTER = new Guid("{4D36E979-E325-11CE-BFC1-08002BE10318}");
        private static readonly Guid GUID_DEVCLASS_PNPPRINTERS = new Guid("{4658EE7E-F050-11D1-B6BD-00C04FA372A7}");
        private static readonly Guid GUID_DEVCLASS_NETCLIENT = new Guid("{4D36E973-E325-11CE-BFC1-08002BE10318}");
        private static readonly Guid GUID_DEVINTERFACE_USBPRINT = new Guid("{28D78FAD-5A12-11D1-AE5B-0000F803A8C2}");
        private static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("{A5DCBF10-6530-11D2-901F-00C04FB951ED}");

        public const string CSecVendors = "Vendors";
        public const int CReceiveBufferSize = 1024;

        public const string sErrWinUSBInvalidID = "{0} inválido [{1}]";
        public const string sErrWinUSBDeviceOutOfRange = "Dispositivo USB num: {0} não existe";
        public const string sErrWinUSBOpening = "Erro {0} ao abrir o Porta USB {1}";
        public const string sErrWinUSBClosing = "Erro {0} ao fechar a Porta USB {1}";
        public const string sErrWinUSBDescriptionNotFound = "Erro, dispositivo [{0}] não encontrado";
        public const string sErrWinUSBNotDeviceFound = "Nenhum dispositivo USB encontrado";
        public const string sErrWinUSBDeviceIsClosed = "Dispositivo USB não está aberto";
        public const string sErrWinUSBSendData = "Erro {0}, ao enviar {1} bytes para USB";
        public const string sErrWinUSBReadData = "Erro {0}, ao ler da USB";

        public const string sDescDevPosPrinter = "Impressora";
        public const string sDescDevLabelPrinter = "Etiquetadora";
        public const string sDescDevFiscal = "SAT";

        #endregion Fields

        #region Constructors

        public OpenUSBStream(USBConfig config) : base(config)
        {
        }

        #endregion Constructors

        #region Properties

        protected override int Available => 0;

        #endregion Properties

        #region Methods

        protected override bool OpenInternal() => throw new NotImplementedException();

        protected override bool CloseInternal() => throw new NotImplementedException();

        #endregion Methods
    }
}