// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
All rights reserved.

--*/

namespace MS.Internal.Printing.Configuration
{
    using System;
    using System.Printing;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System.Security;
    using System.Text;
    using Microsoft.Internal;
    using MS.Internal.ReachFramework;
    using MS.Win32;
    using MS.Internal.PrintWin32Thunk;
    
    using DllImport = MS.Internal.ReachFramework.DllImport;
    
    /// <summary>
    /// Internal proxy class that makes P/Invoke calls into the unmanaged stub provider prntvpt.dll.
    /// </summary>
    /// <remarks>all input parameters to NativeMethods functions are from trusted source</remarks>
    internal static class UnsafeNativeMethods
    {
        #region Public Methods

        /// <summary>
        /// Binds proxy to the specified printer device
        /// </summary>
        /// <param name="deviceName">printer device name</param>
        /// <param name="maxVersion">max schema version supported by client</param>
        /// <param name="prefVersion">schema version preferred by client</param>
        /// <param name="handle">device handle proxy is bound to</param>
        /// <param name="usedVersion">schema version proxy will use</param>
        /// <returns>HRESULT code</returns>
        /// <exception cref="PrintingNotSupportedException">
        /// Printing components are not installed on the client
        /// </exception>
        public static uint PTOpenProviderEx(
            string deviceName,
            int maxVersion,
            int prefVersion,
            out SafePTProviderHandle handle,
            out int usedVersion)
        {
            try {
                uint result = PTOpenProviderExImpl(deviceName, maxVersion, prefVersion, out handle, out usedVersion);
                if (result != 0)
                {
                    if (handle != null && !handle.IsInvalid)
                    {
                        handle.Dispose();
                        handle = null;
                    }
                }
                
                return result;
            }
            catch (DllNotFoundException e)
            {
                throw new PrintingNotSupportedException(String.Empty, e);
            }
        }

        /// <summary>
        /// Unbinds proxy from the printer device and release any associated unmanaged resources
        /// </summary>
        /// <param name="handle">device handle proxy has been bound to</param>
        /// <returns>HRESULT code</returns>
        [DllImport(DllImport.PrntvPt, EntryPoint = "PTCloseProvider", CharSet = CharSet.Unicode, ExactSpelling = true)]
        #pragma warning disable SYSLIB0004 // The Constrained Execution Region (CER) feature is not supported.  
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        #pragma warning restore SYSLIB0004 // The Constrained Execution Region (CER) feature is not supported.  
        public static extern uint PTCloseProviderImpl(IntPtr handle);

        /// <summary>
        /// Gets the PrintCapabilities relative to the given PrintTicket
        /// </summary>
        /// <param name="handle">device handle</param>
        /// <param name="printTicket">Stream that contains XML PrintTicket</param>
        /// <param name="printCapabilities">Stream that XML PrintCapabilities will be written to</param>
        /// <param name="errorMsg">error message if the operation failed</param>
        /// <returns>HRESULT code</returns>
        /// <exception cref="PrintingNotSupportedException">
        /// Printing components are not installed on the client
        /// </exception>
        public static uint PTGetPrintCapabilities(
            SafePTProviderHandle handle,
            IStream printTicket,
            IStream printCapabilities,
            out string errorMsg)
        {
            try
            {
                return PTGetPrintCapabilitiesImpl(handle, printTicket, printCapabilities, out errorMsg);
            }
            catch (DllNotFoundException e)
            {
                throw new PrintingNotSupportedException(String.Empty, e);
            }
        }


        /// <summary>
        /// Merges delta PrintTicket onto base PrintTicket and then validates the merged PrintTicket
        /// </summary>
        /// <param name="handle">device handle</param>
        /// <param name="baseTicket">Stream that contains base XML PrintTicket</param>
        /// <param name="deltaTicket">Stream that contains delta XML PrintTicket</param>
        /// <param name="scope">scope that delta PrintTicket and result PrintTicket will be limited to</param>
        /// <param name="resultTicket">Stream the validated XML PrintTicket it written to</param>
        /// <param name="errorMsg">error message if the operation failed</param>
        /// <returns>HRESULT code</returns>
        /// <exception cref="PrintingNotSupportedException">
        /// Printing components are not installed on the client
        /// </exception>
        public static uint PTMergeAndValidatePrintTicket(
            SafePTProviderHandle handle,
            IStream baseTicket,
            IStream deltaTicket,
            uint scope,
            IStream resultTicket,
            out string errorMsg)
        {
            try
            {
                return PTMergeAndValidatePrintTicketImpl(
                    handle,
                    baseTicket,
                    deltaTicket,
                    scope,
                    resultTicket,
                    out errorMsg
                );
            }
            catch (DllNotFoundException e)
            {
                throw new PrintingNotSupportedException(String.Empty, e);
            }
        }

        /// <summary>
        /// Converts the given Win32 DEVMODE into PrintTicket
        /// </summary>
        /// <param name="handle">device handle</param>
        /// <param name="devMode">buffer that contains the Win32 DEVMODE</param>
        /// <param name="dmSize">size of devMode buffer in bytes</param>
        /// <param name="scope">scope that the result PrintTicket will be limited to</param>
        /// <param name="printTicket">Stream that the converted XML PrintTicket will be written to</param>
        /// <returns>HRESULT code</returns>
        /// <exception cref="PrintingNotSupportedException">
        /// Printing components are not installed on the client
        /// </exception>
        public static uint PTConvertDevModeToPrintTicket(
            SafePTProviderHandle handle,
            uint dmSize,
            HandleRef devMode,
            uint scope,
            IStream printTicket)
        {
            try
            {
                return PTConvertDevModeToPrintTicketImpl(
                    handle,
                    dmSize,
                    devMode,
                    scope,
                    printTicket
                );
            }
            catch (DllNotFoundException e)
            {
                throw new PrintingNotSupportedException(String.Empty, e);
            }
        }

        /// <summary>
        /// Converts the given PrintTicket into Win32 DEVMODE
        /// </summary>
        /// <param name="handle">device handle</param>
        /// <param name="printTicket">Stream that contains the XML PrintTicket</param>
        /// <param name="baseType">type of default DEVMODE to use as base of conversion</param>
        /// <param name="scope">scope that the input PrintTicket will be limited to</param>
        /// <param name="devMode">buffer that contains the converted Win32 DEVMODE</param>
        /// <param name="dmSize">size of devMode buffer in bytes</param>
        /// <param name="errorMsg">error message if the operation failed</param>
        /// <returns>HRESULT code</returns>
        /// <exception cref="PrintingNotSupportedException">
        /// Printing components are not installed on the client
        /// </exception>
        public static uint PTConvertPrintTicketToDevMode(
            SafePTProviderHandle handle,
            IStream printTicket,
            uint baseType,
            uint scope,
            out uint dmSize,
            out IntPtr devMode,
            out string errorMsg)
        {
            try
            {
                return PTConvertPrintTicketToDevModeImpl(
                    handle,
                    printTicket,
                    baseType,
                    scope,
                    out dmSize,
                    out devMode,
                    out errorMsg
                );
            }
            catch (DllNotFoundException e)
            {
                throw new PrintingNotSupportedException(String.Empty, e);
            }
        }

        /// <summary>
        /// Releases buffers associated with print tickets and print capabilities
        /// </summary>
        /// <param name="pBuffer">A pointer to a buffer allocated during a call to a print ticket API.</param>
        /// <returns>If the operation succeeds, the return value is S_OK, otherwise the HRESULT contains an error code</returns>
        /// <exception cref="PrintingNotSupportedException">
        /// Printing components are not installed on the client
        /// </exception>
        public static uint PTReleaseMemory(HandleRef devMode)
        {
            try
            {
                return PTReleaseMemoryImpl(devMode);
            }
            catch (DllNotFoundException e)
            {
                throw new PrintingNotSupportedException(String.Empty, e);
            }
        }
    
        /// <remarks>
        /// It is very important that callers know whether they are dealing with the Ansi or Unicode version
        /// This affects the type of struct returned in the pDefault memeber (e.g it's DEVMODE may be DEVMODEA or DEVMODEW)
        /// To reduce potential for bugs we will only pinvoke to the unicode version
        /// </remarks>
        [DllImport(ExternDll.Winspool, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern bool OpenPrinterW(string pPrinterName, out SafeWinSpoolPrinterHandle printer, HandleRef pDefault);

        /// <remarks>
        /// It is very important that callers know whether they are dealing with the Ansi or Unicode version
        /// The CharSet affects the type of struct returned in the pDefault member (e.g it's DEVMODE may be DEVMODEA or DEVMODEW)
        /// To reduce potential for bugs we will only pinvoke to the unicode version
        /// </remarks>
        [DllImport(ExternDll.Winspool, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern bool GetPrinterW(SafeWinSpoolPrinterHandle printer, uint dwLevel, SafeMemoryHandle pPrinter, uint dwBuf, ref uint dwNeeded);

        /// <remarks>
        /// It is very important that callers know whether they are dealing with the Ansi or Unicode version
        /// This affects the type of data returned in the pDevMode and pOutput members (e.g pDevMode DEVMODE may be DEVMODEA or DEVMODEW)
        /// To reduce potential for bugs we will only pinvoke to the unicode version
        /// </remarks>
        [DllImport(ExternDll.Winspool, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern uint DeviceCapabilitiesW(string pDevice, string pPort, DeviceCapability fwCapabilities, SafeMemoryHandle pOutput, SafeMemoryHandle pDevMode);

        /// <remarks>
        /// It is very important that callers know whether they are dealing with the Ansi or Unicode version
        /// This affects the type of data returned in the devModeOutput member (e.g devModeOutput DEVMODE may be DEVMODEA or DEVMODEW)
        /// To reduce potential for bugs we will only pinvoke to the unicode version
        /// </remarks>
        [DllImport(ExternDll.Winspool, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern int DocumentPropertiesW(
            HandleRef hWnd, 
            SafeWinSpoolPrinterHandle printer,
            string deviceName,
            SafeMemoryHandle devModeOutput,
            SafeMemoryHandle devModeInput,
            DocumentPropertiesFlags mode);

        [DllImport(ExternDll.Winspool, ExactSpelling = true, SetLastError = true)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport(ExternDll.Gdi32, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateICW(string lpszDriver, string lpszDevice, string lpszOutput, SafeMemoryHandle devmodePtr);

        [DllImport(ExternDll.Gdi32, ExactSpelling = true, SetLastError = true)]
        public static extern int GetDeviceCaps(HandleRef hdc, DeviceCap capability);

        [DllImport(ExternDll.Gdi32, ExactSpelling = true, SetLastError = true)]
        public static extern bool DeleteDC(HandleRef hdc);

        [DllImport(ExternDll.User32, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern int LoadStringW(SafeModuleHandle hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern SafeModuleHandle LoadLibraryExW(string lpFileName, IntPtr hFile, LoadLibraryExFlags dwFlags);

        [DllImport(ExternDll.Kernel32, ExactSpelling = true, SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// Creates a COM stream over HGlobal allocated memory
        /// </summary>
        /// <param name="hGlobal">HGlobal to create stream over, IntPtr.Zero to auto allocate memory</param>
        /// <param name="fDeleteOnRelease">Delete the allocated memory when the stream is released</param>
        /// <param name="ppstm">Created Stream</param>
        /// <returns>HRESULT</returns>
        [DllImport(DllImport.Ole32, EntryPoint = "CreateStreamOnHGlobal", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern uint CreateStreamOnHGlobal(SafeMemoryHandle hGlobal, bool fDeleteOnRelease, out IStream ppstm);

        #endregion // Public Methods

        #region Private Methods

        /// <summary>
        /// Opens a handle to the specified printer device
        /// </summary>
        /// <param name="deviceName">printer device name</param>
        /// <param name="maxVersion">max schema version supported by client</param>
        /// <param name="prefVersion">schema version preferred by client</param>
        /// <param name="handle">device handle proxy is bound to</param>
        /// <param name="usedVersion">schema version proxy will use</param>
        /// <returns>HRESULT code</returns>
        [DllImport(DllImport.PrntvPt, EntryPoint = "PTOpenProviderEx", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint PTOpenProviderExImpl(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            int maxVersion,
            int prefVersion,
            out SafePTProviderHandle handle,
            out int usedVersion);

        /// <summary>
        /// Gets the PrintCapabilities relative to the given PrintTicket
        /// </summary>
        /// <param name="handle">device handle</param>
        /// <param name="printTicket">Stream that contains XML PrintTicket</param>
        /// <param name="printCapabilities">Stream that XML PrintCapabilities will be written to</param>
        /// <param name="errorMsg">error message if the operation failed</param>
        /// <returns>HRESULT code</returns>
        [DllImport(DllImport.PrntvPt, EntryPoint = "PTGetPrintCapabilities", CharSet = CharSet.Unicode)]
        private static extern uint PTGetPrintCapabilitiesImpl(
            SafePTProviderHandle handle,
            IStream printTicket,
            IStream printCapabilities,
            [MarshalAs(UnmanagedType.BStr)] out string errorMsg);

        /// <summary>
        /// Merges delta PrintTicket onto base PrintTicket and then validates the merged PrintTicket
        /// </summary>
        /// <param name="handle">device handle</param>
        /// <param name="baseTicket">Stream that contains base XML PrintTicket</param>
        /// <param name="deltaTicket">Stream that contains delta XML PrintTicket</param>
        /// <param name="scope">scope that delta PrintTicket and result PrintTicket will be limited to</param>
        /// <param name="resultTicket">Stream the validated XML PrintTicket it written to</param>
        /// <param name="errorMsg">error message if the operation failed</param>
        /// <returns>HRESULT code</returns>
        [DllImport(DllImport.PrntvPt, EntryPoint = "PTMergeAndValidatePrintTicket", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint PTMergeAndValidatePrintTicketImpl(
            SafePTProviderHandle handle,
            IStream baseTicket,
            IStream deltaTicket,
            uint scope,
            IStream resultTicket,
            [MarshalAs(UnmanagedType.BStr)] out string errorMsg);

        /// <summary>
        /// Converts the given Win32 DEVMODE into PrintTicket
        /// </summary>
        /// <param name="handle">device handle</param>
        /// <param name="devMode">buffer that contains the Win32 DEVMODE</param>
        /// <param name="dmSize">size of devMode buffer in bytes</param>
        /// <param name="scope">scope that the result PrintTicket will be limited to</param>
        /// <param name="printTicket">Stream that the converted XML PrintTicket will be written to</param>
        /// <returns>HRESULT code</returns>
        [DllImport(DllImport.PrntvPt, EntryPoint = "PTConvertDevModeToPrintTicket", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint PTConvertDevModeToPrintTicketImpl(
            SafePTProviderHandle handle,
            uint dmSize,
            HandleRef devMode,
            uint scope,
            IStream printTicket
            );

        /// <summary>
        /// Converts the given PrintTicket into Win32 DEVMODE
        /// </summary>
        /// <param name="handle">device handle</param>
        /// <param name="printTicket">Stream that contains the XML PrintTicket</param>
        /// <param name="baseType">type of default DEVMODE to use as base of conversion</param>
        /// <param name="scope">scope that the input PrintTicket will be limited to</param>
        /// <param name="devMode">buffer that contains the converted Win32 DEVMODE</param>
        /// <param name="dmSize">size of devMode buffer in bytes</param>
        /// <param name="errorMsg">error message if the operation failed</param>
        /// <returns>HRESULT code</returns>
        [DllImport(DllImport.PrntvPt, EntryPoint = "PTConvertPrintTicketToDevMode", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint PTConvertPrintTicketToDevModeImpl(
            SafePTProviderHandle handle,
            IStream printTicket,
            uint baseType,
            uint scope,
            out uint dmSize,
            out IntPtr devMode,
            [MarshalAs(UnmanagedType.BStr)] out string errorMsg);

        /// <summary>
        /// Releases buffers associated with print tickets and print capabilities
        /// </summary>
        /// <param name="pBuffer">A pointer to a buffer allocated during a call to a print ticket API.</param>
        /// <returns>If the operation succeeds, the return value is S_OK, otherwise the HRESULT contains an error code</returns>
        [DllImport(DllImport.PrntvPt, EntryPoint = "PTReleaseMemory", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint PTReleaseMemoryImpl(HandleRef devMode);

        #endregion // Private Methods
    }
}
