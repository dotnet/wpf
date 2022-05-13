// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition and implementation of the internal managed PTProvider and NativeMethods classes.
    These classes hide from PrintTicketManager and PrintTicketConverter the fact and complexity
    of thunking into unmanaged component to get PrintTicket and PrintCapabilities provider services.



--*/

using System;
using System.IO;
using System.Security;
using System.Globalization;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

using System.Printing.Interop;
using System.Printing;
using Microsoft.Internal;

using System.Windows.Xps;
using System.Windows.Xps.Serialization;
using MS.Utility;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using MS.Internal.PrintWin32Thunk; 

namespace MS.Internal.Printing.Configuration
{
    #region SafePTProviderHandle class

    // Subclass Whidbey SafeHandle to wrap unmanaged handle.
    // SafeHandle derives from CriticalFinalizerObject, which makes the finalizer critical and
    // CLR runtime treats CriticalFinalizerObject subclasses specially during interop marshaling
    // and finalization. SafeHandle supports reference counting to prevent handle recycling issue
    // (CriticalHandle doesn't).
    internal sealed class SafePTProviderHandle : System.Runtime.InteropServices.SafeHandle
    {
        // Called by P/Invoke when returning SafeHandle. It's private in order to prevent handle
        // creation by the constructor.
        private SafePTProviderHandle() : base(IntPtr.Zero, true)
        {
        }

        // We should define other constructors if and only if we need to support user-supplied handles.
        // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle() for you.
        public override bool IsInvalid
        {
            get
            {
                // Need to check IsClosed first to determine if the handle is closed. This is because
                // SetHandleAsInvalid implementation on base class just marks handle as closed to abstract
                // any values that invalid handles could have. If handle is not closed, then we need to
                // compare against real invalid values (in case handle was created from the interop call).
                return (IsClosed || (handle == IntPtr.Zero));
            }
        }

        // Implementation of SafeHandle's this abstract method specifies how to free the handle.
        // The boolean returned should be true for success and false if the runtime
        // should fire a SafeHandleCriticalFailure MDA (CustomerDebugProbe) if that
        // MDA is enabled.
        protected override bool ReleaseHandle()
        {
            return PTUtility.IsSuccessCode(UnsafeNativeMethods.PTCloseProviderImpl(this.handle));
        }
    }

    #endregion SafePTProviderHandle class


    #region Internal Types

    /// <summary>
    /// List of error codes that could be returned by unmanaged component.
    /// </summary>
    /// <remarks>values must match the definition in GDIPrintTicket.h</remarks>
    internal enum NativeErrorCode : uint
    {
        /// <summary>
        /// no conflicts resolving needed during PrintTicket validation
        /// </summary>
        S_PT_NO_CONFLICT = 0x00040001,

        /// <summary>
        /// some conflicts were resolved during PrintTicket validation
        /// </summary>
        S_PT_CONFLICT_RESOLVED = 0x00040002,

        /// <summary>
        /// validation failed because of a Schema violation
        /// </summary>
        E_XML_INVALID = 0xC00CE225,

        /// <summary>
        /// client input PrintTicket is not well-formed
        /// </summary>
        /// <remarks>Well-formed PrintTicket means its XML structure complies
        /// with what Print Schema framework defines. Well-formed PrintTicket
        /// is not necessary valid since it can still contain conflict settings.
        /// </remarks>
        E_PRINTTICKET_FORMAT = 0x80040003,

        /// <summary>
        /// client input delta PrintTicket is not well-formed
        /// </summary>
        /// <remarks>Well-formed PrintTicket means its XML structure complies
        /// with what Print Schema framework defines. Well-formed PrintTicket
        /// is not necessary valid since it can still contain conflict settings.
        /// </remarks>
        E_DELTA_PRINTTICKET_FORMAT = 0x80040005,

        /// <summary>
        /// Not implemented error
        /// </summary>
        E_NOTIMPL = 0x80004001
    }

    #endregion Internal Types

    #region PTProvider class

    // current code in following PTProvider class is only using try...finally... to
    // release unmanaged memory. This needs to be changed to use Whidbey's new CriticalFinalizer
    // support when its M3 drop is available

    /// <summary>
    /// Managed PrintTicket provider class that inter-ops with unmanaged DDI driver
    /// </summary>
    internal class PTProvider : PTProviderBase
    {
        #region Constructors

        /// <summary>
        /// Constructs a new PrintTicket provider instance for the given device.
        /// </summary>
        /// <param name="deviceName">name of printer device the provider should be bound to</param>
        /// <param name="maxVersion">max schema version supported by client</param>
        /// <param name="clientVersion">schema version requested by client</param>
        /// <exception cref="PrintQueueException">
        /// The PTProvider instance failed to bind to the specified printer.
        /// </exception>
        /// <exception cref="PrintingNotSupportedException">
        /// Printing components are not installed on the client
        /// </exception>
        public PTProvider(string deviceName, int maxVersion, int clientVersion)
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXPTProviderStart);

            // We are not doing late binding to the device here because we should
            // indicate right away if there was an error in binding the provider
            // to the device.  Doing late binding would mean that any instance
            // method could throw a no such printer exception.
            uint hResult = UnsafeNativeMethods.PTOpenProviderEx(deviceName,
                                                             maxVersion,
                                                             clientVersion,
                                                             out _providerHandle,
                                                             out _schemaVersion);

            if (!PTUtility.IsSuccessCode(hResult))
            {
                throw new PrintQueueException((int)hResult, "PrintConfig.Provider.BindFail", deviceName);
            }

#if _DEBUG
            if (_schemaVersion != clientVersion)
            {
                // PTOpenProviderEx() shouldn't succeed if it can't support the requested version.
                throw new InvalidOperationException("_DEBUG: Client requested Print Schema version " +
                                                    clientVersion.ToString(CultureInfo.CurrentCulture) +
                                                    " doesn't match to provider Print Schema version " +
                                                    _schemaVersion.ToString(CultureInfo.CurrentCulture));
            }
#endif

            // If succeeded, PTOpenProviderEx() function should ensure that a valid _providerHandle
            // is returned and the returned schemaVersion is within valid range (i.e. no greater than maxVersion)
            this._deviceName = deviceName;

            this._thread = Thread.CurrentThread;
            
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXPTProviderEnd);
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Gets the PrintCapabilities relative to the given PrintTicket.
        /// </summary>
        /// <param name="printTicket">The stream that contains XML PrintTicket based on which PrintCapabilities should be built.</param>
        /// <returns>Stream that contains XML PrintCapabilities.</returns>
        /// <exception cref="ObjectDisposedException">
        /// The PTProvider instance has already been disposed.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The input PrintTicket specified by <paramref name="printTicket"/> is not well-formed.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PTProvider instance failed to retrieve the PrintCapabilities.
        /// </exception>
        /// <exception cref="PrintingNotSupportedException">
        /// Printing components are not installed on the client
        /// </exception>
        public override MemoryStream GetPrintCapabilities(MemoryStream printTicket)
        {
            VerifyAccess();

            IStream printCapabilitiesStream = CreateStreamOnHGlobal();
            try
            {
                IStream printTicketStream = IStreamFromMemoryStream(printTicket);
                try
                {
                    string errorMsg;
                    // What happens if the native code returns a iStreamLength that is too long?
                    // One option is we trust providers and don't do run-time check.
                    uint hResult = UnsafeNativeMethods.PTGetPrintCapabilities(_providerHandle, printTicketStream, printCapabilitiesStream, out errorMsg);
                    if (PTUtility.IsSuccessCode(hResult))
                    {
                        RewindIStream(printCapabilitiesStream);
                        return MemoryStreamFromIStream(printCapabilitiesStream);
                    }

                    if (hResult == (uint)NativeErrorCode.E_PRINTTICKET_FORMAT)
                    {
                        throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                                      "{0} {1} {2}",
                                      PrintSchemaTags.Framework.PrintTicketRoot,
                                      PTUtility.GetTextFromResource("FormatException.XMLNotWellFormed"),
                                      errorMsg),
                                      "printTicket");
                    }
                    else
                    {
                        throw new PrintQueueException((int)hResult,
                                                      "PrintConfig.Provider.GetPrintCapFail",
                                                      _deviceName,
                                                      errorMsg);
                    }
                }
                finally
                {
                    DeleteIStream(ref printTicketStream);
                }
            }
            finally
            {
                DeleteIStream(ref printCapabilitiesStream);
            }
        }

        /// <summary>
        /// Merges delta PrintTicket with base PrintTicket and then validates the merged PrintTicket.
        /// </summary>
        /// <param name="basePrintTicket">The MemoryStream that contains base XML PrintTicket.</param>
        /// <param name="deltaPrintTicket">The MemoryStream that contains delta XML PrintTicket.</param>
        /// <param name="scope">scope that delta PrintTicket and result PrintTicket will be limited to</param>
        /// <param name="conflictStatus">The returned conflict resolving status.</param>
        /// <returns>MemoryStream that contains validated and merged PrintTicket XML.</returns>
        /// <exception cref="ObjectDisposedException">
        /// The PTProvider instance has already been disposed.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The base PrintTicket specified by <paramref name="basePrintTicket"/> is not well-formed,
        /// or delta PrintTicket specified by <paramref name="deltaPrintTicket"/> is not well-formed.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PTProvider instance failed to merge and validate the input PrintTicket(s).
        /// </exception>
        /// <exception cref="PrintingNotSupportedException">
        /// Printing components are not installed on the client
        /// </exception>
        public override MemoryStream MergeAndValidatePrintTicket(MemoryStream basePrintTicket,
                                                        MemoryStream deltaPrintTicket,
                                                        PrintTicketScope scope,
                                                        out ConflictStatus conflictStatus)
        {
            VerifyAccess();

            conflictStatus = ConflictStatus.NoConflict;

            IStream validatedPrintTicketStream = CreateStreamOnHGlobal();
            try
            {
                IStream baseTicketStream = IStreamFromMemoryStream(basePrintTicket);
                try
                {
                    IStream deltaTicketStream = IStreamFromMemoryStream(deltaPrintTicket);
                    try
                    {

                        string errorMsg;
                        uint hResult = UnsafeNativeMethods.PTMergeAndValidatePrintTicket(_providerHandle,
                                                                                   baseTicketStream,
                                                                                   deltaTicketStream,
                                                                                   (uint)scope,
                                                                                   validatedPrintTicketStream,
                                                                                   out errorMsg);

                        if (PTUtility.IsSuccessCode(hResult))
                        {
                            // convert the success hResult to an enum value
                            switch (hResult)
                            {
                                case (uint)NativeErrorCode.S_PT_CONFLICT_RESOLVED:
                                {
                                    conflictStatus = ConflictStatus.ConflictResolved;
                                    break;
                                }
                                case (uint)NativeErrorCode.S_PT_NO_CONFLICT:
                                {
                                    conflictStatus = ConflictStatus.NoConflict;
                                    break;
                                }
                                default:
                                {
                                    throw new PrintQueueException((int)hResult,
                                                                  "PrintConfig.Provider.MergeValidateFail",
                                                                  _deviceName);
                                }
                            }

                            RewindIStream(validatedPrintTicketStream);
                            
                            return MemoryStreamFromIStream(validatedPrintTicketStream);
                        }

                        if ((hResult == (uint)NativeErrorCode.E_PRINTTICKET_FORMAT) ||
                             (hResult == (uint)NativeErrorCode.E_DELTA_PRINTTICKET_FORMAT))
                        {
                            throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                                          "{0} {1} {2}",
                                          PrintSchemaTags.Framework.PrintTicketRoot,
                                          PTUtility.GetTextFromResource("FormatException.XMLNotWellFormed"),
                                          errorMsg),
                                          (hResult == (uint)NativeErrorCode.E_PRINTTICKET_FORMAT) ? "basePrintTicket" : "deltaPrintTicket");
                        }
                        else
                        {
                            throw new PrintQueueException((int)hResult,
                                                          "PrintConfig.Provider.MergeValidateFail",
                                                          _deviceName,
                                                          errorMsg);
                        }
                    }
                    finally
                    {
                        DeleteIStream(ref deltaTicketStream);
                    }
                }
                finally
                {
                    DeleteIStream(ref baseTicketStream);
                }
            }
            finally
            {
                DeleteIStream(ref validatedPrintTicketStream);
            }
        }

        /// <summary>
        /// Converts the given Win32 DEVMODE into PrintTicket.
        /// </summary>
        /// <param name="devMode">Byte buffer containing the Win32 DEVMODE.</param>
        /// <param name="scope">scope that the result PrintTicket will be limited to</param>
        /// <returns>MemoryStream that contains the converted XML PrintTicket.</returns>
        /// <exception cref="ObjectDisposedException">
        /// The PTProvider instance has already been disposed.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The DEVMODE specified by <paramref name="devMode"/> is not well-formed.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PTProvider instance failed to convert the DEVMODE to a PrintTicket.
        /// </exception>
        /// <exception cref="PrintingNotSupportedException">
        /// Printing components are not installed on the client
        /// </exception>
        public override MemoryStream ConvertDevModeToPrintTicket(byte[] devMode,
                                                        PrintTicketScope scope)
        {
            VerifyAccess();

            IntPtr umDevMode = Marshal.AllocCoTaskMem(devMode.Length);
            try
            {
                // Allocate unmanaged buffer and copy devMode byte array into the unmanaged buffer
                Marshal.Copy(devMode, 0, umDevMode, devMode.Length);

                IStream printTicketStream = CreateStreamOnHGlobal();
                try
                {
                    uint hResult = UnsafeNativeMethods.PTConvertDevModeToPrintTicket(_providerHandle,
                                                                             (uint)devMode.Length,
                                                                             new HandleRef(this, umDevMode),
                                                                             (uint)scope,
                                                                             printTicketStream);

                    if (PTUtility.IsSuccessCode(hResult))
                    {
                        RewindIStream(printTicketStream);
                        return MemoryStreamFromIStream(printTicketStream);
                    }

                    throw new PrintQueueException((int)hResult,
                                                  "PrintConfig.Provider.DevMode2PTFail",
                                                  _deviceName);
                }
                finally
                {
                    DeleteIStream(ref printTicketStream);
                }
            }
            finally
            {
                if (umDevMode != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(umDevMode);
                    umDevMode = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Converts the given PrintTicket into Win32 DEVMODE.
        /// </summary>
        /// <param name="printTicket">MemoryStream containing the XML PrintTicket.</param>
        /// <param name="baseType">Type of default DEVMODE to use as base of conversion.</param>
        /// <param name="scope">scope that the input PrintTicket will be limited to</param>
        /// <returns>Byte buffer that contains the converted Win32 DEVMODE.</returns>
        /// <exception cref="ObjectDisposedException">
        /// The PTProvider instance has already been disposed.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The PrintTicket specified by <paramref name="printTicket"/> is not well-formed.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PTProvider instance failed to convert the PrintTicket to a DEVMODE.
        /// </exception>
        /// <exception cref="PrintingNotSupportedException">
        /// Printing components are not installed on the client
        /// </exception>
        public override byte[] ConvertPrintTicketToDevMode(MemoryStream printTicket,
                                                  BaseDevModeType baseType,
                                                  PrintTicketScope scope)
        {
            VerifyAccess();

            uint hResult = 0;
            string errorMsg = null;
            
            IntPtr umDevMode = IntPtr.Zero;
            try
            {
                IStream printTicketStream = IStreamFromMemoryStream(printTicket);
                try
                {
                    uint umDevModeLen = 0;
                    hResult = UnsafeNativeMethods.PTConvertPrintTicketToDevMode(_providerHandle,
                                                                                   printTicketStream,
                                                                                   (uint)baseType,
                                                                                   (uint)scope,
                                                                                   out umDevModeLen,
                                                                                   out umDevMode,
                                                                                   out errorMsg);

                    if (PTUtility.IsSuccessCode(hResult))
                    {
                        byte[] devMode = new byte[umDevModeLen];
                        Marshal.Copy(umDevMode, devMode, 0, (int)umDevModeLen);
                        return devMode;
                    }
                }
                finally
                {
                    DeleteIStream(ref printTicketStream);
                }
            }
            finally
            {
                if (umDevMode != IntPtr.Zero)
                {
                    UnsafeNativeMethods.PTReleaseMemory(new HandleRef(this, umDevMode));
                    umDevMode = IntPtr.Zero;
                }
            }
            
            if ((hResult == (uint)NativeErrorCode.E_XML_INVALID) ||
                (hResult == (uint)NativeErrorCode.E_PRINTTICKET_FORMAT))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                              "{0} {1} {2}",
                              PrintSchemaTags.Framework.PrintTicketRoot,
                              PTUtility.GetTextFromResource("FormatException.XMLNotWellFormed"),
                              errorMsg),
                              "printTicket");
            }

            throw new PrintQueueException((int)hResult,
                                              "PrintConfig.Provider.PT2DevModeFail",
                                              _deviceName,
                                              errorMsg);
        }

        public override void Release()
        {
            if (_providerHandle != null)
            {
                _providerHandle.Dispose();
                _providerHandle = null;
                _deviceName = null;
                _thread = null;
            }
        }

        #endregion Public Methods

        #region Dispose Pattern
        /// <summary>
        /// Implement Dispose pattern to release print ticket handle which can't be released by GC in WOW64 due to restriction from prntvpt!PTCloseProvider
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_providerHandle != null)
                {
                    _providerHandle.Dispose();
                    _providerHandle = null;
                }

                _deviceName = null;
                _schemaVersion = 0;
                _thread = null;
            }

            _disposed = true;
        }

        #endregion Dispose Pattern

        #region Private Methods

        private void VerifyAccess()
        {
            if (_providerHandle == null)
            {
                throw new ObjectDisposedException("PTProvider");
            }

            if(_thread != Thread.CurrentThread)
            {
                throw new InvalidOperationException(SR.Get(SRID.PTProvider_VerifyAccess));
            }
        }
        
        /// <summary>
        /// Copies the managed source stream data to a native buffer and exposes the native buffer as an unmanaged IStream
        /// </summary>
        /// <remarks>
        /// This method reads the stream from its current cursor to the end.
        /// Caller is responsible for freeing the native buffer created (by using Marshal.ReleaseComObject on the IStream).
        /// The input memory stream is expected to have a publicly visible byte buffer
        /// </remarks>
        /// <param name="stream">the source MemoryStream</param>
        /// <returns>IStream copy of the input stream</returns>
        private static IStream IStreamFromMemoryStream(MemoryStream stream)
        {
            if (stream == null)
            {
                return null;
            }
            
            long bufferPosition = stream.Position;
            byte[] streamBuffer = stream.GetBuffer();
            int resultLength = ClampToPositiveInt(stream.Length - bufferPosition);
            if(bufferPosition != 0)
            {                
                streamBuffer = new byte[resultLength];
                stream.Read(streamBuffer, 0, resultLength);
                stream.Position = bufferPosition;
            }
        
            IStream result = CreateStreamOnHGlobal();
            try
            {
                Debug.Assert(resultLength >= 0);
                CopyArrayToIStream(streamBuffer, result, (uint)resultLength);
                RewindIStream(result);
            }
            catch
            {
                Marshal.ReleaseComObject(result);
                throw;
            }
            
            return result;
        }
        
        /// <summary>
        /// Copies the native buffer data to the returned managed memory stream
        /// </summary>
        /// <remarks>
        /// Throws anything that Stream.Write can throw. Does not free the native buffer.
        /// The output stream is reset to the beginning of the stream at return time.
        /// </remarks>
        /// <param name="umBuffer">buffer that contains native data</param>
        /// <param name="bufferSize">size of umBuffer buffer</param>
        private static MemoryStream MemoryStreamFromIStream(IStream stream)
        {

            System.Runtime.InteropServices.ComTypes.STATSTG stats;
            stream.Stat(out stats, 1 /* STATFLAG_NONAME */);
            Invariant.Assert(stats.cbSize >= 0);

            ulong iStreamLength = (ulong)stats.cbSize;
            ulong iStreamPosition = 0;

            byte [] result = null;
            unsafe 
            {
                stream.Seek(0, 1 /* STREAM_SEEK_CUR */, new IntPtr(&iStreamPosition));
            }
                
            result = new byte [ClampToPositiveInt(iStreamLength - iStreamPosition)];
            CopyIStreamToArray(stream, result, (uint)result.Length);

            return new MemoryStream(result);
        }

        /// <summary>
        /// Copies byte array to native IStream
        /// </summary>
        /// <remarks>
        /// Writes byteCount bytes starting from from index 0 of a byte array to an IStream starting its current cursor position.
        /// </remarks>
        /// <param name="src">the source array</param>
        /// <param name="dst">the destination IStream</param>
        /// <param name="byteCount">the number of bytes to copy</param>
        private static void CopyArrayToIStream(byte [] src, IStream dst, uint byteCount)
        {
            Invariant.Assert(src.Length >= byteCount);

            // IStream does not auto resize.
            // we must ensure there is enough remaing capacity to write byteCount bytes.
            EnsureRemainingIStreamLength(dst, byteCount);
            
            // Optimisticly try and write the whole IStream into our MemoryStream
            uint totalBytesWritten = 0;
            unsafe
            {
                dst.Write(src, (int)byteCount, new IntPtr(&totalBytesWritten));
            }
                
            if(totalBytesWritten < byteCount)
            {
                // Our optimistic write failed, write BLOCK_TRANSFER_SIZE chunks of the input array into our IStream
                
                // Since totalBytesWritten is less than byteCount which is less than or equal to an int
                // it is safe to cast it to int without data loss. (This happens a couple of times in the loop)
                byte[] data = new byte[BLOCK_TRANSFER_SIZE];
                do 
                {
                    checked 
                    {
                        int bytesToWrite = Math.Min(data.Length, (int)(byteCount - totalBytesWritten));

                        Array.Copy(src, (int)totalBytesWritten, data, 0, bytesToWrite);
                        
                        uint bytesWritten = 0;
                        unsafe
                        {
                            dst.Write(data, bytesToWrite, new IntPtr(&bytesWritten));
                        }

                        if (bytesWritten < 1)
                        {
                            break;
                        }
                            
                        totalBytesWritten += bytesWritten;
                    }
                }
                while (totalBytesWritten < byteCount);
            }
        }
        
        /// <summary>
        /// Copies native IStream to byte array
        /// </summary>
        /// <remarks>
        /// Reads at most byteCount bytes from an IStream from its current cursor position and copies it into the destination array starting at index 0
        /// The input array is expected to have at least byteCount bytes
        /// </remarks>
        /// <param name="src">the source IStream</param>
        /// <param name="dst">the destination MemoryStream</param>
        /// <param name="byteCount">the number of bytes to copy</param>
        private static void CopyIStreamToArray(IStream src, byte [] dst, uint byteCount)
        {
            Invariant.Assert(dst.Length >= byteCount);
                
            // Optimisticly try and read the whole IStream into our MemoryStream                
            uint totalBytesRead = 0;
            unsafe 
            {
                src.Read(dst, (int)byteCount, new IntPtr(&totalBytesRead));
            }

            if (totalBytesRead < byteCount)
            {
                // Our optimistic read failed, write BLOCK_TRANSFER_SIZE chunks of the IStream into our output array                    
                
                // Since totalBytesRead is less than byteCount which is less than or equal to an int
                // it is safe to cast it to int without data loss. (This happens a couple of times in the loop)                
                byte[] data = new byte[BLOCK_TRANSFER_SIZE];
                do 
                {
                    checked 
                    {
                        int bytesToRead = Math.Min(data.Length, (int)(byteCount - totalBytesRead));

                        Array.Clear(data, 0, data.Length);
                        
                        uint bytesRead = 0;
                        unsafe
                        {
                            src.Read(data, bytesToRead, new IntPtr(&bytesRead));
                        }

                        if (bytesRead < 1)
                        {
                            break;
                        }
                        
                        Array.Copy(data, 0, dst, (int)totalBytesRead, bytesRead);
                        totalBytesRead += bytesRead;
                    }
                }
                while (totalBytesRead < byteCount);
            }
        }
        
        /// <summary>
        /// Ensures an IStream has enough capacity to write byteCount more bytes
        /// </summary>
        private static void EnsureRemainingIStreamLength(IStream stream, uint byteCount)
        {
            ulong iStreamPosition = 0;
            unsafe 
            {
                stream.Seek(0, 1 /* STREAM_SEEK_CUR */, new IntPtr(&iStreamPosition));
            }

            System.Runtime.InteropServices.ComTypes.STATSTG stats;
            stream.Stat(out stats, 1 /* STATFLAG_NONAME */);
            Invariant.Assert(stats.cbSize >= 0);            
            long iStreamLength = stats.cbSize;
            
            long newIStreamLength = 0;
            checked 
            {
                newIStreamLength = (long)(iStreamPosition + byteCount);
            }
            
            if(iStreamLength < newIStreamLength)
            {
                stream.SetSize(newIStreamLength);
            }
        }


        /// <summary>
        /// Creates a COM stream over HGlobal allocated memory
        /// </summary>
        /// <param name="hGlobal">HGlobal to create stream over, IntPtr.Zero to auto allocate memory</param>
        /// <param name="fDeleteOnRelease">Delete the allocated memory when the stream is released</param>
        /// <param name="ppstm">Created Stream</param>
        /// <returns>HRESULT</returns>
        private static IStream CreateStreamOnHGlobal()
        {
            IStream result;

            uint hResult = UnsafeNativeMethods.CreateStreamOnHGlobal(SafeMemoryHandle.Wrap(IntPtr.Zero), true, out result);
            if (!PTUtility.IsSuccessCode(hResult))
            {
                Marshal.ThrowExceptionForHR((int)hResult);
            }

            return result;
        }
        
        private static void DeleteIStream(ref IStream stream)
        {
            if (stream != null)
            {
                Marshal.ReleaseComObject(stream);
                stream = null;
            }
        }
        
        private static void RewindIStream(IStream stream)
        {
            stream.Seek(0, 0 /*STREAM_SEEK_SET*/ , IntPtr.Zero);
        }
        
        private static int ClampToPositiveInt(long value)
        {
            return (int)Math.Max(0, Math.Min(value, int.MaxValue));
        }

        private static int ClampToPositiveInt(ulong value)
        {
            return (int)Math.Min(value, int.MaxValue);
        }
        
        #endregion Private Methods

        #region Private Fields

        /// <summary>
        /// name of printer device this provider instance is bound to
        /// </summary>
        private string _deviceName;

        /// <summary>
        /// handle of unmanaged provider this provider instance is bound to
        /// </summary>
        private SafePTProviderHandle _providerHandle;

        /// <summary>
        /// major schema version this provider instance is using
        /// </summary>
        private int _schemaVersion;

        /// <summary>
        /// Thread the PTProvider was created in
        /// </summary>
        private Thread _thread;
        
        /// <summary>
        /// size of block buffer to use when transferring between managed bytes and unmanaged bytes
        /// </summary>
        /// <remarks>
        /// Modifying this could slightly improve performance.
        /// </remarks>
        private const int BLOCK_TRANSFER_SIZE = 0x1000;

        /// <summary>
        /// boolean of whether or not this instance is disposed
        /// </summary>
        private bool _disposed;

        #endregion Private Fields
    }

    #endregion PTProvider class
}
