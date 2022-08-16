// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="EventHandlerExtension.cs" company="OpenAC .Net">
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

using System.ComponentModel;

namespace OpenAC.Net.Devices
{
    internal static class EventHandlerExtension
    {
        /// <summary>
        /// Chama o evento.
        /// </summary>
        /// <param name="eventHandler">The event handler.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        public static void Raise(this PropertyChangingEventHandler eventHandler, object sender, PropertyChangingEventArgs e)
        {
            if (eventHandler == null)
                return;

            if (eventHandler.Target is ISynchronizeInvoke { InvokeRequired: true } synchronizeInvoke)
            {
                synchronizeInvoke.Invoke(eventHandler, new[] { sender, e });
            }
            else
            {
                eventHandler.DynamicInvoke(sender, e);
            }
        }

        /// <summary>
        /// Chama o evento.
        /// </summary>
        /// <param name="eventHandler">The event handler.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        public static void Raise(this PropertyChangedEventHandler eventHandler, object sender, PropertyChangedEventArgs e)
        {
            if (eventHandler == null)
                return;

            if (eventHandler.Target is ISynchronizeInvoke { InvokeRequired: true } synchronizeInvoke)
            {
                synchronizeInvoke.Invoke(eventHandler, new[] { sender, e });
            }
            else
            {
                eventHandler.DynamicInvoke(sender, e);
            }
        }
    }
}