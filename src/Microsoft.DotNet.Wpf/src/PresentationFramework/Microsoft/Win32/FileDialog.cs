// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              FileDialog is an abstract class derived from CommonDialog
//              that implements shared functionality common to both File
//              Open and File Save common dialogs.  It provides a hook
//              procedure that handles messages received while the dialog
//              is visible and numerous properties to control the appearance
//              and behavior of the dialog.
//              The actual call to display the dialog to GetOpenFileName()
//              or GetSaveFileName() (both functions defined in commdlg.dll)
//              is implemented in a derived class's RunFileDialog method.
//
//              When running on Vista, the COM IFileDialog interfaces are used.
//              Creation of the specific IFileOpenDialog and IFileSaveDialog are
//              deferred to the derived classes.
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

    using CharBuffer = MS.Win32.NativeMethods.CharBuffer;
    using HRESULT = MS.Internal.Interop.HRESULT;
    using SecurityHelper = MS.Internal.PresentationFramework.SecurityHelper;

    /// <summary>
    ///    Provides a common base class for wrappers around both the
    ///    File Open and File Save common dialog boxes.  Derives from
    ///    CommonDialog.
    ///
    ///    This class is not intended to be derived from except by
    ///    the OpenFileDialog and SaveFileDialog classes.
    /// </summary>
    public abstract class FileDialog : CommonDialog
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
        protected FileDialog()
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
        //   this object for users' debugging purposes.  It provides the full pathname for
        //   any files selected.
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString() + ": Title: " + Title + ", FileName: ");
            sb.Append(FileName);
            return sb.ToString();
        }

        #endregion Public Methods

        //---------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------
        #region Public Properties

        //
        //   The behavior governed by this property depends
        //   on whether CheckFileExists is set and whether the
        //   filter contains a valid extension to use.  For
        //   details, see the ProcessFileNames function.
        //
        //   It's worth noting that unlike most of these
        //   properties, AddExtension is a custom flag that
        //   is unique to our implementation.  As such, it is
        //   a constant value in our class, not stored in
        //   NativeMethods like the other flags.
        /// <summary>
        ///  Gets or sets a value indicating whether the
        ///  dialog box automatically adds an extension to a
        ///  file name if the user omits the extension.
        /// </summary>
        public bool AddExtension
        {
            get
            {
                return GetOption(OPTION_ADDEXTENSION);
            }
            set
            {

                SetOption(OPTION_ADDEXTENSION, value);
            }
        }


        //
        //   OFN_FILEMUSTEXIST is only used for Open dialog
        //   boxes, according to MSDN.  It implies 
        //   OFN_PATHMUSTEXIST and "cannot be used" with a
        //   Save As dialog box...  in practice, it seems
        //   to be ignored when used with Save As boxes 
        /// <summary>
        ///  Gets or sets a value indicating whether
        ///  the dialog box displays a warning if the 
        ///  user specifies a file name that does not exist.
        /// </summary>
        public virtual bool CheckFileExists
        {
            get
            {
                return GetOption(NativeMethods.OFN_FILEMUSTEXIST);
            }
            set
            {

                SetOption(NativeMethods.OFN_FILEMUSTEXIST, value);
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
                return GetOption(NativeMethods.OFN_PATHMUSTEXIST);
            }
            set
            {

                SetOption(NativeMethods.OFN_PATHMUSTEXIST, value);
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

        //   The actual flag is OFN_NODEREFERENCELINKS (set = do not dereference, unset = deref) - 
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
                return !GetOption(NativeMethods.OFN_NODEREFERENCELINKS);
            }
            set
            {

                SetOption(NativeMethods.OFN_NODEREFERENCELINKS, !value);
            }
        }

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
                // the _fileNames field, then call Path.GetFileName
                // to do the actual work of stripping out the file name
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
        ///  Gets a string array containing the filename of each file selected
        ///  in the dialog box.
        /// </summary>
        public string[] SafeFileNames
        {
            get
            {
                // Retrieve the existing filenames into an array, then make
                // another array of the same length to hold the safe version.
                string[] unsafeFileNames = FileNamesInternal;
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
                    // UNDONE : ChrisAn:  This broke the save file dialog.
                    //string temp = Path.GetFullPath(value); // ensure filename is valid...
                    _fileNames = new string[] { value };
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

                // FileNamesInternal is a property we use to clone
                // the string array before returning it.
                string[] files = FileNamesInternal;
                return files;
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
        ///  If DereferenceLinks is true and the filter string is null, a blank
        ///  filter string (equivalent to "|*.*") will be automatically substituted to work
        ///  around the issue documented in Knowledge Base article 831559
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
                            throw new ArgumentException(SR.Get(SRID.FileDialogInvalidFilter));
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
                return GetOption(NativeMethods.OFN_NOCHANGEDIR);
            }
            set
            {

                SetOption(NativeMethods.OFN_NOCHANGEDIR, value);
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
        ///  Gets or sets a value indicating whether the dialog box accepts only valid
        ///  Win32 file names.
        /// </summary>
        public bool ValidateNames
        {
            get
            {
                return !GetOption(NativeMethods.OFN_NOVALIDATE);
            }
            set
            {

                SetOption(NativeMethods.OFN_NOVALIDATE, !value);
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
        //   We fire this event from DoFileOk.
        public event CancelEventHandler FileOk;

        #endregion Public Events

        //---------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------
        #region Protected Methods

        /// <summary>
        ///  Defines the common dialog box hook procedure that is overridden to add
        ///  specific functionality to the file dialog box.
        /// </summary>
        protected override IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            // Assume we are successful unless we encounter a problem.
            IntPtr returnValue = IntPtr.Zero;

            // Our File Dialogs are Explorer-style dialogs with hook procedure enabled
            // (OFN_ENABLEHOOK | OFN_EXPLORER).  As such, we will get the following
            // messages:  (as per MSDN)
            //
            // WM_INITDIALOG
            // WM_NOTIFY (indicating actions taken by the user or other dialog box events)
            // Messages for any additional controls defined by specifying a child dialog template
            switch ((WindowMessage)msg)
            {
                case WindowMessage.WM_NOTIFY:
                    // Our hookproc is actually the hook procedure for a child template hosted
                    // inside the actual file dialog box.  We want the hwnd of the actual dialog,
                    // so we call GetParent on the hwnd passed to the hookproc.
                    _hwndFileDialog = UnsafeNativeMethods.GetParent(new HandleRef(this, hwnd));

                    // When we receive WM_NOTIFY, lParam is a pointer to an OFNOTIFY
                    // structure that defines the action.  OFNOTIFY is a structure
                    // specific to file open and save dialogs with three members:
                    // (defined in Commdlg.h - see MSDN for more details)
                    // 
                    // struct _OFNOTIFY {
                    //    NMHDR hdr;        // this is a by-value structure;
                    //                      // the implementation in UnsafeNativeMethods breaks it into
                    //                    // hdr_hwndFrom (HWND, handle to control sending message),
                    //                    // hdr_idFrom (UINT, ID of control sending message) and
                    //                    // hdr_code (UINT, one of the CDN_??? notification constants)
                    //                    
                    //    LPOPENFILENAME lpOFN;    // pointer to the OPENFILENAME structure we created in
                    //                    // RunFileDialog when showing this dialog box.
                    //                    
                    //    LPTSTR pszFile;        // if a network sharing violation has occurred, this
                    //                    // is the name of the file affected.  Only valid with
                    //                    // hdr_code = CDN_SHAREVIOLATION.
                    //    }
                    // 
                    // Convert the pointer to our OFNOTIFY stored in lparam to an object using PtrToStructure.
                    NativeMethods.OFNOTIFY notify = (NativeMethods.OFNOTIFY)UnsafeNativeMethods.PtrToStructure(lParam, typeof(NativeMethods.OFNOTIFY));

                    // WM_NOTIFY indicates that the dialog is sending us a notification message.
                    // notify.hdr_code is an int defining which notification is being received.
                    // These codes are integer constants defined originally in commdlg.h.
                    switch (notify.hdr_code)
                    {
                        case NativeMethods.CDN_INITDONE:
                            // CDN_INITDONE is sent by Explorer-style file dialogs when the
                            // system has finished arranging the controls in the dialog box.
                            //
                            // We use this opportunity to move the dialog box to the center
                            // of the appropriate monitor.
                            //
                            // As an aside, this only seems to work the first time we show
                            // a dialog - after that, Windows remembers the position of the
                            // dialog.   But that's the Winforms behavior too, so it's fine.

                            MoveToScreenCenter(new HandleRef(this, _hwndFileDialog));
                            break;


                        case NativeMethods.CDN_SELCHANGE:
                            // CDN_SELCHANGE is sent by Explorer-style file dialogs when the
                            // selection changes in the list box that displays the contents
                            // of the currently opened folder or directory.
                            // 
                            // When we get this message, we check to make sure our character
                            // buffer is big enough to hold all of the filenames that have
                            // been selected.  If it isn't, we create a new, bigger buffer
                            // and substitute it in the OPENFILENAME structure.

                            // Retrieve the OPENFILENAME structure from the OFNOTIFY structure
                            // so we can access the CharBuffer inside it.
                            NativeMethods.OPENFILENAME_I ofn = (NativeMethods.OPENFILENAME_I)
                                UnsafeNativeMethods.PtrToStructure(notify.lpOFN, typeof(NativeMethods.OPENFILENAME_I));


                            // Get the buffer size required to store the selected file names.
                            // We would like to accomplish this by sending a CDM_GETFILEPATH message 
                            // - to which the file dialog responds with the number of unicode 
                            // characters needed to store the file names and paths.
                            // 
                            // Windows Forms used CDM_GETSPEC here, but that only retrieves the length
                            // of the filenames - not of the complete path.  So in cases with network
                            // shortcuts and dereference links enabled, we end up with not enough buffer
                            // and an FNERR_BUFFERTOOSMALL error.
                            //
                            // Unfortunately, CDM_GETFILEPATH returns -1 when a bunch of files are
                            // selected, so changing to it actually makes things worse with very large
                            // cases.  So we'll stick with CDM_GETSPEC plus extra buffer space.
                            //
                            int sizeNeeded = (int)UnsafeNativeMethods.UnsafeSendMessage(_hwndFileDialog,                      // hWnd of window to receive message
                                                                                  (WindowMessage)NativeMethods.CDM_GETSPEC,                          // Msg (message to send)
                                                                                  IntPtr.Zero,                          // wParam (additional info)
                                                                                  IntPtr.Zero);                         // lParam (additional info)

                            if (sizeNeeded > ofn.nMaxFile)
                            {
                                // A bigger buffer is required, so we'll allocate a new
                                // CharBuffer and substitute it for the existing one.

                                //try
                                //{
                                // Make the new buffer equal to the size the dialog told us we needed
                                // plus a reasonable growth factor.
                                int newBufferSize = sizeNeeded + (FILEBUFSIZE / 4);

                                // Allocate a new CharBuffer in the appropriate size.
                                CharBuffer charBufferTmp = CharBuffer.CreateBuffer(newBufferSize);

                                // Allocate unmanaged memory for the buffer and store the pointer.
                                IntPtr newBuffer = charBufferTmp.AllocCoTaskMem();

                                // Free the old, smaller buffer stored in ofn.lpstrFile
                                Marshal.FreeCoTaskMem(ofn.lpstrFile);

                                // Substitute buffer and update the buffer maximum size in
                                // the dialog.
                                ofn.lpstrFile = newBuffer;
                                ofn.nMaxFile = newBufferSize;

                                // Store the reference to the character buffer inside our
                                // class so we can free it when we're done.
                                this._charBuffer = charBufferTmp;

                                // Marshal the OPENFILENAME structure back into the
                                // OFNOTIFY structure, then marshal the OFNOTIFY structure
                                // back into lparam to update the dialog.
                                Marshal.StructureToPtr(ofn, notify.lpOFN, true);
                                Marshal.StructureToPtr(notify, lParam, true);
                                // }
                                // Windows Forms had a catch-all exception handler here
                                // but no justification for why it existed.  If exceptions
                                // are thrown when we grow the buffer, re-add this catch
                                // and perform handling specific to the exception you are seeing.
                                //
                                // I don't see anywhere an exception would be thrown that
                                // we would want to simply discard in this try block, so
                                // we'll remove this catch and let any exceptions through.
                                //
                                // catch (Exception)
                                // {
                                // intentionally not throwing here.
                                // }
                            }
                            break;


                        case NativeMethods.CDN_SHAREVIOLATION:
                            // CDN_SHAREVIOLATION is sent by Explorer-style boxes when OK is clicked
                            // and a network sharing violation occurs for the selected file.
                            // Network sharing violation is a bit misleading of a term - it could
                            // also mean the user doesn't have permissions for the file, or it could mean
                            // the file is already opened by another process on the same machine.
                            // 
                            // We process this message because of some odd behavior seen when a file
                            // is locked for writing.  (for details, see VS Whidbey 95342)
                            //
                            // We get this notification followed by *two* CDN_FILEOK notifications... but only
                            // if the path is entered in the textbox and not selected from the folder view.
                            // 
                            // If we get a CDN_SHAREVIOLATION, we'll set a flag and a counter so we can track
                            // which CDN_FILEOK notification we're on to avoid showing two message boxes.
                            this._ignoreSecondFileOkNotification = true;      // We want to ignore the second CDN_FILEOK
                            this._fileOkNotificationCount = 0;                // to avoid a second prompt by PromptFileOverwrite.
                            break;


                        case NativeMethods.CDN_FILEOK:
                            // CDN_FILEOK is sent when the user specifies a filename and clicks OK.
                            // We need to process the files selected and make sure everything's acceptable.
                            // If it's all OK, we don't need to do anything.
                            // 
                            // To tell the dialogs to stay open after we receive a CDN_FILEOK, we must both
                            // return a non-zero value from this hook procedure and call SetWindowLong to
                            // set a nonzero value for DWL_MSGRESULT.


                            // --- Begin VS Whidbey 95342 Workaround ---
                            // See the CDN_SHAREVIOLATION case above for background info about this issue.
                            if (this._ignoreSecondFileOkNotification)
                            {
                                // We got a CDN_SHAREVIOLATION notification and want to ignore the second CDN_FILEOK notification.
                                // We'll allow the first one through and block the second.
                                // Recall that we initialize _fileOkNotificationCount to 0 when we get the CDN_SHAREVIOLATION.
                                if (this._fileOkNotificationCount == 0)
                                {
                                    // This is the first CDN_FILEOK, record that we received
                                    // it and then allow DoFileOk to be called.
                                    this._fileOkNotificationCount = 1;
                                }
                                else
                                {
                                    // This is the second CDN_FILEOK, so we want to ignore it.
                                    this._ignoreSecondFileOkNotification = false;

                                    // Call SetWindowLong to set the DWL_MSGRESULT value of the file dialog window
                                    // to a non-zero number to tell the dialog to stay open.  
                                    // NativeMethods.InvalidIntPtr is defined as -1.
                                    UnsafeNativeMethods.CriticalSetWindowLong(new HandleRef(this, hwnd),             // hWnd (which window are we affecting)
                                                                      NativeMethods.DWL_MSGRESULT,           // nIndex (which value are we setting)
                                                                      NativeMethods.InvalidIntPtr);          // dwNewLong (what is the new value)

                                    // We also need to return a non-zero value to tell the dialog to stay open.
                                    returnValue = NativeMethods.InvalidIntPtr;
                                    break;
                                }
                            }
                            // --- End VS Whidbey 95342 Workaround ---

                            // Call DoFileOk to check if the files that have been selected
                            // are acceptable.  (See DoFileOk for details.)
                            //
                            // If it returns false, we must notify the dialog box that it
                            // needs to stay open for further input.
                            if (!DoFileOk(notify.lpOFN))
                            {
                                // Call SetWindowLong to set the DWL_MSGRESULT value of the file dialog window
                                // to a non-zero number to tell the dialog to stay open.  
                                // NativeMethods.InvalidIntPtr is defined as -1.
                                UnsafeNativeMethods.CriticalSetWindowLong(new HandleRef(this, hwnd),                  // hWnd (which window are we affecting)
                                                                  NativeMethods.DWL_MSGRESULT,                        // nIndex (which value are we setting)
                                                                  NativeMethods.InvalidIntPtr);               // dwNewLong (what is the new value)

                                // We also need to return a non-zero value to tell the dialog to stay open.
                                returnValue = NativeMethods.InvalidIntPtr;
                                break;
                            }
                            break;
                    }
                    break;
                default:
                    returnValue = base.HookProc(hwnd, msg, wParam, lParam);
                    break;
            }

            // Return IntPtr.Zero to indicate success, unless we have
            // adjusted the return value elsewhere in the function.
            return returnValue;
        }

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
            // Verify thread access here.  Generally we'd want to enforce thread affinity
            // but barring that we really don't want to let multiple instances of this object
            // display native dialogs.
            // On XP, we have a buffer used in the hook that can get corrupted with multi-thread access.

            if (UseVistaDialog)
            {
                return RunVistaDialog(hwndOwner);
            }
            return RunLegacyDialog(hwndOwner);
        }

        /// <summary>
        /// Performs initialization work in preparation for calling RunFileDialog
        /// to show a file open or save dialog box.
        /// </summary>
        private bool RunLegacyDialog(IntPtr hwndOwner)
        {
            // Once we run the dialog, all of our communication with it is handled
            // by processing WM_NOTIFY messages in our hook procedure, this.HookProc.
            // NativeMethods.WndProc is a delegate with the appropriate signature
            // needed for a Win32 window hook procedure.
            NativeMethods.WndProc hookProcPtr = new NativeMethods.WndProc(this.HookProc);

            // Create a new OPENFILENAME structure.  OPENFILENAME is a structure defined
            // in Win32's commdlg.h that contains most of the information needed to
            // successfully display a file dialog box.
            // NOTE:  Despite the name, OPENFILENAME is the proper structure for both
            //        file open and file save dialogs.
            NativeMethods.OPENFILENAME_I ofn = new NativeMethods.OPENFILENAME_I();

            // do everything in a try block, so we always free memory in the finalizer
            try
            {
                // Create an appropriately sized buffer to hold the filenames.
                // The buffer's initial size is controlled by the FILEBUFSIZE constant,
                // an arbitrary value chosen so that we will rarely have to grow the buffer.
                _charBuffer = CharBuffer.CreateBuffer(FILEBUFSIZE);

                // If we have a filename stored in our internal array _fileNames,
                // place it in the buffer as a default filename.
                if (_fileNames != null)
                {
                    _charBuffer.PutString(_fileNames[0]);
                }

                // --- Set up the OPENFILENAME structure ---

                // lStructSize
                // Specifies the length, in bytes, of the structure. 
                ofn.lStructSize = Marshal.SizeOf(typeof(NativeMethods.OPENFILENAME_I));

                // hwndOwner
                // Handle to the window that owns the dialog box. This member can be any
                // valid window handle, or it can be NULL if the dialog box has no owner.
                ofn.hwndOwner = hwndOwner;

                // hInstance
                // This property is ignored unless OFN_ENABLETEMPLATEHANDLE or 
                // OFN_ENABLETEMPLATE are set.  Since we do not set either,
                // hInstance is ignored, so we can set it to zero.
                ofn.hInstance = IntPtr.Zero;

                // lpstrFilter
                // Pointer to a buffer containing pairs of null-terminated filter strings. 
                // The last string in the buffer must be terminated by two NULL characters. 
                // Since our filter strings are stored terminated by vertical bar '|' chars,
                // we call MakeFilterString to reformat and validate the filter string.
                ofn.lpstrFilter = MakeFilterString(_filter, this.DereferenceLinks);

                // nFilterIndex
                // Specifies the index of the currently selected filter in the File Types 
                // control.  Note that since 0 is reserved for a custom filter (which we
                // do not support), our valid filter indexes begin at 1.
                ofn.nFilterIndex = _filterIndex;

                // lpstrFile
                // Pointer to a buffer used to store filenames.  When initializing the
                // dialog, this name is used as an initial value in the File Name edit
                // control.  When files are selected and the function returns, the buffer
                // contains the full path to every file selected.
                ofn.lpstrFile = _charBuffer.AllocCoTaskMem();

                // nMaxFile
                // Size of the lpstrFile buffer in number of Unicode characters.
                ofn.nMaxFile = _charBuffer.Length;

                // lpstrInitialDir
                // Pointer to a null terminated string that can specify the initial directory.
                // A relatively complex algorithm is used to determine which directory is
                // actually used as the initial directory - for details, see MSDN for the
                // OPENFILENAME structure.
                ofn.lpstrInitialDir = _initialDirectory.Value;

                // lpstrTitle
                // Pointer to a string to be placed in the title bar of the dialog box.
                // NULL causes the title bar to display the operating system default string.
                ofn.lpstrTitle = _title.Value;

                // Flags
                // A set of bit flags you can use to initialize the dialog box.
                // Most of these will be set through public properties that then call
                // GetOption or SetOption.  We retrieve the flags using the Options property
                // and then add three additional flags here:
                //
                //     OFN_EXPLORER
                //         display an Explorer-style box (newer style)
                //     OFN_ENABLEHOOK
                //         enable the hook procedure (important for much of our functionality)
                //     OFN_ENABLESIZING
                //         allow the user to resize the dialog box
                //         
                ofn.Flags = Options | (NativeMethods.OFN_EXPLORER |
                                       NativeMethods.OFN_ENABLEHOOK |
                                       NativeMethods.OFN_ENABLESIZING);

                // lpfnHook
                // Pointer to the hook procedure.
                // Ignored unless OFN_ENABLEHOOK is set in Flags.
                ofn.lpfnHook = hookProcPtr;

                // FlagsEx
                // Can be either zero or OFN_EX_NOPLACESBAR, depending on whether
                // the Places Bar (My Computer/Favorites/etc) should be shown on the
                // left side of the file dialog.
                ofn.FlagsEx = NativeMethods.OFN_USESHELLITEM;

                // lpstrDefExt
                // Pointer to a buffer that contains the default extension;  it will
                // be appended to filenames if the user does not type an extension.
                // Only the first three characters are appended by Windows.  If this
                // is NULL, no extension is appended.
                if (_defaultExtension != null && AddExtension)
                {
                    ofn.lpstrDefExt = _defaultExtension;
                }

                // Call into either OpenFileDialog or SaveFileDialog to show the
                // actual dialog box.  This call blocks until the dialog is closed;
                // while dialog is open, all interaction is through HookProc.
                return RunFileDialog(ofn);
            }
            finally
            {
                // Explicitly set the character buffer to null.
                _charBuffer = null;

                // If there is still a pointer to a memory location in
                // ofn.lpstrFile, we explicitly free that memory here.
                if (ofn.lpstrFile != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(ofn.lpstrFile);
                }
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
        internal bool GetOption(int option)
        {
            return (_dialogOptions.Value & option) != 0;
        }

        /// <summary>
        ///     Sets the given option to the given boolean value.
        /// </summary>
        internal void SetOption(int option, bool value)
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
        ///   If OFN_FILEMUSTEXIST is set, we check to be sure the path passed in on the
        ///   fileName parameter exists as an actual file on the hard disk.  If so, we
        ///   call PromptFileNotFound to inform the user that they must select an actual
        ///   file that already exists.
        /// </remarks>
        internal virtual bool PromptUserIfAppropriate(string fileName)
        {
            bool fileExists = true;

            // The only option we deal with in this implementation of
            // PromptUserIfAppropriate is OFN_FILEMUSTEXIST.
            if (GetOption(NativeMethods.OFN_FILEMUSTEXIST))
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

        /// <summary>
        ///     Implements the actual call to GetOpenFileName or GetSaveFileName.
        /// </summary>
        internal abstract bool RunFileDialog(NativeMethods.OPENFILENAME_I ofn);

        #endregion Internal Methods

        //---------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------
        #region Internal Properties
        /// <summary>
        ///  In cases where we need to return an array of strings, we return
        ///  a clone of the array.  We also need to make sure we return a 
        ///  string[0] instead of a null if we don't have any filenames.
        /// </summary>
        internal string[] FileNamesInternal
        {
            get
            {
                if (_fileNames == null)
                {
                    return Array.Empty<string>();
                }
                else
                {
                    return (string[])_fileNames.Clone();
                }
            }
        }


        #endregion Internal Properties

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

        /// <summary>
        ///     Processes the CDN_FILEOK notification, which is sent by an
        ///     Explorer-style Open or Save As dialog box when the user specifies
        ///     a file name and clicks the OK button.
        /// </summary>
        /// <returns>
        /// true if the dialog can close, or false if we need to return to 
        /// the dialog for additional input.
        /// </returns>
        private bool DoFileOk(IntPtr lpOFN)
        {
            NativeMethods.OPENFILENAME_I ofn = (NativeMethods.OPENFILENAME_I)UnsafeNativeMethods.PtrToStructure(lpOFN, typeof(NativeMethods.OPENFILENAME_I));

            // While processing the results we get from the OPENFILENAME struct,
            // we will adjust several properties of our own class to reflect the
            // new data.  In case we discover we need to send the user back to
            // the dialog for further input, we need to be able to revert these
            // changes - so we backup _dialogOptions, _filterIndex and _fileNames.
            //
            // We only assign brand new string arrays to _FileNames, so it's OK
            // to back up by reference here.
            int saveOptions = _dialogOptions.Value;
            int saveFilterIndex = _filterIndex;
            string[] saveFileNames = _fileNames;

            // ok is a flag to determine whether we need to show the dialog
            // again (false) or if we're satisfied with the results we received (true).
            bool ok = false;

            try
            {
                // Replace the ReadOnly flag in DialogOptions with the ReadOnly flag
                // from the OPENFILEDIALOG structure - that is, store the user's
                // choice from the Read Only checkbox so our property is up to date.
                _dialogOptions.Value = _dialogOptions.Value & ~NativeMethods.OFN_READONLY |
                                 ofn.Flags & NativeMethods.OFN_READONLY;

                // Similarly, update the filterIndex to reflect the selected filter.
                _filterIndex = ofn.nFilterIndex;

                // Ask the character buffer to copy the memory from the location 
                // referenced by lpstrFile into our internal character buffer.
                _charBuffer.PutCoTaskMem(ofn.lpstrFile);

                if (!GetOption(NativeMethods.OFN_ALLOWMULTISELECT))
                {
                    // Since we're selecting a single file, make a string
                    // array with a single element containing the entire contents
                    // of the character buffer.
                    _fileNames = new string[] { _charBuffer.GetString() };
                }
                else
                {
                    // Multiselect is a bit more complex - call GetMultiselectFiles
                    // to handle that case.
                    _fileNames = GetMultiselectFiles(_charBuffer);
                }

                // Call ProcessFileNames() to do validation and post-processing
                // tasks (see that function for details;  it checks if files exist,
                // prompts users with message boxes if invalid selections are made, etc.)
                if (ProcessFileNames())
                {
                    // ProcessFileNames returned true, so it's OK to fire the
                    // OnFileOk event.
                    CancelEventArgs ceevent = new CancelEventArgs();
                    OnFileOk(ceevent);

                    // We allow our calling code to do even more post-processing
                    // through the OnFileOk event - and therefore offer them the
                    // opportunity to redisplay the dialog for additional input
                    // using the event arguments if their validation failed.
                    //
                    // If OnFileOk is not handled, ceevent.Cancel will be false.
                    ok = !ceevent.Cancel;
                }
            }
            finally
            {
                // No matter what happened, we need to restore dialog state
                // if the result was not ok=true.
                if (!ok)
                {
                    _dialogOptions.Value = saveOptions;
                    _filterIndex = saveFilterIndex;
                    _fileNames = saveFileNames;
                }
            }
            return ok;
        }

        /// <summary>
        ///     Extracts the filename(s) returned by the file dialog.
        /// </summary>
        ///  Marked static for perf reasons because this function doesn't 
        ///  actually access any instance data as per FxCop CA1822.
        private static string[] GetMultiselectFiles(CharBuffer charBuffer)
        {
            // Iff OFN_ALLOWMULTISELECT is set for an Explorer-style dialog box
            // and the user selects multiple files, lpstrFile points to a string
            // containing the current directory, followed by a NULL, followed by
            // two or more filenames that are NULL separated, with an extra NULL
            // character after the last filename.
            // 
            // We'll use the GetString() function of the character buffer to get
            // two of these null-terminated chunks at a time, one into directory
            // and one into filename.
            string directory = charBuffer.GetString();
            string fileName = charBuffer.GetString();

            // If OFN_ALLOWMULTISELECT is enabled but the user selects only
            // one file, we get the filename and path concatenated together without
            // a null separator.  This will cause our directory variable to 
            // contain the full path and fileName to be empty, so make a new 
            // string array with the contents of directory as its single element.
            // 
            if (fileName.Length == 0)
            {
                return new string[] { directory };
            }

            // If the directory was provided without a directory separator
            // character (typically '\' on Windows) at the end, we add it.
            if (!directory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                directory = directory + Path.DirectorySeparatorChar;
            }

            // Create a generic list of strings to hold the names.
            List<string> names = new List<string>();

            do
            {
                // With DereferenceLinks enabled, we can sometimes end
                // up with full paths provided as filenames.  We need
                // to check for two cases here - the case where the 
                // filename begins with '\', indicating a UNC share path,
                // or the case where we have a full hard disk path
                // (e.g. C:\file.txt), where [1] will be the : volume 
                // separator and [2] will be the \ directory separator.

                bool isUncPath = (fileName[0] == Path.DirectorySeparatorChar && fileName[1] == Path.DirectorySeparatorChar);

                bool isFullPath = (fileName.Length > 3 &&
                                   fileName[1] == Path.VolumeSeparatorChar &&
                                   fileName[2] == Path.DirectorySeparatorChar);

                if (!(isUncPath || isFullPath))
                {
                    // filename is not a full path, so we need to
                    // add on the directory
                    fileName = directory + fileName;
                }

                names.Add(fileName);

                // Get the next filename
                fileName = charBuffer.GetString();
            } while (!String.IsNullOrEmpty(fileName));

            return names.ToArray();
        }

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
                                        // It is placed directly into the OPENFILEDIALOG
                                        // struct used to instantiate the file dialog box.
                                        // Within our code, we only use GetOption and SetOption
                                        // (change from Windows Forms, which sometimes directly
                                        // modified _dialogOptions).  As such, we initialize to 0
                                        // here and then call SetOption to get _dialogOptions
                                        // into the default state.

            //
            // Set some default options
            //
            // - Hide the Read Only check box.
            SetOption(NativeMethods.OFN_HIDEREADONLY, true);

            // - Specifies that the user can type only valid paths and file names. If this flag is
            //   used and the user types an invalid path and file name in the File Name entry field,
            //   we will display a warning in a message box.
            SetOption(NativeMethods.OFN_PATHMUSTEXIST, true);

            // - This is our own flag, not a standard one defined in OPENFILEDIALOG.  We use this to
            //   indicate to ourselves that we should add the default extension automatically if the
            //   user does not enter it in themselves in ProcessFileNames.  (See that function for
            //   details.)
            SetOption(OPTION_ADDEXTENSION, true);


            //
            // Initialize additional properties
            // 
            _title.Value = null;
            _initialDirectory.Value = null;
            _defaultExtension = null;
            _fileNames = null;
            _filter = null;
            _filterIndex = 1;        // The index of the first filter entry is 1, not 0.  
                                     // 0 is reserved for the custom filter functionality
                                     // provided by Windows, which we do not expose to the user.

            // Variables used for bug workaround:
            // When the selected file is locked for writing, we get a sharing violation notification
            // followed by *two* CDN_FILEOK notifications.  These flags are used to track the multiple
            // notifications so we only show one error message box to the user.  
            // For a more complete explanation and PS bug information, see HookProc.
            _ignoreSecondFileOkNotification = false;
            _fileOkNotificationCount = 0;

            // Set this to an empty list so callers can simply add to it.  They can also replace it wholesale.
            CustomPlaces = new List<FileDialogCustomPlace>();
        }

        /// <summary>
        ///     Converts the given filter string to the format required in an OPENFILENAME_I
        ///     structure.
        /// </summary>
        private static string MakeFilterString(string s, bool dereferenceLinks)
        {
            if (String.IsNullOrEmpty(s))
            {
                // Workaround for VSWhidbey bug #95338 (carried over from Winforms implementation)
                // Apparently, when filter is null, the common dialogs in Windows XP will not dereference
                // links properly.  The work around is to provide a default filter;  " |*.*" is used to 
                // avoid localization issues from description text.
                //
                // This behavior is now documented in MSDN on the OPENFILENAME structure, so I don't
                // expect it to change anytime soon.
                if (dereferenceLinks && System.Environment.OSVersion.Version.Major >= 5)
                {
                    s = " |*.*";
                }
                else
                {
                    // Even if we don't need the bug workaround, change empty
                    // strings into null strings.
                    return null;
                }
            }

            StringBuilder nullSeparatedFilter = new StringBuilder(s);

            // Replace the vertical bar with a null to conform to the Windows
            // filter string format requirements
            nullSeparatedFilter.Replace('|', '\0');

            // Append two nulls at the end
            nullSeparatedFilter.Append('\0');
            nullSeparatedFilter.Append('\0');

            // Return the results as a string.
            return nullSeparatedFilter.ToString();
        }

        /// <summary>
        /// Handle the AddExtension property on newly acquired filenames, then
        /// call PromptUserIfAppropriate to display any necessary message boxes.
        ///
        /// Returns false if we need to redisplay the dialog and true otherwise.
        /// </summary>
        private bool ProcessFileNames()
        {
            // Only process the filenames if OFN_NOVALIDATE is not set.
            if (!GetOption(NativeMethods.OFN_NOVALIDATE))
            {
                // Call the FilterExtensions private property to get
                // a list of valid extensions from the filter(s).
                // The first extension from FilterExtensions is the
                // default extension.
                string[] extensions = GetFilterExtensions();

                // For each filename:
                //      -  Process AddExtension
                //      -  Call PromptUserIfAppropriate to display necessary dialog boxes.
                for (int i = 0; i < _fileNames.Length; i++)
                {
                    string fileName = _fileNames[i];

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
                            //
                            // Changed to use StringBuilder for perf reasons as per FxCop CA1818
                            StringBuilder s = new StringBuilder(fileName.Substring(0, fileName.Length - currentExtension.Length));
                            // we don't want to append the extension if it contains wild cards
                            if (extensions[j].IndexOfAny(new char[] { '*', '?' }) == -1)
                            {
                                // No wildcards, so go ahead and append
                                s.Append(".");
                                s.Append(extensions[j]);
                            }

                            // If OFN_FILEMUSTEXIST is not set, or if it is set but the filename we generated
                            // does in fact exist, we update fileName and stop trying new extensions.
                            if (!GetOption(NativeMethods.OFN_FILEMUSTEXIST) || File.Exists(s.ToString()))
                            {
                                fileName = s.ToString();
                                break;
                            }
                        }
                        // Store this filename back in the _fileNames array.
                        _fileNames[i] = fileName;
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
            MessageBoxWithFocusRestore(SR.Get(SRID.FileDialogFileNotFound, fileName),
                    System.Windows.MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        #endregion Private Methods

        //---------------------------------------------------
        //
        // Private Properties
        //
        //---------------------------------------------------
        #region Private Properties

        //   If multiple files are selected, we only return the first filename.
        /// <summary>
        ///  Gets a string containing the full path of the file selected in 
        ///  the file dialog box.
        /// </summary>
        private string CriticalFileName
        {
            get
            {
                if (_fileNames == null)        // No filename stored internally...
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
                string[] tokens = filter.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

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
                    throw new InvalidOperationException(SR.Get(SRID.FileDialogInvalidFilterIndex));
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

        /// <summary>
        ///  Gets an integer representing the Win32 common Open File Dialog OFN_* option flags
        ///  used to display a dialog with the current set of property values.
        /// </summary>
        //
        //   We bitwise AND _dialogOptions with all of the options we consider valid
        //   before returning the resulting bitmask to avoid accidentally setting a
        //   flag we don't intend to.  Note that this list doesn't include a few of the
        //   flags we set right before showing the dialog in RunDialog (like 
        //   NativeMethods.OFN_EXPLORER), since those are only added when creating
        //   the OPENFILENAME structure.
        //  
        //   Also note that our private flags are not included in this list (like
        //   OPTION_ADDEXTENSION)
        protected int Options
        {
            get
            {
                return _dialogOptions.Value & (NativeMethods.OFN_READONLY | NativeMethods.OFN_HIDEREADONLY |
                                  NativeMethods.OFN_NOCHANGEDIR | NativeMethods.OFN_NOVALIDATE |
                                  NativeMethods.OFN_ALLOWMULTISELECT | NativeMethods.OFN_PATHMUSTEXIST |
                                  NativeMethods.OFN_NODEREFERENCELINKS);
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
        private sealed class VistaDialogEvents : IFileDialogEvents, IDisposable
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
        // These methods are intended to be internal AND protected, but C# doesn't allow that declaration.

        internal abstract IFileDialog CreateVistaDialog();

        internal abstract string[] ProcessVistaFiles(IFileDialog dialog);

        #endregion

        #region Internal Methods

        internal virtual void PrepareVistaDialog(IFileDialog dialog)
        {
            dialog.SetDefaultExtension(DefaultExt);

            dialog.SetFileName(CriticalFileName);

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

            // Force no mini mode for the SaveFileDialog.
            // Only accept physically backed locations.
            FOS options = ((FOS)Options & c_VistaFileDialogMask) | FOS.DEFAULTNOMINIMODE | FOS.FORCEFILESYSTEM;
            dialog.SetOptions(options);

            COMDLG_FILTERSPEC[] filterItems = GetFilterItems(Filter);
            if (filterItems.Length > 0)
            {
                dialog.SetFileTypes((uint)filterItems.Length, filterItems);
                dialog.SetFileTypeIndex(unchecked((uint)FilterIndex));
            }

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

        private bool UseVistaDialog
        {
            get { return Environment.OSVersion.Version.Major >= 6; }
        }

        private bool RunVistaDialog(IntPtr hwndOwner)
        {
            IFileDialog dialog = CreateVistaDialog();

            PrepareVistaDialog(dialog);

            using (VistaDialogEvents events = new VistaDialogEvents(dialog, HandleVistaFileOk))
            {
                return dialog.Show(hwndOwner).Succeeded;
            }
        }

        private bool HandleVistaFileOk(IFileDialog dialog)
        {
            // When this callback occurs, the HWND is visible and we need to
            // grab it because it is used for various things like looking up the
            // DialogCaption.
            UnsafeNativeMethods.IOleWindow oleWindow = (UnsafeNativeMethods.IOleWindow)dialog;
            oleWindow.GetWindow(out _hwndFileDialog);

            int saveOptions = _dialogOptions.Value;
            int saveFilterIndex = _filterIndex;
            string[] saveFileNames = _fileNames;
            bool ok = false;

            try
            {
                uint filterIndexTemp = dialog.GetFileTypeIndex();
                _filterIndex = unchecked((int)filterIndexTemp);
                _fileNames = ProcessVistaFiles(dialog);
                if (ProcessFileNames())
                {
                    var cancelArgs = new CancelEventArgs();
                    OnFileOk(cancelArgs);
                    ok = !cancelArgs.Cancel;
                }
            }
            finally
            {
                if (!ok)
                {
                    _fileNames = saveFileNames;
                    _dialogOptions.Value = saveOptions;
                    _filterIndex = saveFilterIndex;
                }
                else
                {
                    if (0 != (Options & NativeMethods.OFN_HIDEREADONLY))
                    {
                        // When the dialog is dismissed OK, the Readonly bit can't be left on if ShowReadOnly was false
                        // Downlevel this happens automatically.  On Vista we need to watch out for it.
                        _dialogOptions.Value &= ~NativeMethods.OFN_READONLY;
                    }
                }
            }
            return ok;
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

        private const FOS c_VistaFileDialogMask = FOS.OVERWRITEPROMPT | FOS.NOCHANGEDIR | FOS.NOVALIDATE | FOS.ALLOWMULTISELECT | FOS.PATHMUSTEXIST | FOS.FILEMUSTEXIST | FOS.CREATEPROMPT | FOS.NODEREFERENCELINKS;

        // _dialogOptions is a set of bit flags used to control the behavior
        // of the Win32 dialog box.
        private SecurityCriticalDataForSet<int> _dialogOptions;

        // These two flags are related to a fix for an issue where Windows
        // sends two FileOK notifications back to back after a sharing
        // violation occurs.  See CDN_SHAREVIOLATION in HookProc for details.
        private bool _ignoreSecondFileOkNotification;
        private int _fileOkNotificationCount;

        // These private variables store data for the various public properties
        // that control the appearance of the file dialog box.
        private SecurityCriticalDataForSet<string> _title;                  // Title bar of the message box
        private SecurityCriticalDataForSet<string> _initialDirectory;       // Starting directory
        private string _defaultExtension;       // Extension appended first if AddExtension
                                                // is enabled
        private string _filter;                 // The file extension filters that display
                                                // in the "Files of Type" box in the dialog
        private int _filterIndex;               // The index of the currently selected
                                                // filter (a default filter index before
                                                // the dialog is called, and the filter
                                                // the user selected afterwards.)  This
                                                // index is 1-based, not 0-based.

        // Since we have to interop with native code to show the file dialogs,
        // we use the CharBuffer class to help with the marshalling of
        // unmanaged memory that stores the user-selected file names.
        private CharBuffer _charBuffer;

        // We store the handle of the file dialog inside our class 
        // for a variety of purposes (like getting the title of the dialog
        // box when we need to show a message box with the same title bar caption)
        private IntPtr _hwndFileDialog;

        // This is the array that stores the filename(s) the user selected in the
        // dialog box.  If Multiselect is not enabled, only the first element
        // of this array will be used.
        private string[] _fileNames;

        // Constant to control the initial size of the character buffer;
        // 8192 is an arbitrary but reasonable size that should minimize the
        // number of times we need to grow the buffer.
        private const int FILEBUFSIZE = 8192;

        // OPTION_ADDEXTENSION is our own bit flag that we use to control our
        // own automatic extension appending feature.
        private const int OPTION_ADDEXTENSION = unchecked(unchecked((int)0x80000000));

        #endregion Private Fields
    }
}
