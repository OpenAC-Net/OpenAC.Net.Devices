﻿// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="FileConfig.cs" company="OpenAC .Net">
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
/// Configuração para dispositivos do tipo arquivo.
/// </summary>
public sealed class FileConfig : BaseConfig
{
    #region Fields

    private string? file;
    private bool createIfNotExits;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="FileConfig"/> com o caminho do arquivo especificado.
    /// </summary>
    public FileConfig() : base("File")
    {
    }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="FileConfig"/> com o caminho do arquivo especificado.
    /// </summary>
    /// <param name="file">O caminho do arquivo a ser utilizado pelo dispositivo.</param>
    public FileConfig(string file) : this()
    {
        this.file = file;
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Obtém ou define o caminho do arquivo a ser utilizado pelo dispositivo.
    /// </summary>
    public string? File
    {
        get => file;
        set => SetProperty(ref file, value);
    }

    /// <summary>
    /// Obtém ou define se o arquivo deve ser criado caso não exista.
    /// </summary>
    public bool CreateIfNotExits
    {
        get => createIfNotExits;
        set => SetProperty(ref createIfNotExits, value);
    }

    #endregion Properties
}