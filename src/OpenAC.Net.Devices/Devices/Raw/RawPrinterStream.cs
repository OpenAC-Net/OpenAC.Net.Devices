// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="RawPrinterStream.cs" company="OpenAC .Net">
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
using System.IO;
using System.Runtime.InteropServices;

namespace OpenAC.Net.Devices
{
    public class RawPrinterStream : Stream
    {
        #region InnerTypes

        public sealed class Windows
        {
            #region InnerTypes

            // Structure and API declarions:
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public class DOCINFOA
            {
                [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
                [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
                [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
            }

            #endregion InnerTypes

            #region Imports

            [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

            [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            private static extern bool ClosePrinter(IntPtr hPrinter);

            [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In][MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

            [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            private static extern bool EndDocPrinter(IntPtr hPrinter);

            [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            private static extern bool StartPagePrinter(IntPtr hPrinter);

            [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            private static extern bool EndPagePrinter(IntPtr hPrinter);

            [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

            #endregion Imports

            #region Methods

            public static bool SendToPrinter(string printerName, byte[] buffer, int offset, int count)
            {
                // Open the printer.
                if (!OpenPrinter(printerName.Normalize(), out var hPrinter, IntPtr.Zero)) return false;

                var bSuccess = false;
                var di = new DOCINFOA
                {
                    pDocName = "RAW Document",
                    pDataType = "RAW"
                };

                // Start a document.
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    // Start a page.
                    if (StartPagePrinter(hPrinter))
                    {
                        var pUnmanagedBytes = Marshal.AllocCoTaskMem(count);

                        try
                        {
                            Marshal.Copy(buffer, offset, pUnmanagedBytes, count);
                            bSuccess = WritePrinter(hPrinter, pUnmanagedBytes, count, out _);
                            EndPagePrinter(hPrinter);
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pUnmanagedBytes);
                        }
                    }

                    EndDocPrinter(hPrinter);
                }

                ClosePrinter(hPrinter);

                return bSuccess;
            }

            #endregion Methods
        }

        public sealed class Unix
        {
            public static bool SendToPrinter(string printerName, byte[] buffer, int offset, int count) => throw new NotImplementedException("Plataforma não suportada.");
        }

        #endregion InnerTypes

        #region Fields

        private MemoryStream stream;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Inicializa uma nova instancia da classe <see cref="RawPrinterStream"/>
        /// </summary>
        /// <param name="printerName">O nome da impressora para onde será enviado os dados.</param>
        public RawPrinterStream(string printerName)
        {
            PrinterName = printerName;
            stream = new MemoryStream();
        }

        #endregion Constructors

        #region Properties

        public string PrinterName { get; }

        public override bool CanRead { get; } = false;

        public override bool CanSeek { get; } = false;

        public override bool CanWrite { get; } = true;

        public override long Length { get; }

        public override long Position { get; set; }

        #endregion Properties

        #region Methods

        public override void Flush()
        {
            var buffer = stream.ToArray();

            if (Environment.OSVersion.Platform == PlatformID.Unix)
                Unix.SendToPrinter(PrinterName, buffer, 0, buffer.Length);
            else
                Windows.SendToPrinter(PrinterName, buffer, 0, buffer.Length);

            stream.Clear();
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value) => throw new NotImplementedException();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) => stream.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            Flush();
            stream.Dispose();
            stream = null;
        }

        #endregion Methods
    }
}