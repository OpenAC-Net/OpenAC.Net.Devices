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
//	     		    Copyright (c) 2014 - 2024 Projeto OpenAC .Net
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
using System.Linq;
using System.Reflection;
using System.Threading;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Logging;
using OpenAC.Net.Devices.Commom;

namespace OpenAC.Net.Devices;

/// <summary>
/// Classe abstrata para comunicação com dispositivos.
/// </summary>
public abstract class OpenDeviceStream : OpenDisposable, IOpenLog
{
    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="OpenDeviceStream"/>.
    /// </summary>
    /// <param name="config">Configurações do dispositivo.</param>
    protected OpenDeviceStream(IDeviceConfig config)
    {
        Config = config;
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Obtém as configurações do dispositivo.
    /// </summary>
    public IDeviceConfig Config { get; }

    /// <summary>
    /// Indica se é possível realizar leitura.
    /// </summary>
    public bool CanRead => Reader != null;

    /// <summary>
    /// Indica se é possível realizar escrita.
    /// </summary>
    public bool CanWrite => Writer != null;

    /// <summary>
    /// Indica se o dispositivo está conectado.
    /// </summary>
    public bool Conectado { get; protected set; }

    /// <summary>
    /// Escritor binário para comunicação com o dispositivo.
    /// </summary>
    protected BinaryWriter? Writer { get; set; }

    /// <summary>
    /// Leitor binário para comunicação com o dispositivo.
    /// </summary>
    protected BinaryReader? Reader { get; set; }

    /// <summary>
    /// Obtém o número de bytes disponíveis para leitura.
    /// </summary>
    protected abstract int Available { get; }

    #endregion Properties

    #region Methods

    /// <summary>
    /// Abre a conexão com o dispositivo.
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
    /// Fecha a conexão com o dispositivo.
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
    /// Limpa os dados do buffer de leitura.
    /// </summary>
    public virtual void Limpar()
    {
        //Limpar stream de leitura.
    }

    /// <summary>
    /// Abre a conexão internamente para controle de porta.
    /// </summary>
    /// <returns>Retorna <c>true</c> se a conexão foi aberta com sucesso; caso contrário, <c>false</c>.</returns>
    protected abstract bool OpenInternal();

    /// <summary>
    /// Fecha a conexão internamente para controle de porta.
    /// </summary>
    /// <returns>Retorna <c>true</c> se a conexão foi fechada com sucesso; caso contrário, <c>false</c>.</returns>
    protected abstract bool CloseInternal();

    /// <summary>
    /// Grava dados na porta de conexão.
    /// </summary>
    /// <param name="dados">Dados a serem enviados.</param>
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
    /// Grava e lê o retorno da porta de conexão.
    /// </summary>
    /// <param name="dados">Dados a serem enviados.</param>
    /// <param name="timeSleep">Tempo de espera entre gravação e leitura.</param>
    /// <returns>Retorna os dados recebidos.</returns>
    public byte[] WriteRead(byte[] dados, int timeSleep = 10)
    {
        if (dados.Length < 1) return [];

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

                    ret.Append(inbyte.Take(read));
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
    /// Lê dados da porta de conexão.
    /// </summary>
    /// <returns>Retorna os dados lidos.</returns>
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

                    ret.Append(inbyte.Take(read));
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
    /// Libera os recursos gerenciados.
    /// </summary>
    protected override void DisposeManaged()
    {
        try
        {
            Reader?.Close();
        }
        catch (Exception e)
        {
            this.Log().Debug($"[{this}.{MethodBase.GetCurrentMethod()?.Name}]:[{GetType().Name}] Dispose Issue closing reader.", e);
        }
        try
        {
            Reader?.Dispose();
        }
        catch (Exception e)
        {
            this.Log().Debug($"[{this}.{MethodBase.GetCurrentMethod()?.Name}]:[{GetType().Name}] Dispose Issue disposing reader.", e);
        }
        try
        {
            Writer?.Close();
        }
        catch (Exception e)
        {
            this.Log().Debug($"[{this}.{MethodBase.GetCurrentMethod()?.Name}]:[{GetType().Name}] Dispose Issue closing writer.", e);
        }
        try
        {
            Writer?.Dispose();
        }
        catch (Exception e)
        {
            this.Log().Debug($"[{this}.{MethodBase.GetCurrentMethod()?.Name}]:[{GetType().Name}] Dispose Issue disposing writer.", e);
        }
    }

    #endregion Methods
}