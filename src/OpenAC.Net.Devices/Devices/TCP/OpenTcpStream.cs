// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="OpenTcpStream.cs" company="OpenAC .Net">
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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;

namespace OpenAC.Net.Devices;

/// <summary>
/// Implementa um stream de dispositivo TCP baseado em <see cref="OpenDeviceStream{TConfig}"/>.
/// </summary>
internal sealed class OpenTcpStream : OpenDeviceStream<TCPConfig>
{
    #region Fields

    private TcpClient? client;

    #endregion Fields

    #region Constructor

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="OpenTcpStream"/>.
    /// </summary>
    /// <param name="config">Configuração TCP a ser utilizada.</param>
    public OpenTcpStream(TCPConfig config) : base(config)
    {
        Guard.Against<ArgumentException>(config.IP!.IsEmpty(), "Endereço não informado.");
        Guard.Against<ArgumentException>(config.Porta < 1 || config.Porta > 65535, "Porta não informada ou inválida.");
    }

    #endregion Constructor

    #region Properties

    protected override int Available => client?.Connected == true ? client.Available : 0;

    #endregion Properties

    #region Methods

    public override void Limpar()
    {
        if (client is not { Connected: true }) return;

        var stream = client.GetStream();
        var bufferSize = Math.Max(Config.ReadBufferSize, 1);
        var buffer = new byte[bufferSize];

        while (client.Available > 0)
        {
            var toRead = Math.Min(client.Available, buffer.Length);
            var read = stream.Read(buffer, 0, toRead);
            if (read < 1) break;
        }
    }

    protected override bool OpenInternal()
    {
        if (client is { Connected: true }) return false;

        DisposeClient();
        client = CreateConfiguredClient();
        client.Connect(Config.IP!, Config.Porta);

        var stream = client.GetStream();
        Reader = new BinaryReader(stream);
        Writer = new BinaryWriter(stream);

        return client.Connected;
    }

    protected override async Task<bool> OpenInternalAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (client is { Connected: true }) return false;

        DisposeClient();
        client = CreateConfiguredClient();

#if NET5_0_OR_GREATER
        await client.ConnectAsync(Config.IP!, Config.Porta, cancellationToken).ConfigureAwait(false);
#else
        await client.ConnectAsync(Config.IP!, Config.Porta).ConfigureAwait(false);
#endif

        var stream = client.GetStream();
        Reader = new BinaryReader(stream);
        Writer = new BinaryWriter(stream);

        return client.Connected;
    }

    protected override bool CloseInternal()
    {
        if (client == null && Reader == null && Writer == null) return false;

        DisposeClient();
        return true;
    }

    private TcpClient CreateConfiguredClient()
    {
        var tcpClient = new TcpClient
        {
            ReceiveTimeout = Config.TimeOut * 1000,
            SendTimeout = Config.TimeOut * 1000,
            ReceiveBufferSize = Math.Max(Config.ReadBufferSize, 1),
            SendBufferSize = Math.Max(Config.WriteBufferSize, 1)
        };

        return tcpClient;
    }

    private void DisposeClient()
    {
        try
        {
            Reader?.Dispose();
        }
        catch
        {
            // Ignora erro ao descartar Reader
        }

        try
        {
            Writer?.Dispose();
        }
        catch
        {
            // Ignora erro ao descartar Writer
        }

        try
        {
            ((IDisposable?)client)?.Dispose();
        }
        catch
        {
            // Ignora erro ao descartar TcpClient
        }

        Reader = null;
        Writer = null;
        client = null;
    }

    #endregion Methods

    #region Dispose Methods

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        DisposeClient();
    }

    #endregion Dispose Methods
}