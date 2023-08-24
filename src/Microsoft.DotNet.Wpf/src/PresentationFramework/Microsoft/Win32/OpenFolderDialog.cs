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
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Text;

    using MS.Internal.AppModel;
    using MS.Internal.Interop;

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

        /// <summary>
        ///  Returns a string representation of the folder dialog with key information
        ///  for debugging purposes.
        /// </summary>
        //   We overload ToString() so that we can provide a useful representation of
        //   this object for users' debugging purposes.
        public override string ToString()
        {
            return base.ToString() + ", FolderName: " + FolderName;
        }

        #endregion Public Methods

        //---------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------
        #region Public Properties

        /// <summary>
        ///  Gets a string containing the folder name component of the 
        ///  folder selected in the dialog box.
        /// 
        ///  Example:  if FolderName = "c:\windows" ,
        ///              SafeFolderName = "windows"
        /// </summary>
        public string SafeFolderName
        {
            get
            {
                // Use the ItemName property to avoid directly accessing
                // the _itemNames field, then call Path.GetFileName
                // to do the actual work of stripping out the folder name
                // from the path.
                string safeFN = Path.GetFileName(CriticalItemName);

                // Check to make sure Path.GetFileName does not return null.
                // If it does, set safeFN to String.Empty instead to accomodate
                // programmers that fail to check for null when reading strings.
                if (safeFN == null)
                {
                    safeFN = String.Empty;
                }

                return safeFN;
            }
        }

        /// <summary>
        ///  Gets a string array containing the name of each folder selected
        ///  in the dialog box.
        /// </summary>
        public string[] SafeFolderNames
        {
            get
            {
                // Retrieve the existing folder names into an array, then make
                // another array of the same length to hold the safe version.
                string[] unsafeFolderNames = CloneItemNames();
                string[] safeFolderNames = new string[unsafeFolderNames.Length];

                for (int i = 0; i < unsafeFolderNames.Length; i++)
                {
                    // Call Path.GetFileName to retrieve only the filename
                    // component of the current full path.
                    safeFolderNames[i] = Path.GetFileName(unsafeFolderNames[i]);

                    // Check to make sure Path.GetFileName does not return null.
                    // If it does, set this filename to String.Empty instead to accomodate
                    // programmers that fail to check for null when reading strings.
                    if (safeFolderNames[i] == null)
                    {
                        safeFolderNames[i] = String.Empty;
                    }
                }

                return safeFolderNames;
            }
        }

        //   If multiple folders are selected, we only return the first folder name.
        /// <summary>
        ///  Gets or sets a string containing the full path of the folder selected in 
        ///  the folder dialog box.
        /// </summary>
        public string FolderName
        {
            get
            {
                return CriticalItemName;
            }
            set
            {

                // Allow users to set a filename to stored in _itemNames.
                // If null is passed in, we clear the entire list.
                // If we get a string, we clear the entire list and make a new one-element
                // array with the new string.
                if (value == null)
                {
                    MutableItemNames = null;
                }
                else
                {
                    // UNDONE : ChrisAn:  This broke the save file dialog.
                    //string temp = Path.GetFullPath(value); // ensure filename is valid...
                    MutableItemNames = new string[] { value };
                }
            }
        }

        /// <summary>
        ///     Gets the folder names of all selected folders in the dialog box.
        /// </summary>
        public string[] FolderNames
        {
            get
            {
                return CloneItemNames();
            }
        }

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
        #region Public Events

        /// <summary>
        ///  Occurs when the user clicks on the Open button on a folder dialog
        ///  box.  
        /// </summary>
        public event CancelEventHandler FolderOk;

        #endregion Public Events

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
        /// Raises the System.Windows.OpenFolderDialog.FolderOk event.
        /// </summary>
        protected override void OnItemOk(CancelEventArgs e)
        {
            if (FolderOk != null)
            {
                FolderOk(this, e);
            }
        }

        #endregion Protected Methods

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
