// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// Description:
// ResourceContainer is an implementation of the abstract Package class. 
// It contains nontrivial overrides for GetPartCore and Exists.
// Many of the methods on Package are not applicable to loading application 
// resources, so the ResourceContainer implementations of these methods throw 
// the NotSupportedException.
// 

using System.Windows.Navigation;
using MS.Internal.Resources;
using System.IO.Packaging;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Windows;
using System.IO;

namespace MS.Internal.AppModel
{
    /// <summary>
    /// ResourceContainer is an implementation of the abstract Package class. 
    /// It contains nontrivial overrides for GetPartCore and Exists.
    /// Many of the methods on Package are not applicable to loading application 
    /// resources, so the ResourceContainer implementations of these methods throw 
    /// the NotSupportedException.
    /// </summary>
    internal sealed class ResourceContainer : Package
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
                if (s_applicationResourceManagerWrapper == null)
                {
                    // load main executable assembly
                    Assembly asmApplication = Application.ResourceAssembly;

                    if (asmApplication != null)
                    {
                        s_applicationResourceManagerWrapper = new ResourceManagerWrapper(asmApplication);
                    }
                }

                return s_applicationResourceManagerWrapper;
            }
        }

        /// <summary>
        /// The FileShare mode to use for opening loose files. Currently this defaults to <see cref="FileShare.Read"/>
        /// Today it is not changed. If we decide that this should change in the future we can easily add a setter here.
        /// </summary>
        internal static FileShare FileShare
        {
            get
            {
                return s_fileShare;
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Constructors
        //
        //------------------------------------------------------

        #region Public Constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        internal ResourceContainer() : base(FileAccess.Read) { }

        #endregion     

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// This method always returns true.  This is because ResourceManager does not have a
        /// simple way to check if a resource exists without loading the resource stream (or failing to)
        /// so checking if a resource exists would be a very expensive task.
        /// A part will later be constructed and returned by GetPart().  This part class contains
        /// a ResourceManager which may or may not contain the requested resource.  When someone 
        /// calls GetStream() on PackagePart then we will attempt to get the stream for the named resource 
        /// and potentially fail.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public override bool PartExists(Uri uri)
        {
            return true;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Constants
        //
        //------------------------------------------------------

        #region Internal Constants

        internal const string XamlExt = ".xaml";
        internal const string BamlExt = ".baml";

        #endregion

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// This method creates a part containing the name of the resource and 
        /// the resource manager that should contain it.  If the resource manager 
        /// does not contain the requested part then when GetStream() is called on
        /// the part it will return null.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected override PackagePart GetPartCore(Uri uri)
        {
            // AppDomain.AssemblyLoad event handler for standalone apps. This is added specifically for designer (Sparkle) scenario.
            // We use the assembly name to fetch the cached resource manager. With this mechanism we will still get resource from the 
            // old version dll when a newer one is loaded. So whenever the AssemblyLoad event is fired, we will need to update the cache 
            // with the newly loaded assembly. This is currently only for designer so not needed for browser hosted apps. 
            // Attach the event handler before the first time we get the ResourceManagerWrapper.
            if (!s_assemblyLoadHandlerAttached)
            {
                AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(OnAssemblyLoadEventHandler);
                s_assemblyLoadHandlerAttached = true;
            }

            ResourceManagerWrapper rmWrapper = GetResourceManagerWrapper(uri, out string partName, out bool isContentFile);

            // If the part name was specified as Content at compile time then we will try to load
            // the file directly. Otherwise we assume the user is looking for a resource.
            if (isContentFile)
            {
                return new ContentFilePart(this, uri);
            }
            else
            {
                // Uri maps to a resource stream.

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

        // AppDomain.AssemblyLoad event handler. Check whether the assembly's ResourceManager has
        // been added to the cache. If it has, we need to update the cache with the newly loaded dll.
        private void OnAssemblyLoadEventHandler(object sender, AssemblyLoadEventArgs args)
        {
            Assembly assembly = args.LoadedAssembly;
            // This is specific for designer (Sparkle) scenario: rebuild and reload dll using Load(Byte[]).
            // We do not care about assemblies loaded into the reflection-only context or the GACed assemblies.
            // For example, in Sparkle whenever a project is built all dependent assemblies will be loaded reflection only.
            // We do no care about those. Only when a assembly is loaded into the execution context, we will need to update the cache. 
            if (!assembly.ReflectionOnly)
            {
                ReadOnlySpan<char> assemblyName = ReflectionUtils.GetAssemblyPartialName(assembly);
                ReflectionUtils.GetAssemblyVersionPlusToken(assembly, out ReadOnlySpan<char> assemblyVersion, out ReadOnlySpan<char> assemblyToken);
                int totalLength = assemblyName.Length + assemblyVersion.Length + assemblyToken.Length;

                Span<char> key = totalLength <= 256 ? stackalloc char[totalLength] : new char[totalLength];
                assemblyName.ToLowerInvariant(key);

                // Check if this newly loaded assembly is in the cache. If so, update the cache.
                // If it is not in cache, do not do anything. It will be added on demand.
                // The key could be Name, Name + Version, Name + PublicKeyToken, or Name + Version + PublicKeyToken.  
                // Otherwise, update the cache with the newly loaded dll.

                // Firstly, check the Name
                UpdateCachedRMW(key.Slice(0, assemblyName.Length), assembly);

                // Check Name + Version
                if (!assemblyVersion.IsEmpty)
                {
                    assemblyVersion.CopyTo(key.Slice(assemblyName.Length));
                    UpdateCachedRMW(key.Slice(0, assemblyName.Length + assemblyVersion.Length), assembly);
                }

                if (!assemblyToken.IsEmpty)
                {
                    // Check Name + Version + KeyToken
                    if (!assemblyVersion.IsEmpty)
                    {
                        assemblyToken.CopyTo(key.Slice(assemblyName.Length + assemblyVersion.Length));
                        UpdateCachedRMW(key.Slice(0, totalLength), assembly);
                    }

                    // Check Name + KeyToken
                    assemblyToken.CopyTo(key.Slice(assemblyName.Length));
                    UpdateCachedRMW(key.Slice(0, assemblyName.Length + assemblyToken.Length), assembly);
                }
            }
        }

        private static void UpdateCachedRMW(ReadOnlySpan<char> key, Assembly assembly)
        {
            if (s_registeredResourceManagersLookup.TryGetValue(key, out ResourceManagerWrapper value) &&
                MayRepoint(value.Assembly, assembly))
            {
                // Update the ResourceManagerWrapper with the new assembly. 
                // Note Package caches Part and Part holds on to ResourceManagerWrapper. Package does not provide a way for 
                // us to update their cache, so we update the assembly that the ResourceManagerWrapper holds on to. This way the 
                // Part cached in the Package class can reference the new dll too. 
                value.Assembly = assembly;
            }
        }

        /// <summary>
        /// Tells whether a cached wrapper may follow a newly loaded assembly with the same name.
        /// </summary>
        /// <remarks>
        /// The AssemblyLoad event fires for a load in any AssemblyLoadContext, and the newcomer is
        /// typically another component's own copy. Moving the wrapper would serve everyone who used
        /// the cached copy, including the parts already cached with it, from that newcomer, so it
        /// only follows a copy loaded into its own context. A component reloaded into a new context
        /// is served a wrapper of its own copy by <see cref="GetWrapperForContext"/> instead.
        /// </remarks>
        private static bool MayRepoint(Assembly held, Assembly incoming)
        {
            if (held is null)
            {
                return true;
            }

            AssemblyLoadContext heldContext = AssemblyLoadContext.GetLoadContext(held);

            return heldContext is null ||
                   heldContext == AssemblyLoadContext.GetLoadContext(incoming);
        }

        /// <summary>
        /// Returns the wrapper a lookup made for the current load context should use.
        /// </summary>
        /// <param name="cached">The wrapper the shared cache holds for the URI.</param>
        /// <param name="assemblyName">The assembly name the URI specifies.</param>
        /// <param name="assemblyVersion">The version the URI specifies, or empty.</param>
        /// <param name="assemblyToken">The public key token the URI specifies, or empty.</param>
        /// <remarks>
        /// The cache holds one wrapper per assembly name for the whole process, created for
        /// whichever context asked first. A lookup made for another context is served by a wrapper
        /// of the copy that context resolves, and the shared cache is left as it is. The copy is
        /// resolved by the identity the URI specifies, not by the cached copy's: a URI without a
        /// version names whatever version the context has, not the version another context chose.
        /// </remarks>
        private static ResourceManagerWrapper GetWrapperForContext(ResourceManagerWrapper cached, string assemblyName, string assemblyVersion, string assemblyToken)
        {
            Assembly cachedAssembly = cached?.Assembly;
            if (cachedAssembly is null)
            {
                return cached;
            }

            AssemblyLoadContext context = AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.Default;
            if (AssemblyLoadContext.GetLoadContext(cachedAssembly) == context)
            {
                return cached;
            }

            Assembly assembly = MS.Internal.WindowsBase.SafeSecurityHelper.GetAssemblyForCurrentContext(
                BaseUriHelper.CreateAssemblyName(assemblyName, assemblyVersion, assemblyToken));
            if (assembly is null || assembly == cachedAssembly)
            {
                return cached;
            }

            if (assembly.Equals(Application.ResourceAssembly))
            {
                return ApplicationResourceManagerWrapper;
            }

            return s_contextResourceManagers.GetValue(assembly, static a => new ResourceManagerWrapper(a));
        }

        /// <summary>
        /// Searches the available ResourceManagerWrapper list for one that matches the given Uri.
        /// It could be either ResourceManagerWrapper for specific library assembly or Application
        /// main assembly. Package enforces that all Uri will be correctly formatted.
        /// </summary>
        /// <param name="uri">Assumed to be relative</param>
        /// <param name="partName">The name of the file in the resource manager</param>
        /// <param name="isContentFile">A flag to indicate that this path is a known loose file at compile time</param>
        /// <returns></returns>
        private static ResourceManagerWrapper GetResourceManagerWrapper(Uri uri, out string partName, out bool isContentFile)
        {
            ResourceManagerWrapper rmwResult = ApplicationResourceManagerWrapper;
            isContentFile = false;

            BaseUriHelper.GetAssemblyNameAndPart(uri, out partName, out string assemblyName, out string assemblyVersion, out string assemblyToken);

            if (!string.IsNullOrEmpty(assemblyName))
            {
                // Create the key, in format of $"{assemblyName}{assemblyVersion}{assemblyToken}"
                int totalLength = assemblyName.Length + assemblyVersion.Length + assemblyToken.Length;
                Span<char> key = totalLength <= 256 ? stackalloc char[totalLength] : new char[totalLength];
                assemblyName.AsSpan().ToLowerInvariant(key);
                assemblyVersion.CopyTo(key.Slice(assemblyName.Length));
                assemblyToken.CopyTo(key.Slice(assemblyName.Length + assemblyVersion.Length));

                // First time; add this to the dictionary and create the wrapper if its not the application entry assembly
                if (!s_registeredResourceManagersLookup.TryGetValue(key.Slice(0, totalLength), out rmwResult))
                {
                    Assembly assembly = BaseUriHelper.GetLoadedAssembly(assemblyName, assemblyVersion, assemblyToken);
                    if (assembly.Equals(Application.ResourceAssembly))
                    {
                        // This Uri maps to Application Entry assembly even though it has ";component".
                        rmwResult = ApplicationResourceManagerWrapper;
                    }
                    else
                    {
                        rmwResult = new ResourceManagerWrapper(assembly);
                    }

                    s_registeredResourceManagersLookup[key.Slice(0, totalLength)] = rmwResult;
                }

                rmwResult = GetWrapperForContext(rmwResult, assemblyName, assemblyVersion, assemblyToken);
            }

            if (rmwResult == ApplicationResourceManagerWrapper)
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
                    throw new IOException(SR.EntryAssemblyIsNull);
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

        private static readonly Dictionary<string, ResourceManagerWrapper> s_registeredResourceManagers = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, ResourceManagerWrapper>.AlternateLookup<ReadOnlySpan<char>> s_registeredResourceManagersLookup = s_registeredResourceManagers.GetAlternateLookup<ReadOnlySpan<char>>();
        // Wrappers of the copies that load contexts other than the cached one resolve, per assembly.
        private static readonly ConditionalWeakTable<Assembly, ResourceManagerWrapper> s_contextResourceManagers = new();
        private static readonly FileShare s_fileShare = FileShare.Read;

        private static ResourceManagerWrapper s_applicationResourceManagerWrapper = null;
        private static bool s_assemblyLoadHandlerAttached = false;

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
