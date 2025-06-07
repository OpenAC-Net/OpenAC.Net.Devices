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
    ///   Ocorre quando um valor da propriedade está sendo alterado.
    /// </summary>
    public event PropertyChangingEventHandler PropertyChanging;

    /// <summary>
    /// Ocorre quando um valor de propriedade é alterado.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Public Events

    #region Constructors

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

    public string Name { get; }

    public bool ControlePorta
    {
        get => controlePorta;
        set => SetProperty(ref controlePorta, value);
    }

    public int TimeOut
    {
        get => timeOut;
        set => SetProperty(ref timeOut, Math.Max(value, 1));
    }

    public int Tentativas
    {
        get => tentativas;
        set => SetProperty(ref tentativas, Math.Max(value, 1));
    }

    public int IntervaloTentativas
    {
        get => intervaloTentativas;
        set => SetProperty(ref intervaloTentativas, Math.Max(value, 1));
    }

    public int ReadBufferSize
    {
        get => readBufferSize;
        set => SetProperty(ref readBufferSize, Math.Max(value, 1));
    }

    public int WriteBufferSize
    {
        get => writeBufferSize;
        set => SetProperty(ref writeBufferSize, Math.Max(value, 1));
    }

    #endregion Properties

    #region Protected Methods

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        Debug.Assert(
            string.IsNullOrEmpty(propertyName) ||
            (GetType().GetRuntimeProperty(propertyName) != null),
            "Check that the property name exists for this instance.");

        PropertyChanged?.Raise(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="propertyNames">The property names.</param>
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
    /// Raises the PropertyChanging event.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    protected virtual void OnPropertyChanging([CallerMemberName] string propertyName = null)
    {
        Debug.Assert(string.IsNullOrEmpty(propertyName) ||
                     GetType().GetRuntimeProperty(propertyName) != null,
            "Check that the property name exists for this instance.");

        PropertyChanging?.Raise(this, new PropertyChangingEventArgs(propertyName));
    }

    /// <summary>
    /// Raises the PropertyChanging event.
    /// </summary>
    /// <param name="propertyNames">The property names.</param>
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
    /// Sets the value of the property to the specified value if it has changed.
    /// </summary>
    /// <typeparam name="TProp">The type of the property.</typeparam>
    /// <param name="currentValue">The current value of the property.</param>
    /// <param name="newValue">The new value of the property.</param>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns><c>true</c> if the property was changed, otherwise <c>false</c>.</returns>
    protected bool SetProperty<TProp>(
        ref TProp currentValue,
        TProp newValue,
        [CallerMemberName] string propertyName = "",
        Action onChanged = null)
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
    /// Sets the value of the property to the specified value if it has changed.
    /// </summary>
    /// <typeparam name="TProp">The type of the property.</typeparam>
    /// <param name="currentValue">The current value of the property.</param>
    /// <param name="newValue">The new value of the property.</param>
    /// <param name="propertyNames">The names of all properties changed.</param>
    /// <returns><c>true</c> if the property was changed, otherwise <c>false</c>.</returns>
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
        [CallerMemberName] string propertyName = null)
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