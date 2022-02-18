// ***********************************************************************
// Assembly         : OpenAC.Net.Devices
// Author           : RFTD
// Created          : 20-12-2018
//
// Last Modified By : RFTD
// Last Modified On : 20-12-2018
// ***********************************************************************
// <copyright file="OpenUSBStream.cs" company="OpenAC .Net">
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
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace OpenAC.Net.Devices
{
    internal sealed class OpenUSBStream : OpenDeviceStream
    {
        #region InnerTypes

        private sealed class SetupApi
        {
            #region Structs

            [StructLayout(LayoutKind.Sequential)]
            public struct SP_DEVINFO_DATA
            {
                public uint cbSize;
                public Guid ClassGuid;
                public uint DevInst;
                public IntPtr Reserved;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct SP_DEVICE_INTERFACE_DATA
            {
                public int cbSize;
                public Guid interfaceClassGuid;
                public int flags;
                private UIntPtr reserved;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public struct SP_DEVICE_INTERFACE_DETAIL_DATA
            {
                public int cbSize;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
                public string DevicePath;
            }

            public enum SetupDiGetDeviceRegistryPropertyEnum : uint
            {
                SPDRP_DEVICEDESC = 0x00000000, // DeviceDesc (R/W)
                SPDRP_HARDWAREID = 0x00000001, // HardwareID (R/W)
                SPDRP_COMPATIBLEIDS = 0x00000002, // CompatibleIDs (R/W)
                SPDRP_UNUSED0 = 0x00000003, // unused
                SPDRP_SERVICE = 0x00000004, // Service (R/W)
                SPDRP_UNUSED1 = 0x00000005, // unused
                SPDRP_UNUSED2 = 0x00000006, // unused
                SPDRP_CLASS = 0x00000007, // Class (R--tied to ClassGUID)
                SPDRP_CLASSGUID = 0x00000008, // ClassGUID (R/W)
                SPDRP_DRIVER = 0x00000009, // Driver (R/W)
                SPDRP_CONFIGFLAGS = 0x0000000A, // ConfigFlags (R/W)
                SPDRP_MFG = 0x0000000B, // Mfg (R/W)
                SPDRP_FRIENDLYNAME = 0x0000000C, // FriendlyName (R/W)
                SPDRP_LOCATION_INFORMATION = 0x0000000D, // LocationInformation (R/W)
                SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E, // PhysicalDeviceObjectName (R)
                SPDRP_CAPABILITIES = 0x0000000F, // Capabilities (R)
                SPDRP_UI_NUMBER = 0x00000010, // UiNumber (R)
                SPDRP_UPPERFILTERS = 0x00000011, // UpperFilters (R/W)
                SPDRP_LOWERFILTERS = 0x00000012, // LowerFilters (R/W)
                SPDRP_BUSTYPEGUID = 0x00000013, // BusTypeGUID (R)
                SPDRP_LEGACYBUSTYPE = 0x00000014, // LegacyBusType (R)
                SPDRP_BUSNUMBER = 0x00000015, // BusNumber (R)
                SPDRP_ENUMERATOR_NAME = 0x00000016, // Enumerator Name (R)
                SPDRP_SECURITY = 0x00000017, // Security (R/W, binary form)
                SPDRP_SECURITY_SDS = 0x00000018, // Security (W, SDS form)
                SPDRP_DEVTYPE = 0x00000019, // Device Type (R/W)
                SPDRP_EXCLUSIVE = 0x0000001A, // Device is exclusive-access (R/W)
                SPDRP_CHARACTERISTICS = 0x0000001B, // Device Characteristics (R/W)
                SPDRP_ADDRESS = 0x0000001C, // Device Address (R)
                SPDRP_UI_NUMBER_DESC_FORMAT = 0X0000001D, // UiNumberDescFormat (R/W)
                SPDRP_DEVICE_POWER_DATA = 0x0000001E, // Device Power Data (R)
                SPDRP_REMOVAL_POLICY = 0x0000001F, // Removal Policy (R)
                SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x00000020, // Hardware Removal Policy (R)
                SPDRP_REMOVAL_POLICY_OVERRIDE = 0x00000021, // Removal Policy Override (RW)
                SPDRP_INSTALL_STATE = 0x00000022, // Device Install State (R)
                SPDRP_LOCATION_PATHS = 0x00000023, // Device Location Paths (R)
                SPDRP_BASE_CONTAINERID = 0x00000024  // Base ContainerID (R)
            }

            #endregion Structs

            #region Methods

            [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

            [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr SetupDiGetClassDevs(ref Guid gClass, [MarshalAs(UnmanagedType.LPStr)] string strEnumerator, IntPtr hParent, uint nFlags);

            [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool SetupDiGetDeviceRegistryProperty(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint property,
                out uint propertyRegDataType, byte[] propertyBuffer, uint propertyBufferSize, out uint requiredSize);

            [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, ref SP_DEVINFO_DATA devInfo, ref Guid interfaceClassGuid,
                uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

            [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
                ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, ref uint requiredSize,
                ref SP_DEVINFO_DATA deviceInfoData);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

            #endregion Methods
        }

        private sealed class Kernel32
        {
            #region Consts

            public const uint INFINITE = 0xFFFFFFFF;
            public const uint WAIT_ABANDONED = 0x00000080;
            public const uint WAIT_OBJECT_0 = 0x00000000;
            public const uint WAIT_TIMEOUT = 0x00000102;

            public const uint PURGE_TXABORT = 0x0001;  // Kill the pending/current writes to the comm port.
            public const uint PURGE_RXABORT = 0x0002;  // Kill the pending/current reads to the comm port.
            public const uint PURGE_TXCLEAR = 0x0004;  // Kill the transmit queue if there.
            public const uint PURGE_RXCLEAR = 0x0008;  // Kill the typeahead buffer if there.

            public const uint ERROR_SUCCESS = 0x0;
            public const uint ERROR_IO_PENDING = 997;

            public static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            [Flags]
            public enum EFileAccess : uint
            {
                //
                // Standart Section
                //

                AccessSystemSecurity = 0x1000000,   // AccessSystemAcl access type
                MaximumAllowed = 0x2000000,     // MaximumAllowed access type

                Delete = 0x10000,
                ReadControl = 0x20000,
                WriteDAC = 0x40000,
                WriteOwner = 0x80000,
                Synchronize = 0x100000,

                StandardRightsRequired = 0xF0000,
                StandardRightsRead = ReadControl,
                StandardRightsWrite = ReadControl,
                StandardRightsExecute = ReadControl,
                StandardRightsAll = 0x1F0000,
                SpecificRightsAll = 0xFFFF,

                FILE_READ_DATA = 0x0001,        // file & pipe
                FILE_LIST_DIRECTORY = 0x0001,       // directory
                FILE_WRITE_DATA = 0x0002,       // file & pipe
                FILE_ADD_FILE = 0x0002,         // directory
                FILE_APPEND_DATA = 0x0004,      // file
                FILE_ADD_SUBDIRECTORY = 0x0004,     // directory
                FILE_CREATE_PIPE_INSTANCE = 0x0004, // named pipe
                FILE_READ_EA = 0x0008,          // file & directory
                FILE_WRITE_EA = 0x0010,         // file & directory
                FILE_EXECUTE = 0x0020,          // file
                FILE_TRAVERSE = 0x0020,         // directory
                FILE_DELETE_CHILD = 0x0040,     // directory
                FILE_READ_ATTRIBUTES = 0x0080,      // all
                FILE_WRITE_ATTRIBUTES = 0x0100,     // all

                //
                // Generic Section
                //

                GenericRead = 0x80000000,
                GenericWrite = 0x40000000,
                GenericExecute = 0x20000000,
                GenericAll = 0x10000000,

                SPECIFIC_RIGHTS_ALL = 0x00FFFF,

                FILE_ALL_ACCESS =
                StandardRightsRequired |
                Synchronize |
                0x1FF,

                FILE_GENERIC_READ =
                StandardRightsRead |
                FILE_READ_DATA |
                FILE_READ_ATTRIBUTES |
                FILE_READ_EA |
                Synchronize,

                FILE_GENERIC_WRITE =
                StandardRightsWrite |
                FILE_WRITE_DATA |
                FILE_WRITE_ATTRIBUTES |
                FILE_WRITE_EA |
                FILE_APPEND_DATA |
                Synchronize,

                FILE_GENERIC_EXECUTE =
                StandardRightsExecute |
                  FILE_READ_ATTRIBUTES |
                  FILE_EXECUTE |
                  Synchronize
            }

            [Flags]
            public enum EFileShare : uint
            {
                None = 0x00000000,
                Read = 0x00000001,
                Write = 0x00000002,
                Delete = 0x00000004
            }

            public enum ECreationDisposition : uint
            {
                New = 1,
                CreateAlways = 2,
                OpenExisting = 3,
                OpenAlways = 4,
                TruncateExisting = 5
            }

            [Flags]
            public enum EFileAttributes : uint
            {
                Readonly = 0x00000001,
                Hidden = 0x00000002,
                System = 0x00000004,
                Directory = 0x00000010,
                Archive = 0x00000020,
                Device = 0x00000040,
                Normal = 0x00000080,
                Temporary = 0x00000100,
                SparseFile = 0x00000200,
                ReparsePoint = 0x00000400,
                Compressed = 0x00000800,
                Offline = 0x00001000,
                NotContentIndexed = 0x00002000,
                Encrypted = 0x00004000,
                Write_Through = 0x80000000,
                Overlapped = 0x40000000,
                NoBuffering = 0x20000000,
                RandomAccess = 0x10000000,
                SequentialScan = 0x08000000,
                DeleteOnClose = 0x04000000,
                BackupSemantics = 0x02000000,
                PosixSemantics = 0x01000000,
                OpenReparsePoint = 0x00200000,
                OpenNoRecall = 0x00100000,
                FirstPipeInstance = 0x00080000
            }

            #endregion Consts

            #region Methods

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern IntPtr CreateFile(string lpFileName, EFileAccess dwDesiredAccess, EFileShare dwShareMode, IntPtr lpSecurityAttributes,
                ECreationDisposition dwCreationDisposition, EFileAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool SetupComm(IntPtr hFile, uint dwInQueue, uint dwOutQueue);

            [DllImport("kernel32.dll", SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Ansi, SetLastError = true)]
            public static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten,
                [In] ref NativeOverlapped lpOverlapped);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

            [DllImport("kernel32.dll")]
            public static extern bool PurgeComm(IntPtr hFile, uint dwFlags);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GetOverlappedResult(IntPtr hFile, [In] ref NativeOverlapped lpOverlapped,
                out uint lpNumberOfBytesTransferred, bool bWait);

            #endregion Methods
        }

        #endregion InnerTypes

        #region Fields

        private static readonly Guid GUID_DEVCLASS_NET = new Guid("{4D36E972-E325-11CE-BFC1-08002BE10318}");
        private static readonly Guid GUID_DEVCLASS_PORT = new Guid("{4D36E978-E325-11CE-BFC1-08002BE10318}");
        private static readonly Guid GUID_DEVCLASS_PRINTER = new Guid("{4D36E979-E325-11CE-BFC1-08002BE10318}");
        private static readonly Guid GUID_DEVCLASS_PNPPRINTERS = new Guid("{4658EE7E-F050-11D1-B6BD-00C04FA372A7}");
        private static readonly Guid GUID_DEVCLASS_NETCLIENT = new Guid("{4D36E973-E325-11CE-BFC1-08002BE10318}");
        private static readonly Guid GUID_DEVINTERFACE_USBPRINT = new Guid("{28D78FAD-5A12-11D1-AE5B-0000F803A8C2}");
        private static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("{A5DCBF10-6530-11D2-901F-00C04FB951ED}");

        public const string CSecVendors = "Vendors";
        public const int CReceiveBufferSize = 1024;

        public const string sErrWinUSBInvalidID = "{0} inválido [{1}]";
        public const string sErrWinUSBDeviceOutOfRange = "Dispositivo USB num: {0} não existe";
        public const string sErrWinUSBOpening = "Erro {0} ao abrir o Porta USB {1}";
        public const string sErrWinUSBClosing = "Erro {0} ao fechar a Porta USB {1}";
        public const string sErrWinUSBDescriptionNotFound = "Erro, dispositivo [{0}] não encontrado";
        public const string sErrWinUSBNotDeviceFound = "Nenhum dispositivo USB encontrado";
        public const string sErrWinUSBDeviceIsClosed = "Dispositivo USB não está aberto";
        public const string sErrWinUSBSendData = "Erro {0}, ao enviar {1} bytes para USB";
        public const string sErrWinUSBReadData = "Erro {0}, ao ler da USB";

        public const string sDescDevPosPrinter = "Impressora";
        public const string sDescDevLabelPrinter = "Etiquetadora";
        public const string sDescDevFiscal = "SAT";

        #endregion Fields

        #region Constructors

        public OpenUSBStream(SerialConfig config) : base(config)
        {
        }

        #endregion Constructors

        #region Properties

        protected override int Available => 0;

        #endregion Properties

        #region Methods

        protected override bool OpenInternal() => throw new NotImplementedException();

        protected override bool CloseInternal() => throw new NotImplementedException();

        #endregion Methods
    }
}