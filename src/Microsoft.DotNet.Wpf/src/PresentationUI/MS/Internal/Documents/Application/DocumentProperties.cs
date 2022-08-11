// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: This is a wrapper for DocumentPropertiesDialog, which caches the values which
//              are displayed in the Dialog, and controls security access.

using MS.Internal.PresentationUI;           // For CriticalDataForSet
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Packaging;                  // For Package
using System.Security;                      // For CriticalData
using System.Windows.TrustUI;               // For string resources
using System.Windows.Xps.Packaging;         // For XpsDocument

namespace MS.Internal.Documents.Application
{
    /// <summary>
    /// Singleton wrapper class for the DocumentPropertiesDialog.
    /// </summary>
    [FriendAccessAllowed]
    internal sealed class DocumentProperties
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region Constructors

        /// <summary>
        /// Constructs the DocumentProperties class.
        /// </summary>
        /// <param name="uri"></param>
        private DocumentProperties(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            _uri = new SecurityCriticalData<Uri>(uri);
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        #region Internal Properties
        /// <summary>
        /// Current package CoreProperties
        /// </summary>
        internal PackageProperties CoreProperties
        {
            get
            {
                // multiple returns are bad, however allocating another local
                // simply to return is worse
                // also we can not cache this decision because the values
                // may change when RM is on / off
                if (_rmProperties != null)
                {
                    return _rmProperties;
                }
                else
                {
                    return _xpsProperties;
                }
            }
        }
        /// <summary>
        /// Gets the current singleton instance of DocumentProperties.
        /// </summary>
        internal static DocumentProperties Current
        {
            get
            {
                return _current;
            }
        }
        /// <summary>
        /// Image representing an XPS Document
        /// </summary>
        internal Image Image
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
            }
        }
        /// <summary>
        /// Filename of the package.
        /// </summary>
        internal string Filename
        {
            get
            {
                if (_filename == null && _uri.Value != null)
                {
                    _filename = Path.GetFileName(_uri.Value.LocalPath);
                }
                return _filename;
            }
        }
        /// <summary>
        /// Size of the package.
        /// </summary>
        internal long FileSize
        {
            get
            {
                long fileSize = 0;

                if (IsFileInfoValid)
                {
                    fileSize = _fileInfo.Length;
                }

                return fileSize;
            }
        }
        /// <summary>
        /// Date package was created.
        /// </summary>
        internal DateTime? FileCreated
        {
            get
            {
                DateTime? fileCreated = null;

                if (IsFileInfoValid)
                {
                    fileCreated = _fileInfo.CreationTime;
                }

                return fileCreated;
            }
        }
        /// <summary>
        /// Date package was last modified.
        /// </summary>
        internal DateTime? FileModified
        {
            get
            {
                DateTime? fileModified = null;

                if (IsFileInfoValid)
                {
                    fileModified = _fileInfo.LastWriteTime;
                }

                return fileModified;
            }
        }
        /// <summary>
        /// Date package was last accessed.
        /// </summary>
        internal DateTime? FileAccessed
        {
            get
            {
                DateTime? fileAccessed = null;

                if (IsFileInfoValid)
                {
                    fileAccessed = _fileInfo.LastAccessTime;
                }

                return fileAccessed;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        #region Internal Methods

        /// <summary>
        /// This method copies members from one PackageProperties object to another.
        /// </summary>
        /// <remarks>
        /// This design is very fragile as if new properties are added by PackageProperties
        /// we will quietly lose data.  An improved implementation would be to have
        /// PackageProperties implement this behavior or have a KeyValue pair exposed
        /// that we would iterate through and copy.
        /// </remarks>
        internal static void Copy(PackageProperties source, PackageProperties target)
        {
            target.Category = source.Category;
            target.ContentStatus = source.ContentStatus;
            target.ContentType = source.ContentType;
            target.Created = source.Created;
            target.Creator = source.Creator;
            target.Description = source.Description;
            target.Identifier = source.Identifier;
            target.Keywords = source.Keywords;
            target.Language = source.Language;
            target.LastModifiedBy = source.LastModifiedBy;
            target.LastPrinted = source.LastPrinted;
            target.Modified = source.Modified;
            target.Revision = source.Revision;
            target.Subject = source.Subject;
            target.Title = source.Title;
            target.Version = source.Version;
        }        

        /// <summary>
        /// Constructs and Initializes the static 'Current' DocumentProperties object which
        /// is the singleton instance.
        /// </summary>
        /// <param name="uri">The URI for the package.</param>
        internal static void InitializeCurrentDocumentProperties(Uri uri)
        {
            // Ensure that DocumentProperties has not been constructed yet, and initialize a
            // new instance.
            System.Diagnostics.Debug.Assert(
                Current == null,
                "DocumentProperties initialized twice.");

            if (Current == null)
            {
                _current = new DocumentProperties(uri);
            }
        }

        internal void SetXpsProperties(PackageProperties properties)
        {
            _xpsProperties = properties;
        }

        internal void SetRightsManagedProperties(PackageProperties properties)
        {
            _rmProperties = properties;
        }

        /// <summary>
        /// Confirms whether the XPS properties exactly match the RM properties
        /// for purposes of determining whether the OPC properties have been changed
        /// in violation of signing policy.        
        /// </summary>
        /// <returns>
        /// This will return true if the properties match
        /// or there are no RM properties in the package,
        /// otherwise it will return false.
        /// </returns>
        internal bool VerifyPropertiesUnchanged()
        {
            if (_xpsProperties == null )
            {
                return false;
            }

            if (_rmProperties == null)
            {
                return true;
            }
                       
            return
               (String.Equals(_xpsProperties.Category, _rmProperties.Category, StringComparison.Ordinal) &&
                String.Equals(_xpsProperties.ContentStatus, _rmProperties.ContentStatus, StringComparison.Ordinal) &&
                String.Equals(_xpsProperties.ContentType, _rmProperties.ContentType, StringComparison.Ordinal) &&
                String.Equals(_xpsProperties.Creator, _rmProperties.Creator, StringComparison.Ordinal) &&
                String.Equals(_xpsProperties.Description, _rmProperties.Description, StringComparison.Ordinal) &&
                String.Equals(_xpsProperties.Identifier, _rmProperties.Identifier, StringComparison.Ordinal) &&
                String.Equals(_xpsProperties.Keywords, _rmProperties.Keywords, StringComparison.Ordinal) &&
                String.Equals(_xpsProperties.Language, _rmProperties.Language, StringComparison.Ordinal) &&
                String.Equals(_xpsProperties.LastModifiedBy, _rmProperties.LastModifiedBy, StringComparison.Ordinal) &&
                String.Equals(_xpsProperties.Revision, _rmProperties.Revision, StringComparison.Ordinal) &&
                String.Equals(_xpsProperties.Subject, _rmProperties.Subject, StringComparison.Ordinal) &&
                String.Equals(_xpsProperties.Title, _rmProperties.Title, StringComparison.Ordinal) &&
                String.Equals(_xpsProperties.Version, _rmProperties.Version, StringComparison.Ordinal) &&
                _xpsProperties.Created == _rmProperties.Created &&
                _xpsProperties.LastPrinted == _rmProperties.LastPrinted &&
                _xpsProperties.Modified == _rmProperties.Modified);
                               
        }

        /// <summary>
        /// ShowDialog:  Displays the DocumentProperties dialog.
        /// </summary>
        internal void ShowDialog()
        {
            // Setup the dialog data once and cache it for future calls.
            if (!_isDataAcquired)
            {
                AcquireData();
                _isDataAcquired = true;
            }

            // Refresh file information before showing the dialog.
            if (_fileInfo != null)
            {
                // We intentionally `swallow exceptions here, since any failure
                // will make _fileInfo invalid as determined by the property
                // IsFileInfoValid. We also don't set _fileInfo to null so that
                // we can call Refresh again. This is useful since the file may
                // be inaccessible now but come back later.
                try
                {
                    _fileInfo.Refresh();
                }
                catch (ArgumentException)
                {
                }
                catch (IOException)
                {
                }
            }

            DocumentPropertiesDialog dialog = null;
            dialog = new DocumentPropertiesDialog();
            dialog.ShowDialog();
            if (dialog != null)
            {
                dialog.Dispose();
            }
        }
        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods
        /// <summary>
        /// AcquireData:  Setup the URI related data for the dialog.
        /// </summary>
        private void AcquireData()
        {
            // Ensure URI exists.
            if (_uri.Value == null)
            {
                return;
            }

            // Determine if the URI represents a file
            if (_uri.Value.IsFile)
            {
                // Determine the full path and assert for file permission
                string filePath = _uri.Value.LocalPath;

                // Get the FileInfo for the current file
                FileInfo fileInfo = new FileInfo(filePath);

                // Check that FileInfo is valid, and save it.
                if (fileInfo.Exists)
                {
                    _fileInfo = fileInfo;
                }
            }
        }
        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        #region Private Properties

        /// <summary>
        /// True if the stored FileInfo is valid and can be used to determine
        /// file properties.
        /// </summary>
        /// <remarks>
        /// If the file has become completely inaccessible (i.e. the drive has
        /// been disconnected, the UNC path is unreachable, etc.), according to
        /// MSDN the Refresh call should throw an ArgumentException or
        /// IOException. In reality it doesn't appear to ever throw. Instead,
        /// if Refresh fails, any future calls to _fileInfo.Exists return
        /// false. Accessing any of the file information properties at that
        /// point throws an exception. As a result, if _fileInfo.Refresh()
        /// fails, we can leave _fileInfo as is and simply check
        /// _fileInfo.Exists before accessing it.
        /// </remarks>
        private bool IsFileInfoValid
        {
            get
            {
                return ((_fileInfo != null) && (_fileInfo.Exists));
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //  These must be checked for null prior to use.  If available, use the CLR properties
        //  above before these.
        //
        //------------------------------------------------------
        #region Private Fields
        private static DocumentProperties       _current = null;
        private SecurityCriticalData<Uri> _uri;

        /// <summary>
        /// The properties in the XpsPackage (OPC).
        /// </summary>
        private PackageProperties _xpsProperties = null;

        /// <summary>
        /// The properties for RightsManaged encrypted packages (OLE).
        /// </summary>
        private PackageProperties _rmProperties = null;

        private bool _isDataAcquired;
        private Image _image = null;
        private string _filename = null;

        private FileInfo _fileInfo;

        #endregion Private Fields
    }
}
