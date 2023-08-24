// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              FileDialog is an abstract class derived from CommonItemDialog
//              that implements shared functionality common to both File
//              Open and File Save common dialogs.  
//              Creation of the specific IFileOpenDialog and IFileSaveDialog are
//              deferred to the derived classes.
//


namespace Microsoft.Win32
{
    using MS.Internal;
    using MS.Internal.AppModel;
    using MS.Internal.Interop;

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using System.Windows;

    /// <summary>
    ///    Provides a common base class for wrappers around both the
    ///    File Open and File Save common dialog boxes.  Derives from
    ///    CommonItemDialog.
    ///
    ///    This class is not intended to be derived from except by
    ///    the OpenFileDialog and SaveFileDialog classes.
    /// </summary>
    public abstract class FileDialog : CommonItemDialog
    {
        //---------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------
        #region Constructors

        /// <summary>
        /// In an inherited class, initializes a new instance of 
        /// the System.Windows.FileDialog class.
        /// </summary>
        private protected FileDialog()
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
            base.Reset();
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
            return base.ToString() + ", FileName: " + FileName;
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
        ///  file selected in the dialog box.
        /// 
        ///  Example:  if FileName = "c:\windows\explorer.exe" ,
        ///              SafeFileName = "explorer.exe"
        /// </summary>
        public string SafeFileName
        {
            get
            {
                // Use the FileName property to avoid directly accessing
                // the _itemNames field, then call Path.GetFileName
                // to do the actual work of stripping out the file name
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
        ///  Gets a string array containing the filename of each file selected
        ///  in the dialog box.
        /// </summary>
        public string[] SafeFileNames
        {
            get
            {
                // Retrieve the existing filenames into an array, then make
                // another array of the same length to hold the safe version.
                string[] unsafeFileNames = CloneItemNames();
                string[] safeFileNames = new string[unsafeFileNames.Length];

                for (int i = 0; i < unsafeFileNames.Length; i++)
                {
                    // Call Path.GetFileName to retrieve only the filename
                    // component of the current full path.
                    safeFileNames[i] = Path.GetFileName(unsafeFileNames[i]);

                    // Check to make sure Path.GetFileName does not return null.
                    // If it does, set this filename to String.Empty instead to accomodate
                    // programmers that fail to check for null when reading strings.
                    if (safeFileNames[i] == null)
                    {
                        safeFileNames[i] = String.Empty;
                    }
                }

                return safeFileNames;
            }
        }

        //   If multiple files are selected, we only return the first filename.
        /// <summary>
        ///  Gets or sets a string containing the full path of the file selected in 
        ///  the file dialog box.
        /// </summary>
        public string FileName
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
                    // UNDONE : This broke the save file dialog.
                    //string temp = Path.GetFullPath(value); // ensure filename is valid...
                    MutableItemNames = new string[] { value };
                }
            }
        }

        /// <summary>
        ///     Gets the file names of all selected files in the dialog box.
        /// </summary>
        public string[] FileNames
        {
            get
            {
                return CloneItemNames();
            }
        }

        //   The behavior governed by this property depends
        //   on whether CheckFileExists is set and whether the
        //   filter contains a valid extension to use.  For
        //   details, see the ProcessFileNames function.
        /// <summary>
        ///  Gets or sets a value indicating whether the
        ///  dialog box automatically adds an extension to a
        ///  file name if the user omits the extension.
        /// </summary>
        public bool AddExtension { get; set; }

        //   FOS_FILEMUSTEXIST is only used for Open dialog
        //   boxes, according to MSDN.  It implies 
        //   FOS_PATHMUSTEXIST and "cannot be used" with a
        //   Save As dialog box...  in practice, it seems
        //   to be ignored when used with Save As boxes 
        /// <summary>
        ///  Gets or sets a value indicating whether
        ///  the dialog box displays a warning if the 
        ///  user specifies a file name that does not exist.
        /// </summary>
        public bool CheckFileExists
        {
            get
            {
                return GetOption(FOS.FILEMUSTEXIST);
            }
            set
            {

                SetOption(FOS.FILEMUSTEXIST, value);
            }
        }


        /// <summary>
        ///  Specifies that the user can type only valid paths and file names. If this flag is
        ///  used and the user types an invalid path and file name in the File Name entry field, 
        ///  a warning is displayed in a message box.
        /// </summary>
        public bool CheckPathExists
        {
            get
            {
                return GetOption(FOS.PATHMUSTEXIST);
            }
            set
            {

                SetOption(FOS.PATHMUSTEXIST, value);
            }
        }

        /// <summary>
        /// The AddExtension property attempts to determine the appropriate extension
        /// by using the selected filter.  The DefaultExt property serves as a fallback - 
        ///  if the extension cannot be determined from the filter, DefaultExt will
        /// be used instead.
        /// </summary>
        public string DefaultExt
        {
            get
            {
                // For string properties, it's important to not return null, as an empty
                // string tends to make more sense to beginning developers.
                return _defaultExtension == null ? String.Empty : _defaultExtension;
            }

            set
            {
                if (value != null)
                {
                    // Use Ordinal here as per FxCop CA1307
                    if (value.StartsWith(".", StringComparison.Ordinal)) // Allow calling code to provide 
                                                                         // extensions like ".ext" - 
                    {
                        value = value.Substring(1);    // but strip out the period to leave only "ext"
                    }
                    else if (value.Length == 0)         // Normalize empty strings to null.
                    {
                        value = null;
                    }
                }
                _defaultExtension = value;
            }
        }

        //   The filter string also controls how the AddExtension feature behaves.  For
        //   details, see the ProcessFileNames method.
        /// <summary>
        ///       Gets or sets the current file name filter string,
        ///       which determines the choices that appear in the "Save as file type" or
        ///       "Files of type" box at the bottom of the dialog box.
        ///
        ///       This is an example filter string:
        ///       Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*"
        /// </summary>
        /// <exception cref="System.ArgumentException">
        ///  Thrown in the setter if the new filter string does not have an even number of tokens
        ///  separated by the vertical bar character '|' (that is, the new filter string is invalid.)
        /// </exception>
        /// <remarks>
        ///     Callers must have FileIOPermission(PermissionState.Unrestricted) to call this API.
        /// </remarks>
        public string Filter
        {
            get
            {
                // For string properties, it's important to not return null, as an empty
                // string tends to make more sense to beginning developers.
                return _filter == null ? String.Empty : _filter;
            }

            set
            {
                if (String.CompareOrdinal(value, _filter) != 0)   // different filter than what we have stored already
                {
                    string updatedFilter = value;

                    if (!String.IsNullOrEmpty(updatedFilter))
                    {
                        // Require the number of segments of the filter string to be even -
                        // in other words, there must only be matched pairs of description and
                        // file extensions.
                        //
                        // This implicitly requires there to be at least one vertical bar in
                        // the filter string - or else formats.Length will be 1, resulting in an
                        // ArgumentException.

                        string[] formats = updatedFilter.Split('|');

                        if (formats.Length % 2 != 0)
                        {
                            throw new ArgumentException(SR.FileDialogInvalidFilter);
                        }
                    }
                    else
                    {   // catch cases like null or "" where the filter string is not invalid but
                        // also not substantive.  We set value to null so that the assignment
                        // below picks up null as the new value of _filter.
                        updatedFilter = null;
                    }

                    _filter = updatedFilter;
                }
            }
        }

        //   Using 1 as the index of the first filter entry is counterintuitive for C#/C++
        //   developers, but is a side effect of a Win32 feature that allows you to add a template
        //   filter string that is filled in when the user selects a file for future uses of the dialog.
        //   We don't support that feature, so only values >1 are valid.
        //  
        //   For details, see MSDN docs for OPENFILENAME Structure, nFilterIndex
        /// <summary>
        ///  Gets or sets the index of the filter currently selected in the file dialog box.
        ///
        ///  NOTE:  The index of the first filter entry is 1, not 0.  
        /// </summary>
        public int FilterIndex
        {
            get
            {
                return _filterIndex;
            }

            set
            {
                _filterIndex = value;
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
        // Public Events
        //
        //---------------------------------------------------
        // #region Public Events
        // #endregion Public Events

        //---------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------
        #region Protected Methods

        /// <summary>
        /// Raises the System.Windows.FileDialog.FileOk event.
        /// </summary>
        protected override void OnItemOk(CancelEventArgs e)
        {
            if (FileOk != null)
            {
                FileOk(this, e);
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
        /// PromptUserIfAppropriate is a virtual function that shows any prompt
        /// message boxes (like "Do you want to overwrite this file") necessary after
        ///  the Open button is pressed in a file dialog.  
        ///  
        /// Return value is false if we showed a dialog box and true if we did not.
        /// (in other words, true if it's OK to continue with the open process and
        /// false if we need to return the user to the dialog to make another selection.)
        /// </summary>
        /// <remarks>
        ///  SaveFileDialog overrides this method to add additional message boxes for
        ///  its unique properties.
        /// 
        ///  For FileDialog:
        ///   If FOS_FILEMUSTEXIST is set, we check to be sure the path passed in on the
        ///   fileName parameter exists as an actual file on the hard disk.  If so, we
        ///   call PromptFileNotFound to inform the user that they must select an actual
        ///   file that already exists.
        /// </remarks>
        internal virtual bool PromptUserIfAppropriate(string fileName)
        {
            bool fileExists = true;

            // The only option we deal with in this implementation of
            // PromptUserIfAppropriate is FOS_FILEMUSTEXIST.
            if (GetOption(FOS.FILEMUSTEXIST))
            {
                try
                {
                    // File.Exists requires a full path, so we call GetFullPath on	
                    // the filename before checking if it exists.
                    string tempPath = Path.GetFullPath(fileName);
                    fileExists = File.Exists(tempPath);
                }
                // FileIOPermission constructor will throw on invalid paths.	
                catch (PathTooLongException)
                {
                    fileExists = false;
                }

                if (!fileExists)
                {
                    // file does not exist, we can't continue
                    // and must display an error
                    // Display the message box
                    PromptFileNotFound(fileName);
                }
            }
            return fileExists;
        }

        #endregion Internal Methods

        #region Internal and Protected Methods

        private protected override void PrepareDialog(IFileDialog dialog)
        {
            base.PrepareDialog(dialog);

            dialog.SetDefaultExtension(DefaultExt);

            COMDLG_FILTERSPEC[] filterItems = GetFilterItems(Filter);
            if (filterItems.Length > 0)
            {
                dialog.SetFileTypes((uint)filterItems.Length, filterItems);
                dialog.SetFileTypeIndex(unchecked((uint)FilterIndex));
            }
        }

        private protected override bool TryHandleItemOk(IFileDialog dialog, out object restoreState)
        {
            restoreState = _filterIndex;
            uint filterIndexTemp = dialog.GetFileTypeIndex();
            _filterIndex = unchecked((int)filterIndexTemp);
            return ProcessFileNames();
        }

        private protected override void RevertItemOk(object state)
        {
            _filterIndex = (int)state;
        }

        #endregion

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
            // - Specifies that we should add the default extension automatically if the
            //   user does not enter it in themselves in ProcessFileNames.  (See that function for
            //   details.)
            AddExtension = true;

            //
            // Initialize additional properties
            //
            _defaultExtension = null;
            _filter = null;
            _filterIndex = 1;        // The index of the first filter entry is 1, not 0.  
                                     // 0 is reserved for the custom filter functionality
                                     // provided by Windows, which we do not expose to the user.
        }

        /// <summary>
        /// Handle the AddExtension property on newly acquired filenames, then
        /// call PromptUserIfAppropriate to display any necessary message boxes.
        ///
        /// Returns false if we need to redisplay the dialog and true otherwise.
        /// </summary>
        private bool ProcessFileNames()
        {
            // Only process the filenames if FOS_NOVALIDATE is not set.
            if (!GetOption(FOS.NOVALIDATE))
            {
                // Call the FilterExtensions private property to get
                // a list of valid extensions from the filter(s).
                // The first extension from FilterExtensions is the
                // default extension.
                string[] extensions = GetFilterExtensions();

                // For each filename:
                //      -  Process AddExtension
                //      -  Call PromptUserIfAppropriate to display necessary dialog boxes.
                for (int i = 0; i < MutableItemNames.Length; i++)
                {
                    string fileName = MutableItemNames[i];

                    // If AddExtension is enabled and we do not already have an extension:            
                    if (AddExtension && !Path.HasExtension(fileName))
                    {
                        // Loop through all extensions, starting with the default extension
                        for (int j = 0; j < extensions.Length; j++)
                        {
                            // Assert for a valid extension
                            Invariant.Assert(!extensions[j].StartsWith(".", StringComparison.Ordinal),
                                        "FileDialog.GetFilterExtensions should not return things starting with '.'");

                            string currentExtension = Path.GetExtension(fileName);

                            // Assert to make sure Path.GetExtension behaves as we think it should, returning
                            // "" if the string is empty and something beginnign with . otherwise.
                            // Use StringComparison.Ordinal as per FxCop CA1307 and CA130.
                            Invariant.Assert(currentExtension.Length == 0 || currentExtension.StartsWith(".", StringComparison.Ordinal),
                                         "Path.GetExtension should return something that starts with '.'");

                            // Because we check Path.HasExtension above, files should
                            // theoretically not have extensions at this stage - but
                            // we'll go ahead and remove an existing extension if it
                            // somehow slipped through.
                            //
                            // Strip out any extension that may be remaining and place the rest 
                            // of the filename in s.

                            string newFilename;
                            if (((ReadOnlySpan<char>)extensions[j]).IndexOfAny('*', '?') != -1)
                            {
                                // we don't want to append the extension if it contains wild cards
                                newFilename = fileName.Substring(0, fileName.Length - currentExtension.Length);
                            }
                            else
                            {
                                newFilename = string.Concat(fileName.AsSpan(0, fileName.Length - currentExtension.Length), ".", extensions[j]);
                            }

                            // If FOS_FILEMUSTEXIST is not set, or if it is set but the filename we generated
                            // does in fact exist, we update fileName and stop trying new extensions.
                            if (!GetOption(FOS.FILEMUSTEXIST) || File.Exists(newFilename))
                            {
                                fileName = newFilename;
                                break;
                            }
                        }
                        // Store this filename back in the _itemNames array.
                        MutableItemNames[i] = fileName;
                    }

                    // Call PromptUserIfAppropriate to show necessary dialog boxes.
                    if (!PromptUserIfAppropriate(fileName))
                    {
                        // We don't want to display a bunch of message boxes
                        // if one has already determined we need to return to
                        // the file dialog, so we will return false to short
                        // circuit additional processing.
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Prompts the user with a System.Windows.MessageBox
        /// when a file does not exist.
        /// </summary>
        private void PromptFileNotFound(string fileName)
        {
            MessageBoxWithFocusRestore(SR.Format(SR.FileDialogFileNotFound, fileName),
                    System.Windows.MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static COMDLG_FILTERSPEC[] GetFilterItems(string filter)
        {
            // Expecting pipe delimited filter string pairs.
            // First is the label, second is semi-colon delimited list of extensions.
            var extensions = new List<COMDLG_FILTERSPEC>();

            if (!string.IsNullOrEmpty(filter))
            {
                string[] tokens = filter.Split('|');
                if (0 == tokens.Length % 2)
                {
                    for (int i = 1; i < tokens.Length; i += 2)
                    {
                        extensions.Add(
                            new COMDLG_FILTERSPEC
                            {
                                pszName = tokens[i - 1],
                                pszSpec = tokens[i],
                            });
                    }
                }
            }
            return extensions.ToArray();
        }

        #endregion Private Methods

        //---------------------------------------------------
        //
        // Private Properties
        //
        //---------------------------------------------------
        #region Private Properties

        /// <summary>
        /// Extracts the file extensions specified by the current file filter into
        /// an array of strings.  None of the extensions contain .'s, and the 
        /// default extension is first.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the filter string stored in the dialog is invalid.
        /// </exception>
        private string[] GetFilterExtensions()
        {
            string filter = this._filter;
            List<string> extensions = new List<string>();

            // Always make the default extension the first in the list,
            // because other functions process files in order accepting the first
            // valid extension they find.  It's a little strange if DefaultExt
            // is not in the filters list, but I guess it's legal.
            if (_defaultExtension != null)
            {
                extensions.Add(_defaultExtension);
            }

            // If we have filters, extract the extensions from the currently selected
            // filter and add them to the extensions list.
            if (filter != null)
            {
                // Filter strings are '|' delimited, so we split on them
                string[] tokens = filter.Split('|', StringSplitOptions.RemoveEmptyEntries);

                // Calculate the index of the token containing extension(s) selected
                // by the FilterIndex property.  Remember FilterIndex is one based.
                // Multiply by 2 because each filter consists of 2 strings.
                // Now subtract one to get to the filter component.
                //
                // example:  Text|*.txt|Pictures|*.jpg|Web Pages|*.htm
                // tokens[]:   0    1       2      3      4        5
                // FilterIndex = 2 selects Pictures;  (2*2)-1 = 3 points to *.jpg in tokens
                //
                int indexOfExtension = (_filterIndex * 2) - 1;

                // Check to be sure our filter index is not out of bounds (that is,
                // greater than the number of filters we actually have).
                // We multiply by 2 here because each filter consists of two strings,
                // description and extensions, both separated by | characters.. so
                // tokens.length is actually twice the number of filters we have.
                if (indexOfExtension >= tokens.Length)
                {
                    throw new InvalidOperationException(SR.FileDialogInvalidFilterIndex);
                }

                // If our filter index is valid (0 is reserved by Windows for custom
                // filter functionality we don't expose, so filters must be 1 or greater)
                if (_filterIndex > 0)
                {
                    // Find our filter in the tokens list, then split it on the
                    // ';' character (which is the filter extension delimiter)
                    string[] exts = tokens[indexOfExtension].Split(';');

                    foreach (string ext in exts)
                    {
                        // Filter extensions should be in the form *.txt or .txt,
                        // so we strip out everything before and including the '.'
                        // before adding the extension to our list.
                        // If the extension has no '.', we just ignore it as invalid.
                        int i = ext.LastIndexOf('.');

                        if (i >= 0)
                        {
                            // start the substring one beyond the location of the '.'
                            // (i) and continue to the end of the string
                            extensions.Add(ext.Substring(i + 1, ext.Length - (i + 1)));
                        }
                    }
                }
            }

            return extensions.ToArray();
        }

        #endregion Private Properties

        //---------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------
        #region Private Fields

        private string _defaultExtension;       // Extension appended first if AddExtension
                                                // is enabled
        private string _filter;                 // The file extension filters that display
                                                // in the "Files of Type" box in the dialog
        private int _filterIndex;               // The index of the currently selected
                                                // filter (a default filter index before
                                                // the dialog is called, and the filter
                                                // the user selected afterwards.)  This
                                                // index is 1-based, not 0-based.

        #endregion Private Fields
    }
}
