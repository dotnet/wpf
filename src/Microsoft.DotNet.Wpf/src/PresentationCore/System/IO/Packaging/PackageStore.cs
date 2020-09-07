// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  
//
//
//  Description:   Collection of packages to be used with PackWebRequest.
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.IO.Packaging;
using System.Security;
using System.Windows.Navigation;
using SecurityHelper=MS.Internal.SecurityHelper; 
using MS.Internal.PresentationCore;     // for ExceptionStringTable

namespace System.IO.Packaging
{
    // Note: we purposely didn't make this class a dictionary to limit the access
    //  to the packages in the store
    //
    /// <summary>
    /// PackageStore: Collection of packages to be used with PackWebRequest.
    /// PackWebRequest will use a package from PackageStore if package uri matches
    /// to obtain the requested part stream
    /// This mechanism can be used so that PackWebRequest wouldn't open a package
    /// multiple times to load different resources from the same package.
    /// </summary>
    /// <remarks>
    /// Note: Packages placed in PackageStore can be used in multi threading environment if it is used in
    ///  conjunction with Xaml Parser (XmlReader.Load). It is up to an application to do proper locking on the package
    ///  when it accesses the package directly.
    /// Note: The access to the packages obtained from PackageStore is not limited (or the access level is same
    ///  as the one that is used to do open). It is up to an application to do proper action in modifying or closing
    ///  packages.
    /// </remarks>
    public static class PackageStore
    {
        static PackageStore()
        {
            _globalLock = new Object();
        }

        #region public Methods

        // 
        /// <summary>
        /// Retrieves a package from the store for the given Uri.
        /// </summary>
        /// <param name="uri">key uri</param>
        /// <returns>Package</returns>
        /// <permission cref="EnvironmentPermission"></permission>
        /// <remarks>
        /// </remarks>
        public static Package GetPackage(Uri uri)
        {
            ValidatePackageUri(uri);
  
            lock (_globalLock)
            {
                Package package = null;

                if (_packages != null && _packages.Contains(uri))
                {
                    package = (Package) _packages[uri];
                }
                
                return package;
            }
        }

        /// <summary>
        /// Adds a uri, package pair to the package store.
        /// </summary>
        /// <param name="uri">key uri</param>
        /// <param name="package">package</param>
        /// <permission cref="EnvironmentPermission"></permission>
        /// <remarks>
        /// If a package with the uri is already in the store,it throws an exception.
        /// The package will not be automatically replaced within the store.
        /// </remarks>
        public static void AddPackage(Uri uri, Package package)
        {
            ValidatePackageUri(uri);

            // There are well-known package types that are only for internal use (for resource loading)
            //  (i.e. ResourceContainer - "application://" and SiteOriginContainer - "siteoforigin://"
            // Adding packages with such key uri will have no effect on PackWebRequest since
            //  they cannot be overriden. So, calling this method with such key Uris should be prevented 
            //  However, uri.Equal cannot be used here since the key Uris are used as a pack Uri form and
            //  only PackUriHelper.ComparePackUri can do the proper comparison of pack Uris.

            Uri packUri = PackUriHelper.Create(uri);
       
            if (PackUriHelper.ComparePackUri(packUri, BaseUriHelper.PackAppBaseUri) == 0 ||
                PackUriHelper.ComparePackUri(packUri, BaseUriHelper.SiteOfOriginBaseUri) == 0)
            {
                throw new ArgumentException(SR.Get(SRID.NotAllowedPackageUri), nameof(uri));
            }

            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            lock (_globalLock)
            {
                if (_packages == null)
                {
                    _packages = new HybridDictionary(2);
                }

                if (_packages.Contains(uri))
                {
                    throw new InvalidOperationException(SR.Get(SRID.PackageAlreadyExists));
                }
                
                _packages.Add(uri, package);
            }
        }

        /// <summary>
        /// Removes a uri, package pair from the package store.
        /// </summary>
        /// <param name="uri">key uri</param>
        /// <permission cref="EnvironmentPermission"></permission>
        /// <remarks>
        /// </remarks>
        public static void RemovePackage(Uri uri)
        {
            ValidatePackageUri(uri);

            lock (_globalLock)
            {
                if (_packages != null)
                {
                    // If the key doesn't exist, it is no op
                    _packages.Remove(uri);
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static void ValidatePackageUri(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException(SR.Get(SRID.UriMustBeAbsolute), "uri");
            }
        }
        

        #endregion Private Methods
    
        #region Private Fields

        // We expect to have no more than 10 packages in the store
        //  per AppDomain for our scenarios
        // ListDictionary is the best fit for this scenarios; otherwise we should be using
        // Hashtable. HybridDictionary already has functionality of switching between
        //  ListDictionary and Hashtable depending on the size of the collection
        static private HybridDictionary _packages;
        static private Object _globalLock;

        #endregion Private Fields
    }
}
