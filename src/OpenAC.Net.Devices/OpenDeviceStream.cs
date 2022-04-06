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
using System.IO;
using System.Reflection;
using System.Threading;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Logging;
using OpenAC.Net.Devices.Commom;

namespace OpenAC.Net.Devices
{
    /// <summary>
    /// Classe para comunicaçõ com dispositivos.
    /// </summary>
    public abstract class OpenDeviceStream : OpenDisposable, IOpenLog
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

        #endregion Constructors

        #region Properties

        public IDeviceConfig Config { get; }

        public bool CanRead => Reader != null;

        public bool CanWrite => Writer != null;

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

                var bytePointer = 0;
                var bytesLeft = dados.Length;
                var tentativas = 0;
                while (bytesLeft > 0)
                {
                    var count = Math.Min(Config.WriteBufferSize, bytesLeft);

                    try
                    {
                        Writer.Write(dados, bytePointer, count);
                    }
                    catch (IOException ex)
                    {
                        this.Log().Error($"[{MethodBase.GetCurrentMethod()?.Name}]: Erro ao enviar dados ao dispositivo", ex);
                        tentativas++;
                        if (tentativas >= Config.Tentativas) throw;

                        Thread.Sleep(Config.IntervaloTentativas);

                        CloseInternal();
                        OpenInternal();
                        Writer.Write(dados, bytePointer, count);
                    }

                    bytePointer += count;
                    bytesLeft -= count;
                }
            }
            finally
            {
                if (Config.ControlePorta) CloseInternal();
            }
        }

        /// <summary>
        /// Grava e le o retorno da porta de conexão.
        /// </summary>
        /// <param name="dados"></param>
        /// <param name="timeSleep"></param>
        /// <returns></returns>
        public byte[] WriteRead(byte[] dados, int timeSleep = 10)
        {
            if (dados.Length < 1) return new byte[0];

            try
            {
                if (Config.ControlePorta) OpenInternal();

                var bytePointer = 0;
                var bytesLeft = dados.Length;
                var tentativas = 0;
                while (bytesLeft > 0)
                {
                    var count = Math.Min(Config.WriteBufferSize, bytesLeft);

                    try
                    {
                        Writer.Write(dados, bytePointer, count);
                    }
                    catch (IOException ex)
                    {
                        this.Log().Error($"[{MethodBase.GetCurrentMethod()?.Name}]: Erro ao enviar dados ao dispositivo", ex);
                        tentativas++;
                        if (tentativas >= Config.Tentativas) throw;

                        Thread.Sleep(Config.IntervaloTentativas);

                        CloseInternal();
                        OpenInternal();
                        Writer.Write(dados, bytePointer, count);
                    }

                    bytePointer += count;
                    bytesLeft -= count;
                }

                Thread.Sleep(timeSleep);

                var ret = new ByteArrayBuilder();
                var bufferSize = Math.Max(Config.ReadBufferSize, 1);
                tentativas = 0;

                while (Available > 0)
                {
                    try
                    {
                        var inbyte = new byte[bufferSize];
                        var read = Reader.Read(inbyte, 0, bufferSize);
                        if (read < 1) continue;

                        var value = (byte)inbyte.GetValue(0);
                        ret.Append(value);
                    }
                    catch (IOException ex)
                    {
                        this.Log().Error($"[{MethodBase.GetCurrentMethod()?.Name}]: Erro ao enviar dados ao dispositivo", ex);
                        tentativas++;
                        if (tentativas >= Config.Tentativas) throw;

                        Thread.Sleep(Config.IntervaloTentativas);

                        CloseInternal();
                        OpenInternal();
                    }
                }

                return ret.ToArray();
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

                var ret = new ByteArrayBuilder();
                var bufferSize = Math.Max(Config.ReadBufferSize, 1);
                var tentativas = 0;

                while (Available > 0)
                {
                    try
                    {
                        var inbyte = new byte[bufferSize];
                        var read = Reader.Read(inbyte, 0, bufferSize);
                        if (read < 1) continue;

                        var value = (byte)inbyte.GetValue(0);
                        ret.Append(value);
                    }
                    catch (IOException ex)
                    {
                        this.Log().Error($"[{MethodBase.GetCurrentMethod()?.Name}]: Erro ao enviar dados ao dispositivo", ex);
                        tentativas++;
                        if (tentativas >= Config.Tentativas) throw;

                        Thread.Sleep(Config.IntervaloTentativas);

                        CloseInternal();
                        OpenInternal();
                    }
                }

                return ret.ToArray();
            }
            finally
            {
                if (Config.ControlePorta) CloseInternal();
            }
        }

        protected override void DisposeManaged()
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
        }

        #endregion Methods
    }
}