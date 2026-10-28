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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
        Config = config ?? throw new ArgumentNullException(nameof(config));
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
    public virtual void Open()
    {
        this.Log().Info($"{new string('-', 80)}" + Environment.NewLine +
                        $"Open - {DateTime.Now:G}" + Environment.NewLine +
                        $"- Config: {Config.Name}" + Environment.NewLine +
                        $"{new string('-', 80)}");

        Conectado = OpenInternal();

        if (Config.ControlePorta && Conectado) CloseInternal();
    }

    /// <summary>
    /// Abre a conexão com o dispositivo de forma assíncrona.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        this.Log().Info($"{new string('-', 80)}" + Environment.NewLine +
                        $"OpenAsync - {DateTime.Now:G}" + Environment.NewLine +
                        $"- Config: {Config.Name}" + Environment.NewLine +
                        $"{new string('-', 80)}");

        Conectado = await OpenInternalAsync(cancellationToken).ConfigureAwait(false);

        if (Config.ControlePorta && Conectado)
            await CloseInternalAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fecha a conexão com o dispositivo.
    /// </summary>
    public virtual void Close()
    {
        this.Log().Info($"{new string('-', 80)}" + Environment.NewLine +
                        $"Close - {DateTime.Now:G}" + Environment.NewLine +
                        $"- Config: {Config.Name}" + Environment.NewLine +
                        $"{new string('-', 80)}");

        if (Config.ControlePorta && Conectado || CloseInternal())
            Conectado = false;
    }

    /// <summary>
    /// Fecha a conexão com o dispositivo de forma assíncrona.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        this.Log().Info($"{new string('-', 80)}" + Environment.NewLine +
                        $"CloseAsync - {DateTime.Now:G}" + Environment.NewLine +
                        $"- Config: {Config.Name}" + Environment.NewLine +
                        $"{new string('-', 80)}");

        if (Config.ControlePorta && Conectado || await CloseInternalAsync(cancellationToken).ConfigureAwait(false))
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
    /// Abre a conexão internamente de forma assíncrona para controle de porta.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Retorna <c>true</c> se a conexão foi aberta com sucesso; caso contrário, <c>false</c>.</returns>
    protected virtual Task<bool> OpenInternalAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(OpenInternal());
    }

    /// <summary>
    /// Fecha a conexão internamente para controle de porta.
    /// </summary>
    /// <returns>Retorna <c>true</c> se a conexão foi fechada com sucesso; caso contrário, <c>false</c>.</returns>
    protected abstract bool CloseInternal();

    /// <summary>
    /// Fecha a conexão internamente de forma assíncrona para controle de porta.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Retorna <c>true</c> se a conexão foi fechada com sucesso; caso contrário, <c>false</c>.</returns>
    protected virtual Task<bool> CloseInternalAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CloseInternal());
    }

    /// <summary>
    /// Grava dados na porta de conexão.
    /// </summary>
    /// <param name="dados">Dados a serem enviados.</param>
    public virtual void Write(byte[] dados)
    {
        if (dados.Length < 1) return;

        try
        {
            if (Config.ControlePorta) OpenInternal();

            WriteDados(dados);
        }
        finally
        {
            if (Config.ControlePorta) CloseInternal();
        }
    }

    /// <summary>
    /// Grava dados na porta de conexão de forma assíncrona.
    /// </summary>
    /// <param name="dados">Dados a serem enviados.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    public virtual async Task WriteAsync(byte[] dados, CancellationToken cancellationToken = default)
    {
        if (dados.Length < 1) return;

        try
        {
            if (Config.ControlePorta)
                await OpenInternalAsync(cancellationToken).ConfigureAwait(false);

            await WriteDadosAsync(dados, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (Config.ControlePorta)
                await CloseInternalAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Grava e lê o retorno da porta de conexão.
    /// </summary>
    /// <param name="dados">Dados a serem enviados.</param>
    /// <param name="timeSleep">Tempo de espera entre gravação e leitura.</param>
    /// <returns>Retorna os dados recebidos.</returns>
    public virtual byte[] WriteRead(byte[] dados, int timeSleep = 10)
    {
        if (dados.Length < 1) return [];

        try
        {
            if (Config.ControlePorta) OpenInternal();

            WriteDados(dados);

            Thread.Sleep(timeSleep);

            return ReadDados(reconnectOnError: false);
        }
        finally
        {
            if (Config.ControlePorta) CloseInternal();
        }
    }

    /// <summary>
    /// Grava e lê o retorno da porta de conexão de forma assíncrona.
    /// </summary>
    /// <param name="dados">Dados a serem enviados.</param>
    /// <param name="timeSleep">Tempo de espera entre gravação e leitura.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Retorna os dados recebidos.</returns>
    public virtual async Task<byte[]> WriteReadAsync(byte[] dados, int timeSleep = 10, CancellationToken cancellationToken = default)
    {
        if (dados.Length < 1) return [];

        try
        {
            if (Config.ControlePorta)
                await OpenInternalAsync(cancellationToken).ConfigureAwait(false);

            await WriteDadosAsync(dados, cancellationToken).ConfigureAwait(false);

            await Task.Delay(timeSleep, cancellationToken).ConfigureAwait(false);

            return await ReadDadosAsync(reconnectOnError: false, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (Config.ControlePorta)
                await CloseInternalAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Lê dados da porta de conexão.
    /// </summary>
    /// <returns>Retorna os dados lidos.</returns>
    public virtual byte[] Read()
    {
        try
        {
            if (Config.ControlePorta) OpenInternal();

            return ReadDados(reconnectOnError: true);
        }
        finally
        {
            if (Config.ControlePorta) CloseInternal();
        }
    }

    /// <summary>
    /// Lê dados da porta de conexão de forma assíncrona.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Retorna os dados lidos.</returns>
    public virtual async Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (Config.ControlePorta)
                await OpenInternalAsync(cancellationToken).ConfigureAwait(false);

            return await ReadDadosAsync(reconnectOnError: true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (Config.ControlePorta)
                await CloseInternalAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void WriteDados(byte[] dados, [CallerMemberName] string? methodName = null)
    {
        var bytePointer = 0;
        var bytesLeft = dados.Length;
        var tentativas = 0;
        while (bytesLeft > 0)
        {
            var count = Math.Min(Config.WriteBufferSize, bytesLeft);

            try
            {
                Writer?.Write(dados, bytePointer, count);
                bytePointer += count;
                bytesLeft -= count;
            }
            catch (IOException ex)
            {
                this.Log().Error($"[{methodName}]: Erro ao enviar dados ao dispositivo", ex);
                tentativas++;
                if (tentativas >= Config.Tentativas) throw;

                Thread.Sleep(Config.IntervaloTentativas);

                CloseInternal();
                OpenInternal();
            }
        }

        Writer?.Flush();
    }

    private async Task WriteDadosAsync(byte[] dados, CancellationToken cancellationToken, [CallerMemberName] string? methodName = null)
    {
        var bytePointer = 0;
        var bytesLeft = dados.Length;
        var tentativas = 0;
        while (bytesLeft > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = Math.Min(Config.WriteBufferSize, bytesLeft);

            try
            {
                if (Writer != null)
                    await Writer.BaseStream.WriteAsync(dados, bytePointer, count, cancellationToken).ConfigureAwait(false);

                bytePointer += count;
                bytesLeft -= count;
            }
            catch (IOException ex)
            {
                this.Log().Error($"[{methodName}]: Erro ao enviar dados ao dispositivo", ex);
                tentativas++;
                if (tentativas >= Config.Tentativas) throw;

                await Task.Delay(Config.IntervaloTentativas, cancellationToken).ConfigureAwait(false);

                await CloseInternalAsync(cancellationToken).ConfigureAwait(false);
                await OpenInternalAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        if (Writer != null)
            await Writer.BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private byte[] ReadDados(bool reconnectOnError, [CallerMemberName] string? methodName = null)
    {
        using var ret = new ByteArrayBuilder();
        var bufferSize = Math.Max(Config.ReadBufferSize, 1);
        var inbyte = new byte[bufferSize];
        var tentativas = 0;

        while (Available > 0)
        {
            try
            {
                var read = Reader?.Read(inbyte, 0, bufferSize) ?? 0;
                if (read < 1) continue;

                ret.Append(inbyte, 0, read);
            }
            catch (IOException ex)
            {
                this.Log().Error($"[{methodName}]: Erro ao ler dados do dispositivo", ex);
                tentativas++;
                if (tentativas >= Config.Tentativas) throw;

                Thread.Sleep(Config.IntervaloTentativas);

                if (reconnectOnError)
                {
                    CloseInternal();
                    OpenInternal();
                }
            }
        }

        return ret.ToArray();
    }

    private async Task<byte[]> ReadDadosAsync(bool reconnectOnError, CancellationToken cancellationToken, [CallerMemberName] string? methodName = null)
    {
        using var ret = new ByteArrayBuilder();
        var bufferSize = Math.Max(Config.ReadBufferSize, 1);
        var inbyte = new byte[bufferSize];
        var tentativas = 0;

        while (Available > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var read = Reader != null
                    ? await Reader.BaseStream.ReadAsync(inbyte, 0, bufferSize, cancellationToken).ConfigureAwait(false)
                    : 0;
                if (read < 1) continue;

                ret.Append(inbyte, 0, read);
            }
            catch (IOException ex)
            {
                this.Log().Error($"[{methodName}]: Erro ao ler dados do dispositivo", ex);
                tentativas++;
                if (tentativas >= Config.Tentativas) throw;

                await Task.Delay(Config.IntervaloTentativas, cancellationToken).ConfigureAwait(false);

                if (reconnectOnError)
                {
                    await CloseInternalAsync(cancellationToken).ConfigureAwait(false);
                    await OpenInternalAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return ret.ToArray();
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
