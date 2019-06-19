// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition and implementation of the internal PrintTicketManager class.
    This class should only be internally used by printing LAPI. Application's
    access to PrintTicket and PrintCapabilities functions should be via printing LAPI.


--*/

using System;
using System.IO;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Globalization;

using System.Printing;
using System.Printing.Interop;
using MS.Internal.Printing.Configuration;
using System.Windows.Xps.Serialization; // for Toolbox

using MS.Utility;
using System.Security;

namespace System.Printing
{
    #region Public Types

    /// <summary>
    /// Possible cases of conflict resolution result of PrintTicket validation.
    /// </summary>
    [ComVisible(false)]
    public enum ConflictStatus
    {
        /// <summary>
        /// PrintTicket validation hasn't found any conflict.
        /// </summary>
        NoConflict = 0,

        /// <summary>
        /// PrintTicket validation has found and resolved conflict(s).
        /// </summary>
        ConflictResolved = 1
    };

    /// <summary>
    /// Struct that contains PrintTicket validation result.
    /// </summary>
    [ComVisible(false)]
    public struct ValidationResult
    {
        #region Constructors

        /// <summary>
        /// Constructor. It's internal so client won't be able to construct one.
        /// </summary>
        /// <param name="validatedPrintTicketStream">resulting PrintTicket stream of the validation</param>
        /// <param name="conflictStatus">conflict resolution result of PrintTicket validation</param>
        internal ValidationResult(MemoryStream   validatedPrintTicketStream,
                                  ConflictStatus conflictStatus)
        {
            _ptStream = validatedPrintTicketStream;
            _status = conflictStatus;
            _printTicket = null;
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Resulting PrintTicket of the validation.
        /// </summary>
        public PrintTicket ValidatedPrintTicket
        {
            get
            {
                if (_printTicket == null)
                {
                    _printTicket = new PrintTicket(_ptStream);
                }

                return _printTicket;
            }
        }

        /// <summary>
        /// Conflict resolution result of the PrintTicket validation.
        /// </summary>
        public ConflictStatus ConflictStatus
        {
            get
            {
                return _status;
            }
        }

        #endregion Public Properties

        #region Public Methods
        
        public override bool Equals(object o)
        {
            if(o == null || !(o is ValidationResult))
            {
                return false;
            }
            
            return Equals((ValidationResult)o);
        }
        
        public override int GetHashCode()
        {
            int hashCode = NullHashCode;
            
            int ptStreamHash = (this._ptStream != null) ? this._ptStream.GetHashCode() : NullHashCode;
            hashCode = (hashCode << 5) + ptStreamHash;            
            hashCode = (hashCode << 5) + this.ValidatedPrintTicket.GetHashCode();
            
            return hashCode;
        }
        
        public static bool operator == (ValidationResult a, ValidationResult b)
        {
            return a.Equals(b);
        }
        
        public static bool operator != (ValidationResult a, ValidationResult b)
        {
            return !(a == b);
        }
        
        #endregion Public Methods
                
        #region Private Methods
        
        private bool Equals(ValidationResult other)
        {
            return 
                   object.Equals(this.ConflictStatus, other.ConflictStatus)
                && object.ReferenceEquals(this._ptStream, other._ptStream)
                && object.ReferenceEquals(this._printTicket, other._printTicket);
        }
        
        #endregion Private Methods
        
        #region Private Fields

        private MemoryStream _ptStream;
        private ConflictStatus _status;
        private PrintTicket _printTicket;

        private const int NullHashCode = 0x61E04917; //sufficiently large prime number
        #endregion Private Fields
    };

    /// <summary>
    /// PrintTicket keyword scoping prefix.
    /// </summary>
    [ComVisible(false)]
    public enum PrintTicketScope
    {
        /// <summary>
        /// Scope for keywords with "Page" prefix.
        /// </summary>
        PageScope = 0,

        /// <summary>
        /// Scope for keywords with "Document" prefix.
        /// </summary>
        DocumentScope = 1,

        /// <summary>
        /// Scope for keywords with "Job" prefix.
        /// </summary>
        JobScope = 2,
    }

    #endregion Public Types

    #region PrintTicketManager class

    /// <summary>
    /// PrintTicketManager class that supports PrintTicket and PrintCapabilities functions.
    /// </summary>
    [MS.Internal.ReachFramework.FriendAccessAllowed]
    internal class PrintTicketManager : IDisposable
    {
        #region Constructors

        /// <summary>
        /// Constructs a new PrintTicketManager instance for the given device.
        /// </summary>
        /// <param name="deviceName">Name of printer device the PrintTicketManager instance should be bound to.</param>
        /// <param name="clientPrintSchemaVersion">Print Schema version requested by client.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="deviceName"/> parameter is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="clientPrintSchemaVersion"/> parameter value is not greater than 0
        /// or is greater than the maximum Print Schema version <see cref="MaxPrintSchemaVersion"/>
        /// PrintTicketManager can support.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PrintTicketManager instance failed to bind to the printer specified by <paramref name="deviceName"/>.
        /// </exception>
        public PrintTicketManager(string deviceName, int clientPrintSchemaVersion)
        {
            // Check input argument
            if (deviceName == null)
            {
                throw new ArgumentNullException("deviceName");
            }

            // Check if we can support the schema version client has requested
            if ((clientPrintSchemaVersion > MaxPrintSchemaVersion) ||
                (clientPrintSchemaVersion <= 0))
            {
                throw new ArgumentOutOfRangeException("clientPrintSchemaVersion");
            }

            // Instantiate a new PTProvider instance. PTProvider constructor throws exception if it fails for any reason.
            _ptProvider = PTProviderBase.Create(deviceName,
                                             MaxPrintSchemaVersion,
                                             clientPrintSchemaVersion);
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the maximum Print Schema version PrintTicketManager can support.
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
        /// Gets the PrintCapabilities relative to the given PrintTicket.
        /// </summary>
        /// <param name="printTicket">The PrintTicket object based on which PrintCapabilities should be built.</param>
        /// <returns>The PrintCapabilities object.</returns>
        /// <remarks>
        /// The <paramref name="printTicket"/> parameter could be null, in which case
        /// the device's default PrintTicket will be used to construct the PrintCapabilities.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// The PrintTicketManager instance has already been disposed.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The input PrintTicket specified by <paramref name="printTicket"/> is not well-formed.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PrintTicketManager instance failed to retrieve the PrintCapabilities.
        /// </exception>
        public PrintCapabilities GetPrintCapabilities(PrintTicket printTicket)
        {
            MemoryStream pcStream = GetPrintCapabilitiesAsXml(printTicket);

            PrintCapabilities retCap = new PrintCapabilities(pcStream);

            pcStream.Close();
            return retCap;
        }

        /// <summary>
        /// Gets the PrintCapabilities (in XML form) relative to the given PrintTicket.
        /// </summary>
        /// <param name="printTicket">The PrintTicket object based on which PrintCapabilities should be built.</param>
        /// <returns>MemoryStream that contains XML PrintCapabilities.</returns>
        /// <remarks>
        /// The <paramref name="printTicket"/> parameter could be null, in which case
        /// the device's default PrintTicket will be used to construct the PrintCapabilities.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// The PrintTicketManager instance has already been disposed.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The input PrintTicket specified by <paramref name="printTicket"/> is not well-formed.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PrintTicketManager instance failed to retrieve the PrintCapabilities.
        /// </exception>
        public MemoryStream GetPrintCapabilitiesAsXml(PrintTicket printTicket)
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXGetPrintCapStart);

            if (_disposed)
            {
                throw new ObjectDisposedException("PrintTicketManager");
            }

            MemoryStream ptStream = null;

            if (printTicket != null)
            {
                ptStream = printTicket.GetXmlStream();
            }

            MemoryStream pcStream = _ptProvider.GetPrintCapabilities(ptStream);

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXGetPrintCapEnd);

            return pcStream;
        }

        /// <summary>
        /// Merges delta PrintTicket with base PrintTicket and then validates the merged PrintTicket.
        /// </summary>
        /// <param name="basePrintTicket">The base PrintTicket.</param>
        /// <param name="deltaPrintTicket">The delta PrintTicket.</param>
        /// <returns>Struct that contains PrintTicket validation result info.</returns>
        /// <remarks>
        /// The <paramref name="deltaPrintTicket"/> parameter could be null, in which case
        /// only validation will be performed on <paramref name="basePrintTicket"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// The PrintTicketManager instance has already been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="basePrintTicket"/> parameter is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The base PrintTicket specified by <paramref name="basePrintTicket"/> is not well-formed,
        /// or delta PrintTicket specified by <paramref name="deltaPrintTicket"/> is not well-formed.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PrintTicketManager instance failed to merge and validate the input PrintTicket(s).
        /// </exception>
        public ValidationResult MergeAndValidatePrintTicket(PrintTicket   basePrintTicket,
                                                            PrintTicket   deltaPrintTicket)
        {
            return MergeAndValidatePrintTicket(basePrintTicket, deltaPrintTicket, PrintTicketScope.JobScope);
        }


        /// <summary>
        /// Merges delta PrintTicket with base PrintTicket and then validates the merged PrintTicket.
        /// </summary>
        /// <param name="basePrintTicket">The base PrintTicket.</param>
        /// <param name="deltaPrintTicket">The delta PrintTicket.</param>
        /// <param name="scope">scope that delta PrintTicket and result PrintTicket will be limited to</param>
        /// <returns>Struct that contains PrintTicket validation result info.</returns>
        /// <remarks>
        /// The <paramref name="deltaPrintTicket"/> parameter could be null, in which case
        /// only validation will be performed on <paramref name="basePrintTicket"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// The PrintTicketManager instance has already been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="basePrintTicket"/> parameter is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="scope"/> parameter is not one of the standard <see cref="PrintTicketScope"/> values.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The base PrintTicket specified by <paramref name="basePrintTicket"/> is not well-formed,
        /// or delta PrintTicket specified by <paramref name="deltaPrintTicket"/> is not well-formed.
        /// </exception>
        /// <exception cref="PrintQueueException">
        /// The PrintTicketManager instance failed to merge and validate the input PrintTicket(s).
        /// </exception>
        public ValidationResult MergeAndValidatePrintTicket(PrintTicket      basePrintTicket,
                                                            PrintTicket      deltaPrintTicket,
                                                            PrintTicketScope scope)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("PrintTicketManager");
            }

            // Base PrintTicket is required. Delta PrintTicket is optional.
            if (basePrintTicket == null)
            {
                throw new ArgumentNullException("basePrintTicket");
            }

            // validate scope value
            if ((scope != PrintTicketScope.PageScope) &&
                (scope != PrintTicketScope.DocumentScope) &&
                (scope != PrintTicketScope.JobScope))
            {
                throw new ArgumentOutOfRangeException("scope");
            }

            MemoryStream baseStream = null, deltaStream = null, resultStream = null;
            ConflictStatus status;

            baseStream = basePrintTicket.GetXmlStream();

            if (deltaPrintTicket != null)
            {
                deltaStream = deltaPrintTicket.GetXmlStream();
            }

            resultStream = _ptProvider.MergeAndValidatePrintTicket(baseStream,
                                                                   deltaStream,
                                                                   scope,
                                                                   out status);

            return new ValidationResult(resultStream, status);
        }

        /// <summary>
        /// Converts the given Win32 DEVMODE into PrintTicket.
        /// </summary>
        /// <param name="devMode">Byte buffer containing the Win32 DEVMODE.</param>
        /// <returns>The converted PrintTicket object.</returns>
        public PrintTicket ConvertDevModeToPrintTicket(byte[] devMode)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("PrintTicketManager");
            }

            return PrintTicketConverter.InternalConvertDevModeToPrintTicket(_ptProvider,
                                                                            devMode,
                                                                            PrintTicketScope.JobScope);
        }

        /// <summary>
        /// Converts the given PrintTicket into Win32 DEVMODE.
        /// </summary>
        /// <param name="printTicket">The PrintTicket to be converted.</param>
        /// <param name="baseType">Type of default DEVMODE to use as base of conversion.</param>
        /// <returns>Byte buffer that contains the converted Win32 DEVMODE.</returns>
        public byte[] ConvertPrintTicketToDevMode(PrintTicket printTicket,
                                                  BaseDevModeType baseType)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("PrintTicketManager");
            }

            return PrintTicketConverter.InternalConvertPrintTicketToDevMode(_ptProvider,
                                                                            printTicket,
                                                                            baseType,
                                                                            PrintTicketScope.JobScope);
        }

        #endregion Public Methods

        #region Dispose Pattern
        /// <summary>
        /// Implement Dispose pattern to release print ticket handle which can't be released by GC in WOW64 due to restriction from prntvpt!PTCloseProvider
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_ptProvider != null)
                {
                    _ptProvider.Dispose();
                    _ptProvider = null;
                }
            }

            _disposed = true;
        }

        #endregion Dispose Pattern

        /// <summary>
        /// Dispose this instance.
        /// </summary>
        public virtual void Release()
        {
            if (!this._disposed)
            {
                _ptProvider.Release();
                _ptProvider = null;

                this._disposed = true;
            }
        }

        #region Private Fields

        /// <summary>
        /// max Print Schema version the manager class can support
        /// </summary>
        private const int _maxPrintSchemaVersion = 1;

        /// <summary>
        /// PrintTicket provider instance the manager instance is using
        /// </summary>
        private PTProviderBase _ptProvider;

        /// <summary>
        /// boolean of whether or not this manager instance is disposed
        /// </summary>
        private bool _disposed;

        #endregion Private Fields
    }

    #endregion PrintTicketManager class
}
