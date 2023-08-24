// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition and implementation of the internal managed PTProvider and NativeMethods classes.
    Base class for interface that hide from PrintTicketManager and PrintTicketConverter the complexity
    of calling into the unmanaged component to get PrintTicket and PrintCapabilities provider services.
 
--*/

using System;
using System.IO;
using System.Printing;
using System.Printing.Interop;
using System.Security;

namespace MS.Internal.Printing.Configuration
{
    #region PTProviderBase class

    // current code in following PTProvider class is only using try...finally... to
    // release unmanaged memory. This needs to be changed to use Whidbey's new CriticalFinalizer
    // support when its M3 drop is available

    /// <summary>
    /// Managed PrintTicket provider class that inter-ops with unmanaged DDI driver
    /// </summary>
    internal abstract class PTProviderBase : IDisposable
    {
        #region Public Methods

        /// <summary>
        /// Creates a new PrintTicket provider instance for the given device.
        /// </summary>
        /// <param name="deviceName">name of printer device the provider should be bound to</param>
        /// <param name="maxVersion">max schema version supported by client</param>
        /// <param name="clientVersion">schema version requested by client</param>
        /// <exception cref="PrintQueueException">
        /// The PTProvider instance failed to bind to the specified printer.
        /// </exception>
        public static PTProviderBase Create(string deviceName, int maxVersion, int clientVersion) {
            PTProviderBase result = null;

            try
            {
                if (!TestHook._isFallbackPrintingEnabled)
                {
                    result = new PTProvider(deviceName, maxVersion, clientVersion);
                }
            }
            catch (PrintingNotSupportedException)
            {
                result = null;
            }

            if (result == null)
            {
                result = new FallbackPTProvider(deviceName, maxVersion, clientVersion);
            }

            return result;
        }

        /// <summary>
        /// Gets the PrintCapabilities relative to the given PrintTicket.
        /// </summary>
        /// <param name="printTicket">The stream that contains XML PrintTicket based on which PrintCapabilities should be built.</param>
        /// <returns>Stream that contains XML PrintCapabilities.</returns>
        public abstract MemoryStream GetPrintCapabilities(MemoryStream printTicket);

        /// <summary>
        /// Merges delta PrintTicket with base PrintTicket and then validates the merged PrintTicket.
        /// </summary>
        /// <param name="basePrintTicket">The MemoryStream that contains base XML PrintTicket.</param>
        /// <param name="deltaPrintTicket">The MemoryStream that contains delta XML PrintTicket.</param>
        /// <param name="scope">scope that delta PrintTicket and result PrintTicket will be limited to</param>
        /// <param name="conflictStatus">The returned conflict resolving status.</param>
        /// <returns>MemoryStream that contains validated and merged PrintTicket XML.</returns>
        public abstract MemoryStream MergeAndValidatePrintTicket(MemoryStream basePrintTicket,
                                                        MemoryStream deltaPrintTicket,
                                                        PrintTicketScope scope,
                                                        out ConflictStatus conflictStatus);

        /// <summary>
        /// Converts the given Win32 DEVMODE into PrintTicket.
        /// </summary>
        /// <param name="devMode">Byte buffer containing the Win32 DEVMODE.</param>
        /// <param name="scope">scope that the result PrintTicket will be limited to</param>
        /// <returns>MemoryStream that contains the converted XML PrintTicket.</returns>
        public abstract MemoryStream ConvertDevModeToPrintTicket(byte[] devMode, PrintTicketScope scope);

        /// <summary>
        /// Converts the given PrintTicket into Win32 DEVMODE.
        /// </summary>
        /// <param name="printTicket">MemoryStream containing the XML PrintTicket.</param>
        /// <param name="baseType">Type of default DEVMODE to use as base of conversion.</param>
        /// <param name="scope">scope that the input PrintTicket will be limited to</param>
        /// <returns>Byte buffer that contains the converted Win32 DEVMODE.</returns>
        public abstract byte[] ConvertPrintTicketToDevMode(MemoryStream printTicket, BaseDevModeType baseType, PrintTicketScope scope);

        #endregion Public Methods

        #region Protected Members

        /// <summary>
        /// Releases resources
        /// </summary>
        public abstract void Release();

        #endregion

        # region Dispose Pattern
        /// <summary>
        /// Implement Dispose pattern to release handle to print device which can't be released by GC in WOW64
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        #endregion
    }

    #endregion PTProviderBase class
}
