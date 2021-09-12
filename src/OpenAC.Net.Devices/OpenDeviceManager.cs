// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="OpenDeviceManager.cs" company="OpenAC .Net">
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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenAC.Net.Core;

namespace OpenAC.Net.Devices
{
    public static class OpenDeviceManager
    {
        #region Fields

        private static readonly Dictionary<string, Type> communications;

        #endregion Fields

        #region Constructors

        static OpenDeviceManager()
        {
            communications = new Dictionary<string, Type>
            {
                {"LPT", typeof(OpenSerialStream)},
                {"COM", typeof(OpenSerialStream)},
                {"TCP", typeof(OpenTcpStream)},
#if NETFULL
                // Precisa ser ajustada para ser usado no linux.
                {"RAW", typeof(OpenRawStream)},
                // Ainda em desenvolvimento
                //{"USB", typeof(OpenUSBStream)},
#endif
            };
        }

        #endregion Constructors

        /// <summary>
        /// Registrar uma nova classe de comunicação
        /// </summary>
        /// <param name="tag"></param>
        /// <typeparam name="T"></typeparam>
        public static void Register<T>(string tag) where T : OpenDeviceStream
        {
            communications.Add(tag, typeof(T));
        }

        /// <summary>
        /// Função para checar se a porta é valida
        /// </summary>
        /// <param name="porta"></param>
        /// <returns></returns>
        public static bool IsValidPort(string porta)
        {
            return (from c in communications
                    where porta.ToUpper().StartsWith(c.Key)
                    select c.Value).Any();
        }

        /// <summary>
        /// Retorna a classe para comunicação
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static OpenDeviceStream GetCommunication(OpenDeviceConfig config)
        {
            var communication = (from c in communications
                                 where config.Porta.ToUpper().StartsWith(c.Key)
                                 select c.Value).FirstOrDefault();

            Guard.Against<OpenException>(communication == null, "Classe de comunicação não localizada.");
            return (OpenDeviceStream)Activator.CreateInstance(communication, config);
        }
    }
}