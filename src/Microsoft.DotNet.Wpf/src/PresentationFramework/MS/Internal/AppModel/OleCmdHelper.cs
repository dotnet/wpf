// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//          This is a helper class used for interop to process the
//          IOleCommandTarget calls in browser hosting scenario
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

using System.Windows.Threading;
using System.Windows;
using System.Security;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Controls;

using MS.Internal.Documents;                               // DocumentApplicationDocumentViewer
using MS.Internal.PresentationFramework;                   // SecurityHelper
using MS.Internal.KnownBoxes;
using MS.Win32;

namespace MS.Internal.AppModel
{
    #region OleCmdHelper class
    // <summary>
    // OleCmd helper class for processing IOleCommandTarget calls in browser hosting scenario
    // </summary>
    internal sealed class OleCmdHelper : MarshalByRefObject
    {
        internal const int
            OLECMDERR_E_NOTSUPPORTED = unchecked((int)0x80040100),
            OLECMDERR_E_DISABLED = unchecked((int)0x80040101),
            OLECMDERR_E_UNKNOWNGROUP = unchecked((int)0x80040104);
        internal const uint CommandUnsupported = 0;
        internal const uint CommandEnabled = (uint)(UnsafeNativeMethods.OLECMDF.OLECMDF_ENABLED | UnsafeNativeMethods.OLECMDF.OLECMDF_SUPPORTED);
        internal const uint CommandDisabled = (uint)UnsafeNativeMethods.OLECMDF.OLECMDF_SUPPORTED;

        // IMPORTANT: Keep this in sync with wcp\host\inc\hostservices.idl
        internal static readonly Guid CGID_ApplicationCommands = new Guid(0xebbc8a63, 0x8559, 0x4892, 0x97, 0xa8, 0x31, 0xe9, 0xb0, 0xe9, 0x85, 0x91);
        internal static readonly Guid CGID_EditingCommands = new Guid(0xc77ce45, 0xd1c, 0x4f2a, 0xb2, 0x93, 0xed, 0xd5, 0xe2, 0x7e, 0xba, 0x47);

        internal OleCmdHelper()
        {
        }

        /// <remarks>
        /// The native code passes queries here only for the recognized command groups:
        /// standard (NULL), ApplicaitonCommands, EditingCommands.
        /// </remarks>
        internal void QueryStatus(Guid guidCmdGroup, uint cmdId, ref uint flags)
        {
            /***IMPORTANT:
              Make sure to return allowed and appropriate values according to the specification of 
              IOleCommandTarget::QueryStatus(). In particular:
                - OLECMDF_SUPPORTED without OLECMDF_ENABLED should not be blindly returned for 
                    unrecognized commands.
                - Some code in IE treats OLECMDERR_E_xxx differently from generic failures.
                - E_NOTIMPL is not an acceptable return value.
            */

            if (Application.Current == null || Application.IsShuttingDown == true)
            {
                Marshal.ThrowExceptionForHR(NativeMethods.E_FAIL);
            }

            // Get values from mapping here else mark them as disabled ==>
            // i.e "supported but not enabled" and is the equivalent of disabled since
            // there is no explicit "disabled" OLECMD flag

            IDictionary oleCmdMappingTable = GetOleCmdMappingTable(guidCmdGroup);
            if (oleCmdMappingTable == null)
            {
                Marshal.ThrowExceptionForHR(OleCmdHelper.OLECMDERR_E_UNKNOWNGROUP);
            }
            CommandWithArgument command = oleCmdMappingTable[cmdId] as CommandWithArgument;
            if (command == null)
            {
                flags = CommandUnsupported;
                return;
            }
            // Go through the Dispatcher in order to use its SynchronizationContext and also 
            // so that any application exception caused during event routing is reported via 
            // Dispatcher.UnhandledException.
            // The above code is not in the callback, because it throws, and we don't want the
            // application to get these exceptions. (The COM Interop layer turns them into HRESULTs.)
            bool enabled = (bool)Application.Current.Dispatcher.Invoke(
                DispatcherPriority.Send, new DispatcherOperationCallback(QueryEnabled), command);
            flags = enabled ? CommandEnabled : CommandDisabled;
        }

        private object QueryEnabled(object command)
        {
            if (Application.Current.MainWindow == null)
                return false;
            IInputElement target = FocusManager.GetFocusedElement(Application.Current.MainWindow);
            if (target == null)
            {
                // This will always succeed because Window is IInputElement
                target = (IInputElement)Application.Current.MainWindow;
            }
            return BooleanBoxes.Box(((CommandWithArgument)command).QueryEnabled(target, null));
        }

        /// <remarks>
        /// The native code passes here only commands of the recognized command groups:
        /// standard (NULL), ApplicaitonCommands, EditingCommands.
        /// </remarks>
        internal void ExecCommand(Guid guidCmdGroup, uint commandId, object arg)
        {
            if (Application.Current == null || Application.IsShuttingDown == true)
            {
                Marshal.ThrowExceptionForHR(NativeMethods.E_FAIL);
            }

            int hresult = (int)Application.Current.Dispatcher.Invoke(
                DispatcherPriority.Send,
                new DispatcherOperationCallback(ExecCommandCallback),
                new object[] { guidCmdGroup, commandId, arg });
            // Note: ExecCommandCallback() returns an HRESULT instead of throwing for the reason
            // explained in QueryStatus().
            if (hresult < 0)
            {
                Marshal.ThrowExceptionForHR(hresult);
            }
        }

        private object ExecCommandCallback(object arguments)
        {
            object[] args = (object[])arguments;
            Invariant.Assert(args.Length == 3);
            Guid guidCmdGroup = (Guid)args[0];
            uint commandId = (uint)args[1];
            object arg = args[2];

            IDictionary oleCmdMappingTable = GetOleCmdMappingTable(guidCmdGroup);
            if (oleCmdMappingTable == null)
                return OLECMDERR_E_UNKNOWNGROUP;
            CommandWithArgument command = oleCmdMappingTable[commandId] as CommandWithArgument;
            if (command == null)
                return OLECMDERR_E_NOTSUPPORTED;

            if (Application.Current.MainWindow == null)
                return OLECMDERR_E_DISABLED;
            IInputElement target = FocusManager.GetFocusedElement(Application.Current.MainWindow);
            if (target == null)
            {
                // This will always succeed because Window is IInputElement
                target = (IInputElement)Application.Current.MainWindow;
            }
            return command.Execute(target, arg) ? NativeMethods.S_OK : OLECMDERR_E_DISABLED;
        }

        private IDictionary GetOleCmdMappingTable(Guid guidCmdGroup)
        {
            IDictionary mappingTable = null;

            if (guidCmdGroup.Equals(CGID_ApplicationCommands))
            {
                EnsureApplicationCommandsTable();
                mappingTable = _applicationCommandsMappingTable.Value;
            }
            else if (guidCmdGroup.Equals(Guid.Empty))
            {
                EnsureOleCmdMappingTable();
                mappingTable = _oleCmdMappingTable.Value;
            }
            else if (guidCmdGroup.Equals(CGID_EditingCommands))
            {
                EnsureEditingCommandsTable();
                mappingTable = _editingCommandsMappingTable.Value;
            }

            return mappingTable;
        }
        private void EnsureOleCmdMappingTable()
        {
            if (_oleCmdMappingTable.Value == null)
            {
                _oleCmdMappingTable.Value = new SortedList(10);

                //Add applevel commands here
                _oleCmdMappingTable.Value.Add((uint)UnsafeNativeMethods.OLECMDID.OLECMDID_SAVE, new CommandWithArgument(ApplicationCommands.Save));
                _oleCmdMappingTable.Value.Add((uint)UnsafeNativeMethods.OLECMDID.OLECMDID_SAVEAS, new CommandWithArgument(ApplicationCommands.SaveAs));
                _oleCmdMappingTable.Value.Add((uint)UnsafeNativeMethods.OLECMDID.OLECMDID_PRINT, new CommandWithArgument(ApplicationCommands.Print));
                _oleCmdMappingTable.Value.Add((uint)UnsafeNativeMethods.OLECMDID.OLECMDID_CUT, new CommandWithArgument(ApplicationCommands.Cut));
                _oleCmdMappingTable.Value.Add((uint)UnsafeNativeMethods.OLECMDID.OLECMDID_COPY, new CommandWithArgument(ApplicationCommands.Copy));
                _oleCmdMappingTable.Value.Add((uint)UnsafeNativeMethods.OLECMDID.OLECMDID_PASTE, new CommandWithArgument(ApplicationCommands.Paste));
                _oleCmdMappingTable.Value.Add((uint)UnsafeNativeMethods.OLECMDID.OLECMDID_PROPERTIES, new CommandWithArgument(ApplicationCommands.Properties));

                //Set the Enabled property of Stop and Refresh commands correctly
                _oleCmdMappingTable.Value.Add((uint)UnsafeNativeMethods.OLECMDID.OLECMDID_REFRESH, new CommandWithArgument(NavigationCommands.Refresh));
                _oleCmdMappingTable.Value.Add((uint)UnsafeNativeMethods.OLECMDID.OLECMDID_STOP, new CommandWithArgument(NavigationCommands.BrowseStop));
            }
        }

        private void EnsureApplicationCommandsTable()
        {
            if (_applicationCommandsMappingTable.Value == null)
            {
                /* we want to possible add 26 entries, so the capacity should be
                 * 26/0.72 = 19 for default of 1.0 load factor*/
                _applicationCommandsMappingTable.Value = new Hashtable(19);

                //Add applevel commands here
                // Note: The keys are added as uint type so that the default container comparer works
                // when we try to look up a command by a uint cmdid.
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.Edit_Cut, new CommandWithArgument(ApplicationCommands.Cut));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.Edit_Copy, new CommandWithArgument(ApplicationCommands.Copy));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.Edit_Paste, new CommandWithArgument(ApplicationCommands.Paste));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.Edit_SelectAll, new CommandWithArgument(ApplicationCommands.SelectAll));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.Edit_Find, new CommandWithArgument(ApplicationCommands.Find));

                // Add standard navigation commands
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Refresh, new CommandWithArgument(NavigationCommands.Refresh));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Stop, new CommandWithArgument(NavigationCommands.BrowseStop));

                // add document viewer commands
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.Edit_Digitalsignatures_SignDocument, new CommandWithArgument(DocumentApplicationDocumentViewer.Sign));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.Edit_Digitalsignatures_RequestSignature, new CommandWithArgument(DocumentApplicationDocumentViewer.RequestSigners));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.Edit_Digitalsignatures_ViewSignature, new CommandWithArgument(DocumentApplicationDocumentViewer.ShowSignatureSummary));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.Edit_Permission_Set, new CommandWithArgument(DocumentApplicationDocumentViewer.ShowRMPublishingUI));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.Edit_Permission_View, new CommandWithArgument(DocumentApplicationDocumentViewer.ShowRMPermissions));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.Edit_Permission_Restrict, new CommandWithArgument(DocumentApplicationDocumentViewer.ShowRMCredentialManager));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Zoom_In, new CommandWithArgument(NavigationCommands.IncreaseZoom));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Zoom_Out, new CommandWithArgument(NavigationCommands.DecreaseZoom));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Zoom_400, new CommandWithArgument(NavigationCommands.Zoom, 400));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Zoom_250, new CommandWithArgument(NavigationCommands.Zoom, 250));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Zoom_150, new CommandWithArgument(NavigationCommands.Zoom, 150));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Zoom_100, new CommandWithArgument(NavigationCommands.Zoom, 100));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Zoom_75, new CommandWithArgument(NavigationCommands.Zoom, 75));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Zoom_50, new CommandWithArgument(NavigationCommands.Zoom, 50));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Zoom_25, new CommandWithArgument(NavigationCommands.Zoom, 25));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Zoom_PageWidth, new CommandWithArgument(DocumentViewer.FitToWidthCommand));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Zoom_WholePage, new CommandWithArgument(DocumentViewer.FitToHeightCommand));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Zoom_TwoPages, new CommandWithArgument(DocumentViewer.FitToMaxPagesAcrossCommand, 2));
                _applicationCommandsMappingTable.Value.Add((uint)AppCommands.View_Zoom_Thumbnails, new CommandWithArgument(DocumentViewer.ViewThumbnailsCommand));
            }
        }

        private void EnsureEditingCommandsTable()
        {
            if (_editingCommandsMappingTable.Value == null)
            {
                _editingCommandsMappingTable.Value = new SortedList(2);
                // Note: The keys are added as uint type so that the default container comparer works
                // when we try to look up a command by a uint cmdid.
                _editingCommandsMappingTable.Value.Add((uint)EditingCommandIds.Backspace,
                    new CommandWithArgument(System.Windows.Documents.EditingCommands.Backspace));
                _editingCommandsMappingTable.Value.Add((uint)EditingCommandIds.Delete,
                    new CommandWithArgument(System.Windows.Documents.EditingCommands.Delete));
            }
        }

        private SecurityCriticalDataForSet<SortedList> _oleCmdMappingTable;
        private SecurityCriticalDataForSet<Hashtable> _applicationCommandsMappingTable;
        private SecurityCriticalDataForSet<SortedList> _editingCommandsMappingTable;
    }
    #endregion OleCmdHelper class

    #region CommandAndArgument class

    /// <summary>
    /// This wrapper class helps store default arguments for commands.
    /// The primary scenario for this class is the Zoom command where we
    /// have multiple menu items and want to fire a single event with an
    /// argument.  We cannot attach an argument value to the native menu
    /// item so when we do the translation we add it.
    /// </summary>
    internal class CommandWithArgument
    {
        public CommandWithArgument(RoutedCommand command) : this(command, null)
        { }

        public CommandWithArgument(RoutedCommand command, object argument)
        {
            _command = new SecurityCriticalDataForSet<RoutedCommand>(command);
            _argument = argument;
        }

        public bool Execute(IInputElement target, object argument)
        {
            if (argument == null)
            {
                argument = _argument;
            }

            // ISecureCommand is used to enforce user-initiated invocation. Cut, Copy and Paste
            // are marked as such. See ApplicationCommands.GetRequiredPermissions.
            if (_command.Value is ISecureCommand)
            {
                bool unused;
                if (_command.Value.CriticalCanExecute(argument, target, /* trusted: */ true, out unused))
                {
                    _command.Value.ExecuteCore(argument, target, /* userInitiated: */ true);
                    return true;
                }
                return false;
            }
            if (_command.Value.CanExecute(argument, target))
            {
                _command.Value.Execute(argument, target);
                return true;
            }
            return false;
        }


        public bool QueryEnabled(IInputElement target, object argument)
        {
            if (argument == null)
            {
                argument = _argument;
            }

            // ISecureCommand is used to enforce user-initiated invocation. Cut, Copy and Paste
            // are marked as such. See ApplicationCommands.GetRequiredPermissions.
            if (_command.Value is ISecureCommand)
            {
                bool unused;
                return _command.Value.CriticalCanExecute(argument, target, /* trusted: */ true, out unused);
            }
            return _command.Value.CanExecute(argument, target);
        }

        public RoutedCommand Command
        {
            get
            {
                return _command.Value;
            }
        }

        private object _argument;

        private SecurityCriticalDataForSet<RoutedCommand> _command;
    }

    #endregion
}
