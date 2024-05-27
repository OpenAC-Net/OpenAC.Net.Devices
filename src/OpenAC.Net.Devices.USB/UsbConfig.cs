// ***********************************************************************
// Assembly         : OpenAC.Net.Devices.USB
// Author           : RFTD
// Created          : 11-06-2022
//
// Last Modified By : RFTD
// Last Modified On : 11-06-2022
// ***********************************************************************
// <copyright file="USBConfig.cs" company="OpenAC .Net">
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

namespace OpenAC.Net.Devices.USB
{
    public sealed class UsbConfig : BaseConfig
    {
        #region Fields

        private int vendorId;
        private int productId;

        #endregion Fields

        #region Constructors

        public UsbConfig() : base("USB")
        {
        }

        public UsbConfig(int vendor, int product) : this()
        {
            vendorId = vendor;
            productId = product;
        }

        #endregion Constructors

        #region Properties

        public int VendorId
        {
            get => vendorId;
            set => SetProperty(ref vendorId, value);
        }

        public int ProductId
        {
            get => productId;
            set => SetProperty(ref productId, value);
        }

        #endregion Properties
    }
}