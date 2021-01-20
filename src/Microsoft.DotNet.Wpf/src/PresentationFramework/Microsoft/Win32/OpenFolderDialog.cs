// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              OpenFolderDialog is a sealed class derived from FileDialog that
//              sets the pick folders option. The class does not support the legacy
//              SHBrowseForFolder dialog.

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
    public sealed class OpenFolderDialog : FileDialog
    {
        //---------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------
        #region Constructors

        //   We add a constructor for our OpenFolderDialog since we have
        //   additional initialization tasks to do in addition to the
        //   ones that FileDialog's constructor takes care of.
        /// <summary>
        ///  Initializes a new instance of the OpenFolderDialog class.
        /// </summary>
        public OpenFolderDialog() : base()
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
        //#region Public Properties
        //#endregion Public Properties

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

        internal override bool RunFileDialog(NativeMethods.OPENFILENAME_I ofn)
        {
            // Windows XP is not supported by .NET Core so this path should never execute.

            // An alternative would be to override the RunDialog method and duplicate
            // System.Windows.Forms.FolderBrowserDialog.RunDialogOld code when UseVista is false.

            throw new PlatformNotSupportedException();
        }

        internal override void PrepareVistaDialog(IFileDialog dialog)
        {
            base.PrepareVistaDialog(dialog);

            FOS options = dialog.GetOptions() | FOS.PICKFOLDERS;
            dialog.SetOptions(options);
        }

        internal override string[] ProcessVistaFiles(IFileDialog dialog)
        {
            var openDialog = (IFileOpenDialog)dialog;
            IShellItem item = openDialog.GetResult();
            return new[] { item.GetDisplayName(SIGDN.DESKTOPABSOLUTEPARSING) };
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
        //  We only perform OpenFolderDialog() specific reset tasks here;
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
