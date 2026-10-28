// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 28-10-2026
//
// Last Modified By : RFTD
// Last Modified On : 28-10-2026
// ***********************************************************************
// <copyright file="OSPlatformHelper.cs" company="OpenAC .Net">
//		        		   The MIT License (MIT)
//	     		    Copyright (c) 2014 - 2026 Projeto OpenAC .Net
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
using System.Runtime.InteropServices;

namespace OpenAC.Net.Devices.Commom;

/// <summary>
/// Utilitário interno para detecção consistente do sistema operacional em todos os TargetFrameworks.
/// </summary>
internal static class OSPlatformHelper
{
    /// <summary>
    /// Indica se o sistema operacional atual é Windows.
    /// </summary>
    public static bool IsWindows { get; } = DetectWindows();

    /// <summary>
    /// Indica se o sistema operacional atual é macOS (Darwin/OSX).
    /// </summary>
    public static bool IsMacOS { get; } = DetectMacOS();

    /// <summary>
    /// Indica se o sistema operacional atual é Linux.
    /// </summary>
    public static bool IsLinux { get; } = !IsWindows && !IsMacOS &&
                                          Environment.OSVersion.Platform is PlatformID.Unix;

    private static bool DetectWindows()
    {
#if NET462
        return Environment.OSVersion.Platform is PlatformID.Win32NT or PlatformID.Win32Windows or PlatformID.Win32S;
#else
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
    }

    private static bool DetectMacOS()
    {
#if NET462
        if (Environment.OSVersion.Platform == PlatformID.MacOSX)
            return true;

        return Environment.OSVersion.Platform == PlatformID.Unix &&
               Directory.Exists("/System/Library/CoreServices");
#else
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#endif
    }
}
