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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenAC.Net.Devices;

/// <summary>
/// Stream para envio de dados diretamente para uma impressora no modo RAW.
/// </summary>
public class RawPrinterStream : Stream
{
    #region InnerTypes

    /// <summary>
    /// Métodos e estruturas auxiliares para envio de dados RAW para impressoras no Windows.
    /// </summary>
    private static class Windows
    {
        #region InnerTypes
    
        /// <summary>
        /// Estrutura que contém informações sobre o documento a ser impresso.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class DOCINFOA
        {
            /// <summary>
            /// Nome do documento.
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)] public string? pDocName;
    
            /// <summary>
            /// Caminho do arquivo de saída (opcional).
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile;
    
            /// <summary>
            /// Tipo de dados enviados para a impressora.
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)] public string? pDataType;
        }
    
        #endregion InnerTypes
    
        #region Imports
    
        /// <summary>
        /// Abre uma conexão com a impressora especificada.
        /// </summary>
        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);
    
        /// <summary>
        /// Fecha a conexão com a impressora.
        /// </summary>
        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool ClosePrinter(IntPtr hPrinter);
    
        /// <summary>
        /// Inicia um novo trabalho de impressão.
        /// </summary>
        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In][MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);
    
        /// <summary>
        /// Finaliza o trabalho de impressão.
        /// </summary>
        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);
    
        /// <summary>
        /// Inicia uma nova página de impressão.
        /// </summary>
        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);
    
        /// <summary>
        /// Finaliza a página de impressão atual.
        /// </summary>
        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);
    
        /// <summary>
        /// Envia dados para a impressora.
        /// </summary>
        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);
    
        #endregion Imports
    
        #region Methods
    
        /// <summary>
        /// Envia um buffer de bytes diretamente para a impressora especificada.
        /// </summary>
        /// <param name="printerName">Nome da impressora.</param>
        /// <param name="buffer">Dados a serem enviados.</param>
        public static void SendToPrinter(string printerName, byte[] buffer)
        {
            // Abre a impressora.
            if (!OpenPrinter(printerName.Normalize(), out var hPrinter, IntPtr.Zero)) return;
    
            var di = new DOCINFOA
            {
                pDocName = "RAW Document",
                pOutputFile = null,
                pDataType = "RAW"
            };
    
            // Inicia o documento.
            if (StartDocPrinter(hPrinter, 1, di))
            {
                // Inicia a página.
                if (StartPagePrinter(hPrinter))
                {
                    var pUnmanagedBytes = Marshal.AllocCoTaskMem(buffer.Length);
    
                    try
                    {
                        Marshal.Copy(buffer, 0, pUnmanagedBytes, buffer.Length);
                        WritePrinter(hPrinter, pUnmanagedBytes, buffer.Length, out _);
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
        }
    
        #endregion Methods
    }

    private static class Unix
    {
        public static void SendToPrinter(string printerName, byte[] buffer)
        {
            var file = Path.GetTempFileName();
            File.WriteAllBytes(file, buffer);

            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = "lp",
                Arguments = $"-d \"{printerName}\" {file}"
            };

            var process = Process.Start(startInfo);
            process?.WaitForExit();
            File.Delete(file);
        }
    }

    #endregion InnerTypes

    #region Fields

    private MemoryStream? stream;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="RawPrinterStream"/>
    /// </summary>
    /// <param name="printerName">O nome da impressora para onde será enviado os dados.</param>
    public RawPrinterStream(string printerName)
    {
        PrinterName = printerName;
        stream = new MemoryStream();
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Obtém o nome da impressora para onde os dados serão enviados.
    /// </summary>
    public string PrinterName { get; }

    /// <inheritdoc />
    public override bool CanRead => false;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override long Length => stream?.Length ?? 0;

    /// <inheritdoc />
    public override long Position
    {
        get => stream?.Position ?? 0;
        set
        {
            if (stream == null) return;
            
            stream.Position = value;
        }
    }

    #endregion Properties

    #region Methods

    /// <inheritdoc />
    public override void Flush()
    {
        try
        {
            var buffer = stream?.ToArray() ?? [];

            if (Environment.OSVersion.Platform == PlatformID.Unix)
                Unix.SendToPrinter(PrinterName, buffer);
            else
                Windows.SendToPrinter(PrinterName, buffer);

            stream?.Clear();
        }
        catch (Exception e)
        {
            throw new IOException("Erro ao enviar dados a Impressora", e);
        }
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotImplementedException();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => stream?.Write(buffer, offset, count);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        Flush();
        stream?.Dispose();
        stream = null;
    }

    #endregion Methods
}