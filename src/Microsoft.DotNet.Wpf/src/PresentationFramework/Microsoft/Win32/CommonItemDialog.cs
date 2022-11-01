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
    using System.Security;
    using System.Text;
    using System.Threading;
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
        protected CommonItemDialog()
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
        ///  Classes derived from FileDialog are expected to 
        ///  call Base.Reset() at the beginning of their
        ///  implementation of Reset() if they choose to
        ///  override this function.
        /// </summary>
        public override void Reset()
        {
            Initialize();
        }

        /// <summary>
        ///  Returns a string representation of the file dialog with key information
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

        //   The actual flag is FOS_NODEREFERENCELINKS (set = do not dereference, unset = deref) - 
        //   while we have true = dereference and false=do not dereference.  Because we expose
        //   the opposite of the Windows flag as a property to be clearer, we need to negate 
        //   the value in both the getter and the setter here.
        /// <summary>
        ///  Gets or sets a value indicating whether the dialog box returns the location 
        ///  of the file referenced by the shortcut or whether it returns the location 
        ///  of the shortcut (.lnk).
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
        ///  Restores the current directory to its original value if the user
        ///  changed the directory while searching for files.
        ///
        ///  This property is only valid for SaveFileDialog;  it has no effect
        ///  when set on an OpenFileDialog.
        /// </summary>
        public bool RestoreDirectory
        {
            get
            {
                return GetOption(FOS.NOCHANGEDIR);
            }
            set
            {

                SetOption(FOS.NOCHANGEDIR, value);
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

        #endregion Public Properties

        //---------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------
        #region Public Events

        /// <summary>
        ///  Occurs when the user clicks on the Open or Save button on a file dialog
        ///  box.  
        /// </summary>
        public event CancelEventHandler FileOk;

        #endregion Public Events

        //---------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------
        #region Protected Methods

        /// <summary>
        /// Raises the System.Windows.FileDialog.FileOk event.
        /// </summary>
        protected void OnFileOk(CancelEventArgs e)
        {
            if (FileOk != null)
            {
                FileOk(this, e);
            }
        }

        //  Because this class, FileDialog, is the parent class for both OpenFileDialog
        //  and SaveFileDialog, this function will perform the common setup tasks
        //  shared between Open and Save, and will then call RunFileDialog, which is
        //  overridden in both of the derived classes to show the correct dialog.
        //  Both derived classes know about the COM IFileDialog interfaces and can 
        //  display those if they're available and there aren't any properties set
        //  that should cause us not to.
        //
        /// <summary>
        /// Performs initialization work in preparation for calling RunFileDialog
        /// to show a file open or save dialog box.
        /// </summary>
        protected override bool RunDialog(IntPtr hwndOwner)
        {
            IFileDialog dialog = CreateDialog();

            PrepareDialog(dialog);

            using (VistaDialogEvents events = new VistaDialogEvents(dialog, HandleFileOk))
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

        //---------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------
        //#region Internal Properties
        //#endregion Internal Properties

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

            //
            // Initialize additional properties
            // 
            _title.Value = null;
            _initialDirectory.Value = null;

            // Set this to an empty list so callers can simply add to it.  They can also replace it wholesale.
            CustomPlaces = new List<FileDialogCustomPlace>();
        }

        private protected virtual bool HandleFileOk(IFileDialog dialog)
        {
            // When this callback occurs, the HWND is visible and we need to
            // grab it because it is used for various things like looking up the
            // DialogCaption.
            UnsafeNativeMethods.IOleWindow oleWindow = (UnsafeNativeMethods.IOleWindow)dialog;
            oleWindow.GetWindow(out _hwndFileDialog);
            return true;
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

        #region Vista COM interfaces Augmentation

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

        public IList<FileDialogCustomPlace> CustomPlaces { get; set; }

        #region Internal and Protected Methods

        private protected abstract IFileDialog CreateDialog();

        private protected virtual void PrepareDialog(IFileDialog dialog)
        {
            if (!string.IsNullOrEmpty(InitialDirectory))
            {
                IShellItem initialDirectory = ShellUtil.GetShellItemForPath(InitialDirectory);
                if (initialDirectory != null)
                {
                    // Setting both of these so the dialog doesn't display errors when a remembered folder is missing.
                    dialog.SetDefaultFolder(initialDirectory);
                    dialog.SetFolder(initialDirectory);
                }
            }

            dialog.SetTitle(Title);

            // Only accept physically backed locations.
            FOS options = _dialogOptions.Value | FOS.FORCEFILESYSTEM;
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

        #endregion

        #region Private Methods

        private static IShellItem ResolveCustomPlace(FileDialogCustomPlace customPlace)
        {
            // Use the KnownFolder Guid if it exists.  Otherwise use the Path.
            return ShellUtil.GetShellItemForPath(ShellUtil.GetPathForKnownFolder(customPlace.KnownFolder) ?? customPlace.Path);
        }

        #endregion

        #endregion

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

        // We store the handle of the file dialog inside our class 
        // for a variety of purposes (like getting the title of the dialog
        // box when we need to show a message box with the same title bar caption)
        private IntPtr _hwndFileDialog;

        #endregion Private Fields
    }
}
