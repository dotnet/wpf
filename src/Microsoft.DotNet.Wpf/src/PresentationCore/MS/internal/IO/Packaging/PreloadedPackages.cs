// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//  
//
//
//  Description:   Collection of preloaded packages to be used with
//                  PackWebRequest.
//

using System.Collections.Specialized;
using System.IO.Packaging;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using MS.Internal.PresentationCore;     // for ExceptionStringTable

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// PreloadedPackages
    /// </summary>
    /// <remarks>Note: we purposely didn't make this class a dictionary since it is an internal
    ///  class and we won't be using even half of the dictionary functionalities.
    ///  If this class becomes a public class which is strongly discouraged, this class
    ///  needs to implement IDictionary.</remarks>
    internal static class PreloadedPackages 
    {
        //------------------------------------------------------
        //
        //  Static Constructors
        //
        //------------------------------------------------------
        static PreloadedPackages()
        {
            _globalLock = new Object();
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        #region Internal Methods

        /// <summary>
        /// GetPackage given a uri
        /// </summary>
        /// <param name="uri">uri to match on</param>
        /// <returns>object if found - else null</returns>
        /// <exception cref="ArgumentException">uri must be absolute</exception>
        internal static Package GetPackage(Uri uri)
        {
            bool ignored;
            return GetPackage(uri, out ignored);
        }

        /// <summary>
        /// GetPackage given a uri
        /// </summary>
        /// <param name="uri">uri to match on</param>
        /// <param name="threadSafe">true if the returned package is threadsafe - undefined if null is returned</param>
        /// <returns>object if found - else null</returns>
        /// <exception cref="ArgumentException">uri must be absolute</exception>
        internal static Package GetPackage(Uri uri, out bool threadSafe)
        {
            ValidateUriKey(uri);

            lock (_globalLock)
            {
                Package package = null;
                threadSafe = false;

                if (_packagePairs != null)
                {
                    PackageThreadSafePair packagePair = _packagePairs[uri] as PackageThreadSafePair;
                    if (packagePair != null)
                    {
                        package = packagePair.GetPackageForCurrentContext();
                        threadSafe = packagePair.ThreadSafe;
                    }
                }

                return package;
            }
        }

        /// <summary>
        /// AddPackage - default to non-thread-safe
        /// </summary>
        /// <param name="uri">uri to use for matching</param>
        /// <param name="package">package object to serve content from</param>
        /// <remarks>Adds a uri, content pair to the cache. If the uri is already
        /// in the cache, this removes the old content and replaces it.
        /// The object will not be subject to automatic removal from the cache</remarks>
        internal static void AddPackage(Uri uri, Package package)
        {
            AddPackage(uri, package, false);
        }

        /// <summary>
        /// AddPackage
        /// </summary>
        /// <param name="uri">uri to use for matching</param>
        /// <param name="package">package object to serve content from</param>
        /// <param name="threadSafe">is package thread-safe?</param>
        /// <remarks>Adds a uri, content pair to the cache. If the uri is already
        /// in the cache, this removes the old content and replaces it.
        /// The object will not be subject to automatic removal from the cache</remarks>
        internal static void AddPackage(Uri uri, Package package, bool threadSafe)
        {
            AddPackage(uri, package, threadSafe, createForLoadContext: null);
        }

        /// <summary>
        /// Adds a uri, package pair to the package cache, with a factory for the instance a load
        /// context other than the default one is served.
        /// </summary>
        /// <param name="uri">key uri</param>
        /// <param name="package">package served to the default load context</param>
        /// <param name="threadSafe">whether the package is thread-safe</param>
        /// <param name="createForLoadContext">
        /// Creates the package served while another AssemblyLoadContext is the current contextual
        /// reflection context, so that what a package caches is never shared across contexts;
        /// <see langword="null"/> to serve every context the same package.
        /// </param>
        internal static void AddPackage(Uri uri, Package package, bool threadSafe, Func<Package> createForLoadContext)
        {
            ValidateUriKey(uri);

            lock (_globalLock)
            {
                if (_packagePairs == null)
                {
                    _packagePairs = new HybridDictionary(3);
                }

                _packagePairs.Add(uri, new PackageThreadSafePair(package, threadSafe, createForLoadContext));
            }
        }

        /// <summary>
        /// RemovePackage
        /// </summary>
        /// <param name="uri">uri of the package that needs to be removed </param>
        /// <remarks>Removes the package corresponding to the uri from the cache. If a matching uri isn't found 
        ///  the status of the cache doesn't change and no exception is throwen
        /// </remarks>
        internal static void RemovePackage(Uri uri)
        {
            ValidateUriKey(uri);

            lock (_globalLock)
            {
                _packagePairs?.Remove(uri);
            }
        }

        // Null the instance.  Similar to Dispose, but not quite.
        internal static void Clear()
        {
            lock (_globalLock)
            {
                _packagePairs = null;
            }
        }

        private static void ValidateUriKey(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);

            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException(SR.UriMustBeAbsolute, nameof(uri));
            }
        }

        #endregion Internal Methods

        /// <summary>
        /// Package-bool pair where the bool represents the thread-safety status of the package
        /// </summary>
        private class PackageThreadSafePair
        {
            //------------------------------------------------------
            //
            //  Internal Constructors
            //
            //------------------------------------------------------
            internal PackageThreadSafePair(Package package, bool threadSafe, Func<Package> createForLoadContext)
            {
                Invariant.Assert(package != null);

                _package = package;
                _threadSafe = threadSafe;
                _createForLoadContext = createForLoadContext;
            }

            /// <summary>
            /// The package served to the current load context: the shared one, unless a factory was
            /// given and an AssemblyLoadContext other than the default one is current.
            /// </summary>
            internal Package GetPackageForCurrentContext()
            {
                AssemblyLoadContext context = AssemblyLoadContext.CurrentContextualReflectionContext;

                if (_createForLoadContext == null || context == null || context == AssemblyLoadContext.Default)
                {
                    return _package;
                }

                return _contextPackages.GetValue(context, _ => _createForLoadContext());
            }

            //------------------------------------------------------
            //
            //  Internal Properties
            //
            //------------------------------------------------------
            /// <summary>
            /// Package
            /// </summary>
            internal Package Package
            {
                get
                {
                    return _package;
                }
            }

            /// <summary>
            /// True if package is thread-safe
            /// </summary>
            internal bool ThreadSafe
            {
                get
                {
                    return _threadSafe;
                }
            }

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------
            private readonly Package _package;
            private readonly bool    _threadSafe;
            private readonly Func<Package> _createForLoadContext;
            private readonly ConditionalWeakTable<AssemblyLoadContext, Package> _contextPackages = new();
        }


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields

        // We are expect to have no more than 10 preloaded packages
        //  per AppDomain for our scenarios
        // ListDictionary is the best fit for this scenarios; otherwise we should be using
        // Hashtable. HybridDictionary already has functionality of switching between
        //  ListDictionary and Hashtable depending on the size of the collection
        private static HybridDictionary _packagePairs;
        private static readonly Object  _globalLock;

        #endregion Private Fields
    }
}
