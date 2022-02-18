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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenAC.Net.Core.Logging;

namespace OpenAC.Net.Devices
{
    /// <summary>
    /// Classe para comunicaçõ com dispositivos.
    /// </summary>
    public abstract class OpenDeviceStream : IDisposable, IOpenLog
    {
        #region Fields

        private bool disposed;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Inicializa uma nova instancia da classe <see cref="OpenDeviceStream"/>
        /// </summary>
        /// <param name="config">Configurações da classe</param>
        protected OpenDeviceStream(IDeviceConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// Destructor da classe
        /// </summary>
        ~OpenDeviceStream() => Dispose(false);

        #endregion Constructors

        #region Properties

        public IDeviceConfig Config { get; }

        public bool Conectado { get; protected set; }

        protected BinaryWriter Writer { get; set; }

        protected BinaryReader Reader { get; set; }

        protected abstract int Available { get; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Abre a conexão
        /// </summary>
        public void Open()
        {
            this.Log().Info($"{new string('-', 80)}" + Environment.NewLine +
                            $"Open - {DateTime.Now:G}" + Environment.NewLine +
                            $"- Config: {Config.Name}" + Environment.NewLine +
                            $"{new string('-', 80)}");

            Conectado = OpenInternal();

            if (Config.ControlePorta && Conectado) CloseInternal();
        }

        /// <summary>
        /// Fecha a conexão
        /// </summary>
        public void Close()
        {
            this.Log().Info($"{new string('-', 80)}" + Environment.NewLine +
                            $"Close - {DateTime.Now:G}" + Environment.NewLine +
                            $"- Config: {Config.Name}" + Environment.NewLine +
                            $"{new string('-', 80)}");

            if (Config.ControlePorta && Conectado)
                Conectado = false;
            else if (CloseInternal())
                Conectado = false;
        }

        /// <summary>
        /// Limpa os dados do Buffer de leitura.
        /// </summary>
        public virtual void Limpar()
        {
            //Limpar stream de leitura.
        }

        /// <summary>
        /// Abre a conexão internamente para o controle de porta.
        /// </summary>
        /// <returns></returns>
        protected abstract bool OpenInternal();

        /// <summary>
        /// Fecha a conexão internamente para o controle de porta.
        /// </summary>
        /// <returns></returns>
        protected abstract bool CloseInternal();

        /// <summary>
        /// Grava dados na porta de conexão.
        /// </summary>
        /// <param name="dados"></param>
        public void Write(byte[] dados)
        {
            if (dados.Length < 1) return;

            try
            {
                if (Config.ControlePorta) OpenInternal();

                Writer.Write(Encoding.Convert(Encoding.UTF8, Config.Encoding, dados), 0, dados.Length);
            }
            finally
            {
                if (Config.ControlePorta) CloseInternal();
            }
        }

        /// <summary>
        /// Le dados da porta de conexão
        /// </summary>
        /// <returns></returns>
        public byte[] Read()
        {
            try
            {
                if (Config.ControlePorta) OpenInternal();

                var ret = new List<byte>();

                while (Available > 0)
                {
                    var inbyte = new byte[1];
                    Reader.Read(inbyte, 0, 1);
                    if (inbyte.Length < 1) continue;

                    var value = (byte)inbyte.GetValue(0);
                    ret.Add(value);
                }

                return !ret.Any() ? new byte[0] : Encoding.Convert(Config.Encoding, Encoding.UTF8, ret.ToArray());
            }
            finally
            {
                if (Config.ControlePorta) CloseInternal();
            }
        }

        /// <summary>
        /// Função executa no dispose da classe.
        /// </summary>
        protected virtual void OnDisposing()
        {
        }

        /// <summary>
        /// Metodo responsavel pelo dispose da classe de comunicação.
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                try
                {
                    Reader?.Close();
                }
                catch (Exception e)
                {
                    this.Log().Debug($"[{this}.{MethodBase.GetCurrentMethod().Name}]:[{this.GetType().Name}] Dispose Issue closing reader.", e);
                }
                try
                {
                    Reader?.Dispose();
                }
                catch (Exception e)
                {
                    this.Log().Debug($"[{this}.{MethodBase.GetCurrentMethod().Name}]:[{this.GetType().Name}] Dispose Issue disposing reader.", e);
                }
                try
                {
                    Writer?.Close();
                }
                catch (Exception e)
                {
                    this.Log().Debug($"[{this}.{MethodBase.GetCurrentMethod().Name}]:[{this.GetType().Name}] Dispose Issue closing writer.", e);
                }
                try
                {
                    Writer?.Dispose();
                }
                catch (Exception e)
                {
                    this.Log().Debug($"[{this}.{MethodBase.GetCurrentMethod().Name}]:[{this.GetType().Name}] Dispose Issue disposing writer.", e);
                }
                try
                {
                    OnDisposing();
                }
                catch (Exception e)
                {
                    this.Log().Debug($"[{this}.{MethodBase.GetCurrentMethod().Name}]:[{this.GetType().Name}] Dispose Issue during overridable dispose.", e);
                }
            }

            disposed = true;
        }

        /// <summary>
        /// Implementação da interface IDisposable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion Methods
    }
}