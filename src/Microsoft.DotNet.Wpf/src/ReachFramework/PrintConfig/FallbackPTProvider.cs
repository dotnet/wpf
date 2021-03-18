// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Abstract:

    Definition and implementation of the internal managed PTProvider and NativeMethods classes.
    These classes hide from PrintTicketManager and PrintTicketConverter the fact and complexity
    of thunking into unmanaged component to get PrintTicket and PrintCapabilities provider services.


--*/

namespace MS.Internal.Printing.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Printing;
    using System.Printing.Interop;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Windows.Xps.Serialization;
    using System.Xml;
    using MS.Utility;
    using MS.Internal.PrintWin32Thunk;
    using MS.Internal.ReachFramework;


    /// <summary>
    /// Managed PrintTicket provider class that inter-ops with unmanaged DDI driver
    /// </summary>
    internal class FallbackPTProvider : PTProviderBase
    {
        #region Constructors

        /// <summary>
        /// Constructs a new PrintTicket provider instance for the given device.
        /// </summary>
        /// <param name="deviceName">name of printer device the provider should be bound to</param>
        /// <param name="maxVersion">max schema version supported by client</param>
        /// <param name="clientVersion">schema version requested by client</param>
        /// <exception cref="PrintQueueException">
        /// The FallbackPTProvider instance failed to bind to the specified printer.
        /// </exception>
        public FallbackPTProvider(string deviceName, int maxVersion, int clientVersion)
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXPTProviderStart);

            // We are not doing late binding to the device here because we should
            // indicate right away if there was an error in binding the provider
            // to the device.  Doing late binding would mean that any instance
            // method could throw a no such printer exception.
            if (false == UnsafeNativeMethods.OpenPrinterW(deviceName, out this._deviceHandle, new HandleRef(this, IntPtr.Zero)))
            {
                throw new PrintQueueException(Marshal.GetLastWin32Error(), "PrintConfig.Provider.BindFail", deviceName);
            }

            try
            {
                PRINTER_INFO_2 info = GetPrinterInfo2W();
                this._deviceName = info.pPrinterName;
                this._driverName = info.pDriverName;
                this._portName = info.pPortName;
                if (info.pDevMode != null)
                {
                    this._driverVersion = info.pDevMode.DriverVersion;
                }
            }
            catch (Win32Exception win32Exception)
            {
                throw new PrintQueueException(win32Exception.ErrorCode, "PrintConfig.Provider.BindFail", deviceName, win32Exception);
            }

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
        public override MemoryStream GetPrintCapabilities(MemoryStream printTicket)
        {
            VerifyAccess();

            InternalPrintTicket internalTicket = null;
            try
            {
                internalTicket = (printTicket != null) ? new InternalPrintTicket(printTicket) : null;
            }
            catch (XmlException xmlException)
            {
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        "{0} {1} {2}",
                        PrintSchemaTags.Framework.PrintTicketRoot,
                        PTUtility.GetTextFromResource("FormatException.XMLNotWellFormed"),
                        xmlException.Message),
                    "printTicket",
                    xmlException);
            }

            DevMode defaultDevMode = GetDEVMODE(BaseDevModeType.UserDefault);
            DevMode devMode = defaultDevMode.Clone();

            PrintTicketToDevMode(devMode, internalTicket, PrintTicketScope.JobScope, DevModeFields.All);

            MemoryStream capabilitiesStream = new MemoryStream();

            WinSpoolPrinterCapabilities capabilities = GetCapabilities(devMode);
            try
            {
                PrintCapabilitiesWriter writer = new PrintCapabilitiesWriter(capabilitiesStream, "ns0000", this.OemDriverNamespace, false);
                try
                {
                    writer.WriteStartDocument();
                    {
                        writer.WritePageDevmodeSnapshot(devMode.ByteData);

                        if (capabilities.HasICMIntent)
                        {
                            writer.WritePageICMRenderingIntentFeature();
                        }

                        if (capabilities.HasICMMethod)
                        {
                            writer.WritePageColorManagementFeature();
                        }

                        if (capabilities.CanCollate)
                        {
                            writer.WriteDocumentCollateFeature();
                        }

                        int minCopies = capabilities.MinCopies;
                        int maxCopies = capabilities.MaxCopies;

                        int defaultCopies = minCopies;
                        defaultCopies = Math.Max(minCopies, Math.Min(defaultDevMode.Copies, maxCopies));

                        writer.WriteJobCopiesAllDocumentsParameterDef(minCopies, maxCopies, defaultCopies);

                        writer.WriteJobNUpAllDocumentsContiguously(capabilities.NUp);

                        writer.WriteJobDuplexAllDocumentsContiguouslyFeature(capabilities.CanDuplex);

                        if (capabilities.CanScale)
                        {
                            writer.WritePageScalingFeature(1, 1000, 100);
                        }

                        writer.WritePageMediaSizeFeature(
                            capabilities.Papers,
                            capabilities.PaperNames,
                            PrintSchemaShim.TenthOfMillimeterToMicrons(capabilities.PaperSizes)
                        );

                        writer.WritePageResolutionFeature(capabilities.Resolutions);

                        int logicalPixelsX, logicalPixelsY;
                        int physicalWidth, physicalHeight;
                        int physicalOffsetX, physicalOffsetY;
                        int horizontalResolution, verticalResolution;

                        bool gotDevCaps = capabilities.TryGetDeviceCapabilities(
                            out logicalPixelsX, out logicalPixelsY,
                            out physicalWidth, out physicalHeight,
                            out physicalOffsetX, out physicalOffsetY,
                            out horizontalResolution, out verticalResolution);

                        if (gotDevCaps && logicalPixelsX > 0 && logicalPixelsY > 0)
                        {
                            int imageableSizeWidth = PrintSchemaShim.DpiToMicrons(physicalWidth, logicalPixelsX);
                            int imageableSizeHeight = PrintSchemaShim.DpiToMicrons(physicalHeight, logicalPixelsY);
                            int originWidth = PrintSchemaShim.DpiToMicrons(physicalOffsetX, logicalPixelsY);
                            int originHeight = PrintSchemaShim.DpiToMicrons(physicalOffsetY, logicalPixelsY);
                            int extentWidth = PrintSchemaShim.DpiToMicrons(horizontalResolution, logicalPixelsY);
                            int extentHeight = PrintSchemaShim.DpiToMicrons(verticalResolution, logicalPixelsY);

                            writer.WritePageImageableSizeProperty(
                                imageableSizeWidth, imageableSizeHeight,
                                originWidth, originHeight,
                                extentWidth, extentHeight);
                        }

                        writer.WritePageOrientationFeature(capabilities.LandscapeOrientation);

                        writer.WritePageOutputColorFeature(capabilities.HasColor);

                        writer.WriteJobInputBinFeature(capabilities.Bins, capabilities.BinNames);

                        writer.WritePageMediaTypeFeature(capabilities.MediaTypes, capabilities.MediaTypeNames);

                        if (capabilities.TrueType)
                        {
                            writer.WritePageTrueTypeFontModeFeature();

                            writer.WritePageDeviceFontSubstitutionFeature();
                        }
                    }
                    writer.WriteEndDocument();
                }
                finally
                {
                    writer.Release();
                    writer = null;
                }
            }
            finally
            {
                capabilities.Release();
                capabilities = null;
            }

            // calls Security Critical Dispose on a private resource
            capabilitiesStream.Position = 0;

            return capabilitiesStream;
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
        public override MemoryStream MergeAndValidatePrintTicket(MemoryStream basePrintTicket,
                                                        MemoryStream deltaPrintTicket,
                                                        PrintTicketScope scope,
                                                        out ConflictStatus conflictStatus)
        {
            VerifyAccess();

            DevModeFields supportedFields = DevModeFields.All;
            DevMode defaultDevMode = GetDEVMODE(BaseDevModeType.PrinterDefault);

            InternalPrintTicket baseTicket = (basePrintTicket != null) ? new InternalPrintTicket(basePrintTicket) : null;
            DevMode baseDevMode = defaultDevMode.Clone();
            PrintTicketToDevMode(baseDevMode, baseTicket, scope, supportedFields);

            InternalPrintTicket deltaTicket = (deltaPrintTicket != null) ? new InternalPrintTicket(deltaPrintTicket) : null;
            DevMode deltaDevMode = defaultDevMode.Clone();
            PrintTicketToDevMode(deltaDevMode, deltaTicket, scope, supportedFields);

            // DevMode Merge - Copy fields set in baseDevMode but not set in deltaDevMode
            deltaDevMode.Copy(baseDevMode, baseDevMode.Fields & (~deltaDevMode.Fields));

            conflictStatus = Validate(deltaDevMode) ? ConflictStatus.NoConflict : ConflictStatus.ConflictResolved;

            InternalPrintTicket validatedTicket = DevModeToPrintTicket(deltaDevMode, scope, supportedFields);

            return validatedTicket.XmlStream;
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
        public override MemoryStream ConvertDevModeToPrintTicket(byte[] devMode, PrintTicketScope scope)
        {
            VerifyAccess();

            if (devMode == null)
            {
                throw new ArgumentNullException("devMode");
            }

            InternalPrintTicket result = DevModeToPrintTicket(new DevMode(devMode), scope, DevModeFields.All);
            return result.XmlStream;
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
        public override byte[] ConvertPrintTicketToDevMode(MemoryStream printTicket,
                                                  BaseDevModeType baseType,
                                                  PrintTicketScope scope)
        {
            VerifyAccess();

            InternalPrintTicket ticket = (printTicket != null) ? new InternalPrintTicket(printTicket) : null;

            DevMode result = GetDEVMODE(baseType);

            DevModeFields supportedFields;
            WinSpoolPrinterCapabilities capabilities = GetCapabilities(result);
            try
            {
                supportedFields = capabilities.Fields;
            }
            finally
            {
                capabilities.Release();
            }

            PrintTicketToDevMode(result, ticket, scope, supportedFields);

            return result.ByteData;
        }

        public override void Release()
        {
            if (_deviceHandle != null)
            {
                _deviceHandle.Dispose();
            }

            this._deviceHandle = null;
            this._deviceName = null;
            this._driverName = null;
            this._driverVersion = 0;
            this._portName = null;
            this._printTicketNamespace = null;
        }

        #endregion Public Methods

        #region Private Properties

        /// <summary>
        /// Namespace for OEM specific elements
        /// </summary>
        /// <remarks>
        /// Based on Print Schema Reference Guide v1.0 Appendix E.2
        /// </remarks>
        private string OemDriverNamespace
        {
            get
            {
                if (this._printTicketNamespace == null)
                {
                    string deviceNamepace = string.Format(
                            CultureInfo.InvariantCulture,
                            DeviceNamespaceFormat,
                            BuildInfo.WCP_VERSION_SUFFIX,
                            this._driverName,
                            this._driverVersion);

                    if (!Uri.IsWellFormedUriString(deviceNamepace, UriKind.Absolute))
                    {
                        deviceNamepace = string.Format(
                            CultureInfo.InvariantCulture,
                            DeviceNamespaceFormat,
                            BuildInfo.WCP_VERSION_SUFFIX,
                            #pragma warning disable SYSLIB0013
                            Uri.EscapeUriString(this._driverName),
                            #pragma warning restore SYSLIB0013
                            this._driverVersion);
                    }

                    this._printTicketNamespace = deviceNamepace;
                }

                return this._printTicketNamespace;
            }
        }

        #endregion Private Properties

        #region Private Methods

        private bool Validate(DevMode devMode)
        {
            bool settingsChanged = false;

            // Create a copy of the devmode before validating... if anything
            // changes, then we'll report it
            DevMode originalDevMode = devMode.Clone();

            // Validate the DEVMODE via a call to the driver.            
            SetDocumentProperties(devMode.ByteData, true);

            // Compare the devmods to decide whether any features 
            // changed.  Ignore changes in the DM_FORMNAME field 
            // because the shim may clear it in selecting a paper size.

            // If other paper size fields are set in the DEVMODE, ignore the form field
            // because some driver change set / clear the form field during DocumentProperties
            // without actually changing the selected paper size.
            if (originalDevMode.IsAnyFieldSet(DevModeFields.DM_PAPERWIDTH | DevModeFields.DM_PAPERLENGTH | DevModeFields.DM_PAPERSIZE))
            {
                originalDevMode.Fields &= ~DevModeFields.DM_FORMNAME;
                originalDevMode.Fields |= devMode.Fields & DevModeFields.DM_FORMNAME;
                originalDevMode.FormName = devMode.FormName;
            }

            // Ignore device name in the original DEVMODE.  Copy the full buffer so that any extra 
            // garbage in the device name after the string also matches.  The device name isn't actually
            // a setting per-se.  Version changes should be noted, because the same content may mean different
            // things in different versions of a driver.
            originalDevMode.DeviceName = devMode.DeviceName;

            // Do the actual comparison.  Note that if there are issues in the private DEVMODE similar 
            // to the issues fixed up in the public DEVMODE, there may be false positive return values 
            // from this API.  There's no way to fix this issue.        
            for (int i = 0; i < originalDevMode.ByteData.Length; i++)
            {
                if (originalDevMode.ByteData[i] != devMode.ByteData[i])
                {
                    settingsChanged = true;
                }
            }

            WinSpoolPrinterCapabilities capabilities = GetCapabilities(null);
            try
            {
                PrintSchemaShim.PruneFeatures(devMode, capabilities);
            }
            finally
            {
                capabilities.Release();
            }

            return settingsChanged;
        }

        private void PrintTicketToDevMode(DevMode devMode, InternalPrintTicket ticket, PrintTicketScope scope, DevModeFields supportedFields)
        {
            if (ticket != null)
            {
                // Apply the DEVMODE snapshot from the print ticket to our starting DEVMODE, if supported.
                DevMode ticketDevMode = PrintSchemaShim.TryGetEmbeddedDevMode(ticket, this.OemDriverNamespace);
                if (ticketDevMode != null)
                {
                    devMode.CompatibleCopy(ticketDevMode);
                }

                PrintSchemaShim.CopyTicketToDevMode(devMode, ticket, scope, supportedFields);
            }
        }

        private InternalPrintTicket DevModeToPrintTicket(DevMode devmode, PrintTicketScope scope, DevModeFields supportedFields)
        {
            InternalPrintTicket resultTicket = new InternalPrintTicket();
            PrintSchemaShim.TryEmbedDevMode(resultTicket, this.OemDriverNamespace, devmode);
            PrintSchemaShim.CopyDevModeToTicket(resultTicket, devmode, scope, supportedFields);
            return resultTicket;
        }

        private WinSpoolPrinterCapabilities GetCapabilities(DevMode devMode)
        {
            return new WinSpoolPrinterCapabilities(this._deviceName, this._driverName, this._portName, devMode);
        }

        /// <summary>
        /// Updates the current printer device default DEVMODE
        /// </summary>
        /// <param name="devModeBytes">New DEVMODE settings to apply</param>
        /// <param name="biDirectional">If true updates the devModeBytes array with resolved merge conflicts</param>
        /// </summary>
        private void SetDocumentProperties(byte[] devModeBytes, bool biDirectional)
        {
            long result = -1;
            DocumentPropertiesFlags flags = biDirectional ? (DocumentPropertiesFlags.DM_IN_BUFFER | DocumentPropertiesFlags.DM_OUT_BUFFER) : DocumentPropertiesFlags.DM_IN_BUFFER;
            SafeMemoryHandle outPtr = SafeMemoryHandle.Null;

            using (SafeMemoryHandle buffer = SafeMemoryHandle.Create(devModeBytes.Length))
            {
                buffer.CopyFromArray(devModeBytes, 0, devModeBytes.Length);

                if (biDirectional)
                {
                    outPtr = buffer;
                }

                result = UnsafeNativeMethods.DocumentPropertiesW(new HandleRef(this, IntPtr.Zero), this._deviceHandle, this._deviceName, outPtr, buffer, flags);
                    
                if (result < 0)
                {
                    throw new Win32Exception();
                }

                if (!outPtr.IsInvalid)
                {
                    outPtr.CopyToArray(devModeBytes, 0, devModeBytes.Length);
                }
            }
        }

        ///<summary>
        /// Obtain a DEVMODE from a printer
        ///</summary>
        ///<param name="pHandle">Printer handle</param>
        ///<param name="baseType">Type of DEVMODE (printer default or user default)</param>
        ///<param name="devModeBytes">DEVMODE bytes</param>
        ///<returns>False if the call fails</returns>
        private DevMode GetDEVMODE(BaseDevModeType baseType)
        {
            DevMode result;

            switch (baseType)
            {
                case BaseDevModeType.PrinterDefault:
                {
                    PRINTER_INFO_2 info = GetPrinterInfo2W();
                    result = info.pDevMode;
                    break;
                }

                case BaseDevModeType.UserDefault:
                {
                    PRINTER_INFO_8_AND_9 info = GetPrinterInfo8Or9W(true);
                    if (info.pDevMode != null)
                    {
                        result = info.pDevMode;
                    }
                    else
                    {
                        // No user default devmode, try the printer default, which is 
                        // effectively the user default if their isn't a per user default
                        // overriding it.
                        result = GetDEVMODE(BaseDevModeType.PrinterDefault);
                    }
                    
                    break;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException("baseType");
                }
            }

            if (result == null)
            {
                throw new PrintSystemException();
            }

            return result;
        }

        /// <summary>
        /// Gets a PRINTER_INFO_2 structure from the providers device
        /// </summary>
        /// <returns>A PRINTER_INFO_2 structure</returns>
        private PRINTER_INFO_2 GetPrinterInfo2W()
        {
            PRINTER_INFO_2WGetter result = new PRINTER_INFO_2WGetter();

            if (!GetPrinterW(2, result.Callback))
            {
                throw new Win32Exception();
            }

            return result.PRINTER_INFO_2;
        }

        /// <summary>
        /// Gets a PRINTER_INFO_8 or a PRINTER_INFO_9 structure from the providers device
        /// </summary>
        /// <returns>A PRINTER_INFO_8 or PRINTER_INFO_9 structure</returns>
        private PRINTER_INFO_8_AND_9 GetPrinterInfo8Or9W(bool getPrinterInfo8)
        {
            PRINTER_INFO_8_AND_9Getter result = new PRINTER_INFO_8_AND_9Getter();

            if (!GetPrinterW((uint)(getPrinterInfo8 ? 8 : 9), result.Callback))
            {
                throw new Win32Exception();
            }

            return result.PRINTER_INFO_8_AND_9;
        }

        ///<summary>
        /// Executes an action delegate on an unmanaged buffer representing printer information
        ///</summary>
        ///<param name="dwLevel">Level of printer information to process</param>
        ///<param name="action">Delegate that processes printer information</param>
        ///<returns>False if tha call fails</returns>
        private bool GetPrinterW(uint dwLevel, Action<HGlobalBuffer> action)
        {
            uint dwNeeded = 0;

            UnsafeNativeMethods.GetPrinterW(this._deviceHandle, dwLevel, SafeMemoryHandle.Null, dwNeeded, ref dwNeeded);
            if (dwNeeded > 0)
            {
                HGlobalBuffer pPrinterBuffer = new HGlobalBuffer((int)dwNeeded);
                try
                {
                    if (UnsafeNativeMethods.GetPrinterW(this._deviceHandle, dwLevel, pPrinterBuffer.Handle, dwNeeded, ref dwNeeded))
                    {
                        action(pPrinterBuffer);
                        return true;
                    }
                }
                finally
                {
                    pPrinterBuffer.Release();
                }
            }

            return false;
        }

        private void VerifyAccess()
        {
            if (_deviceHandle == null)
            {
                throw new ObjectDisposedException("PTProvider");
            }
        }

        #endregion Private Methods

        #region Dispose Pattern
        /// <summary>
        /// Implement Dispose pattern to release printer handle which can't be released by GC in WOW64
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_deviceHandle != null)
                {
                    _deviceHandle.Dispose();
                    _deviceHandle = null;
                }

                _deviceName = null;
                _driverName = null;
                _portName = null;
                _driverVersion = 0;
                _printTicketNamespace = null;
            }

            _disposed = true;
        }

        #endregion Dispose Pattern

        #region Private Fields

        private string _deviceName;

        private string _driverName;

        private string _portName;

        private ushort _driverVersion;

        private SafeWinSpoolPrinterHandle _deviceHandle;

        private string _printTicketNamespace;

        /// <summary>
        /// boolean of whether or not this instance is disposed
        /// </summary>
        private bool _disposed;

        #endregion Private Fields

        #region Constants

        private const string DeviceNamespaceFormat = "http://schemas.microsoft.com/windows/printing/oemdriverpt/netfx{0}/{1}/v{2}";

        #endregion

        #region Inner Classes

        private class PRINTER_INFO_2WGetter
        {
            public PRINTER_INFO_2 PRINTER_INFO_2;

            /// <summary>
            /// A callback that copies PRINTER_INFO_2 fields from an unmanaged buffer
            /// </summary>
            public void Callback(HGlobalBuffer pPrinterBuffer)
            {
                // PRINTER_INFO_2 layout from http://msdn.microsoft.com/en-us/library/dd162845(VS.85).aspx
                //
                //typedef struct _PRINTER_INFO_2 {
                //   LPTSTR               pServerName;
                //   LPTSTR               pPrinterName;
                //   LPTSTR               pShareName;
                //   LPTSTR               pPortName;
                //   LPTSTR               pDriverName;
                //   LPTSTR               pComment;
                //   LPTSTR               pLocation;
                //   LPDEVMODE            pDevMode;
                //   LPTSTR               pSepFile;
                //   LPTSTR               pPrintProcessor;
                //   LPTSTR               pDatatype;
                //   LPTSTR               pParameters;
                //   PSECURITY_DESCRIPTOR pSecurityDescriptor;
                //   DWORD                Attributes;
                //   DWORD                Priority;
                //   DWORD                DefaultPriority;
                //   DWORD                StartTime;
                //   DWORD                UntilTime;
                //   DWORD                Status;
                //   DWORD                cJobs;
                //   DWORD                AveragePPM;
                // } PRINTER_INFO_2, *PPRINTER_INFO_2;


                PRINTER_INFO_2 = new PRINTER_INFO_2();

                bool shouldRelease = false;
                pPrinterBuffer.Handle.DangerousAddRef(ref shouldRelease);
                try
                {
                    IntPtr ptr = pPrinterBuffer.Handle.DangerousGetHandle();

                    //   LPTSTR               pPrinterName;
                    IntPtr pPrinterName = Marshal.ReadIntPtr(ptr, 1 * Marshal.SizeOf(typeof(IntPtr)));
                    if (pPrinterName != IntPtr.Zero)
                    {
                        PRINTER_INFO_2.pPrinterName = Marshal.PtrToStringUni(pPrinterName);
                    }

                    //   LPTSTR               pPortName;
                    IntPtr pPortName = Marshal.ReadIntPtr(ptr, 3 * Marshal.SizeOf(typeof(IntPtr)));
                    if (pPortName != IntPtr.Zero)
                    {
                        PRINTER_INFO_2.pPortName = Marshal.PtrToStringUni(pPortName);
                    }

                    //   LPTSTR               pDriverName;
                    IntPtr pDriverName = Marshal.ReadIntPtr(ptr, 4 * Marshal.SizeOf(typeof(IntPtr)));
                    if (pDriverName != IntPtr.Zero)
                    {
                        PRINTER_INFO_2.pDriverName = Marshal.PtrToStringUni(pDriverName);
                    }

                    //   LPDEVMODE            pDevMode;
                    IntPtr pDevMode = Marshal.ReadIntPtr(ptr, 7 * Marshal.SizeOf(typeof(IntPtr)));
                    if (pDevMode != IntPtr.Zero)
                    {
                        PRINTER_INFO_2.pDevMode = DevMode.FromIntPtr(pDevMode);
                    }
                }
                finally
                {
                    if (shouldRelease)
                    {
                        pPrinterBuffer.Handle.DangerousRelease();
                    }
                }
            }
        }

        private class PRINTER_INFO_8_AND_9Getter
        {
            public PRINTER_INFO_8_AND_9 PRINTER_INFO_8_AND_9;

            /// <summary>
            /// A callback that copies PRINTER_INFO_8_AND_9 fields from an unmanaged buffer
            /// </summary>
            public void Callback(HGlobalBuffer pPrinterBuffer)
            {
                // PRINTER_INFO_8 layout from http://msdn.microsoft.com/en-us/library/dd162851(VS.85).aspx
                //
                // typedef struct _PRINTER_INFO_8 {
                //  LPDEVMODE pDevMode;
                // } PRINTER_INFO_8, *PPRINTER_INFO_8;
                //
                // PRINTER_INFO_9 layout from http://msdn.microsoft.com/en-us/library/dd162852(VS.85).aspx
                //
                // typedef struct _PRINTER_INFO_9 {
                //  LPDEVMODE pDevMode;
                // } PRINTER_INFO_9, *PPRINTER_INFO_9;

                //  LPDEVMODE pDevMode;

                PRINTER_INFO_8_AND_9 = new PRINTER_INFO_8_AND_9();

                bool shouldRelease = false;
                pPrinterBuffer.Handle.DangerousAddRef(ref shouldRelease);
                try
                {
                    IntPtr pDevMode = Marshal.ReadIntPtr(pPrinterBuffer.Handle.DangerousGetHandle());
                    if (pDevMode != IntPtr.Zero)
                    {
                        PRINTER_INFO_8_AND_9.pDevMode = DevMode.FromIntPtr(pDevMode);
                    }
                }
                finally
                {
                    if (shouldRelease)
                    {
                        pPrinterBuffer.Handle.DangerousRelease();
                    }
                }
            }
        }

        #endregion
    }
}
