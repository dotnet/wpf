// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              OpenFileDialog is a sealed class derived from FileDialog that
//              implements File Open dialog-specific functions.  It contains
//              the actual commdlg.dll call to GetOpenFileName() as well as
//              additional properties relevant only to save dialogs.
//


namespace Microsoft.Win32
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security;
    using System.Windows;

    using MS.Internal.AppModel;
    using MS.Internal.Interop;
    using MS.Internal.PresentationFramework;
    using MS.Win32;

    /// <summary>
    ///  Represents a common dialog box that allows the user to open one or more file(s). 
    ///  This class cannot be inherited.
    /// </summary>
    public sealed class OpenFileDialog : FileDialog
    {
        //---------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------
        #region Constructors

        //   We add a constructor for our OpenFileDialog since we have
        //   additional initialization tasks to do in addition to the
        //   ones that FileDialog's constructor takes care of.
        /// <summary>
        ///  Initializes a new instance of the OpenFileDialog class.
        /// </summary>
        public OpenFileDialog() : base()
        {
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
        ///  Opens the file selected by the user with read-only permission,
        ///  whether or not the Read Only checkbox is checked in the dialog.
        /// </summary>
        ///  The filename used to open the file is the first element of the
        ///  FileNamesInternal array.
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if there are no filenames stored in the OpenFileDialog.
        /// </exception>
        /// <Remarks>
        ///     Callers must have FileDialogPermission(FileDialogPermissionAccess.Open) to call this API.
        /// </Remarks>
        public Stream OpenFile()
        {
            string filename = null;

            // FileNamesInternal never returns null.
            // If the dialog hasn't yet been shown, it returns an array of 0 items.
            string[] cachedFileNames = FileNamesInternal;
            if (cachedFileNames.Length != 0)
            {
                filename = cachedFileNames[0];
            }

            // If we got an empty or null filename, throw an exception to
            // tell the user we don't have any files to open.
            if (String.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException(SR.Get(SRID.FileNameMustNotBeNull));
            }

            FileStream fileStream = null;

            fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

            // Create a new FileStream from the file and return it.
            return fileStream;
        }

        /// <summary>
        ///  Opens the files selected by the user with read-only permission and
        ///  returns an array of streams, one per file.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if there are no filenames stored in the OpenFileDialog
        /// </exception>
        /// <Remarks>
        ///     Callers must have FileDialogPermission(FileDialogPermissionAccess.Open) to call this API.
        /// </Remarks>
        public Stream[] OpenFiles()
        {
            // Cache FileNamesInternal to avoid perf issues as per
            // FxCop #CA1817
            String[] cachedFileNames = FileNamesInternal;

            // Create an array to hold the streams that is exactly
            // as long as FileNamesInternal.
            Stream[] streams = new Stream[cachedFileNames.Length];

            // For each element in FileNamesInternal:
            for (int i = 0; i < cachedFileNames.Length; i++)
            {
                // Verify that the filename at this index in the FileNamesInternal
                // array is not null or empty.
                string filename = cachedFileNames[i];

                if (String.IsNullOrEmpty(filename))
                {
                    throw new InvalidOperationException(SR.Get(SRID.FileNameMustNotBeNull));
                }

                FileStream fileStream = null;

                fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

                // Open the file and add it to the list of streams.
                streams[i] = fileStream;
            }

            // Return the array of open streams.
            return streams;
        }

        //   We override the FileDialog implementation to set a default
        //   for OFN_FILEMUSTEXIST in addition to the other option flags
        //   defined in FileDialog.
        /// <summary>
        ///  Resets all properties to their default values.
        /// </summary>
        /// <Remarks>
        ///     Callers must have FileIOPermission(PermissionState.Unrestricted) to call this API.
        /// </Remarks>
        public override void Reset()
        {

            // it is VERY important that the base.reset() call remain here
            // and be located at the top of this function.
            // Since most of the initialization for this class is actually
            // done in the FileDialog class, we must call that implementation
            // before we can finish with the Initialize() call specific to our
            // derived class.
            base.Reset();

            Initialize();
        }

        #endregion Public Methods

        //---------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------
        #region Public Properties

        //   OFN_ALLOWMULTISELECT
        //   Specifies that the File Name list box allows multiple 
        //   selections.
        // 
        /// <summary>
        /// Gets or sets an option flag indicating whether the 
        /// dialog box allows multiple files to be selected.
        /// </summary>
        public bool Multiselect
        {
            get
            {
                return GetOption(NativeMethods.OFN_ALLOWMULTISELECT);
            }
            set
            {
                SetOption(NativeMethods.OFN_ALLOWMULTISELECT, value);
            }
        }

        //  OFN_READONLY
        //  Causes the Read Only check box to be selected initially 
        //  when the dialog box is created. This flag indicates the 
        //  state of the Read Only check box when the dialog box is 
        //  closed.
        //
        /// <summary>
        /// Gets or sets a value indicating whether the read-only 
        /// check box is selected.
        /// </summary>
        public bool ReadOnlyChecked
        {
            get
            {
                return GetOption(NativeMethods.OFN_READONLY);
            }
            set
            {
                SetOption(NativeMethods.OFN_READONLY, value);
            }
        }

        // 
        //  Our property is the inverse of the Win32 flag, 
        //  OFN_HIDEREADONLY.
        // 
        //  OFN_HIDEREADONLY
        //  Hides the Read Only check box.
        //
        /// <summary>
        /// Gets or sets a value indicating whether the dialog 
        /// contains a read-only check box.  
        /// </summary>
        public bool ShowReadOnly
        {
            get
            {
                // OFN_HIDEREADONLY is the inverse of our property,
                // so negate the results of GetOption...
                return !GetOption(NativeMethods.OFN_HIDEREADONLY);
            }
            set
            {
                // ... and SetOption.
                SetOption(NativeMethods.OFN_HIDEREADONLY, !value);
            }
        }

        #endregion Public Properties

        //---------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------
        //#region Public Events
        //#endregion Public Events

        //---------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------
        #region Protected Methods

        /// <summary>
        ///  Demands permissions appropriate to the dialog to be shown.
        /// </summary>
        protected override void CheckPermissionsToShowDialog()
        {
            base.CheckPermissionsToShowDialog();
        }

        #endregion Protected Methods

        //---------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------
        #region Internal Methods

        /// <summary>
        ///  Performs the actual call to display a file open dialog.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is an invalid filename, if
        /// a subclass failure occurs or if the buffer length
        /// allocated to store the filenames occurs.
        /// </exception>
        /// <remarks>
        ///  The call chain is ShowDialog > RunDialog > 
        ///  RunFileDialog (this function).  In
        ///  FileDialog.RunDialog, we created the OPENFILENAME
        ///  structure - so all this function needs to do is
        ///  call GetOpenFileName and process the result code.
        /// </remarks>
        internal override bool RunFileDialog(NativeMethods.OPENFILENAME_I ofn)
        {
            bool result = false;

            // Make the actual call to GetOpenFileName.  This function
            // blocks on GetOpenFileName until the entire dialog display
            // is completed - any interaction we have with the dialog
            // while it's open takes place through our HookProc.  The
            // return value is a bool;  true = success.
            result = UnsafeNativeMethods.GetOpenFileName(ofn);

            if (!result)    // result was 0 (false), so an error occurred.
            {
                // Something may have gone wrong - check for error conditions
                // by calling CommDlgExtendedError to get the specific error.
                int errorCode = UnsafeNativeMethods.CommDlgExtendedError();

                // Throw an appropriate exception if we know what happened:
                switch (errorCode)
                {
                    // FNERR_INVALIDFILENAME is usually triggered when an invalid initial filename is specified
                    case NativeMethods.FNERR_INVALIDFILENAME:
                        throw new InvalidOperationException(SR.Get(SRID.FileDialogInvalidFileName, SafeFileName));

                    case NativeMethods.FNERR_SUBCLASSFAILURE:
                        throw new InvalidOperationException(SR.Get(SRID.FileDialogSubClassFailure));

                    // note for FNERR_BUFFERTOOSMALL:
                    // This error likely indicates a problem with our buffer size growing code;
                    // take a look at that part of HookProc if customers report this error message is occurring.
                    case NativeMethods.FNERR_BUFFERTOOSMALL:
                        throw new InvalidOperationException(SR.Get(SRID.FileDialogBufferTooSmall));

                        /* 
                         * According to MSDN, the following errors can also occur, but we do not handle them as
                         * they are very unlikely, and if they do occur, they indicate a catastrophic failure.
                         * Most are related to features we do not wrap in our implementation.
                         *
                         * CDERR_DIALOGFAILURE
                         * CDERR_FINDRESFAILURE 
                         * CDERR_NOHINSTANCE 
                         * CDERR_INITIALIZATION 
                         * CDERR_NOHOOK
                         * CDERR_LOCKRESFAILURE 
                         * CDERR_NOTEMPLATE 
                         * CDERR_LOADRESFAILURE 
                         * CDERR_STRUCTSIZE 
                         * CDERR_LOADSTRFAILURE 
                         * CDERR_MEMALLOCFAILURE 
                         * CDERR_MEMLOCKFAILURE 
                         */
                }
            }
            return result;
        }

        internal override string[] ProcessVistaFiles(IFileDialog dialog)
        {
            var openDialog = (IFileOpenDialog)dialog;
            if (Multiselect)
            {
                IShellItemArray results = openDialog.GetResults();
                uint count = results.GetCount();
                string[] paths = new string[count];
                for (uint i = 0; i < count; ++i)
                {
                    IShellItem item = results.GetItemAt(i);
                    paths[i] = item.GetDisplayName(SIGDN.DESKTOPABSOLUTEPARSING);
                }
                return paths;
            }
            else
            {
                IShellItem item = openDialog.GetResult();
                return new[] { item.GetDisplayName(SIGDN.DESKTOPABSOLUTEPARSING) };
            }
        }

        internal override IFileDialog CreateVistaDialog()
        {
            return (IFileDialog)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID.FileOpenDialog)));
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
        //
        //  We only perform OpenFileDialog() specific reset tasks here;
        //  it's the calling code's responsibility to ensure that the
        //  base is initialized first.
        //
        private void Initialize()
        {
            // OFN_FILEMUSTEXIST
            // Specifies that the user can type only names of existing files
            // in the File Name entry field. If this flag is specified and 
            // the user enters an invalid name, we display a warning in a 
            // message box.   Implies OFN_PATHMUSTEXIST.
            SetOption(NativeMethods.OFN_FILEMUSTEXIST, true);
        }

        #endregion Private Methods

        //---------------------------------------------------
        //
        // Private Properties
        //
        //---------------------------------------------------
        //#region Private Properties
        //#endregion Private Properties

        //---------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------
        //#region Private Fields
        //#endregion Private Fields        
    }
}
