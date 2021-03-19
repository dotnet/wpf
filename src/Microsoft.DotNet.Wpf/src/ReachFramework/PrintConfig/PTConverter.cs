// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition and implementation of the public PrintTicketConverter class.
    This class is separated from printing LAPI and exposed directly because
    we don't want printing LAPI to have dependency on the legacy binary DEVMODE.


--*/

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;

using System.Printing;
using MS.Internal.Printing.Configuration;
using System.Security;

//[assembly:System.Runtime.InteropServices.ComVisibleAttribute(false)]

namespace System.Printing.Interop
{
    #region Public Types

    /// <summary>
    /// Types of default DEVMODE to use as base of PrintTicket to DEVMODE conversion.
    /// </summary>
    [ComVisible(false)]
    public enum BaseDevModeType
    {
        /// <summary>
        /// User-default DEVMODE as base of conversion.
        /// </summary>
        UserDefault = 0,

        /// <summary>
        /// Printer-default DEVMODE as base of conversion.
        /// </summary>
        PrinterDefault = 1
    }

    #endregion Public Types

    #region PrintTicketConverter class

    /// <summary>
    /// PrintTicketConverter class that supports conversions between PrintTicket and DEVMODE.
    /// </summary>
    [ComVisible(false)]
    public sealed class PrintTicketConverter : IDisposable
    {
        #region Constructors

        /// <summary>
        /// Constructs a new PrintTicketConverter instance for the given device.
        /// </summary>
        /// <param name="deviceName">Name of printer device the PrintTicketConverter instance should be bound to.</param>
        /// <param name="clientPrintSchemaVersion">Print Schema version requested by client.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="deviceName"/> parameter is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="clientPrintSchemaVersion"/> parameter value is not greater than 0
        /// or is greater than the maximum Print Schema version <see cref="MaxPrintSchemaVersion"/>
        /// PrintTicketConverter can support.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PrintTicketConverter instance failed to bind to the printer specified by <paramref name="deviceName"/>.
        /// </exception>
        public PrintTicketConverter(string deviceName, int clientPrintSchemaVersion)
        {
            // Check input argument
            if (deviceName == null)
            {
                throw new ArgumentNullException(nameof(deviceName));
            }

            // Check if we can support the schema version client has requested
            if ((clientPrintSchemaVersion > MaxPrintSchemaVersion) ||
                (clientPrintSchemaVersion <= 0))
            {
                throw new ArgumentOutOfRangeException(nameof(clientPrintSchemaVersion));
            }

            // Instantiate the provider object this converter instance will use.
            // PTProvider constructor throws exception if it fails for any reason.
            _ptProvider = PTProviderBase.Create(deviceName,
                                         MaxPrintSchemaVersion,
                                         clientPrintSchemaVersion);

            //Create Dispatcher object to insure the PrintTicketConverted is utilized from the same thread
            _accessVerifier = new PrintSystemDispatcherObject();
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the maximum Print Schema version PrintTicketConverter can support.
        /// </summary>
        public static int MaxPrintSchemaVersion
        {
            get
            {
                return _maxPrintSchemaVersion;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Converts the given Win32 DEVMODE into PrintTicket.
        /// </summary>
        /// <param name="devMode">Byte buffer containing the Win32 DEVMODE.</param>
        /// <returns>The converted PrintTicket object.</returns>
        /// <exception cref="ObjectDisposedException">
        /// The PrintTicketConverter instance has already been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="devMode"/> parameter is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The DEVMODE specified by <paramref name="devMode"/> is not well-formed.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PrintTicketConverter instance failed to convert the DEVMODE to a PrintTicket.
        /// </exception>
        public PrintTicket ConvertDevModeToPrintTicket(byte[] devMode)
        {
            return ConvertDevModeToPrintTicket(devMode, PrintTicketScope.JobScope);
        }

        /// <summary>
        /// Converts the given Win32 DEVMODE into PrintTicket.
        /// </summary>
        /// <param name="devMode">Byte buffer containing the Win32 DEVMODE.</param>
        /// <param name="scope">scope that the result PrintTicket will be limited to</param>
        /// <returns>The converted PrintTicket object.</returns>
        /// <exception cref="ObjectDisposedException">
        /// The PrintTicketConverter instance has already been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="devMode"/> parameter is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="scope"/> parameter is not one of the standard <see cref="PrintTicketScope"/> values.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The DEVMODE specified by <paramref name="devMode"/> is not well-formed.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PrintTicketConverter instance failed to convert the DEVMODE to a PrintTicket.
        /// </exception>
        public PrintTicket ConvertDevModeToPrintTicket(byte[]           devMode,
                                                       PrintTicketScope scope)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("PrintTicketConverter");
            }

            //Check to insure that the PrintTicketConverter is being called from the same thread that instantiated it
            VerifyAccess();

            return InternalConvertDevModeToPrintTicket(_ptProvider, devMode, scope);
        }

        /// <summary>
        /// Converts the given PrintTicket into Win32 DEVMODE.
        /// </summary>
        /// <param name="printTicket">The PrintTicket to be converted.</param>
        /// <param name="baseType">Type of default DEVMODE to use as base of conversion.</param>
        /// <returns>Byte buffer that contains the converted Win32 DEVMODE.</returns>
        /// <exception cref="ObjectDisposedException">
        /// The PrintTicketConverter instance has already been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="printTicket"/> parameter is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="baseType"/> parameter is not one of the standard <see cref="BaseDevModeType"/> values.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The PrintTicket specified by <paramref name="printTicket"/> is not well-formed.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PrintTicketConverter instance failed to convert the PrintTicket to a DEVMODE.
        /// </exception>
        public byte[] ConvertPrintTicketToDevMode(PrintTicket printTicket,
                                                  BaseDevModeType baseType)
        {
            return ConvertPrintTicketToDevMode(printTicket, baseType, PrintTicketScope.JobScope);
        }

        /// <summary>
        /// Converts the given PrintTicket into Win32 DEVMODE.
        /// </summary>
        /// <param name="printTicket">The PrintTicket to be converted.</param>
        /// <param name="baseType">Type of default DEVMODE to use as base of conversion.</param>
        /// <param name="scope">scope that the input PrintTicket will be limited to</param>
        /// <returns>Byte buffer that contains the converted Win32 DEVMODE.</returns>
        /// <exception cref="ObjectDisposedException">
        /// The PrintTicketConverter instance has already been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="printTicket"/> parameter is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="baseType"/> parameter is not one of the standard <see cref="BaseDevModeType"/> values.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="scope"/> parameter is not one of the standard <see cref="PrintTicketScope"/> values.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The PrintTicket specified by <paramref name="printTicket"/> is not well-formed.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PrintTicketConverter instance failed to convert the PrintTicket to a DEVMODE.
        /// </exception>
        public byte[] ConvertPrintTicketToDevMode(PrintTicket      printTicket,
                                                  BaseDevModeType  baseType,
                                                  PrintTicketScope scope)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("PrintTicketConverter");
            }

            //Check to insure that the PrintTicketConverter is being called from the same thread that instantiated it
            VerifyAccess();

            return InternalConvertPrintTicketToDevMode(_ptProvider, printTicket, baseType, scope);
        }

        #endregion Public Methods

        #region Internal Methods

        internal static PrintTicket InternalConvertDevModeToPrintTicket(PTProviderBase provider,
                                                                        byte[] devMode,
                                                                        PrintTicketScope scope)
        {
            // validate devMode parameter
            if (devMode == null)
            {
                throw new ArgumentNullException(nameof(devMode));
            }

            // validate sope parameter
            if ((scope != PrintTicketScope.PageScope) &&
                (scope != PrintTicketScope.DocumentScope) &&
                (scope != PrintTicketScope.JobScope))
            {
                throw new ArgumentOutOfRangeException(nameof(scope));
            }

            MemoryStream ptStream = provider.ConvertDevModeToPrintTicket(devMode, scope);

            return new PrintTicket(ptStream);
        }

        internal static byte[] InternalConvertPrintTicketToDevMode(PTProviderBase provider,
                                                                   PrintTicket printTicket,
                                                                   BaseDevModeType baseType,
                                                                   PrintTicketScope scope)
        {
            // Input PrinTicket can't be null.
            if (printTicket == null)
            {
                throw new ArgumentNullException(nameof(printTicket));
            }

            // Validate the base type value.
            if ((baseType != BaseDevModeType.UserDefault) &&
                (baseType != BaseDevModeType.PrinterDefault))
            {
                throw new ArgumentOutOfRangeException(nameof(baseType));
            }

            // Validate scope value.
            if ((scope != PrintTicketScope.PageScope) &&
                (scope != PrintTicketScope.DocumentScope) &&
                (scope != PrintTicketScope.JobScope))
            {
                throw new ArgumentOutOfRangeException(nameof(scope));
            }

            MemoryStream ptStream = printTicket.GetXmlStream();

            return provider.ConvertPrintTicketToDevMode(ptStream, baseType, scope);
        }

        #endregion Internal Methods

        #region Private methods

        private void VerifyAccess()
        {
            _accessVerifier.VerifyThreadLocality();
        }
        

        #endregion Private Methods

        // No need to implement the finalizer since we are using SafeHandle to wrap the unmanaged resource.

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            DisposeImpl();
        }

        /// <summary>
        /// Dispose this PrintTicketConverter instance.
        /// </summary>
        public void Dispose()
        {
            DisposeImpl();
        }

        /// <summary>
        /// Dispose this instance.
        /// </summary>
        private void DisposeImpl()
        {
            if (!this._disposed)
            {
                _ptProvider.Release();
                _ptProvider = null;
                this._disposed = true;
            }
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// max Print Schema version the converter class can support
        /// </summary>
        private const int _maxPrintSchemaVersion = 1;

        /// <summary>
        /// PrintTicket provider instance the converter instance is using
        /// </summary>
        private PTProviderBase _ptProvider;

        /// <summary>
        /// boolean of whether or not this converter instance is disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Dispatcher object to keep track of thread access
        /// </summary>
        private PrintSystemDispatcherObject _accessVerifier;

        #endregion Private Fields
    }

    #endregion PrintTicketConverter class
}
