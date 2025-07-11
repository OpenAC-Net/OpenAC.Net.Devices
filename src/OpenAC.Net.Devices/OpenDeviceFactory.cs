﻿// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="OpenDeviceFactory.cs" company="OpenAC .Net">
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
using System.Collections.Generic;
using System.Linq;
using OpenAC.Net.Core;

namespace OpenAC.Net.Devices;

/// <summary>
/// Classe para gerenciar os tipos de comunicação.
/// </summary>
public static class OpenDeviceFactory
{
    #region Fields

    private static readonly Dictionary<Type, Type> Communications;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// Construtor estático da classe <see cref="OpenDeviceFactory"/>.
    /// Inicializa o dicionário de mapeamento entre tipos de configuração e tipos de dispositivos de comunicação.
    /// </summary>
    static OpenDeviceFactory()
    {
        Communications = new Dictionary<Type, Type>
        {
            {typeof(SerialConfig), typeof(OpenSerialStream)},
            {typeof(TCPConfig), typeof(OpenTcpStream)},
            {typeof(RawConfig), typeof(OpenRawStream)},
            {typeof(FileConfig), typeof(OpenFileStream)}
        };
    }

    #endregion Constructors

    #region Methods

    /// <summary>
    /// Registrar uma nova classe de comunicação
    /// </summary>
    /// <typeparam name="TConfig">Classe de configuração da classe device sendo registrada.</typeparam>
    /// <typeparam name="TDevice">Classe de comunicação a ser registrada</typeparam>
    public static void Register<TConfig, TDevice>()
        where TConfig : IDeviceConfig
        where TDevice : OpenDeviceStream
    {
        Communications.Add(typeof(TConfig), typeof(TDevice));
    }

    /// <summary>
    /// Cria e retorna uma instância da classe de comunicação apropriada com base na configuração informada.
    /// </summary>
    /// <param name="config">Instância de configuração que define o tipo de comunicação desejado.</param>
    /// <returns>Uma instância de <see cref="OpenDeviceStream"/> correspondente à configuração fornecida.</returns>
    /// <exception cref="OpenException">Lançada caso não seja encontrada uma classe de comunicação compatível com a configuração.</exception>
    public static OpenDeviceStream Create(IDeviceConfig config)
    {
        var configType = config.GetType();
        var device = (from c in Communications
            where c.Key == configType || configType.IsAssignableFrom(c.Key)
            select c.Value).FirstOrDefault();

        if(device == null) throw new OpenException("Classe de comunicação não localizada.");
        return (OpenDeviceStream?)Activator.CreateInstance(device, config) ?? throw new OpenException("Erro ao instanciar a classe de comunicação.");
    }

    #endregion Methods
}