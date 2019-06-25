// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !DONOTREFPRINTINGASMMETA

using System;
using System.Printing.Interop;
using System.Printing;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Controls;

namespace MS.Internal.Printing
{
    internal partial class Win32PrintDialog
    {
        /// <summary>
        /// This class abstracts the Marshaling of the PrintDlgEx structure to/from
        /// Native memory for displaying the Win32 print dialog via the PrintDlgEx
        /// API.  The reason for this abstraction is to handle a known issue with
        /// this API.  The 32-bit and 64-bit versions of this method have different
        /// packing schemes for the input structure that need to be handled separately
        /// to function correctly depending on the CPU archetecture.
        /// </summary>
        private sealed class PrintDlgExMarshaler : IDisposable
        {
            #region Constructor

            /// <summary>
            /// Construct and initialize a PrintDlgExMarshaler instance.
            /// </summary>
            /// <param name="owner">
            /// A Win32 window handle to use as the parent of this dialog.
            /// </param>
            /// <param name="dialog">
            /// A reference to the Win32PrintDialog that contains the necessary
            /// data to display a Win32 Print Dialog via the PrintDlgEx call.
            /// </param>
            internal
            PrintDlgExMarshaler(
                IntPtr owner,
                Win32PrintDialog dialog
                )
            {
                _ownerHandle = owner;
                _dialog = dialog;
                _unmanagedPrintDlgEx = IntPtr.Zero;
            }

            #endregion Constructor

            #region Finalizer

            ~PrintDlgExMarshaler()
            {
                this.Dispose(true);
            }

            #endregion Finalizer

            #region Internal properties

            /// <summary>
            /// Gets an IntPtr that points to unmanaged memory that represents
            /// a PRINTDLGEX structure for calling into PrintDlgEx Win32 API.
            /// </summary>
            internal IntPtr UnmanagedPrintDlgEx
            {
                get
                {
                    return _unmanagedPrintDlgEx;
                }
            }

            #endregion Internal properties

            #region Internal methods

            /// <summary>
            /// This method synchronizes the internal PRINTDLGEX unmanaged data
            /// structure with the internal Win32 print dialog configuration
            /// parameters.  You call this prior to a call to PrintDlgEx Win32
            /// API to configure the unmanaged memory.
            /// </summary>
            internal
            UInt32
            SyncFromStruct()
            {
                if (_unmanagedPrintDlgEx == IntPtr.Zero)
                {
                    return NativeMethods.PD_RESULT_CANCEL;
                }

                UInt32 dialogResult = AcquireResultFromPrintDlgExStruct(_unmanagedPrintDlgEx);

                if ((dialogResult == NativeMethods.PD_RESULT_PRINT) ||
                     (dialogResult == NativeMethods.PD_RESULT_APPLY))
                {
                    IntPtr devModeHandle;
                    string printerName;
                    UInt32 flags;
                    PageRange pageRange;

                    ExtractPrintDataAndDevMode(
                        _unmanagedPrintDlgEx,
                        out printerName,
                        out flags,
                        out pageRange,
                        out devModeHandle);

                    _dialog.PrintQueue = AcquirePrintQueue(printerName);
                    _dialog.PrintTicket = AcquirePrintTicket(devModeHandle, printerName);

                    if ((flags & NativeMethods.PD_PAGENUMS) == NativeMethods.PD_PAGENUMS)
                    {
                        if (pageRange.PageFrom > pageRange.PageTo)
                        {
                            int temp = pageRange.PageTo;
                            pageRange.PageTo = pageRange.PageFrom;
                            pageRange.PageFrom = temp;
                        }

                        _dialog.PageRangeSelection = PageRangeSelection.UserPages;
                        _dialog.PageRange = pageRange;
                    }
                    else if ((flags & NativeMethods.PD_SELECTION) == NativeMethods.PD_SELECTION)
                    {
                        _dialog.PageRangeSelection = PageRangeSelection.SelectedPages;
                    }
                    else if ((flags & NativeMethods.PD_CURRENTPAGE) == NativeMethods.PD_CURRENTPAGE)
                    {
                        _dialog.PageRangeSelection = PageRangeSelection.CurrentPage;
                    }
                    else
                    {
                        _dialog.PageRangeSelection = PageRangeSelection.AllPages;
                    }
                }

                return dialogResult;
            }

            /// <summary>
            /// This method synchronizes the managed Win32 data with the PRINTDLGEX
            /// unmanaged data structure.  It is used after a successful call to the
            /// PrintDlgEx Win32 API.
            /// </summary>
            internal
            void
            SyncToStruct()
            {
                if (_unmanagedPrintDlgEx != IntPtr.Zero)
                {
                    FreeUnmanagedPrintDlgExStruct(_unmanagedPrintDlgEx);
                }

                //
                // If parent is not valid then get the desktop window handle.
                //
                if (_ownerHandle == IntPtr.Zero)
                {
                    _ownerHandle = MS.Win32.UnsafeNativeMethods.GetDesktopWindow();
                }

                //
                // Allocate an unmanaged PRINTDLGEX structure with our current internal settings.
                //
                _unmanagedPrintDlgEx = AllocateUnmanagedPrintDlgExStruct();
            }

            #endregion Internal methods

            #region Private helper methods

            /// <summary>
            /// Clean up any resources being used.
            /// </summary>
            /// <param name="disposing">
            /// true if managed resources should be disposed; otherwise, false.
            /// </param>
            private
            void
            Dispose(
                bool disposing
                )
            {
                if (disposing)
                {
                    if (_unmanagedPrintDlgEx != IntPtr.Zero)
                    {
                        FreeUnmanagedPrintDlgExStruct(_unmanagedPrintDlgEx);
                        _unmanagedPrintDlgEx = IntPtr.Zero;
                    }
                }
            }

            /// <summary>
            /// Extracts the printer name, flags, pageRange, and devmode handle from the
            /// given PRINTDLGEX structure.
            /// </summary>
            /// <param name="unmanagedBuffer">
            /// An unmanaged buffer representing the PRINTDLGEX structure.  This structure
            /// is passed around as an unmanaged buffer since the 32-bit and 64-bit versions
            /// of this buffer are not the same and need to be handled uniquely.
            /// </param>
            /// <param name="printerName">
            /// An out parameter to store store the printer name.
            /// </param>
            /// <param name="flags">
            /// The set of flags that are inside the PRINTDIALOGEX structure.
            /// </param>
            /// <param name="pageRange">
            /// The  user specified page range that is in PRINTDLGEX structure.
            /// This is only useful if the PD_PAGENUMS bit is set in the flags.
            /// </param>
            /// <param name="devModeHandle">
            /// An out parameter to store the devmode handle.
            /// </param>
            private
            void
            ExtractPrintDataAndDevMode(
                IntPtr unmanagedBuffer,
                out string printerName,
                out UInt32 flags,
                out PageRange pageRange,
                out IntPtr devModeHandle
                )
            {
                IntPtr devNamesHandle = IntPtr.Zero;
                IntPtr pageRangePtr = IntPtr.Zero;

                //
                // Extract the devmode and devnames handles from the appropriate PRINTDLGEX structure
                //
                if (!Is64Bit())
                {
                    NativeMethods.PRINTDLGEX32 pdex = (NativeMethods.PRINTDLGEX32)Marshal.PtrToStructure(
                        unmanagedBuffer,
                        typeof(NativeMethods.PRINTDLGEX32));
                    devModeHandle = pdex.hDevMode;
                    devNamesHandle = pdex.hDevNames;
                    flags = pdex.Flags;
                    pageRangePtr = pdex.lpPageRanges;
                }
                else
                {
                    NativeMethods.PRINTDLGEX64 pdex = (NativeMethods.PRINTDLGEX64)Marshal.PtrToStructure(
                        unmanagedBuffer,
                        typeof(NativeMethods.PRINTDLGEX64));
                    devModeHandle = pdex.hDevMode;
                    devNamesHandle = pdex.hDevNames;
                    flags = pdex.Flags;
                    pageRangePtr = pdex.lpPageRanges;
                }

                //
                // Get a managed copy of the page ranges.  This only matters if the PD_PAGENUMS bit is
                // set in the flags.
                //
                if (((flags & NativeMethods.PD_PAGENUMS) == NativeMethods.PD_PAGENUMS) &&
                     (pageRangePtr != IntPtr.Zero))
                {
                    NativeMethods.PRINTPAGERANGE pageRangeStruct = (NativeMethods.PRINTPAGERANGE)Marshal.PtrToStructure(
                            pageRangePtr,
                            typeof(NativeMethods.PRINTPAGERANGE));

                    pageRange = new PageRange((int)pageRangeStruct.nFromPage, (int)pageRangeStruct.nToPage);
                }
                else
                {
                    pageRange = new PageRange(1);
                }

                //
                // Get a managed copy of the device name
                //
                if (devNamesHandle != IntPtr.Zero)
                {
                    IntPtr pDevNames = IntPtr.Zero;
                    try
                    {
                        pDevNames = UnsafeNativeMethods.GlobalLock(devNamesHandle);

                        NativeMethods.DEVNAMES devNames = (NativeMethods.DEVNAMES)Marshal.PtrToStructure(
                            pDevNames,
                            typeof(NativeMethods.DEVNAMES));
                        int devNamesOffset = checked(devNames.wDeviceOffset * Marshal.SystemDefaultCharSize);
                        printerName = Marshal.PtrToStringAuto(pDevNames + devNamesOffset);
                    }
                    finally
                    {
                        if (pDevNames != IntPtr.Zero)
                        {
                            UnsafeNativeMethods.GlobalUnlock(devNamesHandle);
                        }
                    }
                }
                else
                {
                    printerName = string.Empty;
                }
            }

            /// <summary>
            /// Acquires an instance of the PrintQueue that cooresponds to the given printer name.
            /// </summary>
            /// <param name="printerName">
            /// The printer name to search for.
            /// </param>
            private
            PrintQueue
            AcquirePrintQueue(
                string printerName
                )
            {
                PrintQueue printQueue = null;

                EnumeratedPrintQueueTypes[] types = new EnumeratedPrintQueueTypes[] {
                    EnumeratedPrintQueueTypes.Local,
                    EnumeratedPrintQueueTypes.Connections
                };

                //
                // This forces us to acquire the cached version of the print queues.
                // This theoretically should prevent crashing in the printing system
                // since all it is doing is reading the registry.
                //
                PrintQueueIndexedProperty[] props = new PrintQueueIndexedProperty[] {
                    PrintQueueIndexedProperty.Name,
                    PrintQueueIndexedProperty.QueueAttributes
                };

                //
                // Get the PrintQueue instance for the printer
                //
                using (LocalPrintServer server = new LocalPrintServer())
                {
                    foreach (PrintQueue queue in server.GetPrintQueues(props, types))
                    {
                        if (printerName.Equals(queue.FullName, StringComparison.OrdinalIgnoreCase))
                        {
                            printQueue = queue;
                            break;
                        }
                    }
                }
                if (printQueue != null)
                {
                    printQueue.InPartialTrust = true;
                }

                return printQueue;
            }

            /// <summary>
            /// Acquires an instance of a PrintTicket given a handle to a DEVMODE and a printer name.
            /// </summary>
            /// <param name="devModeHandle">
            /// The DEVMODE handle to use for the PrintTicket.
            /// </param>
            /// <param name="printQueueName">
            /// The printer name for the PrintTicket converter.
            /// </param>
            private
            PrintTicket
            AcquirePrintTicket(
                IntPtr devModeHandle,
                string printQueueName
                )
            {
                PrintTicket printTicket = null;
                byte[] devModeData = null;

                //
                // Copy the devmode into a byte array
                //
                IntPtr pDevMode = IntPtr.Zero;
                try
                {
                    pDevMode = UnsafeNativeMethods.GlobalLock(devModeHandle);

                    NativeMethods.DEVMODE devMode = (NativeMethods.DEVMODE)Marshal.PtrToStructure(
                        pDevMode,
                        typeof(NativeMethods.DEVMODE));
                    devModeData = new byte[devMode.dmSize + devMode.dmDriverExtra];
                    Marshal.Copy(pDevMode, devModeData, 0, devModeData.Length);
                }
                finally
                {
                    if (pDevMode != IntPtr.Zero)
                    {
                        UnsafeNativeMethods.GlobalUnlock(devModeHandle);
                    }
                }

                //
                // Convert the devmode data to a PrintTicket object
                //
                using (PrintTicketConverter ptConverter = new PrintTicketConverter(
                            printQueueName,
                            PrintTicketConverter.MaxPrintSchemaVersion))
                {
                    printTicket = ptConverter.ConvertDevModeToPrintTicket(devModeData);
                }

                return printTicket;
            }

            /// <summary>
            /// Extracts the result value from a given PRINTDLGEX structure.
            /// </summary>
            /// <param name="unmanagedBuffer">
            /// An unmanaged buffer representing the PRINTDLGEX structure.  This structure
            /// is passed around as an unmanaged buffer since the 32-bit and 64-bit versions
            /// of this buffer are not the same and need to be handled uniquely.
            /// </param>
            private
            UInt32
            AcquireResultFromPrintDlgExStruct(
                IntPtr unmanagedBuffer
                )
            {
                UInt32 result = 0;

                //
                // Extract the devmode and devnames handles from the appropriate PRINTDLGEX structure
                //
                if (!Is64Bit())
                {
                    NativeMethods.PRINTDLGEX32 pdex = (NativeMethods.PRINTDLGEX32)Marshal.PtrToStructure(
                        unmanagedBuffer,
                        typeof(NativeMethods.PRINTDLGEX32));
                    result = pdex.dwResultAction;
                }
                else
                {
                    NativeMethods.PRINTDLGEX64 pdex = (NativeMethods.PRINTDLGEX64)Marshal.PtrToStructure(
                        unmanagedBuffer,
                        typeof(NativeMethods.PRINTDLGEX64));
                    result = pdex.dwResultAction;
                }

                return result;
            }

            /// <summary>
            /// Allocates an unmanaged buffer for the PRINTDLGEX structure and
            /// sets the initial values on it.
            ///
            /// NOTE:  The reason for returning an unmanaged pointer to a buffer
            /// and not just a structure that can be passed to the unmanaged APIs
            /// is because this class is providing an abstraction to a problem that
            /// exists in the unmanaged print world where the 32-bit and 64-bit
            /// structure packing is inconsistent.
            /// </summary>
            private
            IntPtr
            AllocateUnmanagedPrintDlgExStruct()
            {
                IntPtr unmanagedBuffer = IntPtr.Zero;

                NativeMethods.PRINTPAGERANGE range;
                range.nToPage = (uint)_dialog.PageRange.PageTo;
                range.nFromPage = (uint)_dialog.PageRange.PageFrom;

                uint defaultFlags =
                            NativeMethods.PD_ALLPAGES |
                            NativeMethods.PD_USEDEVMODECOPIESANDCOLLATE |
                            NativeMethods.PD_DISABLEPRINTTOFILE |
                            NativeMethods.PD_HIDEPRINTTOFILE;
                try
                {
                    if (!Is64Bit())
                    {
                        NativeMethods.PRINTDLGEX32 pdex = new NativeMethods.PRINTDLGEX32();
                        pdex.hwndOwner = _ownerHandle;
                        pdex.nMinPage = _dialog.MinPage;
                        pdex.nMaxPage = _dialog.MaxPage;
                        pdex.Flags = defaultFlags;

                        if (_dialog.SelectedPagesEnabled)
                        {
                            if (_dialog.PageRangeSelection == PageRangeSelection.SelectedPages)
                            {
                                pdex.Flags |= NativeMethods.PD_SELECTION;
                            }
                        }
                        else
                        {
                            pdex.Flags |= NativeMethods.PD_NOSELECTION;
                        }

                        if (_dialog.CurrentPageEnabled)
                        {
                            if (_dialog.PageRangeSelection == PageRangeSelection.CurrentPage)
                            {
                                pdex.Flags |= NativeMethods.PD_CURRENTPAGE;
                            }
                        }
                        else
                        {
                            pdex.Flags |= NativeMethods.PD_NOCURRENTPAGE;
                        }

                        if (_dialog.PageRangeEnabled)
                        {
                            pdex.lpPageRanges = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeMethods.PRINTPAGERANGE)));
                            pdex.nMaxPageRanges = 1;

                            if (_dialog.PageRangeSelection == PageRangeSelection.UserPages)
                            {
                                pdex.nPageRanges = 1;
                                Marshal.StructureToPtr(range, pdex.lpPageRanges, false);
                                pdex.Flags |= NativeMethods.PD_PAGENUMS;
                            }
                            else
                            {
                                pdex.nPageRanges = 0;
                            }
                        }
                        else
                        {
                            pdex.lpPageRanges = IntPtr.Zero;
                            pdex.nMaxPageRanges = 0;
                            pdex.Flags |= NativeMethods.PD_NOPAGENUMS;
                        }

                        //
                        // If we know a print queue and print ticket, then we need to try to select
                        // the printer in the dialog and use the print ticket.  We allocate and setup
                        // a DEVNAMES structure for this as well as convert the PrintTicket to a
                        // DEVMODE.
                        //
                        if (_dialog.PrintQueue != null)
                        {
                            pdex.hDevNames = AllocateAndInitializeDevNames(_dialog.PrintQueue.FullName);

                            if (_dialog.PrintTicket != null)
                            {
                                pdex.hDevMode = AllocateAndInitializeDevMode(
                                    _dialog.PrintQueue.FullName,
                                    _dialog.PrintTicket);
                            }
                        }

                        int cbBufferSize = Marshal.SizeOf(typeof(NativeMethods.PRINTDLGEX32));
                        unmanagedBuffer = Marshal.AllocHGlobal(cbBufferSize);
                        Marshal.StructureToPtr(pdex, unmanagedBuffer, false);
                    }
                    else
                    {
                        NativeMethods.PRINTDLGEX64 pdex = new NativeMethods.PRINTDLGEX64();
                        pdex.hwndOwner = _ownerHandle;
                        pdex.nMinPage = _dialog.MinPage;
                        pdex.nMaxPage = _dialog.MaxPage;
                        pdex.Flags = defaultFlags;

                        if (_dialog.SelectedPagesEnabled)
                        {
                            if (_dialog.PageRangeSelection == PageRangeSelection.SelectedPages)
                            {
                                pdex.Flags |= NativeMethods.PD_SELECTION;
                            }
                        }
                        else
                        {
                            pdex.Flags |= NativeMethods.PD_NOSELECTION;
                        }

                        if (_dialog.CurrentPageEnabled)
                        {
                            if (_dialog.PageRangeSelection == PageRangeSelection.CurrentPage)
                            {
                                pdex.Flags |= NativeMethods.PD_CURRENTPAGE;
                            }
                        }
                        else
                        {
                            pdex.Flags |= NativeMethods.PD_NOCURRENTPAGE;
                        }

                        if (_dialog.PageRangeEnabled)
                        {
                            pdex.lpPageRanges = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeMethods.PRINTPAGERANGE)));
                            pdex.nMaxPageRanges = 1;

                            if (_dialog.PageRangeSelection == PageRangeSelection.UserPages)
                            {
                                pdex.nPageRanges = 1;
                                Marshal.StructureToPtr(range, pdex.lpPageRanges, false);
                                pdex.Flags |= NativeMethods.PD_PAGENUMS;
                            }
                            else
                            {
                                pdex.nPageRanges = 0;
                            }
                        }
                        else
                        {
                            pdex.lpPageRanges = IntPtr.Zero;
                            pdex.nMaxPageRanges = 0;
                            pdex.Flags |= NativeMethods.PD_NOPAGENUMS;
                        }

                        //
                        // If we know a print queue and print ticket, then we need to try to select
                        // the printer in the dialog and use the print ticket.  We allocate and setup
                        // a DEVNAMES structure for this as well as convert the PrintTicket to a
                        // DEVMODE.
                        //
                        if (_dialog.PrintQueue != null)
                        {
                            pdex.hDevNames = AllocateAndInitializeDevNames(_dialog.PrintQueue.FullName);

                            if (_dialog.PrintTicket != null)
                            {
                                pdex.hDevMode = AllocateAndInitializeDevMode(
                                    _dialog.PrintQueue.FullName,
                                    _dialog.PrintTicket);
                            }
                        }

                        int cbBufferSize = Marshal.SizeOf(typeof(NativeMethods.PRINTDLGEX64));
                        unmanagedBuffer = Marshal.AllocHGlobal(cbBufferSize);
                        Marshal.StructureToPtr(pdex, unmanagedBuffer, false);
                    }
                }
                catch (Exception)
                {
                    if (unmanagedBuffer != null)
                    {
                        FreeUnmanagedPrintDlgExStruct(unmanagedBuffer);
                        unmanagedBuffer = IntPtr.Zero;
                    }

                    throw;
                }

                return unmanagedBuffer;
            }

            /// <summary>
            /// Frees an unmanaged buffer.
            /// </summary>
            /// <param name="unmanagedBuffer">
            /// An unmanaged buffer representing the PRINTDLGEX structure.  This structure
            /// is passed around as an unmanaged buffer since the 32-bit and 64-bit versions
            /// of this buffer are not the same and need to be handled uniquely.
            /// </param>
            private
            void
            FreeUnmanagedPrintDlgExStruct(
                IntPtr unmanagedBuffer
                )
            {
                if (unmanagedBuffer == IntPtr.Zero)
                {
                    return;
                }

                IntPtr devModeHandle = IntPtr.Zero;
                IntPtr devNamesHandle = IntPtr.Zero;
                IntPtr pageRangePtr = IntPtr.Zero;

                //
                // Extract the devmode and devnames handles from the appropriate PRINTDLGEX structure
                //
                if (!Is64Bit())
                {
                    NativeMethods.PRINTDLGEX32 pdex = (NativeMethods.PRINTDLGEX32)Marshal.PtrToStructure(
                        unmanagedBuffer,
                        typeof(NativeMethods.PRINTDLGEX32));
                    devModeHandle = pdex.hDevMode;
                    devNamesHandle = pdex.hDevNames;
                    pageRangePtr = pdex.lpPageRanges;
                }
                else
                {
                    NativeMethods.PRINTDLGEX64 pdex = (NativeMethods.PRINTDLGEX64)Marshal.PtrToStructure(
                        unmanagedBuffer,
                        typeof(NativeMethods.PRINTDLGEX64));
                    devModeHandle = pdex.hDevMode;
                    devNamesHandle = pdex.hDevNames;
                    pageRangePtr = pdex.lpPageRanges;
                }

                if (devModeHandle != IntPtr.Zero)
                {
                    UnsafeNativeMethods.GlobalFree(devModeHandle);
                }

                if (devNamesHandle != IntPtr.Zero)
                {
                    UnsafeNativeMethods.GlobalFree(devNamesHandle);
                }

                if (pageRangePtr != IntPtr.Zero)
                {
                    UnsafeNativeMethods.GlobalFree(pageRangePtr);
                }

                Marshal.FreeHGlobal(unmanagedBuffer);
            }

            /// <summary>
            /// Returns a boolean value representing whether the current runtime is
            /// 32-bit or 64-bit.
            /// </summary>
            private
            bool
            Is64Bit()
            {
                IntPtr temp = IntPtr.Zero;
                return Marshal.SizeOf(temp) == 8;
            }

            /// <summary>
            /// This method allocates a DEVNAMES structure in unmanaged code and configures
            /// it to point to the specified printer.
            /// </summary>
            /// <param name="printerName">
            /// The printer name to use for the DEVNAMES structure.
            /// </param>
            /// <returns>
            /// Returns an IntPtr pointing to a memory address in unmanaged code where
            /// the structure has been initialized.
            /// </returns>
            private
            IntPtr
            AllocateAndInitializeDevNames(
                string printerName
                )
            {
                IntPtr hDevNames = IntPtr.Zero;
                char[] printer = printerName.ToCharArray();

                //
                // Enough memory for the printer name character array + 3 null characters
                // + the DEVNAMES structure.  The 3 null characters are for the end of the
                // printer name string, and 2 distinct empty strings.
                //
                int cbDevNames = checked(((printer.Length + 3) * Marshal.SystemDefaultCharSize) +
                    Marshal.SizeOf(typeof(NativeMethods.DEVNAMES)));
                hDevNames = Marshal.AllocHGlobal(cbDevNames);

                ushort baseOffset = (ushort)Marshal.SizeOf(typeof(NativeMethods.DEVNAMES));

                //
                // The wDeviceOffset contains an offset in characters to the printer name
                // within the DEVNAMES structure.  The wDriverOffset and wOutputOffset
                // are offsets to two distinct empty strings that just happen to follow
                // the printer name string.  For more information on how this structure is
                // layed out, please review the MSDN documentation for DEVNAMES structure.
                //
                NativeMethods.DEVNAMES devNames;
                devNames.wDeviceOffset = (ushort)(baseOffset / Marshal.SystemDefaultCharSize);
                devNames.wDriverOffset = checked((ushort)(devNames.wDeviceOffset + printer.Length + 1));
                devNames.wOutputOffset = checked((ushort)(devNames.wDriverOffset + 1));
                devNames.wDefault = 0;
                Marshal.StructureToPtr(devNames, hDevNames, false);

                //
                // Calculate the position of the string containing the printer name
                //   - Printer name character array immediately follows the DEVNAMES structure
                //   - followed by 3 NULL characters
                //
                IntPtr offsetName =
                    checked((IntPtr)((long)hDevNames + (long)baseOffset));
                IntPtr offsetNull =
                    checked((IntPtr)((long)offsetName + (printer.Length * Marshal.SystemDefaultCharSize)));

                //
                // Write the printer name and 3 NULL characters.  The first NULL character
                // is to null terminate our printer name string.  The second and third null
                // characters are to create to distinct empty strings within the buffer.
                //
                // NOTE: The byte array contains the NULL characters in a form that we are
                // able to write them out.  There are 3 * char size bytes total.
                //
                byte[] nulls = new byte[3 * Marshal.SystemDefaultCharSize];
                Array.Clear(nulls, 0, nulls.Length);
                Marshal.Copy(
                    printer,
                    0,
                    offsetName,
                    printer.Length);
                Marshal.Copy(
                    nulls,
                    0,
                    offsetNull,
                    nulls.Length);

                return hDevNames;
            }

            /// <summary>
            /// This method allocates and initializes an unmanaged DEVMODE structure
            /// for use to by the PrintDlgEx method based on the specified printer
            /// and PrintTicket object.
            /// </summary>
            /// <param name="printerName">
            /// The printer that is being targetted by the PrintTicket.
            /// </param>
            /// <param name="printTicket">
            /// The PrintTicket that will be converted to a DEVMODE.
            /// </param>
            /// <returns>
            /// An unmanaged pointer to an unmanaged DEVMODE structure that can be
            /// used in the PRINTDLGEX structure for a call to PrintDlgEx.
            /// </returns>
            private
            IntPtr
            AllocateAndInitializeDevMode(
                string printerName,
                PrintTicket printTicket
                )
            {
                byte[] devModeData = null;

                //
                // Convert the PrintTicket object to a DEVMODE
                //
                using (PrintTicketConverter ptConverter = new PrintTicketConverter(
                            printerName,
                            PrintTicketConverter.MaxPrintSchemaVersion))
                {
                    devModeData = ptConverter.ConvertPrintTicketToDevMode(
                        printTicket,
                        BaseDevModeType.UserDefault);
                }

                //
                // Make the dev mode data a DEVMODE structure in global memory
                //
                IntPtr hDevMode = Marshal.AllocHGlobal(devModeData.Length);
                Marshal.Copy(devModeData, 0, hDevMode, devModeData.Length);

                return hDevMode;
            }

            #endregion Private helper methods

            #region Private data

            private
            Win32PrintDialog _dialog;

            private
            IntPtr _unmanagedPrintDlgEx;

            private
            IntPtr _ownerHandle;

            #endregion Private data

            #region IDisposable implementation

            /// <summary>
            /// Implements the Dispose method for IDisposable.  This ensures that
            /// any unmanaged PRINTDLGEX structure that was allocated by the class
            /// will be property freed when this class goes away.
            /// </summary>
            public
            void
            Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            #endregion IDisposable implementation
        }
    }
}
#endif
