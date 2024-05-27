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

using System.IO;
using HidSharp;
using OpenAC.Net.Core;

namespace OpenAC.Net.Devices.USB
{
    public sealed class OpenUsbStream : OpenDeviceStream<USBConfig>
    {
        #region Constructors

        public OpenUsbStream(USBConfig config) : base(config)
        {
            
        }

        #endregion Constructors

        #region Properties

        protected override int Available => 0;

        #endregion Properties

        #region Methods

        protected override bool OpenInternal()
        {
            var device = DeviceList.Local.GetHidDeviceOrNull(Config.VendorId, Config.ProductId);
            if (device == null) throw new OpenException("Dispositivo não localizado");

            if (!device.TryOpen(out var stream)) return false;
            Writer = new BinaryWriter(stream);
            Reader = new BinaryReader(stream);
            return true;
        }

        protected override bool CloseInternal()
        {
            Reader?.Dispose();
            Writer?.Dispose();

            Reader = null;
            Writer = null;
            return true;
        }

        #endregion Methods
    }
}