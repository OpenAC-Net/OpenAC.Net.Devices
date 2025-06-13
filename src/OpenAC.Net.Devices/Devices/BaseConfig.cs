// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="BaseConfig.cs" company="OpenAC .Net">
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
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OpenAC.Net.Devices;

/// <summary>
/// Classe base abstrata para configuração de dispositivos, implementando notificações de alteração de propriedades.
/// </summary>
public abstract class BaseConfig : INotifyPropertyChanged, INotifyPropertyChanging, IDeviceConfig
{
    #region Fields

    private bool controlePorta = true;
    private int timeOut;
    private int tentativas;
    private int intervaloTentativas;
    private int readBufferSize;
    private int writeBufferSize;

    #endregion Fields

    #region Public Events

    /// <summary>
    /// Evento disparado antes de uma propriedade ser alterada.
    /// </summary>
    public event PropertyChangingEventHandler? PropertyChanging;

    /// <summary>
    /// Evento disparado após uma propriedade ser alterada.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion Public Events

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="BaseConfig"/>.
    /// </summary>
    /// <param name="name">Nome da configuração do dispositivo.</param>
    protected BaseConfig(string name)
    {
        Name = name;
        ControlePorta = true;
        timeOut = 3;
        tentativas = 3;
        intervaloTentativas = 3000;
        readBufferSize = 200;
        writeBufferSize = 3000;
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Nome da configuração do dispositivo.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Indica se o controle da porta do dispositivo está habilitado.
    /// </summary>
    public bool ControlePorta
    {
        get => controlePorta;
        set => SetProperty(ref controlePorta, value);
    }

    /// <summary>
    /// Tempo limite (em segundos) para operações do dispositivo.
    /// </summary>
    public int TimeOut
    {
        get => timeOut;
        set => SetProperty(ref timeOut, Math.Max(value, 1));
    }

    /// <summary>
    /// Número de tentativas para operações do dispositivo.
    /// </summary>
    public int Tentativas
    {
        get => tentativas;
        set => SetProperty(ref tentativas, Math.Max(value, 1));
    }

    /// <summary>
    /// Intervalo em milissegundos entre as tentativas de operação do dispositivo.
    /// </summary>
    public int IntervaloTentativas
    {
        get => intervaloTentativas;
        set => SetProperty(ref intervaloTentativas, Math.Max(value, 1));
    }

    /// <summary>
    /// Tamanho do buffer de leitura do dispositivo.
    /// </summary>
    public int ReadBufferSize
    {
        get => readBufferSize;
        set => SetProperty(ref readBufferSize, Math.Max(value, 1));
    }

    /// <summary>
    /// Tamanho do buffer de escrita do dispositivo.
    /// </summary>
    public int WriteBufferSize
    {
        get => writeBufferSize;
        set => SetProperty(ref writeBufferSize, Math.Max(value, 1));
    }

    #endregion Properties

    #region Protected Methods

    /// <summary>
    /// Dispara o evento <see cref="PropertyChanged"/> para notificar que uma propriedade foi alterada.
    /// </summary>
    /// <param name="propertyName">Nome da propriedade que foi alterada. Preenchido automaticamente pelo compilador se não especificado.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Debug.Assert(
            string.IsNullOrEmpty(propertyName) ||
            (GetType().GetRuntimeProperty(propertyName) != null),
            "Check that the property name exists for this instance.");

        PropertyChanged?.Raise(this, new PropertyChangedEventArgs(propertyName));
    }
    
    /// <summary>
    /// Dispara o evento <see cref="PropertyChanged"/> para múltiplas propriedades informadas.
    /// </summary>
    /// <param name="propertyNames">Nomes das propriedades que foram alteradas.</param>
    protected void OnPropertyChanged(params string[] propertyNames)
    {
        if (propertyNames == null)
        {
            throw new ArgumentNullException(nameof(propertyNames));
        }

        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
    
    /// <summary>
    /// Dispara o evento <see cref="PropertyChanging"/> para notificar que uma propriedade está prestes a ser alterada.
    /// </summary>
    /// <param name="propertyName">Nome da propriedade que será alterada. Preenchido automaticamente pelo compilador se não especificado.</param>
    protected virtual void OnPropertyChanging([CallerMemberName] string? propertyName = null)
    {
        Debug.Assert(string.IsNullOrEmpty(propertyName) ||
                     GetType().GetRuntimeProperty(propertyName) != null,
            "Check that the property name exists for this instance.");

        PropertyChanging?.Raise(this, new PropertyChangingEventArgs(propertyName));
    }
    
    /// <summary>
    /// Dispara o evento <see cref="PropertyChanging"/> para múltiplas propriedades informadas.
    /// </summary>
    /// <param name="propertyNames">Nomes das propriedades que serão alteradas.</param>
    protected void OnPropertyChanging(params string[] propertyNames)
    {
        if (propertyNames == null)
        {
            throw new ArgumentNullException(nameof(propertyNames));
        }

        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanging(propertyName);
        }
    }

    /// <summary>
    /// Define o valor da propriedade especificada se ela tiver sido alterada.
    /// </summary>
    /// <typeparam name="TProp">Tipo da propriedade.</typeparam>
    /// <param name="currentValue">Valor atual da propriedade (por referência).</param>
    /// <param name="newValue">Novo valor a ser definido.</param>
    /// <param name="propertyName">Nome da propriedade (opcional, preenchido automaticamente).</param>
    /// <param name="onChanged">Ação opcional a ser executada após a alteração.</param>
    /// <returns><c>true</c> se a propriedade foi alterada; caso contrário, <c>false</c>.</returns>
    protected bool SetProperty<TProp>(
        ref TProp currentValue,
        TProp newValue,
        [CallerMemberName] string propertyName = "",
        Action? onChanged = null)
    {
        if (EqualityComparer<TProp>.Default.Equals(currentValue, newValue))
            return false;

        OnPropertyChanging(propertyName);
        currentValue = newValue;
        onChanged?.Invoke();
        OnPropertyChanged(propertyName);

        return true;
    }
    
    /// <summary>
    /// Define o valor da propriedade especificada se ela tiver sido alterada, notificando múltiplas propriedades.
    /// </summary>
    /// <typeparam name="TProp">Tipo da propriedade.</typeparam>
    /// <param name="currentValue">Valor atual da propriedade (por referência).</param>
    /// <param name="newValue">Novo valor a ser definido.</param>
    /// <param name="propertyNames">Nomes das propriedades a serem notificadas.</param>
    /// <returns><c>true</c> se a propriedade foi alterada; caso contrário, <c>false</c>.</returns>
    protected bool SetProperty<TProp>(
        ref TProp currentValue,
        TProp newValue,
        params string[] propertyNames)
    {
        if (EqualityComparer<TProp>.Default.Equals(currentValue, newValue))
            return false;

        OnPropertyChanging(propertyNames);
        currentValue = newValue;
        OnPropertyChanged(propertyNames);

        return true;
    }

    /// <summary>
    /// Sets the value of the property to the specified value if it has changed.
    /// </summary>
    /// <param name="equal">A function which returns <c>true</c> if the property value has changed, otherwise <c>false</c>.</param>
    /// <param name="action">The action where the property is set.</param>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns><c>true</c> if the property was changed, otherwise <c>false</c>.</returns>
    protected bool SetProperty(
        Func<bool> equal,
        Action action,
        [CallerMemberName] string? propertyName = null)
    {
        if (equal())
        {
            return false;
        }

        OnPropertyChanging(propertyName);
        action();
        OnPropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    /// Sets the value of the property to the specified value if it has changed.
    /// </summary>
    /// <param name="equal">A function which returns <c>true</c> if the property value has changed, otherwise <c>false</c>.</param>
    /// <param name="action">The action where the property is set.</param>
    /// <param name="propertyNames">The property names.</param>
    /// <returns><c>true</c> if the property was changed, otherwise <c>false</c>.</returns>
    protected bool SetProperty(
        Func<bool> equal,
        Action action,
        params string[] propertyNames)
    {
        if (equal())
        {
            return false;
        }

        OnPropertyChanging(propertyNames);
        action();
        OnPropertyChanged(propertyNames);

        return true;
    }

    #endregion Protected Methods
}