// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="IDeviceConfig.cs" company="OpenAC .Net">
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

namespace OpenAC.Net.Devices;

/// <summary>
/// Interface para implementação das configurações da classe de comunicação.
/// </summary>
public interface IDeviceConfig
{
    /// <summary>
    /// Retorna o nome do dispositivo desta configuração.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Retorna/define se o controle da porta será feito de forma automatica.
    /// </summary>
    bool ControlePorta { get; set; }

    /// <summary>
    /// Retorna/define o timeout da conexão.
    /// </summary>
    int TimeOut { get; set; }

    /// <summary>
    /// Retorna/define o número de tentativas de conexão.
    /// </summary>
    int Tentativas { get; set; }

    /// <summary>
    /// Retorna/define o intervalo entre as tentativas de conexão.
    /// </summary>
    int IntervaloTentativas { get; set; }

    /// <summary>
    /// Retorna/define o tamanho do buffer de leitura.
    /// </summary>

    int ReadBufferSize { get; set; }

    /// <summary>
    /// Retorna/define o tamanho do buffer de escrita.
    /// </summary>

    int WriteBufferSize { get; set; }
}