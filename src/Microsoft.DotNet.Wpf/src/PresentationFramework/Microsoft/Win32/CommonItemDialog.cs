// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              CommonItemDialog is an abstract class derived from CommonDialog
//              that implements shared functionality common to all IFileDialog
//              variants. It provides a common options storage and events handling.
//

namespace Microsoft.Win32
{
    using MS.Internal;
    using MS.Internal.AppModel;
    using MS.Internal.Interop;
    using MS.Win32;

    using System;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;

    using HRESULT = MS.Internal.Interop.HRESULT;

    /// <summary>
    ///    Provides a common base class for wrappers around both the
    ///    File Open and File Save common dialog boxes.  Derives from
    ///    CommonDialog.
    ///
    ///    This class is not intended to be derived from except by
    ///    the OpenFileDialog and SaveFileDialog classes.
    /// </summary>
    public abstract class CommonItemDialog : CommonDialog
    {
        //---------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------
        #region Constructors

        /// <summary>
        /// In an inherited class, initializes a new instance of 
        /// the System.Windows.CommonItemDialog class.
        /// </summary>
        private protected CommonItemDialog()
        {
            // Call Initialize to set defaults for fields
            // and to set defaults for some option flags.
            // Initialize() is also called from the virtual
            // Reset() function to restore defaults.
            Initialize();
        }

        #endregion Constructors

        //---------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------
        #region Public Methods

        /// <summary>
        ///  Resets all properties to their default values.
        ///  Classes derived from CommonItemDialog are expected to 
        ///  call Base.Reset() at the beginning of their
        ///  implementation of Reset() if they choose to
        ///  override this function.
        /// </summary>
        public override void Reset()
        {
            Initialize();
        }

        /// <summary>
        ///  Returns a string representation of the dialog with key information
        ///  for debugging purposes.
        /// </summary>
        //   We overload ToString() so that we can provide a useful representation of
        //   this object for users' debugging purposes.
        public override string ToString()
        {
            return base.ToString() + ": Title: " + Title;
        }

        #endregion Public Methods

        //---------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------
        #region Public Properties

        //   FOS_DONTADDTORECENT
        //   Do not add the item being opened or saved to the recent documents list (SHAddToRecentDocs).
        //
        /// <summary>
        ///  Gets or sets a value indicating whether the dialog box will add the item
        ///  being opened or saved to the recent documents list.
        /// </summary>
        public bool AddToRecent
        {
            get
            {
                return !GetOption(FOS.DONTADDTORECENT);
            }
            set
            {

                SetOption(FOS.DONTADDTORECENT, !value);
            }
        }

        /// <summary>
        ///  Gets or sets a Guid to associate with the dialog's persisted state.
        /// </summary>
        public Guid? ClientGuid { get; set; }

        /// <summary>
        ///  Gets or sets the directory displayed by the file dialog box
        ///  if there is not a recently used directory value available.
        /// </summary>
        public string DefaultDirectory
        {
            get
            {
                // Avoid returning a null string - return String.Empty instead.
                return _defaultDirectory.Value == null ? String.Empty : _defaultDirectory.Value;
            }
            set
            {

                _defaultDirectory.Value = value;
            }
        }

        //   The actual flag is FOS_NODEREFERENCELINKS (set = do not dereference, unset = deref) - 
        //   while we have true = dereference and false=do not dereference.  Because we expose
        //   the opposite of the Windows flag as a property to be clearer, we need to negate 
        //   the value in both the getter and the setter here.
        /// <summary>
        ///  Gets or sets a value indicating whether the dialog box returns the location 
        ///  of the file referenced by the shortcut or whether it returns the location 
        ///  of the shortcut (.lnk). Not all dialogs allow users to select shortcuts.
        /// </summary>
        public bool DereferenceLinks
        {
            get
            {
                return !GetOption(FOS.NODEREFERENCELINKS);
            }
            set
            {

                SetOption(FOS.NODEREFERENCELINKS, !value);
            }
        }

        /// <summary>
        ///  Gets or sets the initial directory displayed by the file dialog box.
        /// </summary>
        public string InitialDirectory
        {
            get
            {
                // Avoid returning a null string - return String.Empty instead.
                return _initialDirectory.Value == null ? String.Empty : _initialDirectory.Value;
            }
            set
            {

                _initialDirectory.Value = value;
            }
        }

        /// <summary>
        ///  Gets or sets the directory displayed as the navigation root for the dialog.
        ///  Items in the navigation pane are replaced with the specified item, to guide the user
        ///  from navigating outside of the namespace.
        /// </summary>
        public string RootDirectory
        {
            get
            {
                // Avoid returning a null string - return String.Empty instead.
                return _rootDirectory.Value == null ? String.Empty : _rootDirectory.Value;
            }
            set
            {

                _rootDirectory.Value = value;
            }
        }

        //   FOS_FORCESHOWHIDDEN
        //   Include hidden and system items.
        //
        /// <summary>
        ///  Gets or sets a value indicating whether the dialog box will show
        ///  hidden and system items regardless of user preferences.
        /// </summary>
        public bool ShowHiddenItems
        {
            get
            {
                return GetOption(FOS.FORCESHOWHIDDEN);
            }
            set
            {

                SetOption(FOS.FORCESHOWHIDDEN, value);
            }
        }

        /// <summary>
        ///       Gets or sets a string shown in the title bar of the file dialog.
        ///       If this property is null, a localized default from the operating
        ///       system itself will be used (typically something like "Save As" or "Open")
        /// </summary>
        public string Title
        {
            get
            {
                // Avoid returning a null string - return String.Empty instead.
                return _title.Value == null ? String.Empty : _title.Value;
            }
            set
            {

                _title.Value = value;
            }
        }

        //   If false, the file dialog boxes will allow invalid characters in the returned file name. 
        //   We are actually responsible for dealing with this flag - it determines whether all of the
        //   processing in ProcessFileNames (which includes things such as the AddExtension feature)
        //   occurs.
        /// <summary>
        ///  Gets or sets a value indicating whether to check for situations that would prevent
        ///  an application from opening the selected file, such as sharing violations or access denied errors.
        /// </summary>
        public bool ValidateNames
        {
            get
            {
                return !GetOption(FOS.NOVALIDATE);
            }
            set
            {

                SetOption(FOS.NOVALIDATE, !value);
            }
        }

        public IList<FileDialogCustomPlace> CustomPlaces { get; set; }

        #endregion Public Properties

        //---------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------
        #region Protected Methods

        /// <summary>
        /// Handles the IFileDialogEvents.OnFileOk callback.
        /// </summary>
        protected virtual void OnItemOk(CancelEventArgs e) { }

        //  Because this class, CommonItemDialog, is the parent class for OpenFileDialog
        //  SaveFileDialog and OpenFolderDialog, this function will perform the common setup tasks
        //  shared between the dialogs.
        //
        /// <summary>
        /// Performs initialization work in preparation
        /// to show a file open, file save or folder open dialog box.
        /// </summary>
        protected override bool RunDialog(IntPtr hwndOwner)
        {
            IFileDialog dialog = CreateDialog();

            PrepareDialog(dialog);

            using (VistaDialogEvents events = new VistaDialogEvents(dialog, HandleItemOk))
            {
                return dialog.Show(hwndOwner).Succeeded;
            }
        }

        #endregion Protected Methods

        //---------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------
        #region Internal Methods

        /// <summary>
        ///  Returns the state of the given options flag.
        /// </summary>
        internal bool GetOption(FOS option)
        {
            return (_dialogOptions.Value & option) != 0;
        }

        /// <summary>
        ///     Sets the given option to the given boolean value.
        /// </summary>
        internal void SetOption(FOS option, bool value)
        {
            if (value)
            {
                // if value is true, bitwise OR the option with _dialogOptions
                _dialogOptions.Value |= option;
            }
            else
            {
                // if value is false, AND the bitwise complement of the 
                // option with _dialogOptions
                _dialogOptions.Value &= ~option;
            }
        }

        /// <summary>
        ///  Prompts the user with a System.Windows.MessageBox
        ///  with the given parameters. It also ensures that
        ///  the focus is set back on the window that had
        ///  the focus to begin with (before we displayed
        ///  the MessageBox).
        ///
        ///  Returns the choice the user made in the message box
        ///  (true if MessageBoxResult.Yes,
        ///   false if OK or MessageBoxResult.No)
        /// 
        ///  We have to do this instead of just calling MessageBox because
        ///  of an issue where keyboard navigation would fail after showing
        ///  a message box.  See http://bugcheck/default.asp?URL=/Bugs/URT/84016.asp
        ///  (WinForms ASURT 80262)
        /// </summary>
        internal bool MessageBoxWithFocusRestore(string message,
                         MessageBoxButton buttons,
                         MessageBoxImage image)
        {
            bool ret = false;

            // Get the window that currently has focus and temporarily cache a handle to it
            IntPtr focusHandle = UnsafeNativeMethods.GetFocus();

            try
            {
                // Show the message box and compare the return value to MessageBoxResult.Yes to get the
                // actual return value.
                ret = (MessageBox.Show(message, DialogCaption, buttons, image, MessageBoxResult.OK /*default button is OK*/, 0)
                       ==
                       MessageBoxResult.Yes);
            }
            finally
            {
                // Return focus to the window that had focus before we showed the messagebox.
                // SetFocus can handle improper hwnd values, including null.
                UnsafeNativeMethods.SetFocus(new HandleRef(this, focusHandle));
            }
            return ret;
        }

        #endregion Internal Methods

        #region Internal and Protected Methods

        private protected abstract IFileDialog CreateDialog();

        private protected virtual void PrepareDialog(IFileDialog dialog)
        {
            if (ClientGuid is Guid guid)
            {
                dialog.SetClientGuid(ref guid);
            }

            if (!string.IsNullOrEmpty(DefaultDirectory))
            {
                IShellItem defaultDirectory = ShellUtil.GetShellItemForPath(DefaultDirectory);
                if (defaultDirectory != null)
                {
                    dialog.SetDefaultFolder(defaultDirectory);
                }
            }

            if (!string.IsNullOrEmpty(InitialDirectory))
            {
                IShellItem initialDirectory = ShellUtil.GetShellItemForPath(InitialDirectory);
                if (initialDirectory != null)
                {
                    // Setting both of these so the dialog doesn't display errors when a remembered folder is missing.
                    if (string.IsNullOrEmpty(DefaultDirectory))
                    {
                        dialog.SetDefaultFolder(initialDirectory);
                    }
                    dialog.SetFolder(initialDirectory);
                }
            }

            if (!string.IsNullOrEmpty(RootDirectory))
            {
                IShellItem rootDirectory = ShellUtil.GetShellItemForPath(RootDirectory);
                if (rootDirectory != null && dialog is IFileDialog2 dialog2)
                {
                    dialog2.SetNavigationRoot(rootDirectory);
                }
            }

            dialog.SetTitle(Title);
            dialog.SetFileName(CriticalItemName);

            FOS options = _dialogOptions.Value;
            dialog.SetOptions(options);

            IList<FileDialogCustomPlace> places = CustomPlaces;
            if (places != null && places.Count != 0)
            {
                foreach (FileDialogCustomPlace customPlace in places)
                {
                    IShellItem shellItem = ResolveCustomPlace(customPlace);
                    if (shellItem != null)
                    {
                        try
                        {
                            dialog.AddPlace(shellItem, FDAP.BOTTOM);
                        }
                        catch (ArgumentException)
                        {
                            // The dialog doesn't allow some ShellItems to be set as Places (like device ports).
                            // Silently swallow errors here.
                        }
                    }
                }
            }
        }

        // The FileOk event expects all properties to be set, but if the event is cancelled, they need to be reverted.
        // This method is called inside a try block, and inheritors can store any data to be reverted in the revertState.
        private protected virtual bool TryHandleItemOk(IFileDialog dialog, out object revertState)
        {
            revertState = null;
            return true;
        }

        // This method is called inside a finally block when OK event was cancelled.
        // Inheritors should revert properties to the state before the dialog was shown, so that it can be shown again.
        private protected virtual void RevertItemOk(object state) { }

        #endregion

        //---------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------
        #region Internal Properties

        //   If multiple files are selected, we only return the first filename.
        /// <summary>
        ///  Gets a string containing the full path of the file or folder selected in 
        ///  the dialog box.
        /// </summary>
        private protected string CriticalItemName
        {
            get
            {
                if (_itemNames?.Length > 0)
                {
                    return _itemNames[0];
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        private protected string[] MutableItemNames
        {
            get { return _itemNames; }
            set { _itemNames = value; }
        }

        /// <summary>
        ///  In cases where we need to return an array of strings, we return
        ///  a clone of the array.  We also need to make sure we return a 
        ///  string[0] instead of a null if we don't have any filenames.
        /// </summary>
        private protected string[] CloneItemNames()
        {
            if (_itemNames == null)
            {
                return Array.Empty<string>();
            }
            else
            {
                return (string[])_itemNames.Clone();
            }
        }

        #endregion Internal Properties

        //---------------------------------------------------
        //
        // Internal Events
        //
        //---------------------------------------------------
        //#region Internal Events
        //#endregion Internal Events

        //---------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------
        #region Private Methods

        //  Provides the actual implementation of initialization tasks.
        //  Initialize() is called from both the constructor and the
        //  public Reset() function to set default values for member
        //  variables and for the options bitmask.
        private void Initialize()
        {
            // 
            // Initialize Options Flags
            // 
            _dialogOptions.Value = 0;   // _dialogOptions is an int containing a set of
                                        // bit flags used to initialize the dialog box.
                                        // Within our code, we only use GetOption and SetOption
                                        // (change from Windows Forms, which sometimes directly
                                        // modified _dialogOptions).  As such, we initialize to 0
                                        // here and then call SetOption to get _dialogOptions
                                        // into the default state.

            //
            // Set some default options
            //
            // - Specifies that the user can type only valid paths and file names. If this flag is
            //   used and the user types an invalid path and file name in the File Name entry field,
            //   we will display a warning in a message box.
            SetOption(FOS.PATHMUSTEXIST, true);

            // - Force no mini mode for the SaveFileDialog.
            SetOption(FOS.DEFAULTNOMINIMODE, true);

            // Only accept physically backed locations.
            SetOption(FOS.FORCEFILESYSTEM, true);

            //
            // Initialize additional properties
            // 
            _itemNames = null;
            _title.Value = null;
            _initialDirectory.Value = null;
            _defaultDirectory.Value = null;
            _rootDirectory.Value = null;

            // Set this to an empty list so callers can simply add to it.  They can also replace it wholesale.
            CustomPlaces = new List<FileDialogCustomPlace>();
            ClientGuid = null;

        }

        private bool HandleItemOk(IFileDialog dialog)
        {
            // When this callback occurs, the HWND is visible and we need to
            // grab it because it is used for various things like looking up the
            // DialogCaption.
            UnsafeNativeMethods.IOleWindow oleWindow = (UnsafeNativeMethods.IOleWindow)dialog;
            oleWindow.GetWindow(out _hwndFileDialog);

            string[] saveItemNames = _itemNames;
            object saveState = null;
            bool ok = false;

            try
            {
                IShellItem[] shellItems = ResolveResults(dialog);
                _itemNames = GetParsingNames(shellItems);

                if (TryHandleItemOk(dialog, out saveState))
                {
                    var cancelArgs = new CancelEventArgs();
                    OnItemOk(cancelArgs);
                    ok = !cancelArgs.Cancel;
                }
            }
            finally
            {
                if (!ok)
                {
                    RevertItemOk(saveState);
                    _itemNames = saveItemNames;
                }
            }
            return ok;
        }

        private static string[] GetParsingNames(IShellItem[] items)
        {
            if (items == null)
            {
                return null;
            }

            string[] names = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                names[i] = items[i].GetDisplayName(SIGDN.DESKTOPABSOLUTEPARSING);
            }
            return names;
        }

        private static IShellItem[] ResolveResults(IFileDialog dialog)
        {
            // covers both file and folder dialogs
            if (dialog is IFileOpenDialog openDialog)
            {
                IShellItemArray results = openDialog.GetResults();
                uint count = results.GetCount();

                IShellItem[] items = new IShellItem[count];
                for (uint i = 0; i < count; ++i)
                {
                    items[i] = results.GetItemAt(i);
                }

                return items;
            }
            else
            {
                IShellItem item = dialog.GetResult();
                return new[] { item };
            }
        }

        private static IShellItem ResolveCustomPlace(FileDialogCustomPlace customPlace)
        {
            // Use the KnownFolder Guid if it exists.  Otherwise use the Path.
            return ShellUtil.GetShellItemForPath(ShellUtil.GetPathForKnownFolder(customPlace.KnownFolder) ?? customPlace.Path);
        }

        #endregion Private Methods

        //---------------------------------------------------
        //
        // Private Properties
        //
        //---------------------------------------------------
        #region Private Properties

        /// <summary>
        ///  Gets a string containing the title of the file dialog.
        /// </summary>
        //   When showing message boxes onscreen, we want them to have the
        //   same title bar as the file open or save dialog itself.  We can't
        //   just use the Title property, because if it's null the operating
        //   system substitutes a standard localized title.
        //
        //   The solution is this private property, which returns the title of the
        //   file dialog (using the stored handle of the dialog _hwndFileDialog to
        //   call GetWindowText). 
        // 
        //   It is designed to only be called by MessageBoxWithFocusRestore.
        private string DialogCaption
        {
            get
            {
                if (!UnsafeNativeMethods.IsWindow(new HandleRef(this, _hwndFileDialog)))
                {
                    return String.Empty;
                }

                // Determine the length of the text we want to retrieve...
                int textLen = UnsafeNativeMethods.GetWindowTextLength(new HandleRef(this, _hwndFileDialog));
                // then make a StringBuilder...
                StringBuilder sb = new StringBuilder(textLen + 1);
                // and call GetWindowText to fill it up...
                UnsafeNativeMethods.GetWindowText(new HandleRef(this, _hwndFileDialog),
                           sb /*target string*/,
                           sb.Capacity /* max # of chars to copy before truncation occurs */
                           );
                // then return the results.
                return sb.ToString();
            }
        }

        #endregion Private Properties

        /// <summary>
        /// Events sink for IFileDialog.  MSDN says to return E_NOTIMPL for several, but not all, of these methods when we don't want to support them.
        /// </summary>
        /// <remarks>
        /// Be sure to explictly Dispose of it, or use it in a using block.  Unadvise happens as a result of Dispose.
        /// </remarks>
        private protected sealed class VistaDialogEvents : IFileDialogEvents, IDisposable
        {
            public delegate bool OnOkCallback(IFileDialog dialog);

            private IFileDialog _dialog;

            private OnOkCallback _okCallback;
            uint _eventCookie;

            public VistaDialogEvents(IFileDialog dialog, OnOkCallback okCallback)
            {
                _dialog = dialog;
                _eventCookie = dialog.Advise(this);
                _okCallback = okCallback;
            }

            HRESULT IFileDialogEvents.OnFileOk(IFileDialog pfd)
            {
                return _okCallback(pfd) ? HRESULT.S_OK : HRESULT.S_FALSE;
            }

            HRESULT IFileDialogEvents.OnFolderChanging(IFileDialog pfd, IShellItem psiFolder)
            {
                return HRESULT.E_NOTIMPL;
            }

            HRESULT IFileDialogEvents.OnFolderChange(IFileDialog pfd)
            {
                return HRESULT.S_OK;
            }

            HRESULT IFileDialogEvents.OnSelectionChange(IFileDialog pfd)
            {
                return HRESULT.S_OK;
            }

            HRESULT IFileDialogEvents.OnShareViolation(IFileDialog pfd, IShellItem psi, out FDESVR pResponse)
            {
                pResponse = FDESVR.DEFAULT;
                return HRESULT.S_OK;
            }

            HRESULT IFileDialogEvents.OnTypeChange(IFileDialog pfd)
            {
                return HRESULT.S_OK;
            }

            HRESULT IFileDialogEvents.OnOverwrite(IFileDialog pfd, IShellItem psi, out FDEOR pResponse)
            {
                pResponse = FDEOR.DEFAULT;
                return HRESULT.S_OK;
            }

            void IDisposable.Dispose()
            {
                _dialog.Unadvise(_eventCookie);
            }
        }

        //---------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------
        #region Private Fields

        // _dialogOptions is a set of bit flags used to control the behavior
        // of the Win32 dialog box.
        private SecurityCriticalDataForSet<FOS> _dialogOptions;

        // These private variables store data for the various public properties
        // that control the appearance of the file dialog box.
        private SecurityCriticalDataForSet<string> _title;                  // Title bar of the message box
        private SecurityCriticalDataForSet<string> _initialDirectory;       // Starting directory
        private SecurityCriticalDataForSet<string> _defaultDirectory;       // Starting directory if no recent
        private SecurityCriticalDataForSet<string> _rootDirectory;          // Topmost directory

        // We store the handle of the file dialog inside our class 
        // for a variety of purposes (like getting the title of the dialog
        // box when we need to show a message box with the same title bar caption)
        private IntPtr _hwndFileDialog;

        // This is the array that stores the item(s) the user selected in the
        // dialog box.  If Multiselect is not enabled, only the first element
        // of this array will be used.
        private string[] _itemNames;

        #endregion Private Fields
    }
}
