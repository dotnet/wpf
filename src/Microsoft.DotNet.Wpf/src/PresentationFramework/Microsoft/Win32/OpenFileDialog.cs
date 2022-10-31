// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              OpenFileDialog is a sealed class derived from FileDialog that
//              implements File Open dialog-specific functions.  It contains
//              additional properties relevant only to save dialogs.
//


namespace Microsoft.Win32
{
    using Microsoft.Win32.CommonDialogControls;

    using MS.Internal.AppModel;
    using MS.Internal.Interop;

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Windows;

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
        ///  FileNames array.
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if there are no filenames stored in the OpenFileDialog.
        /// </exception>
        /// <Remarks>
        ///     Callers must have FileDialogPermission(FileDialogPermissionAccess.Open) to call this API.
        /// </Remarks>
        public Stream OpenFile()
        {
            string filename = CriticalFileName;

            // If we got an empty or null filename, throw an exception to
            // tell the user we don't have any files to open.
            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException(SR.Get(SRID.FileNameMustNotBeNull));
            }

            return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
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
            // Cache FileNames to avoid perf issues as per
            // FxCop #CA1817
            string[] cachedFileNames = CloneFileNames();

            // Create an array to hold the streams that is exactly
            // as long as FileNames.
            Stream[] streams = new Stream[cachedFileNames.Length];

            // For each element in FileNames:
            for (int i = 0; i < cachedFileNames.Length; i++)
            {
                // Verify that the filename at this index in the FileNames
                // array is not null or empty.
                string filename = cachedFileNames[i];

                if (string.IsNullOrEmpty(filename))
                {
                    throw new InvalidOperationException(SR.Get(SRID.FileNameMustNotBeNull));
                }

                // Open the file and add it to the list of streams.
                streams[i] = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            // Return the array of open streams.
            return streams;
        }

        //   We override the FileDialog implementation to set a default
        //   for FOS_FILEMUSTEXIST in addition to the other option flags
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

        //   FOS_ALLOWMULTISELECT
        //   Enables the user to select multiple items in the open dialog. 
        // 
        /// <summary>
        /// Gets or sets an option flag indicating whether the 
        /// dialog box allows multiple files to be selected.
        /// </summary>
        public bool Multiselect
        {
            get
            {
                return GetOption(FOS.ALLOWMULTISELECT);
            }
            set
            {
                SetOption(FOS.ALLOWMULTISELECT, value);
            }
        }

        //  OFN_HIDEREADONLY currently not supported #6346
        /// <summary>
        /// Gets or sets a value indicating whether the read-only 
        /// check box is selected.
        /// </summary>
        public bool ReadOnlyChecked { get; set; }

        //  OFN_HIDEREADONLY currently not supported #6346
        /// <summary>
        /// Gets or sets a value indicating whether the dialog 
        /// contains a read-only check box.  
        /// </summary>
        public bool ShowReadOnly
        {
            get
            {
                return _showReadOnly;
            }
            set
            {
                _showReadOnly = false;
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
        // #region Protected Methods
        // #endregion Protected Methods

        //---------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------
        #region Internal Methods

        private protected override IFileDialog CreateDialog()
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
            // FOS_FILEMUSTEXIST
            // Specifies that the user can type only names of existing files
            // in the File Name entry field. If this flag is specified and 
            // the user enters an invalid name, we display a warning in a 
            // message box.   Implies FOS_PATHMUSTEXIST.
            SetOption(FOS.FILEMUSTEXIST, true);
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

        private bool _showReadOnly = false;

        //#endregion Private Fields        
    }
}
