// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="OpenDeviceStream.cs" company="OpenAC .Net">
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
using System.Linq;
using System.Text;
using OpenAC.Net.Core.Logging;

namespace OpenAC.Net.Devices
{
    public abstract class OpenDeviceStream : IDisposable, IOpenLog
    {
        #region Constructors

        protected OpenDeviceStream(OpenDeviceConfig config)
        {
            Config = config;
        }

        ~OpenDeviceStream()
        {
            Dispose(false);
        }

        #endregion Constructors

        #region Properties

        public OpenDeviceConfig Config { get; }

        public bool Conectado { get; protected set; }

        #endregion Properties

        #region Methods

        public void Open()
        {
            this.Log().Info($"{new string('-', 80)}" + Environment.NewLine +
                            $"Open - {DateTime.Now:G}" + Environment.NewLine +
                            $"- Device: {GetType().Name}" + Environment.NewLine +
                            $"- TimeOut: {Config.TimeOut}" + Environment.NewLine +
                            $"- Serial.: {Config.Porta} - BAUD={Config.Baud} DATA={Config.DataBits} PARITY={Config.Parity} STOP={Config.StopBits} HANDSHAKE={Config.Handshake}" + Environment.NewLine +
                            $"{new string('-', 80)}");

            Conectado = OpenInternal();

            if (Config.ControlePorta) CloseInternal();
        }

        public void Close()
        {
            this.Log().Info($"{new string('-', 80)}" + Environment.NewLine +
                            $"- Device: {GetType().Name}" + Environment.NewLine +
                            $"{new string('-', 80)}");

            Conectado = CloseInternal();
        }

        public virtual void Limpar()
        {
            //Limpar stream de leitura.
        }

        protected abstract bool OpenInternal();

        protected abstract bool CloseInternal();

        public void Write(byte[] dados)
        {
            if (dados.Length < 1) return;
            if (Config.ControlePorta) OpenInternal();

            WriteInternal(Encoding.Convert(Encoding.UTF8, Config.Encoding, dados));
        }

        public byte[] Read()
        {
            try
            {
                var dados = ReadInternal();
                return !dados.Any() ? dados : Encoding.Convert(Config.Encoding, Encoding.UTF8, dados);
            }
            finally
            {
                if (Config.ControlePorta) CloseInternal();
            }
        }

        protected abstract void WriteInternal(byte[] dados);

        protected abstract byte[] ReadInternal();

        protected abstract void Dispose(bool disposing);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion Methods
    }
}