// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
// ContentFilePart is an implementation of the abstract PackagePart class. It contains an override for GetStreamCore.
//

using System;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Resources;
using System.IO;
using System.Resources;
using System.Globalization;
using System.Security;
using System.Security.Permissions;
using System.Windows.Navigation;
using System.Diagnostics;
using System.Reflection;

namespace MS.Internal.AppModel
{
    /// <summary>
    /// ContentFilePart is an implementation of the abstract PackagePart class. It contains an override for GetStreamCore.
    /// </summary>
    internal class ContentFilePart : System.IO.Packaging.PackagePart
    {
        //------------------------------------------------------
        //
        //  Public Constructors
        //
        //------------------------------------------------------

        #region Public Constructors

        /// <securitynote>
        /// Critical    -   Accesses member _fullPath.
        /// TreatAsSafe -   Initializing _fullPath to null is safe
        /// </securitynote>
        internal ContentFilePart(Package container, Uri uri) :
                base(container, uri)
        {
            Invariant.Assert(Application.ResourceAssembly != null, "If the entry assembly is null no ContentFileParts should be created");
            _fullPath = null;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <securitynote>
        /// Critical    -   Calls critical methods GetEntryAssemblyLocation() and CriticalOpenFile() 
        ///                 and accesses critical member _fullPath.
        /// TreatAsSafe -   The Uri supplied at construction is read only and must be on the list
        ///                 of loose content files supplied at application compile time.  It is ok
        ///                 to return the stream because we know that the stream will be read only
        ///                 and cannot be used to get the application into an invalid state.
        /// </securitynote>
        protected override Stream GetStreamCore(FileMode mode, FileAccess access)
        {
            Stream stream = null;

            if (_fullPath == null)
            {
                // File name will be a path relative to the applications directory.
                // - We do not want to use SiteOfOriginContainer.SiteOfOrigin because
                //   for deployed files the <Content> files are deployed with the application.
                Uri codeBase = GetEntryAssemblyLocation();

                string assemblyName, assemblyVersion, assemblyKey;
                string filePath;

                // For now, only Application assembly supports content files, 
                // so we can simply ignore the assemblyname etc.
                // In the future, we may extend this support for regular library assembly,
                // assemblyName will be used to predict the right file path.

                BaseUriHelper.GetAssemblyNameAndPart(Uri, out filePath, out assemblyName, out assemblyVersion, out assemblyKey);

                // filePath should not have leading slash.  GetAssemblyNameAndPart( ) can guarantee it.
                Uri file = new Uri(codeBase, filePath);
                _fullPath = file.LocalPath;
            }

            stream = CriticalOpenFile(_fullPath);

            if (stream == null)
            {
                throw new IOException(SR.Get(SRID.UnableToLocateResource, Uri.ToString()));
            }

            return stream;
        }

        protected override string GetContentTypeCore()
        {
            return MS.Internal.MimeTypeMapper.GetMimeTypeFromUri(new Uri(Uri.ToString(), UriKind.RelativeOrAbsolute)).ToString();
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods


        /// <securitynote>
        /// Asserts for to get the location of the entry assembly
        /// </securitynote>
        private Uri GetEntryAssemblyLocation()
        {
            Uri entryLocation = null;
            System.Security.PermissionSet permissionSet = new PermissionSet(null);
            permissionSet.AddPermission(new FileIOPermission(PermissionState.Unrestricted));
            permissionSet.Assert();
            try
            {
                entryLocation = new Uri(Application.ResourceAssembly.CodeBase);
            }
            catch (Exception ex)
            {
                if (CriticalExceptions.IsCriticalException(ex))
                {
                    throw;
                }
                // `Swallow any other exceptions to avoid disclosing the critical path.
                // 
                // Possible Exceptions: ArgumentException, ArgumentNullException, PathTooLongException
                // DirectoryNotFoundException, IOException, UnauthorizedAccessException, 
                // ArgumentOutOfRangeException, FileNotFoundException, NotSupportedException
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }
            return entryLocation;
        }

        /// <securitynote>
        /// Asserts to open the file
        /// </securitynote>
        private Stream CriticalOpenFile(string filename)
        {
            Stream s = null;
            FileIOPermission filePermission = new FileIOPermission(FileIOPermissionAccess.Read, filename);
            filePermission.Assert();
            try
            {
                s = System.IO.File.Open(filename, FileMode.Open, FileAccess.Read, ResourceContainer.FileShare);
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }
            return s;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Members

        /// <securitynote>
        /// Contains critical path information that shouldn't be disclosed.
        /// </securitynote>
        private string _fullPath;

        #endregion Private Members
    }
}

