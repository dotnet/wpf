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
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Security;
    using System.Text;
    using System.Windows;

    using MS.Internal.AppModel;
    using MS.Internal.Interop;
    using MS.Internal.PresentationFramework;
    using MS.Win32;

    /// <summary>
    ///  Represents a common dialog box that allows the user to open one or more file(s). 
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
        ///  Returns a string representation of the file dialog with key information
        ///  for debugging purposes.
        /// </summary>
        //   We overload ToString() so that we can provide a useful representation of
        //   this object for users' debugging purposes.  It provides the full pathname for
        //   any folder selected.
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString() + ", FolderName: ");
            sb.Append(FolderName);
            return sb.ToString();
        }

        #endregion Public Methods

        //---------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------
        #region Public Properties

        /// <summary>
        ///  Gets a string containing the filename component of the 
        ///  folder selected in the dialog box.
        /// 
        ///  Example:  if FolderName = "c:\windows\sytem32" ,
        ///              SafeFolderName = "system32"
        /// </summary>
        public string SafeFolderName
        {
            get
            {
                // Use the FileName property to avoid directly accessing
                // the _fileNames field, then call Path.GetFileName
                // to do the actual work of stripping out the folder name
                // from the path.
                string safeFN = Path.GetFileName(CriticalFileName);

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
        ///  Gets or sets a string containing the full path of the file selected in 
        ///  the file dialog box.
        /// </summary>
        public string FolderName
        {
            get
            {
                return CriticalFileName;
            }
            set
            {
                // Allow users to set a filename to stored in _fileNames.
                // If null is passed in, we clear the entire list.
                // If we get a string, we clear the entire list and make a new one-element
                // array with the new string.
                if (value == null)
                {
                    _fileNames = null;
                }
                else
                {
                    _fileNames = new string[] { value };
                }
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

        private string[] ProcessFolders(IFileDialog dialog)
        {
            var openDialog = (IFileOpenDialog)dialog;
            IShellItem item = openDialog.GetResult();
            return new[] { item.GetDisplayName(SIGDN.DESKTOPABSOLUTEPARSING) };
        }

        private protected override IFileDialog CreateDialog()
        {
            return (IFileDialog)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID.FileOpenDialog)));
        }

        private protected override void PrepareDialog(IFileDialog dialog)
        {
            base.PrepareDialog(dialog);

            dialog.SetFileName(CriticalFileName);
        }

        private protected override bool HandleFileOk(IFileDialog dialog)
        {
            if (!base.HandleFileOk(dialog))
            {
                return false;
            }

            string[] saveFileNames = _fileNames;
            bool ok = false;

            try
            {
                _fileNames = ProcessFolders(dialog);

                var cancelArgs = new CancelEventArgs();
                OnFileOk(cancelArgs);
                ok = !cancelArgs.Cancel;
            }
            finally
            {
                if (!ok)
                {
                    _fileNames = saveFileNames;
                }
            }
            return ok;
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
            // now enforced by FOS_PICKFOLDERS.
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
        #region Private Properties

        /// <summary>
        ///  Gets a string containing the full path of the file selected in 
        ///  the file dialog box.
        /// </summary>
        private string CriticalFileName
        {
            get
            {
                if (_fileNames == null)     // No filename stored internally...
                {
                    return String.Empty;    // So we return String.Empty
                }
                else
                {
                    // Return the first filename in the array if it is non-empty.
                    if (_fileNames[0].Length > 0)
                    {
                        return _fileNames[0];
                    }
                    else
                    {
                        return String.Empty;
                    }
                }
            }
        }

        #endregion Private Properties

        //---------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------
        #region Private Fields

        // This is the array that stores the filename(s) the user selected in the
        // dialog box.  If Multiselect is not enabled, only the first element
        // of this array will be used.
        private string[] _fileNames;

        #endregion Private Fields
    }
}
