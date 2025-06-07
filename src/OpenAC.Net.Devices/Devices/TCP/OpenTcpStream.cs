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
using System.Net;
using System.Net.Sockets;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;

namespace OpenAC.Net.Devices;

internal sealed class OpenTcpStream : OpenDeviceStream<TCPConfig>
{
    #region Fields

    private TcpClient client;
    private readonly IPEndPoint conEndPoint;

    #endregion Fields

    #region Constructor

    public OpenTcpStream(TCPConfig config) : base(config)
    {
        Guard.Against<ArgumentException>(config.IP.IsEmpty(), "Endereço não informados");
        Guard.Against<ArgumentException>(config.Porta < 1, "Porta não informados");

        conEndPoint = new IPEndPoint(IPAddress.Parse(config.IP), config.Porta);
        client = new TcpClient();
    }

    #endregion Constructor

    #region Properties

    protected override int Available => client?.Available ?? 0;

    #endregion Properties

    #region Methods

    public override async void Limpar()
    {
        if (client is not { Connected: true }) return;

        var stream = client.GetStream();

        while (client.Available > 0)
        {
            var inbyte = new byte[1];
            await stream.ReadAsync(inbyte, 0, 1);
        }
    }

    protected override bool OpenInternal()
    {
        if (client.Connected) return false;

        client.Connect(conEndPoint);

        var stream = client.GetStream();
        Reader = new BinaryReader(stream);
        Writer = new BinaryWriter(stream);

        return client.Connected;
    }

    protected override bool CloseInternal()
    {
        if (!client.Connected) return false;

        ((IDisposable)client)?.Dispose();
        Reader?.Dispose();
        Writer?.Dispose();

        Reader = null;
        Writer = null;

        client = null;
        client = new TcpClient();

        return !client.Connected;
    }

    #endregion Methods

    #region Dispose Methods

    protected override void DisposeManaged()
    {
        base.DisposeManaged();
        ((IDisposable)client)?.Dispose();
    }

    #endregion Dispose Methods
}