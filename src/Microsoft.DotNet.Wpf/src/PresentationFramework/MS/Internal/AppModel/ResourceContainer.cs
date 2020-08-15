// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
// ResourceContainer is an implementation of the abstract Package class. 
// It contains nontrivial overrides for GetPartCore and Exists.
// Many of the methods on Package are not applicable to loading application 
// resources, so the ResourceContainer implementations of these methods throw 
// the NotSupportedException.
// 

using System;
using System.IO.Packaging;
using System.IO;
using System.Collections.Generic;
using System.Windows.Resources;
using System.Resources;
using System.Reflection;
using System.Globalization;
using MS.Internal.PresentationFramework;                   // SafeSecurityHelper
using System.Windows;
using System.Windows.Navigation;
using MS.Internal.Resources;
using System.Windows.Interop;
using System.Security;

namespace MS.Internal.AppModel
{
    // <summary>
    // ResourceContainer is an implementation of the abstract Package class. 
    // It contains nontrivial overrides for GetPartCore and Exists.
    // Many of the methods on Package are not applicable to loading application 
    // resources, so the ResourceContainer implementations of these methods throw 
    // the NotSupportedException.
    // </summary>
    internal class ResourceContainer : System.IO.Packaging.Package
    {
        //------------------------------------------------------
        //
        //  Static Methods
        //
        //------------------------------------------------------

        #region Static Methods


        internal static ResourceManagerWrapper ApplicationResourceManagerWrapper
        {
            get
            {
                if (_applicationResourceManagerWrapper == null)
                {
                    // load main excutable assembly
                    Assembly asmApplication = Application.ResourceAssembly;

                    if (asmApplication != null)
                    {
                        _applicationResourceManagerWrapper = new ResourceManagerWrapper(asmApplication);
                    }
                }

                return _applicationResourceManagerWrapper;
            }
        }

        // <summary>
        //  The FileShare mode to use for opening loose files.  Currently this defaults to FileShare.Read
        //  Today it is not changed.  If we decide that this should change in the future we can easily add
        //  a seter here.
        // </summary>
        internal static FileShare FileShare
        {
            get
            {
                return _fileShare;
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Constructors
        //
        //------------------------------------------------------

        #region Public Constructors

        // <summary>
        // Default Constructor
        // </summary>
        internal ResourceContainer() : base(FileAccess.Read)
        {
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        // None  

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------        

        #region Public Methods

        // <summary>
        // This method always returns true.  This is because ResourceManager does not have a
        // simple way to check if a resource exists without loading the resource stream (or failing to)
        // so checking if a resource exists would be a very expensive task.
        // A part will later be constructed and returned by GetPart().  This part class contains
        // a ResourceManager which may or may not contain the requested resource.  When someone 
        // calls GetStream() on PackagePart then we will attempt to get the stream for the named resource 
        // and potentially fail.
        // </summary>
        // <param name="uri"></param>
        // <returns></returns>
        public override bool PartExists(Uri uri)
        {
            return true;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Internal Constructors
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Members

        internal const string XamlExt = ".xaml";
        internal const string BamlExt = ".baml";

        #endregion


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Protected Constructors
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        // <summary>
        // This method creates a part containing the name of the resource and 
        // the resource manager that should contain it.  If the resource manager 
        // does not contain the requested part then when GetStream() is called on
        // the part it will return null.
        // </summary>
        // <param name="uri"></param>
        // <returns></returns>

        protected override PackagePart GetPartCore(Uri uri)
        {
            string partName;
            bool isContentFile;

            // AppDomain.AssemblyLoad event handler for standalone apps. This is added specifically for designer (Sparkle) scenario.
            // We use the assembly name to fetch the cached resource manager. With this mechanism we will still get resource from the 
            // old version dll when a newer one is loaded. So whenever the AssemblyLoad event is fired, we will need to update the cache 
            // with the newly loaded assembly. This is currently only for designer so not needed for browser hosted apps. 
            // Attach the event handler before the first time we get the ResourceManagerWrapper.
            if (!assemblyLoadhandlerAttached)
            {
                AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(OnAssemblyLoadEventHandler);
                assemblyLoadhandlerAttached = true;
            }

            ResourceManagerWrapper rmWrapper = GetResourceManagerWrapper(uri, out partName, out isContentFile);

            // If the part name was specified as Content at compile time then we will try to load
            // the file directly.  Otherwise we assume the user is looking for a resource.
            if (isContentFile)
            {
                return new ContentFilePart(this, uri);
            }
            else
            {
                // Uri mapps to a resource stream.

                // Make sure the resource id is exactly same as the one we used to create Resource 
                // at compile time.
                partName = ResourceIDHelper.GetResourceIDFromRelativePath(partName);

                return new ResourcePart(this, uri, partName, rmWrapper);
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // AppDomain.AssemblyLoad event handler. Check whether the assembly's resourcemanager has
        // been added to the cache. If it has, we need to update the cache with the newly loaded dll.
        private void OnAssemblyLoadEventHandler(object sender, AssemblyLoadEventArgs args)
        {
            Assembly assembly = args.LoadedAssembly;
            // This is specific for designer (Sparkle) scenario: rebuild and reload dll using Load(Byte[]).
            // We do not care about assemblies loaded into the reflection-only context or the Gaced assemblies.
            // For example, in Sparkle whenever a project is built all dependent assemblies will be loaded reflection only.
            // We do no care about those. Only when a assembly is loaded into the execution context, we will need to update the cache. 
            #pragma warning disable SYSLIB0005 // 'Assembly.GlobalAssemblyCache' is obsolete.
            if ((!assembly.ReflectionOnly) && (!assembly.GlobalAssemblyCache))
            #pragma warning restore SYSLIB0005 // 'Assembly.GlobalAssemblyCache' is obsolete.
            {
                AssemblyName assemblyInfo = new AssemblyName(assembly.FullName);

                string assemblyName = assemblyInfo.Name.ToLowerInvariant();
                string assemblyKey = string.Empty;
                string key = assemblyName;

                // Check if this newly loaded assembly is in the cache. If so, update the cache.
                // If it is not in cache, do not do anything. It will be added on demand.
                // The key could be Name, Name + Version, Name + PublicKeyToken, or Name + Version + PublicKeyToken.  
                // Otherwise, update the cache with the newly loaded dll.

                // First check Name. 
                UpdateCachedRMW(key, args.LoadedAssembly);

                string assemblyVersion = assemblyInfo.Version.ToString();

                if (!String.IsNullOrEmpty(assemblyVersion))
                {
                    key = key + assemblyVersion;

                    // Check Name + Version
                    UpdateCachedRMW(key, args.LoadedAssembly);
                }

                byte[] reqKeyToken = assemblyInfo.GetPublicKeyToken();
                for (int i = 0; i < reqKeyToken.Length; i++)
                {
                    assemblyKey += reqKeyToken[i].ToString("x", NumberFormatInfo.InvariantInfo);
                }

                if (!String.IsNullOrEmpty(assemblyKey))
                {
                    key = key + assemblyKey;

                    // Check Name + Version + KeyToken
                    UpdateCachedRMW(key, args.LoadedAssembly);

                    key = assemblyName + assemblyKey;

                    // Check Name + KeyToken
                    UpdateCachedRMW(key, args.LoadedAssembly);
                }
            }
        }

        private void UpdateCachedRMW(string key, Assembly assembly)
        {
            if (_registeredResourceManagers.ContainsKey(key))
            {
                // Update the ResourceManagerWrapper with the new assembly. 
                // Note Package caches Part and Part holds on to ResourceManagerWrapper. Package does not provide a way for 
                // us to update their cache, so we update the assembly that the ResourceManagerWrapper holds on to. This way the 
                // Part cached in the Package class can reference the new dll too. 
                _registeredResourceManagers[key].Assembly = assembly;
            }
        }

        // <summary>
        // Searches the available ResourceManagerWrapper list for one that matches the given Uri.
        // It could be either ResourceManagerWrapper for specific libary assembly or Application
        // main assembly. Package enforces that all Uri will be correctly formated.
        // </summary>
        // <param name="uri">Assumed to be relative</param>
        // <param name="partName">The name of the file in the resource manager</param>
        // <param name="isContentFile">A flag to indicate that this path is a known loose file at compile time</param>
        // <returns></returns>
        private ResourceManagerWrapper GetResourceManagerWrapper(Uri uri, out string partName, out bool isContentFile)
        {
            string assemblyName;
            string assemblyVersion;
            string assemblyKey;
            ResourceManagerWrapper rmwResult = ApplicationResourceManagerWrapper;

            isContentFile = false;

            BaseUriHelper.GetAssemblyNameAndPart(uri, out partName, out assemblyName, out assemblyVersion, out assemblyKey);

            if (!String.IsNullOrEmpty(assemblyName))
            {
                string key = assemblyName + assemblyVersion + assemblyKey;

                _registeredResourceManagers.TryGetValue(key.ToLowerInvariant(), out rmwResult);

                // first time. Add this to the hash table
                if (rmwResult == null)
                {
                    Assembly assembly;

                    assembly = BaseUriHelper.GetLoadedAssembly(assemblyName, assemblyVersion, assemblyKey);

                    if (assembly.Equals(Application.ResourceAssembly))
                    {
                        // This Uri maps to Application Entry assembly even though it has ";component".

                        rmwResult = ApplicationResourceManagerWrapper;
                    }
                    else
                    {
                        rmwResult = new ResourceManagerWrapper(assembly);
                    }

                    _registeredResourceManagers[key.ToLowerInvariant()] = rmwResult;
                }
            }

            if ((rmwResult == ApplicationResourceManagerWrapper))
            {
                if (rmwResult != null)
                {
                    // If this is not a resource from a component then it might be
                    // a content file and not an application resource.
                    if (ContentFileHelper.IsContentFile(partName))
                    {
                        isContentFile = true;
                        rmwResult = null;
                    }
                }
                else
                {
                    // Throw when Application.ResourceAssembly is null. 
                    throw new IOException(SR.Get(SRID.EntryAssemblyIsNull));
                }
            }

            return rmwResult;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Members

        private static Dictionary<string, ResourceManagerWrapper> _registeredResourceManagers = new Dictionary<string, ResourceManagerWrapper>();
        private static ResourceManagerWrapper _applicationResourceManagerWrapper = null;
        private static FileShare _fileShare = FileShare.Read;
        private static bool assemblyLoadhandlerAttached = false;

        #endregion Private Members

        //------------------------------------------------------
        //
        //  Uninteresting (but required) overrides
        //
        //------------------------------------------------------

        #region Uninteresting (but required) overrides

        protected override PackagePart CreatePartCore(Uri uri, string contentType, CompressionOption compressionOption)
        {
            return null;
        }

        protected override void DeletePartCore(Uri uri)
        {
            throw new NotSupportedException();
        }

        protected override PackagePart[] GetPartsCore()
        {
            throw new NotSupportedException();
        }

        protected override void FlushCore()
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
