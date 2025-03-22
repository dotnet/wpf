// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal.AppModel;
using MS.Internal.Interop;
using System.IO;
using System.Windows;

//
// 
// Description:
//              SaveFileDialog is a sealed class derived from FileDialog that
//              implements File Save dialog-specific functions.  It contains
//              additional properties relevant only to save dialogs.
//
// 


namespace Microsoft.Win32
{
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
        public SaveFileDialog() : base()
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
        public Stream OpenFile()
        {

            // Extract the first filename from the ItemNames list.
            string filename = CriticalItemName;

            // If we got an empty or null filename, throw an exception to
            // tell the user we don't have any files to open.
            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException(SR.FileNameMustNotBeNull);
            }

            // Create a new FileStream from the file and return it.
            return new FileStream(filename, FileMode.Create, FileAccess.ReadWrite);
        }

        //
        //   We override the FileDialog implementation to set a default
        //   for FOS_FILEMUSTEXIST in addition to the other option flags
        //   defined in FileDialog.
        /// <summary>
        ///  Resets all properties to their default values.
        /// </summary>
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

        //   If the user specifies a file that does not exist, this flag causes our code
        //   to prompt the user for permission to create the file. If the user chooses 
        //   to create the file, the dialog box closes and the function returns the 
        //   specified name; otherwise, the dialog box remains open.
        //
        //   We use our own prompt, so not using FOS_CREATEPROMPT (which is for open dialogs only).
        // 
        /// <summary>
        ///  Gets or sets a value indicating whether the dialog box prompts the user for
        ///  permission to create a file if the user specifies a file that does not exist.
        /// </summary>
        public bool CreatePrompt { get; set; }

        /// <summary>
        ///  Gets or sets a value indicating whether the dialog box will attempt to create
        ///  a test file at the selected path (default is true). If this flag is not set,
        ///  the calling application must handle errors, such as denial of access,
        ///  discovered when the item is created.
        /// </summary>
        public bool CreateTestFile
        {
            get
            {
                return !GetOption(FOS.NOTESTFILECREATE);
            }
            set
            {
                SetOption(FOS.NOTESTFILECREATE, !value);
            }
        }

        //   Causes our code to generate a message box if the selected file already 
        //   exists. The user must confirm whether to overwrite the file.
        //  
        //   We use our own prompt, so not using FOS_OVERWRITEPROMPT (for backward compatibility).
        //
        /// <summary>
        /// Gets or sets a value indicating whether the Save As dialog box displays a 
        /// warning if the user specifies a file name that already exists.
        /// </summary>
        public bool OverwritePrompt { get; set; }

        #endregion Public Properties

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
        ///   Then, if FOS_OVERWRITEPROMPT (for a message box if the selected file already exists)
        ///   or FOS_CREATEPROMPT (for a message box if a file is specified that does not exist)
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
         
            bool fExist = File.Exists(fileName);

            // If the file does not exist, check if CreatePrompt is
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

            // If the file already exists, check if OverwritePrompt is
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

        private protected override IFileDialog CreateDialog()
        {
            return (IFileDialog)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID.FileSaveDialog)));
        }

        #endregion Internal Methods

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
            // Causes the Save As dialog box to generate a message box if 
            // the selected file already exists. The user must confirm 
            // whether to overwrite the file.  Default is true.
            OverwritePrompt = true;
        }

        /// <summary>
        /// Prompts the user with a System.Windows.MessageBox
        /// when a file is about to be created. This method is
        /// invoked when the CreatePrompt property is true and the specified file
        ///  does not exist. A return value of false prevents the dialog from closing.
        /// </summary>
        private bool PromptFileCreate(string fileName)
        {
            return MessageBoxWithFocusRestore(SR.Format(SR.FileDialogCreatePrompt, fileName),
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
            return MessageBoxWithFocusRestore(SR.Format(SR.FileDialogOverwritePrompt, fileName),
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
        }

        #endregion Private Methods
    }
}
