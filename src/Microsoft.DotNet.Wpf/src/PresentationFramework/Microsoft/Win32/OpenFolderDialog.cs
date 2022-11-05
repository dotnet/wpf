// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              OpenFolderDialog is a sealed class derived from CommonItemDialog that
//              implements folder picking-specific functions.  It contains
//              additional properties relevant only to folder open dialog.
//

namespace Microsoft.Win32
{
    using MS.Internal.AppModel;
    using MS.Internal.Interop;

    using System;

    /// <summary>
    ///  Represents a common dialog box that allows the user to open one or more folder(s). 
    ///  This class cannot be inherited.
    /// </summary>
    public sealed class OpenFolderDialog : CommonItemDialog
    {
        //---------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------
        #region Constructors

        //   We add a constructor for our OpenFolderDialog since we have
        //   additional initialization tasks to do in addition to the
        //   ones that CommonItemDialog's constructor takes care of.
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

        //   We override the CommonItemDialog implementation to set
        //   FOS_PICKFOLDERS in addition to the other option flags
        //   defined in CommonItemDialog.
        /// <summary>
        ///  Resets all properties to their default values.
        /// </summary>
        /// <Remarks>
        ///     Callers must have FileIOPermission(PermissionState.Unrestricted) to call this API.
        /// </Remarks>
        public override void Reset()
        {
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
        /// dialog box allows multiple folders to be selected.
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
        //  We only perform OpenFolderDialog() specific reset tasks here;
        //  it's the calling code's responsibility to ensure that the
        //  base is initialized first.
        //
        private void Initialize()
        {
            // FOS_FILEMUSTEXIST
            // Folder must exist. Otherwise, the folder name remaining in
            // text box might be returned as a nested folder. This flag is
            // now enforced by FOS_PICKFOLDERS in the native API.
            SetOption(FOS.FILEMUSTEXIST, true);

            // FOS_PICKFOLDERS
            // This is a folder selecting dialog.
            SetOption(FOS.PICKFOLDERS, true);
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
