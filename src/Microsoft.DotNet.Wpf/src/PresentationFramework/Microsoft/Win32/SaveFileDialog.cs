// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              SaveFileDialog is a sealed class derived from FileDialog that
//              implements File Save dialog-specific functions.  It contains
//              the actual commdlg.dll call to GetSaveFileName() as well as
//              additional properties relevant only to save dialogs.
//
// 


namespace Microsoft.Win32
{
    using MS.Internal.AppModel;
    using MS.Internal.Interop;
    using MS.Internal.PresentationFramework;
    using MS.Win32;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security;
    using System.Windows;

    /// <summary>
    /// Represents a common dialog box that allows the user to specify options 
    /// for saving a file. This class cannot be inherited.
    /// </summary>
    public sealed class SaveFileDialog : FileDialog
    {
        //---------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------
        #region Constructors

        //   We add a constructor for our SaveFileDialog since we have
        //   additional initialization tasks to do in addition to the
        //   ones that FileDialog's constructor takes care of.
        /// <summary>
        ///  Initializes a new instance of the SaveFileDialog class.
        /// </summary>
        public SaveFileDialog()
            : base()
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
        ///  Opens the file selected by the user with read-only permission.  
        /// </summary>
        ///  The filename used to open the file is the first element of the
        ///  FileNamesInternal array.
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if there are no filenames stored in the SaveFileDialog.
        /// </exception>
        /// <Remarks>
        ///     Callers must have UIPermission.AllWindows to call this API.
        /// </Remarks>
        public Stream OpenFile()
        {

            // Extract the first filename from the FileNamesInternal list.
            // We can do this safely because FileNamesInternal never returns
            // null - if _fileNames is null, FileNamesInternal returns new string[0];
            string filename = FileNamesInternal.Length > 0 ? FileNamesInternal[0] : null;

            // If we got an empty or null filename, throw an exception to
            // tell the user we don't have any files to open.
            if (String.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException(SR.Get(SRID.FileNameMustNotBeNull));
            }

            // Create a new FileStream from the file and return it.
            return new FileStream(filename, FileMode.Create, FileAccess.ReadWrite);
        }

        //
        //   We override the FileDialog implementation to set a default
        //   for OFN_FILEMUSTEXIST in addition to the other option flags
        //   defined in FileDialog.
        /// <summary>
        ///  Resets all properties to their default values.
        /// </summary>
        /// <Remarks>
        ///     Callers must have UIPermission.AllWindows to call this API.
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

        //   OFN_CREATEPROMPT
        //   If the user specifies a file that does not exist, this flag causes our code
        //   to prompt the user for permission to create the file. If the user chooses 
        //   to create the file, the dialog box closes and the function returns the 
        //   specified name; otherwise, the dialog box remains open.
        // 
        /// <summary>
        ///  Gets or sets a value indicating whether the dialog box prompts the user for
        ///  permission to create a file if the user specifies a file that does not exist.
        /// </summary>
        /// <Remarks>
        ///     Callers must have UIPermission.AllWindows to call this API.
        /// </Remarks>
        public bool CreatePrompt
        {
            get
            {
                return GetOption(NativeMethods.OFN_CREATEPROMPT);
            }
            set
            {

                SetOption(NativeMethods.OFN_CREATEPROMPT, value);
            }
        }

        //   OFN_OVERWRITEPROMPT
        //   Causes our code to generate a message box if the selected file already 
        //   exists. The user must confirm whether to overwrite the file.
        //  
        /// <summary>
        /// Gets or sets a value indicating whether the Save As dialog box displays a 
        /// warning if the user specifies a file name that already exists.
        /// </summary>
        /// <Remarks>
        ///     Callers must have UIPermission.AllWindows to call this API.
        /// </Remarks>
        public bool OverwritePrompt
        {
            get
            {
                return GetOption(NativeMethods.OFN_OVERWRITEPROMPT);
            }
            set
            {

                SetOption(NativeMethods.OFN_OVERWRITEPROMPT, value);
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
        //#region Protected Methods
        //#endregion Protected Methods

        //---------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------
        #region Internal Methods

        /// <summary>
        ///  PromptUserIfAppropriate overrides a virtual function from FileDialog that show
        ///  message boxes (like "Do you want to overwrite this file") necessary after
        ///  the Save button is pressed in a file dialog.  
        /// 
        ///  Return value is false if we showed a dialog box and true if we did not.
        ///  (in other words, true if it's OK to continue with the save process and
        ///  false if we need to return the user to the dialog to make another selection.)
        /// </summary>
        /// <remarks>
        ///   We first call the base class implementation to deal with any messages handled there.
        ///   Then, if OFN_OVERWRITEPROMPT (for a message box if the selected file already exists)
        ///   or OFN_CREATEPROMPT (for a message box if a file is specified that does not exist)
        ///   flags are set, we check to see if it is appropriate to show the dialog(s) in this
        ///   method.  If so, we then call PromptFileOverwrite or PromptFileCreate, respectively.
        /// </remarks>
        internal override bool PromptUserIfAppropriate(string fileName)
        {
            // First, call the FileDialog implementation of PromptUserIfAppropriate
            // so any processing that happens there can occur.  If it returns false,
            // we'll stop processing (to avoid showing more than one message box 
            // before returning control to the user) and return false as well.
            if (!base.PromptUserIfAppropriate(fileName))
            {
                return false;
            }
         
            bool fExist = File.Exists(Path.GetFullPath(fileName));


            // If the file does not exist, check if OFN_CREATEPROMPT is
            // set.  If so, display the appropriate message box and act
            // on the user's choice.
            // Note that File.Exists requires a full path as a parameter.
            if (CreatePrompt && !fExist)
            {
                if (!PromptFileCreate(fileName))
                {
                    return false;
                }
            }

            // If the file already exists, check if OFN_OVERWRITEPROMPT is
            // set.  If so, display the appropriate message box and act
            // on the user's choice.
            // Note that File.Exists requires a full path as a parameter.
            if (OverwritePrompt && fExist)
            {
                if (!PromptFileOverwrite(fileName))
                {
                    return false;
                }
            }

            // Since all dialog boxes we showed resulted in a positive outcome,
            // returning true allows the file dialog box to close.
            return true;
        }

        /// <summary>
        ///  Performs the actual call to display a file save dialog.
        /// </summary>
        /// <remarks>
        ///  The call chain is ShowDialog > RunDialog > 
        ///  RunFileDialog (this function).  In
        ///  FileDialog.RunDialog, we created the OPENFILENAME
        ///  structure - so all this function needs to do is
        ///  call GetSaveFileName and process the result code.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is an invalid filename, if
        /// a subclass failure occurs or if the buffer length
        /// allocated to store the filenames occurs.
        /// </exception>
        internal override bool RunFileDialog(NativeMethods.OPENFILENAME_I ofn)
        {
            bool result = false;

            // Make the actual call to GetSaveFileName.  This function
            // blocks on GetSaveFileName until the entire dialog display
            // is completed - any interaction we have with the dialog
            // while it's open takes place through our HookProc.  The
            // return value is a bool;  true = success.
            result = UnsafeNativeMethods.GetSaveFileName(ofn);

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
                         * CDERR_INITIALIZATION 
                         * CDERR_LOADRESFAILURE 
                         * CDERR_LOADSTRFAILURE 
                         * CDERR_LOCKRESFAILURE 
                         * CDERR_MEMALLOCFAILURE 
                         * CDERR_MEMLOCKFAILURE 
                         * CDERR_NOHINSTANCE 
                         * CDERR_NOHOOK 
                         * CDERR_NOTEMPLATE 
                         * CDERR_STRUCTSIZE 
                         */
                }
            }
            return result;
        }

        internal override string[] ProcessVistaFiles(IFileDialog dialog)
        {
            IShellItem item = dialog.GetResult();
            return new[] { item.GetDisplayName(SIGDN.DESKTOPABSOLUTEPARSING) };
        }

        internal override IFileDialog CreateVistaDialog()
        {
            return (IFileDialog)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID.FileSaveDialog)));
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
        //  We only perform SaveFileDialog() specific reset tasks here;
        //  it's the calling code's responsibility to ensure that the
        //  base is initialized first.
        private void Initialize()
        {
            // OFN_OVERWRITEPROMPT
            // Causes the Save As dialog box to generate a message box if 
            // the selected file already exists. The user must confirm 
            // whether to overwrite the file.  Default is true.
            SetOption(NativeMethods.OFN_OVERWRITEPROMPT, true);
        }

        /// <summary>
        /// Prompts the user with a System.Windows.MessageBox
        /// when a file is about to be created. This method is
        /// invoked when the CreatePrompt property is true and the specified file
        ///  does not exist. A return value of false prevents the dialog from closing.
        /// </summary>
        private bool PromptFileCreate(string fileName)
        {
            return MessageBoxWithFocusRestore(SR.Get(SRID.FileDialogCreatePrompt, fileName),
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Prompts the user when a file is about to be overwritten. This method is
        /// invoked when the "overwritePrompt" property is true and the specified
        /// file already exists. A return value of false prevents the dialog from
        /// closing.
        /// </summary>
        private bool PromptFileOverwrite(string fileName)
        {
            return MessageBoxWithFocusRestore(SR.Get(SRID.FileDialogOverwritePrompt, fileName),
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
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
