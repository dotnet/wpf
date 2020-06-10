// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Windows.Threading;
using System.Text;
using MS.Utility;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Diagnostics;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Resources;
using MS.Win32;
using MS.Internal;
using MS.Internal.Ink;
using MS.Internal.Interop;
using MS.Internal.PresentationFramework;                   // SafeSecurityHelper
using System.Windows.Baml2006;
using System.Xaml.Permissions;

// Disable pragma warnings to enable PREsharp pragmas
#pragma warning disable 1634, 1691

namespace System.Windows
{
    /// <summary>
    ///     SystemResources loads system theme data into the system resources collection.
    /// </summary>
    internal static class SystemResources
    {
        // ------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------

        #region Methods

        /// <summary>
        ///     Returns a resource for the given key type from the system resources collection.
        /// </summary>
        /// <param name="key">The resource id to search for.</param>
        /// <returns>The resource if it exists, null otherwise.</returns>
        //[CodeAnalysis("AptcaMethodsShouldOnlyCallAptcaMethods")] //Tracking Bug: 29647
        internal static object FindThemeStyle(DependencyObjectType key)
        {
            // Find a cached theme style
            object resource = _themeStyleCache[key];
            if (resource != null)
            {
                // Found a cached value
                if (resource == _specialNull)
                {
                    // We cached a null, set it to a real null
                    return null; // Null resource found
                }

                return resource;
            }

            // Find the resource from the system resources collection
            resource = FindResourceInternal(key.SystemType);

            // The above read operation was lock free. Writing
            // to the cache will need a lock though
            lock (ThemeDictionaryLock)
            {
                if (resource != null)
                {
                    // Cache the value
                    _themeStyleCache[key] = resource;
                }
                else
                {
                    // Record nulls so we don't bother doing lookups for them later
                    // Any theme changes will clear these values
                    _themeStyleCache[key] = _specialNull;
                }
            }

            return resource;
        }

        /// <summary>
        ///     Returns a resource of the given name from the system resources collection.
        /// </summary>
        /// <param name="key">The resource id to search for.</param>
        /// <returns>The resource if it exists, null otherwise.</returns>
        //[CodeAnalysis("AptcaMethodsShouldOnlyCallAptcaMethods")] //Tracking Bug: 29647
        internal static object FindResourceInternal(object key)
        {
            // Call Forwarded
            return FindResourceInternal(key, false /*allowDeferredResourceReference*/, false /*mustReturnDeferredResourceReference*/);
        }

        internal static object FindResourceInternal(object key, bool allowDeferredResourceReference, bool mustReturnDeferredResourceReference)
        {
            // Ensure that resource changes on this thread can be heard and handled
            EnsureResourceChangeListener();

            object resource = null;
            Type typeKey = null;
            ResourceKey resourceKey = null;

            bool isTraceEnabled = EventTrace.IsEnabled(EventTrace.Keyword.KeywordXamlBaml | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose);

            // System resource keys can only be of type Type or of type ResourceKey
            typeKey = key as Type;
            resourceKey = (typeKey == null) ? (key as ResourceKey) : null;

            if (isTraceEnabled)
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientResourceFindBegin,
                                                    EventTrace.Keyword.KeywordXamlBaml | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose,
                                                    (key == null) ? "null" : key.ToString());
            }


            if ((typeKey == null) && (resourceKey == null))
            {
                // Not a valid key
                if (isTraceEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientResourceFindEnd, EventTrace.Keyword.KeywordXamlBaml | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose);
                }
                return null;
            }

            // Check if the value was already cached
            if (!FindCachedResource(key, ref resource, mustReturnDeferredResourceReference))
            {
                // Cache miss, do a lookup
                if (isTraceEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientResourceCacheMiss, EventTrace.Keyword.KeywordXamlBaml | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose);
                }

                lock (ThemeDictionaryLock)
                {
                    bool canCache = true;
                    SystemResourceKey sysKey = (resourceKey != null) ? resourceKey as SystemResourceKey : null;
                    if (sysKey != null)
                    {
                        // Check the list of system metrics
                        if (!mustReturnDeferredResourceReference)
                        {
                            resource = sysKey.Resource;
                        }
                        else
                        {
                            resource = new DeferredResourceReferenceHolder(sysKey, sysKey.Resource);
                        }
                        if (isTraceEnabled)
                        {
                            EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientResourceStock, EventTrace.Keyword.KeywordXamlBaml | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, sysKey.ToString());
                        }
                    }
                    else
                    {
                        // Do a dictionary lookup
                        resource = FindDictionaryResource(key, typeKey, resourceKey, isTraceEnabled, allowDeferredResourceReference, mustReturnDeferredResourceReference, out canCache);
                    }

                    if ((canCache && !allowDeferredResourceReference) || resource == null)
                    {
                        // Cache the resource
                        CacheResource(key, resource, isTraceEnabled);
                    }
                }
            }

            if (isTraceEnabled)
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientResourceFindEnd, EventTrace.Keyword.KeywordXamlBaml | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose);
            }

            return resource;
        }

        #region ResourceDictionaryDiagnostics Support

        /// <summary>
        /// Enumerates all themed <see cref="ResourceDictionary"/> instances
        /// </summary>
        internal static ReadOnlyCollection<ResourceDictionaryInfo> ThemedResourceDictionaries
        {
            get
            {
                List<ResourceDictionaryInfo> dictionaries = new List<ResourceDictionaryInfo>();

                if (_dictionaries != null)
                {
                    lock (ThemeDictionaryLock)
                    {
                        if (_dictionaries != null)
                        {
                            foreach (var dictionary in _dictionaries)
                            {
                                ResourceDictionaryInfo info = dictionary.Value.ThemedDictionaryInfo;
                                if (info.ResourceDictionary != null)
                                {
                                    dictionaries.Add(info);
                                }
                            }
                        }
                    }
                }

                return dictionaries.AsReadOnly();
            }
        }

        /// <summary>
        /// Enumerates all generic <see cref="ResourceDictionary"/> instances
        /// </summary>
        internal static ReadOnlyCollection<ResourceDictionaryInfo> GenericResourceDictionaries
        {
            get
            {
                List<ResourceDictionaryInfo> dictionaries = new List<ResourceDictionaryInfo>();

                if (_dictionaries != null)
                {
                    lock (ThemeDictionaryLock)
                    {
                        if (_dictionaries != null)
                        {
                            foreach (var dictionary in _dictionaries)
                            {
                                ResourceDictionaryInfo info = dictionary.Value.GenericDictionaryInfo;
                                if (info.ResourceDictionary != null)
                                {
                                    dictionaries.Add(info);
                                }
                            }
                        }
                    }
                }

                return dictionaries.AsReadOnly();
            }
        }

        #endregion

        #endregion

        // ------------------------------------------------
        //
        // Implementation
        //
        // ------------------------------------------------

        #region Implementation

        //[CodeAnalysis("AptcaMethodsShouldOnlyCallAptcaMethods")] //Tracking Bug: 29647
        internal static void CacheResource(object key, object resource, bool isTraceEnabled)
        {
            // Thread safety handled by FindResourceInternal. Be sure to have locked _resourceCache.SyncRoot.

            if (resource != null)
            {
                // Cache the value
                _resourceCache[key] = resource;

                if (isTraceEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientResourceCacheValue, EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose);
                }
            }
            else
            {
                // Record nulls so we don't bother doing lookups for them later
                // Any theme changes will clear these values
                _resourceCache[key] = _specialNull;

                if (isTraceEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientResourceCacheNull, EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose);
                }
            }
        }

        #region Resource Value Lookup

        private static bool FindCachedResource(object key, ref object resource, bool mustReturnDeferredResourceReference)
        {
            // reading the cache is lock free

            resource = _resourceCache[key];
            bool found = (resource != null);

            if (resource == _specialNull)
            {
                // We cached a null, set it to a real null
                resource = null;
            }
            else
            {
                DispatcherObject dispatcherObject = resource as DispatcherObject;
                if (dispatcherObject != null)
                {
                    // The current thread may not have access to this object.
                    dispatcherObject.VerifyAccess();
                }
            }

            if (found && mustReturnDeferredResourceReference)
            {
                resource = new DeferredResourceReferenceHolder(key, resource);
            }

            return found;
        }

        /// <summary>
        ///     Searches for a resource inside a ResourceDictionary.
        /// </summary>
        /// <param name="key">The original key.</param>
        /// <param name="typeKey">The key cast to Type.</param>
        /// <param name="resourceKey">The key cast to ResourceKey.</param>
        /// <param name="isTraceEnabled">Tracing on/off.</param>
        /// <param name="allowDeferredResourceReference">
        ///     If this flag is true the resource will not actually be inflated from Baml.
        ///     Instead we will return an instance of DeferredDictionaryReference which
        ///     can later be used to query the resource
        /// </param>
        /// <param name="mustReturnDeferredResourceReference">
        ///     If this method is true this method will always return a
        ///     DeferredThemeResourceReference instance which envelopes the underlying resource.
        /// </param>
        /// <param name="canCache">Whether callers can cache the value.</param>
        /// <returns></returns>
        private static object FindDictionaryResource(
            object      key,
            Type        typeKey,
            ResourceKey resourceKey,
            bool        isTraceEnabled,
            bool        allowDeferredResourceReference,
            bool        mustReturnDeferredResourceReference,
            out bool    canCache)
        {
            // Thread safety handled by FindResourceInternal. Be sure to have locked _resourceCache.SyncRoot.

            Debug.Assert(typeKey != null || resourceKey != null, "typeKey or resourceKey should be non-null");

            canCache = true;
            object resource = null;
            Assembly assembly = (typeKey != null) ? typeKey.Assembly : resourceKey.Assembly;

            if ((assembly == null) || IgnoreAssembly(assembly))
            {
                // Without an assembly, we can't figure out which dictionary to look at.
                // Also, ignore some common assemblies we know to not contain resources.
                return null;
            }

            ResourceDictionaries dictionaries = EnsureDictionarySlot(assembly);
            ResourceDictionary dictionary = dictionaries.LoadThemedDictionary(isTraceEnabled);
            if (dictionary != null)
            {
                resource = LookupResourceInDictionary(dictionary, key, allowDeferredResourceReference, mustReturnDeferredResourceReference, out canCache);
            }

            if (resource == null)
            {
                dictionary = dictionaries.LoadGenericDictionary(isTraceEnabled);
                if (dictionary != null)
                {
                    resource = LookupResourceInDictionary(dictionary, key, allowDeferredResourceReference, mustReturnDeferredResourceReference, out canCache);
                }
            }

            if (resource != null)
            {
                // Resources coming out of the dictionary may need to be frozen
                Freeze(resource);
            }

            return resource;
        }

        /// <summary>
        ///     Looks in the ResourceDictionary for the desired resource.
        /// </summary>
        /// <param name="dictionary">The ResourceDictionary to look in.</param>
        /// <param name="key">The key for the resource.</param>
        /// <param name="allowDeferredResourceReference">
        ///     If this flag is true the resource will not actually be inflated from Baml.
        ///     Instead we will return an instance of DeferredDictionaryReference which
        ///     can later be used to query the resource
        /// </param>
        /// <param name="mustReturnDeferredResourceReference">
        ///     If this method is true this method will always return a
        ///     DeferredThemeResourceReference instance which envelopes the underlying resource.
        /// </param>
        /// <param name="canCache">Whether callers should cache the value.</param>
        /// <returns>The resource if found and successfully loaded, null otherwise.</returns>
        private static object LookupResourceInDictionary(
            ResourceDictionary  dictionary,
            object              key,
            bool                allowDeferredResourceReference,
            bool                mustReturnDeferredResourceReference,
            out bool            canCache)
        {
            object resource = null;
            IsSystemResourcesParsing = true;

            try
            {
                resource = dictionary.FetchResource(key, allowDeferredResourceReference, mustReturnDeferredResourceReference, out canCache);
            }
            finally
            {
                IsSystemResourcesParsing = false;
            }

            return resource;
        }

        /// <summary>
        ///     Unbinds a Freezable from its Context.
        /// </summary>
        /// <param name="resource">The resource to freeze.</param>
        private static void Freeze(object resource)
        {
            Freezable freezable = resource as Freezable;
            if (freezable != null && !freezable.IsFrozen)
            {
                freezable.Freeze();
            }
        }

        #endregion

        #region Dictionary Loading

        /// <summary>
        ///     Returns the dictionary cache slot associated with the given assembly.
        /// </summary>
        /// <param name="assembly">The desired assembly</param>
        /// <returns>The cache slot.</returns>
        /// <remarks>
        ///     This method has exactly one caller (FindDictionaryResource) which in turn is called by
        ///     exactly one caller (FindResourceInternal(object,bool,bool). FindResourceInternal
        ///     synchronizes on ThemeDictionaryLock, which ensures that EnsureDictionarySlot always
        ///     executes under that lock.
        /// </remarks>
        private static ResourceDictionaries EnsureDictionarySlot(Assembly assembly)
        {
            ResourceDictionaries dictionaries = null;
            if (_dictionaries != null)
            {
                _dictionaries.TryGetValue(assembly, out dictionaries);
            }
            else
            {
                // We will be caching data, create the cache
                _dictionaries = new Dictionary<Assembly, ResourceDictionaries>(1);
            }

            if (dictionaries == null)
            {
                // Ensure the cache slot is created
                dictionaries = new ResourceDictionaries(assembly);
                _dictionaries.Add(assembly, dictionaries);
            }

            return dictionaries;
        }

        private static bool IgnoreAssembly(Assembly assembly)
        {
            return (assembly == MsCorLib) || (assembly == PresentationCore) || (assembly == WindowsBase);
        }

        private static Assembly MsCorLib
        {
            get
            {
                if (_mscorlib == null)
                {
                    _mscorlib = typeof(string).Assembly;
                }

                return _mscorlib;
            }
        }

        private static Assembly PresentationFramework
        {
            get
            {
                if (_presentationFramework == null)
                {
                    _presentationFramework = typeof(FrameworkElement).Assembly;
                }

                return _presentationFramework;
            }
        }

        private static Assembly PresentationCore
        {
            get
            {
                if (_presentationCore == null)
                {
                    _presentationCore = typeof(UIElement).Assembly;
                }

                return _presentationCore;
            }
        }

        private static Assembly WindowsBase
        {
            get
            {
                if (_windowsBase == null)
                {
                    _windowsBase = typeof(DependencyObject).Assembly;
                }

                return _windowsBase;
            }
        }

        /// <summary>
        ///     Loads and caches the generic and themed resource dictionaries for an assembly.
        /// </summary>
        internal class ResourceDictionaries
        {
            /// <summary>
            ///     Creates an instance of this class.
            /// </summary>
            /// <param name="assembly">The assembly that this class represents.</param>
            internal ResourceDictionaries(Assembly assembly)
            {
                _assembly = assembly;

                // No themed or generic dictionary is loaded yet.
                _themedDictionaryAssembly = null;
                _themedDictionarySourceUri = null;
                _genericDictionaryAssembly = null;
                _genericDictionarySourceUri = null;

                if (assembly == PresentationFramework)
                {
                    // Since we know all the information about PresentationFramework in advance,
                    // we can pre-initialize that data.
                    _assemblyName = PresentationFrameworkName;

                    // There is no generic dictionary
                    _genericDictionary = null;
                    _genericLoaded = true;
                    _genericLocation = ResourceDictionaryLocation.None;

                    // Themed dictionaries are all external
                    _themedLocation = ResourceDictionaryLocation.ExternalAssembly;
                    _locationsLoaded = true;
                }
                else
                {
                    _assemblyName = SafeSecurityHelper.GetAssemblyPartialName(assembly);
                }
            }

            /// <summary>
            ///     Resets the themed dictionaries. This is used when the theme changes.
            /// </summary>
            internal void ClearThemedDictionary()
            {
                ResourceDictionaryInfo info = ThemedDictionaryInfo;

                _themedLoaded = false;
                _themedDictionary = null;
                _themedDictionaryAssembly = null;
                _themedDictionarySourceUri = null;

                if (info.ResourceDictionary != null)
                {
                    SystemResources.ThemedDictionaryUnloaded?.Invoke(null, new ResourceDictionaryUnloadedEventArgs(info));
                }
            }

            /// <summary>
            ///     Returns the theme dictionary associated with this assembly.
            /// </summary>
            /// <param name="isTraceEnabled">Whether debug tracing is enabled.</param>
            /// <returns>The dictionary if loaded, otherwise null.</returns>
            internal ResourceDictionary LoadThemedDictionary(bool isTraceEnabled)
            {
                if (!_themedLoaded)
                {
                    LoadDictionaryLocations();

                    if (_preventReEnter || (_themedLocation == ResourceDictionaryLocation.None))
                    {
                        // We are already in the middle of parsing this resource dictionary, avoid infinite loops.
                        // OR, there are no themed resources.
                        return null;
                    }

                    IsSystemResourcesParsing = true;
                    _preventReEnter = true;
                    try
                    {
                        ResourceDictionary dictionary = null;

                        // Get the assembly to look inside for resources.
                        string dictionaryAssemblyName;
                        bool external = (_themedLocation == ResourceDictionaryLocation.ExternalAssembly);
                        if (external)
                        {
                            LoadExternalAssembly(false /* classic */, false /* generic */, out _themedDictionaryAssembly, out dictionaryAssemblyName);
                        }
                        else
                        {
                            _themedDictionaryAssembly = _assembly;
                            dictionaryAssemblyName = _assemblyName;
                        }

                        if (_themedDictionaryAssembly != null)
                        {
                            dictionary = LoadDictionary(_themedDictionaryAssembly, dictionaryAssemblyName, ThemedResourceName, isTraceEnabled, out _themedDictionarySourceUri);
                            if ((dictionary == null) && !external)
                            {
                                // Themed resources should have been inside the source assembly, but failed to load.
                                // Try falling back to external in case this is a theme that shipped later.
                                LoadExternalAssembly(false /* classic */, false /* generic */, out _themedDictionaryAssembly, out dictionaryAssemblyName);
                                if (_themedDictionaryAssembly != null)
                                {
                                    dictionary = LoadDictionary(_themedDictionaryAssembly, dictionaryAssemblyName, ThemedResourceName, isTraceEnabled, out _themedDictionarySourceUri);
                                }
                            }
                        }

                        if ((dictionary == null) && UxThemeWrapper.IsActive)
                        {
                            // If a non-classic dictionary failed to load, then try to load classic.
                            if (external)
                            {
                                LoadExternalAssembly(true /* classic */, false /* generic */, out _themedDictionaryAssembly, out dictionaryAssemblyName);
                            }
                            else
                            {
                                _themedDictionaryAssembly = _assembly;
                                dictionaryAssemblyName = _assemblyName;
                            }

                            if (_themedDictionaryAssembly != null)
                            {
                                dictionary = LoadDictionary(_themedDictionaryAssembly, dictionaryAssemblyName, ClassicResourceName, isTraceEnabled, out _themedDictionarySourceUri);
                            }
                        }

                        _themedDictionary = dictionary;
                        _themedLoaded = true;

                        if (_themedDictionary != null)
                        {
                            SystemResources.ThemedDictionaryLoaded?.Invoke(null, new ResourceDictionaryLoadedEventArgs(ThemedDictionaryInfo));
                        }
                    }
                    finally
                    {
                        _preventReEnter = false;
                        IsSystemResourcesParsing = false;
                    }
                }

                return _themedDictionary;
            }

            /// <summary>
            ///     Returns the generic dictionary associated with this assembly.
            /// </summary>
            /// <param name="isTraceEnabled">Whether debug tracing is enabled.</param>
            /// <returns>The dictionary if loaded, otherwise null.</returns>
            internal ResourceDictionary LoadGenericDictionary(bool isTraceEnabled)
            {
                if (!_genericLoaded)
                {
                    LoadDictionaryLocations();

                    if (_preventReEnter || (_genericLocation == ResourceDictionaryLocation.None))
                    {
                        // We are already in the middle of parsing this resource dictionary, avoid infinite loops.
                        return null;
                    }

                    IsSystemResourcesParsing = true;
                    _preventReEnter = true;
                    try
                    {
                        ResourceDictionary dictionary = null;

                        // Get the assembly to look inside
                        string dictionaryAssemblyName;
                        if (_genericLocation == ResourceDictionaryLocation.ExternalAssembly)
                        {
                            LoadExternalAssembly(false /* classic */, true /* generic */, out _genericDictionaryAssembly, out dictionaryAssemblyName);
                        }
                        else
                        {
                            _genericDictionaryAssembly = _assembly;
                            dictionaryAssemblyName = _assemblyName;
                        }

                        if (_genericDictionaryAssembly != null)
                        {
                            dictionary = LoadDictionary(_genericDictionaryAssembly, dictionaryAssemblyName, GenericResourceName, isTraceEnabled, out _genericDictionarySourceUri);
                        }

                        _genericDictionary = dictionary;
                        _genericLoaded = true;

                        if (_genericDictionary != null)
                        {
                            SystemResources.GenericDictionaryLoaded?.Invoke(null, new ResourceDictionaryLoadedEventArgs(GenericDictionaryInfo));
                        }
                    }
                    finally
                    {
                        _preventReEnter = false;
                        IsSystemResourcesParsing = false;
                    }
                }

                return _genericDictionary;
            }

            /// <summary>
            ///     Loads the assembly attribute indicating where dictionaries are stored.
            /// </summary>
            private void LoadDictionaryLocations()
            {
                if (!_locationsLoaded)
                {
                    ThemeInfoAttribute locations = ThemeInfoAttribute.FromAssembly(_assembly);
                    if (locations != null)
                    {
                        _themedLocation = locations.ThemeDictionaryLocation;
                        _genericLocation = locations.GenericDictionaryLocation;
                    }
                    else
                    {
                        _themedLocation = ResourceDictionaryLocation.None;
                        _genericLocation = ResourceDictionaryLocation.None;
                    }
                    _locationsLoaded = true;
                }
            }

            /// <summary>
            ///     Loads an associated theme assembly based on a main assembly.
            /// </summary>
            private void LoadExternalAssembly(bool classic, bool generic, out Assembly assembly, out string assemblyName)
            {
                StringBuilder sb = new StringBuilder(_assemblyName.Length + 10);

                sb.Append(_assemblyName);
                sb.Append(".");

                if (generic)
                {
                    sb.Append("generic");
                }
                else if (classic)
                {
                    sb.Append("classic");
                }
                else
                {
                    sb.Append(UxThemeWrapper.ThemeName);
                }

                assemblyName = sb.ToString();
                string fullName = SafeSecurityHelper.GetFullAssemblyNameFromPartialName(_assembly, assemblyName);

                assembly = null;
                try
                {
                    assembly = Assembly.Load(fullName);
                }
                // There is no Assembly.Exists API to determine if an Assembly exists.
                // There is also no way to determine if an Assembly's format is good prior to loading it.
                // So, the exception must be caught. assembly will continue to be null and returned.
#pragma warning disable 6502
                catch (FileNotFoundException)
                {
                }
                catch (BadImageFormatException)
                {
                }

                // Wires themes KnownTypeHelper
                if (_assemblyName == PresentationFrameworkName && assembly != null)
                {
                    Type knownTypeHelper = assembly.GetType("Microsoft.Windows.Themes.KnownTypeHelper");
                    if (knownTypeHelper != null)
                    {
                        MS.Internal.WindowsBase.SecurityHelper.RunClassConstructor(knownTypeHelper);
                    }
                }
#pragma warning restore 6502
            }

            /// <summary>
            ///     The string to use as the key to load the .NET resource stream that contains themed resources.
            /// </summary>
            internal static string ThemedResourceName
            {
                get
                {
                    // the race conditions described in UxThemeWrapper apply
                    // here as well, and are solved with the same techniques
                    string themedResourceName = _themedResourceName;

                    while (themedResourceName == null)
                    {
                        themedResourceName = UxThemeWrapper.ThemedResourceName; // guaranteed not null

                        // try to update the shared member
                        string currentName = System.Threading.Interlocked.CompareExchange(ref _themedResourceName, themedResourceName, null);

                        if (currentName != null && currentName != themedResourceName)
                        {
                            // another thread updated with a different value, which might
                            // be stale.  To be safe, retry as if a fresh ThemeChange occurred
                            _themedResourceName = null;
                            themedResourceName = null;
                        }
                    }

                    return themedResourceName;
                }
            }

            /// <summary>
            /// <see cref="ResourceDictionaryInfo"/> encapsulating information about the generic ResourceDictionary represented
            /// by this <see cref="ResourceDictionaries"/> instance.
            /// </summary>
            /// <remarks>
            /// It is possible that the ResourceDictionaryInfo instance returned here could be cached to improve performance.
            /// (i) Typical applicaitons will have O(1) RD instances, (ii) this property will be called only during development
            /// or debugging, (iii) caching will introduce some redundant storage of information (like _assembly, _genericDictionary)
            /// or require additional refactoring to eliminate those redundances, and (iv) caching would potentially introduce
            /// additional instances that would remain alive even in scenarios wherein a debugger is not attached. Given these
            /// factors, we are not caching the ResourceDictionaryInfo instances returned by this property's getter.
            /// </remarks>
            internal ResourceDictionaryInfo GenericDictionaryInfo
            {
                get
                {
                    return new ResourceDictionaryInfo(_assembly, _genericDictionaryAssembly, _genericDictionary, _genericDictionarySourceUri);
                }
            }

            /// <summary>
            /// <see cref="ResourceDictionaryInfo"/> encapsulating information about the themed ResourceDictionary represented
            /// by this <see cref="ResourceDictionaries"/> instance.
            /// </summary>
            /// <remarks>
            /// See remarks for <see cref="GenericDictionaryInfo"/>
            /// </remarks>
            internal ResourceDictionaryInfo ThemedDictionaryInfo
            {
                get
                {
                    return new ResourceDictionaryInfo(_assembly, _themedDictionaryAssembly, _themedDictionary, _themedDictionarySourceUri);
                }
            }

            /// <summary>
            ///     Loads a ResourceDictionary from within an assembly's .NET resource store.
            /// </summary>
            /// <param name="assembly">The owning assembly.</param>
            /// <param name="assemblyName">The name of the owning assembly.</param>
            /// <param name="resourceName">The name of the desired theme resource.</param>
            /// <param name="isTraceEnabled">Whether tracing is enabled.</param>
            /// <param name="dictionarySourceUri">
            ///     Returns the pack:// Uri of the baml from which the ResourceDictionary instance returned
            ///     by this method was loaded. This Uri is captured for use with <see cref="ResourceDictionaryDiagnostics"/>.
            /// </param>
            /// <returns>The dictionary if found and loaded successfully, null otherwise.</returns>
            private ResourceDictionary LoadDictionary(Assembly assembly, string assemblyName, string resourceName, bool isTraceEnabled, out Uri dictionarySourceUri)
            {
                ResourceDictionary dictionary = null;
                dictionarySourceUri = null;

                // Create the resource manager that will load the byte array
                ResourceManager rm = new ResourceManager(assemblyName + ".g", assembly);

                resourceName = resourceName + ".baml";
                // Load the resource stream
                Stream stream = null;
                try
                {
                    stream = rm.GetStream(resourceName, CultureInfo.CurrentUICulture);
                }
                // There is no ResourceManager.HasManifest in order to detect this case before an exception is thrown.
                // Likewise, there is no way to know if loading a resource will fail prior to loading it.
                // So, the exceptions must be caught. stream will continue to be null and handled accordingly later.
#pragma warning disable 6502

                catch (MissingManifestResourceException)
                {
                    // No usable resources in the assembly
                }
                catch (MissingSatelliteAssemblyException)
                {
                    // No usable resources in the assembly
                }
#if !DEBUG
                catch (InvalidOperationException)
                {
                    // Object not stored correctly
                }
#endif

#pragma warning restore 6502

                if (stream != null)
                {
                    Baml2006ReaderSettings settings = new Baml2006ReaderSettings();
                    settings.OwnsStream = true;
                    settings.LocalAssembly = assembly;

                    // For system themes, we don't seem to be passing the BAML Uri to the Baml2006Reader
                    Baml2006Reader bamlReader = new Baml2006ReaderInternal(stream, new Baml2006SchemaContext(settings.LocalAssembly), settings);

                    System.Xaml.XamlObjectWriterSettings owSettings = XamlReader.CreateObjectWriterSettingsForBaml();
                    if (assembly != null)
                    {
                        owSettings.AccessLevel = XamlAccessLevel.AssemblyAccessTo(assembly);

                        AssemblyName asemblyName = new AssemblyName(assembly.FullName);
                        Uri streamUri = null;
                        string packUri = string.Format("pack://application:,,,/{0};v{1};component/{2}", asemblyName.Name, asemblyName.Version.ToString(), resourceName);
                        if (Uri.TryCreate(packUri, UriKind.Absolute, out streamUri))
                        {
                            if (XamlSourceInfoHelper.IsXamlSourceInfoEnabled)
                            {
                                owSettings.SourceBamlUri = streamUri;
                            }

                            dictionarySourceUri = streamUri;
                        }
                    }

                    System.Xaml.XamlObjectWriter writer = new System.Xaml.XamlObjectWriter(bamlReader.SchemaContext, owSettings);

                    System.Xaml.XamlServices.Transform(bamlReader, writer);

                    dictionary = (ResourceDictionary)writer.Result;

                    if (isTraceEnabled && (dictionary != null))
                    {
                        EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientResourceBamlAssembly,
                                                            EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose,
                                                            assemblyName);
                    }
                }

                return dictionary;
            }

            internal static void OnThemeChanged()
            {
                _themedResourceName = null;
            }

            private ResourceDictionary _genericDictionary;
            private ResourceDictionary _themedDictionary;
            private bool _genericLoaded = false;
            private bool _themedLoaded = false;
            private bool _preventReEnter = false;
            private bool _locationsLoaded = false;
            private string _assemblyName;
            private Assembly _assembly;
            private ResourceDictionaryLocation _genericLocation;
            private ResourceDictionaryLocation _themedLocation;

            private static string _themedResourceName;

            // Used for ResourceDictionaryDiagnostics support
            private Assembly _themedDictionaryAssembly;
            private Assembly _genericDictionaryAssembly;
            private Uri _themedDictionarySourceUri;
            private Uri _genericDictionarySourceUri;
        }

        #endregion

        #region Value Changes

        // The hwndNotify is referenced by the _hwndNotify static field, but
        // PreSharp will think that the hwndNotify is local and should be disposed.
#pragma warning disable 6518

        /// <summary>
        /// Ensures that a a notify-window is created corresponding to <see cref="ProcessDpiAwarenessContextValue"/>
        /// This is the default HWND used to listen for theme-change messages.
        /// 
        /// When <see cref="IsPerMonitorDpiScalingActive"/> is true, additional notification windows are created 
        /// on-demand by <see cref="EnsureResourceChangeListener(DpiUtil.HwndDpiInfo)"/>
        /// as the need arises. For e.g., when <see cref="System.Windows.Interop.HwndHost"/> calls into <see cref="GetDpiAwarenessCompatibleNotificationWindow(HandleRef)"/>,
        /// we would look for a notify-window that matches both (a) DPI Awareness Context and (b) DPI Scale factor of the foreign window from HwndHost to return. If none is found,
        /// we would create one and add it to our list in <see cref="_hwndNotify"/> and return the newly created notify-window.
        /// 
        /// Over the lifetime of a DPI-aware application, unique notify-windows could be created for each combination 
        /// of <see cref="DpiAwarenessContextValue"/> + DPI that is observed - i.e., for each unique <see cref="DpiUtil.HwndDpiInfo"/>. This
        /// would be bounded by:
        ///     #(valid DpiAwarenessContextValues) + 2*#(unique DPI values that are observed during calls to <see cref="GetDpiAwarenessCompatibleNotificationWindow(HandleRef)"/>)
        /// The only DpiAwarenessContextValues for which unique DPI values would matter are <see cref="DpiAwarenessContextValue.PerMonitorAware"/> and 
        /// <see cref="DpiAwarenessContextValue.PerMonitorAwareVersion2"/>. For <see cref="DpiAwarenessContextValue.SystemAware"/> and <see cref="DpiAwarenessContextValue.Unaware"/>,
        /// the OS would never report back anything other than the System DPI and 96 respectively, and thus we would only ever maintain one entry for those
        /// DpiAwarenessContextValues.
        /// </summary>
        private static void EnsureResourceChangeListener()
        {
            // Create a new notify window if we haven't already created any corresponding to ProcessDpiAwarenessContextValue for this thread.
            if (_hwndNotify == null || 
                _hwndNotifyHook == null || 
                _hwndNotify.Count == 0 ||
                _hwndNotify.Keys.FirstOrDefault((hwndDpiContext) => hwndDpiContext.DpiAwarenessContextValue == ProcessDpiAwarenessContextValue) == null)
            {
                _hwndNotify = new Dictionary<DpiUtil.HwndDpiInfo, SecurityCriticalDataClass<HwndWrapper>>();
                _hwndNotifyHook = new Dictionary<DpiUtil.HwndDpiInfo, HwndWrapperHook>();
                _dpiAwarenessContextAndDpis = new List<DpiUtil.HwndDpiInfo>();

                var hwndDpiInfo = CreateResourceChangeListenerWindow(ProcessDpiAwarenessContextValue);
                _dpiAwarenessContextAndDpis.Add(hwndDpiInfo);
            }
        }

        /// <summary>
        /// Ensures that a notify-window corresponding to a given HwndDpiInfo(=DpiAwarenessContextValue + DpiScale) has been
        /// created.
        /// </summary>
        /// <param name="hwndDpiInfo">Represents a combination of <see cref="DpiAwarenessContextValue"/> and <see cref="DpiScale2"/></param>
        /// <remarks>
        /// Since we create the new window using the (left, top) screen coordinates of the monitor that contained the HWND
        /// whose characteristics we are trying to match, we are guaranteed that the DPI Scale factor of the newly created HWND
        /// would be identical to that of the reference HWND.
        /// </remarks>
        private static bool EnsureResourceChangeListener(DpiUtil.HwndDpiInfo hwndDpiInfo)
        {
            EnsureResourceChangeListener();

            // It's meaningless to ensure RCL for Invalid DACV
            if (hwndDpiInfo.DpiAwarenessContextValue == DpiAwarenessContextValue.Invalid)
            {
                return false;
            }

            if (!_hwndNotify.ContainsKey(hwndDpiInfo))
            {
                var hwndDpiInfoKey = 
                    CreateResourceChangeListenerWindow(
                        hwndDpiInfo.DpiAwarenessContextValue, 
                        hwndDpiInfo.ContainingMonitorScreenRect.left, 
                        hwndDpiInfo.ContainingMonitorScreenRect.top);

                if (hwndDpiInfoKey == hwndDpiInfo &&                        // If hwndDpiInfoKey != hwndDpiInfo, something is wrong, abort. 
                    !_dpiAwarenessContextAndDpis.Contains(hwndDpiInfo))
                {
                    _dpiAwarenessContextAndDpis.Add(hwndDpiInfo);
                }
            }

            return _hwndNotify.ContainsKey(hwndDpiInfo);
        }

        /// <summary>
        /// Creates notify-window by switching the thread to <paramref name="dpiContextValue"/> temporarily, adds the 
        /// corresponding <see cref="HwndWrapper"/> to <see cref="_hwndNotify"/>, and registers relevant hooks
        /// </summary>
        /// <param name="dpiContextValue">DPI Awareness Context for which notify-window has to be ensured</param>
        /// <param name="x">x-coordinate position on the screen where the window is to be created</param>
        /// <param name="y">y-coordinate position on the screen where the window is to be created</param>
        /// <remarks>
        /// Assumes that <see cref="_hwndNotify"/> and <see cref="_hwndNotifyHook"/> have been initialized. This method
        /// must only be called by <see cref="EnsureResourceChangeListener"/> or <see cref="EnsureResourceChangeListener(DpiUtil.HwndDpiInfo)"/>
        /// </remarks>
        /// <returns><see cref="DpiUtil.HwndDpiInfo"/> of the newly created <see cref="HwndWrapper"/>, which is also a new key added into <see cref="_hwndNotify"/></returns>
        private static DpiUtil.HwndDpiInfo CreateResourceChangeListenerWindow(DpiAwarenessContextValue dpiContextValue, int x = 0, int y = 0, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
        {
            Debug.Assert(_hwndNotify != null);
            Debug.Assert(_hwndNotifyHook != null);
            Debug.Assert(callerName.Equals(nameof(EnsureResourceChangeListener), StringComparison.InvariantCulture));

            using (DpiUtil.WithDpiAwarenessContext(dpiContextValue))
            {
                // Create a top-level, invisible window so we can get the WM_THEMECHANGE notification
                // and for HwndHost to park non-visible HwndHosts.
                var hwndNotify =
                    new HwndWrapper(
                    classStyle: 0,
                    style: NativeMethods.WS_POPUP | NativeMethods.WS_DISABLED,
                    exStyle: 0,
                    x: x,
                    y: y,
                    width: 0,
                    height: 0,
                    name: "SystemResourceNotifyWindow",
                    parent: IntPtr.Zero,
                    hooks: null);

                // Do not call into DpiUtil.GetExtendedDpiInfoForWindow()
                // unless IsPerMonitorDpiscalingActive == true. DpiUtil.GetExtendedDpiInfoForWindow()
                // in turn calls into methods that are only supported on platforms with high DPI
                // support (for e.g., not supported on Windows 7). 
                var hwndDpiInfo = 
                    IsPerMonitorDpiScalingActive ?
                    DpiUtil.GetExtendedDpiInfoForWindow(hwndNotify.Handle):
                    new DpiUtil.HwndDpiInfo(dpiContextValue, GetDpiScaleForUnawareOrSystemAwareContext(dpiContextValue));

                Debug.Assert(!_hwndNotify.ContainsKey(hwndDpiInfo));
                Debug.Assert(hwndDpiInfo.DpiAwarenessContextValue == dpiContextValue);

                _hwndNotify[hwndDpiInfo] = new SecurityCriticalDataClass<HwndWrapper>(hwndNotify);
                _hwndNotify[hwndDpiInfo].Value.Dispatcher.ShutdownFinished += OnShutdownFinished;
                _hwndNotifyHook[hwndDpiInfo] = new HwndWrapperHook(SystemThemeFilterMessage);
                _hwndNotify[hwndDpiInfo].Value.AddHook(_hwndNotifyHook[hwndDpiInfo]);

                return hwndDpiInfo;
            }
        }

        private static void OnShutdownFinished(object sender, EventArgs args)
        {
            if (_hwndNotify != null && _hwndNotify.Count != 0)
            {
                foreach (var hwndDpiInfo in _dpiAwarenessContextAndDpis)
                {
                    _hwndNotify[hwndDpiInfo].Value.Dispose();
                    _hwndNotifyHook[hwndDpiInfo] = null;
                }
            }

            _hwndNotify?.Clear();
            _hwndNotify = null;

            _hwndNotifyHook?.Clear();
            _hwndNotifyHook = null;
        }

        /// <summary>
        /// Obtains the DPI Scale factor value given the current non-Per Monitor Aware DPI Awareness Context Value
        /// </summary>
        /// <param name="dpiContextValue"></param>
        /// <returns>DPI Scale value</returns>
        /// <remarks>Defaults to System DPI if an invalid <paramref name="dpiContextValue"/> is supplied</remarks>
        private static DpiScale2 GetDpiScaleForUnawareOrSystemAwareContext(DpiAwarenessContextValue dpiContextValue)
        {
            Debug.Assert(dpiContextValue == DpiAwarenessContextValue.Unaware || dpiContextValue == DpiAwarenessContextValue.SystemAware);

            DpiScale2 dpiScale = null;
            switch (dpiContextValue)
            {
                case DpiAwarenessContextValue.Unaware:
                    dpiScale = DpiScale2.FromPixelsPerInch(DpiUtil.DefaultPixelsPerInch, DpiUtil.DefaultPixelsPerInch);
                    break;
                case DpiAwarenessContextValue.SystemAware:
                default:
                    dpiScale = DpiUtil.GetSystemDpi();
                    break;
            }

            return dpiScale;
        }

#pragma warning restore 6518

    private static void OnThemeChanged()
        {
            ResourceDictionaries.OnThemeChanged();
            UxThemeWrapper.OnThemeChanged();
            ThemeDictionaryExtension.OnThemeChanged();

            lock (ThemeDictionaryLock)
            {
                // Clear the resource cache
                _resourceCache.Clear();

                // Clear the themeStyleCache
                _themeStyleCache.Clear();

                // Clear the themed dictionaries
                if (_dictionaries != null)
                {
                    foreach (ResourceDictionaries dictionaries in _dictionaries.Values)
                    {
                        dictionaries.ClearThemedDictionary();
                    }
                }
            }
        }

        private static void OnSystemValueChanged()
        {
            lock (ThemeDictionaryLock)
            {
                // Collect the list of keys for the values that will need to be removed
                // Note: We don't immediately remove them because the Key list is not
                // static.

                List<SystemResourceKey> keys = new List<SystemResourceKey>();

                foreach (object key in _resourceCache.Keys)
                {
                    SystemResourceKey resKey = key as SystemResourceKey;
                    if (resKey != null)
                    {
                        keys.Add(resKey);
                    }
                }

                // Remove the values

                int count = keys.Count;
                for (int i = 0; i < count; i++)
                {
                    _resourceCache.Remove(keys[i]);
                }
            }
        }

        private static object InvalidateTreeResources(Object args)
        {
            object[] argsArray = (object[])args;
            PresentationSource source = (PresentationSource)argsArray[0];
            if (!source.IsDisposed)
            {
                FrameworkElement fe = source.RootVisual as FrameworkElement;
                if (fe != null)
                {
                    bool isSysColorsOrSettingsChange = (bool)argsArray[1];
                    if (isSysColorsOrSettingsChange)
                    {
                        TreeWalkHelper.InvalidateOnResourcesChange(fe, null, ResourcesChangeInfo.SysColorsOrSettingsChangeInfo);
                    }
                    else
                    {
                        TreeWalkHelper.InvalidateOnResourcesChange(fe, null, ResourcesChangeInfo.ThemeChangeInfo);
                    }

                    System.Windows.Input.KeyboardNavigation.AlwaysShowFocusVisual = SystemParameters.KeyboardCues;
                    fe.CoerceValue(System.Windows.Input.KeyboardNavigation.ShowKeyboardCuesProperty);

                    SystemResourcesAreChanging = true;
                    // Update FontFamily properties on root elements
                    fe.CoerceValue(TextElement.FontFamilyProperty);
                    fe.CoerceValue(TextElement.FontSizeProperty);
                    fe.CoerceValue(TextElement.FontStyleProperty);
                    fe.CoerceValue(TextElement.FontWeightProperty);
                    SystemResourcesAreChanging = false;

                    PopupRoot popupRoot = fe as PopupRoot;
                    if (popupRoot != null && popupRoot.Parent != null)
                    {
                        popupRoot.Parent.CoerceValue(Popup.HasDropShadowProperty);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// This method calls into PresentationCore internally to update Tabelt devices when the system settings change.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        private static void InvalidateTabletDevices(WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            
            // Don't forward messages to tablets if the stack is turned off.
            
            // If the StylusLogic for this thread has not been instantiated, do not attempt to forward
            // tablet messages.  This stops potential instantiations of Dispatcher during process
            // shutdown.  StylusLogic should be instantiated by <see cref="HwndSource.Initialize">.
            if (StylusLogic.IsStylusAndTouchSupportEnabled
                && StylusLogic.IsInstantiated
                && _hwndNotify != null
                && _hwndNotify.Count != 0)
            {
                Dispatcher dispatcher = Hwnd.Dispatcher;
                if (dispatcher?.InputManager != null)
                {
                    
                    // Switch to using CurrentStylusLogic mechanism and guard against the stack
                    // not being enabled.
                    StylusLogic.CurrentStylusLogic.HandleMessage(msg, wParam, lParam);
                }
            }
        }

        private static void InvalidateResources(bool isSysColorsOrSettingsChange)
        {
            SystemResourcesHaveChanged = true;

            Dispatcher dispatcher = isSysColorsOrSettingsChange ? null : Dispatcher.FromThread(System.Threading.Thread.CurrentThread);
            if (dispatcher != null || isSysColorsOrSettingsChange)
            {
                foreach (PresentationSource source in PresentationSource.CriticalCurrentSources)
                {
                    if (!source.IsDisposed && (isSysColorsOrSettingsChange || (source.Dispatcher == dispatcher)))
                    {
                        source.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                      new DispatcherOperationCallback(InvalidateTreeResources),
                                                      new object[]{source, isSysColorsOrSettingsChange});
                    }
                }
            }
        }
        private static IntPtr SystemThemeFilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            WindowMessage message = (WindowMessage)msg;
            switch (message)
            {
                case WindowMessage.WM_DEVICECHANGE:
                    InvalidateTabletDevices(message, wParam, lParam);

                    // If there was an invalidation made to a Mouse metric,
                    // then resource references need to be invalidated.
                    if (SystemParameters.InvalidateDeviceDependentCache())
                    {
                        OnSystemValueChanged();
                        InvalidateResources(true); // Invalidate all resource since this should happen only once
                    }
                    break;

                case WindowMessage.WM_DISPLAYCHANGE:
                    InvalidateTabletDevices(message, wParam, lParam);

                    // If there was an invalidation made to a Display metric,
                    // then resource references need to be invalidated.
                    if (SystemParameters.InvalidateDisplayDependentCache())
                    {
                        OnSystemValueChanged();
                        InvalidateResources(true); // Invalidate all resource since this should happen only once
                    }
                    break;

                case WindowMessage.WM_POWERBROADCAST:
                    // If there was an invalidation made to a Power Setting,
                    // then resource references need to be invalidated.
                    if (NativeMethods.IntPtrToInt32(wParam) == NativeMethods.PBT_APMPOWERSTATUSCHANGE &&
                        SystemParameters.InvalidatePowerDependentCache())
                    {
                        OnSystemValueChanged();
                        InvalidateResources(true); // Invalidate all resource since this should happen only once
                    }
                    break;

                case WindowMessage.WM_THEMECHANGED:
                    SystemColors.InvalidateCache();
                    SystemParameters.InvalidateCache();
                    SystemParameters.InvalidateDerivedThemeRelatedProperties();
                    OnThemeChanged();
                    InvalidateResources(false); // Only invalidate this thread's resources, other threads will get a chance
                    break;


                case WindowMessage.WM_SYSCOLORCHANGE:
                    // If there was an invalidation made to a system color or brush,
                    // then resource references need to be invalidated.
                    if (SystemColors.InvalidateCache())
                    {
                        OnSystemValueChanged();
                        InvalidateResources(true); // Invalidate all resource since this should happen only once
                    }
                    break;

                case WindowMessage.WM_SETTINGCHANGE:
                    InvalidateTabletDevices(message, wParam, lParam); // Update tablet device settings

                    // If there was an invalidation made to a system param or metric,
                    // then resource references need to be invalidated.
                    if (SystemParameters.InvalidateCache((int)wParam))
                    {
                        OnSystemValueChanged();
                        InvalidateResources(true); // Invalidate all resource since this should happen only once

                        // NOTICE-2005/06/17-WAYNEZEN,
                        // We have to invoke the below method after InvalidateResources.
                        // So the tablet ink HighContrastHelper can pick up the correct HighContrast setting.
                        HighContrastHelper.OnSettingChanged();
                    }

                    SystemParameters.InvalidateWindowFrameThicknessProperties();
                    break;

                case WindowMessage.WM_TABLET_ADDED:
                    InvalidateTabletDevices(message, wParam, lParam);
                    break;

                case WindowMessage.WM_TABLET_DELETED:
                    InvalidateTabletDevices(message, wParam, lParam);
                    break;

                case WindowMessage.WM_DWMNCRENDERINGCHANGED:
                case WindowMessage.WM_DWMCOMPOSITIONCHANGED:
                    SystemParameters.InvalidateIsGlassEnabled();
                    break;

                case WindowMessage.WM_DWMCOLORIZATIONCOLORCHANGED:
                    SystemParameters.InvalidateWindowGlassColorizationProperties();
                    break;
            }

            return IntPtr.Zero ;

        }

        internal static bool ClearBitArray(BitArray cacheValid)
        {
            bool changed = false;

            for (int i = 0; i < cacheValid.Count; i++)
            {
                if (ClearSlot(cacheValid, i))
                {
                    changed = true;
                }
            }

            return changed;
        }

        internal static bool ClearSlot(BitArray cacheValid, int slot)
        {
            if (cacheValid[slot])
            {
                cacheValid[slot] = false;
                return true;
            }

            return false;
        }

        #endregion

        // Flag the parser to create DeferredThemeReferences for thread safety
        internal static bool IsSystemResourcesParsing
        {
            get
            {
                return _parsing > 0;
            }
            set
            {
                if (value)
                {
                    _parsing++;
                }
                else
                {
                    _parsing--;
                }
            }
        }

        // This is the lock used to protect access to the
        // theme dictionaries and the associated cache.
        internal static object ThemeDictionaryLock
        {
            get { return _resourceCache.SyncRoot; }
        }

        /// <summary>
        /// Returns the <see cref="DpiAwarenessContextValue"/> of the current process
        /// as reported by <see cref="HwndTarget"/>
        /// 
        /// If <see cref="HwndTarget"/> has yet to initialize this information, the process
        /// is queried directly for this information. 
        /// </summary>
        private static DpiAwarenessContextValue ProcessDpiAwarenessContextValue
        {
            get
            {
                if (HwndTarget.IsProcessUnaware == true)
                {
                    return DpiAwarenessContextValue.Unaware;
                }

                if (HwndTarget.IsProcessSystemAware == true)
                {
                    return DpiAwarenessContextValue.SystemAware;
                }

                if (HwndTarget.IsProcessPerMonitorDpiAware == true)
                {
                    return DpiAwarenessContextValue.PerMonitorAware;
                }

                // HwndTarget has not been initialized yet - ask the current process
                // directly for its process DPI Awareness context value
                return DpiUtil.GetProcessDpiAwarenessContextValue(IntPtr.Zero);
            }
        }

        /// <summary>
        /// Reports whether per-monitor DPI scaling is active - i.e., 
        /// the process is (a) manifested for per-monitor DPI awareness, 
        /// (b) WPF recognizes this and has met the right preconditions (TFM, AppContext
        /// switches, OS version etc.) to turn on the DPI processing capabilities.
        /// </summary>
        private static bool IsPerMonitorDpiScalingActive
        {
            get
            {
                return HwndTarget.IsPerMonitorDpiScalingEnabled && 
                    (ProcessDpiAwarenessContextValue == DpiAwarenessContextValue.PerMonitorAware || 
                    ProcessDpiAwarenessContextValue == DpiAwarenessContextValue.PerMonitorAwareVersion2);
            }
        }

        /// <summary>
        /// This used to be the internal accessor for the
        /// HWND intended to watch for messages. It has since been changed
        /// into a private accessor, and replaced with <see cref="GetDpiCompatibleNotificationWindow(HandleRef)"/>
        /// 
        /// This accessor now returns the notify-window corresponding to the current process
        /// only. When a notify window corresponding to another HWND is needed (for e.g., to
        /// re-parent that HWND under the said notify-window), use <see cref="GetDpiCompatibleNotificationWindow(HandleRef)"/>
        /// to obtain a matching (w.r.t. DPI Awareness Context) notify-window.
        /// </summary>
        private static HwndWrapper Hwnd
        {
            get
            {
                EnsureResourceChangeListener();

                var hwndDpiInfo = _hwndNotify.Keys.FirstOrDefault((hwndDpiContext) => hwndDpiContext.DpiAwarenessContextValue == ProcessDpiAwarenessContextValue);
                Debug.Assert(hwndDpiInfo != null);

                // will throw when a match is not found, which should never happen because we just called Ensure...()
                return _hwndNotify[hwndDpiInfo].Value;
            }
        }

        /// <summary>
        /// Returns a notify-window with a DPI Awareness Context and DPI Scale factor that matches
        /// that of <paramref name="hwnd"/>
        /// </summary>
        /// <param name="hwnd">HWND to which DPI Awareness Context and DPI Scale factor is to be matched</param>
        /// <returns>Appropriate notify-window</returns>
        /// <remarks>
        /// Currently, this is used by <see cref="HwndHost"/> as a place to parent
        /// child HWND's when they are disconnected.
        /// 
        /// We attempt to select a notify-window that matches the DPI Awareness Context and DPI Scale factor
        /// of <paramref name="hwnd"/>. If one is not found, then we create a new notify-window that matches
        /// those two characteristics. 
        /// 
        /// Ensuring that the DPI Awareness contexts match is necessary to avoid unexpected behavior. The documentation
        /// for (Win32 function) SetParent outlines the problems associated with re-parenting of HWND's with mismatched
        /// DPI Awareness Contexts:
        /// 
        ///     Unexpected behavior or errors may occur if hWndNewParent and hWndChild are running in different DPI awareness modes. 
        ///     The table below outlines this behavior:
        ///     <list type="table">
        ///         <!-- Heading -->
        ///         <item>
        ///             <term>Operation</term>
        ///             <term>Windows 8.1</term>
        ///             <term>Windows 10(1607 and earlier)</term>
        ///             <term>Windows 10(1703 and later)</term>
        ///         </item>
        ///         <!-- In-proc behavior -->
        ///         <item>
        ///             <term>SetParent (In-Proc)</term>    
        ///             <term>N/A</term>
        ///             <term><b>Forced reset</b> (of current process)</term>            
        ///             <term><b>Fail</b>(ERROR_INVALID_STATE)</term>
        ///         </item>
        ///         <!-- Cross-proc behaviror -->
        ///         <item>
        ///             <term>SetParent(Cross-Proc)</term>
        ///             <term><b>Forced reset</b>(of child window's process)</term>
        ///             <term><b>Forced reset</b>(of child window's process)</term>
        ///             <term><b>Forced reset</b>(of child window's process)</term>
        ///         </item>
        ///     </list>
        ///     
        /// This sort of unexpected behavior is further complicated by the fact that the HWND presented by HwndHost might,
        /// in turn, host WPF controls again. (Some real-world applications, notably Visual Studio, use HwndHost to host
        /// native windows that in turn host WPF again). 
        /// 
        /// WPF does not make any allowances for changes to the DPI Awareness Context of an HWND/window after it has been created. Windows/Win32
        /// does not have a consistent model for dealing with reparenting of HWND's (observe the "In-Proc" row in the above table) with 
        /// mismatched DPI Awareness Contexts, nor is there any notification mechanism when this happens. 
        /// 
        /// Our data structures and book-keeping in the UI thread, as well as the render thread, could start deviating from reality in unexpected ways 
        /// if this were to happen. We do in fact dynamically query the DPI of HWND's as often as possible, but we have not designed the DPI support with 
        /// the assumption that the DPI Awareness Context of an HWND is mutable. The safest approach for us here is to avoid the problem before it is
        /// created, and remove any potential for recharacterization of an HWND's DPI Awareness Context. 
        /// 
        /// Ensuring that the DPI Scale Factor of the notify-window matches that of the reference HWND is only necessary when
        /// working with Per-Monitor Aware (or Per Monitor Aware v2) HWND's. Matching of DPI Scale factor ensures that the
        /// child-window (the one being supplied by HwndHost, and likely created and owned by the application, sometimes out-of-proc)
        /// does not receive WM_DPICHANGED, WM_DPICHANGED_AFTERPARENT, WM_DPICHANGED_BEFOREPARENT messages, and in turn, it is not
        /// susceptible to unexpected (and sometimes ill-defined - note that these notify-windows are zero sized windows) size-change
        /// requests.
        /// </remarks>
        internal static HwndWrapper GetDpiAwarenessCompatibleNotificationWindow(HandleRef hwnd)
        {
            var processDpiAwarenessContextValue = ProcessDpiAwarenessContextValue;

            // Do not call into DpiUtil.GetExtendedDpiInfoForWindow(), DpiUtil.GetWindowDpi etc.
            // unless IsPerMonitorDpiscalingActive == true. DpiUtil.GetExtendedDpiInfoForWindow(), 
            // DpiUtil.GetWindowDpi() etc.in turn call into methods that are only supported on platforms
            // with high DPI support (for e.g., not supported on Windows 7). 
            DpiUtil.HwndDpiInfo hwndDpiInfo =
                IsPerMonitorDpiScalingActive ?
                DpiUtil.GetExtendedDpiInfoForWindow(hwnd.Handle, fallbackToNearestMonitorHeuristic: true) :
                new DpiUtil.HwndDpiInfo(processDpiAwarenessContextValue, GetDpiScaleForUnawareOrSystemAwareContext(processDpiAwarenessContextValue));

            if (EnsureResourceChangeListener(hwndDpiInfo))
            {
                return _hwndNotify[hwndDpiInfo].Value;
            }

            return null;
        }

        /// <summary>
        /// Makes sure the listener window is the last one to get the Dispatcher.ShutdownFinished notification,
        /// thus giving any child windows a chance to respond first. See HwndHost.BuildOrReparentWindow().
        /// </summary>
        internal static void DelayHwndShutdown()
        {
            if (_hwndNotify != null && _hwndNotify.Count != 0)
            {
                Dispatcher d = Dispatcher.CurrentDispatcher;
                d.ShutdownFinished -= OnShutdownFinished;
                d.ShutdownFinished += OnShutdownFinished;
            }
        }

        #endregion

        #region Data

        [ThreadStatic] private static int _parsing;

        /// <summary>
        /// List of {<see cref="DpiAwarenessContextValue"/> , <see cref="DpiScale2"/>} combinations for which notify-windows are being maintained
        /// </summary>
        [ThreadStatic] private static List<DpiUtil.HwndDpiInfo> _dpiAwarenessContextAndDpis;

        [ThreadStatic] private static Dictionary<DpiUtil.HwndDpiInfo, SecurityCriticalDataClass<HwndWrapper>> _hwndNotify;
        [ThreadStatic]  private static Dictionary<DpiUtil.HwndDpiInfo, HwndWrapperHook> _hwndNotifyHook;

        private static Hashtable _resourceCache = new Hashtable();
        private static DTypeMap _themeStyleCache = new DTypeMap(100); // This is based upon the max DType.ID found in MSN scenario
        private static Dictionary<Assembly, ResourceDictionaries> _dictionaries;

        private static object _specialNull = new object();

        internal const string GenericResourceName = "themes/generic";
        internal const string ClassicResourceName = "themes/classic";

        private static Assembly _mscorlib;
        private static Assembly _presentationFramework;
        private static Assembly _presentationCore;
        private static Assembly _windowsBase;
        internal const string PresentationFrameworkName = "PresentationFramework";

        // SystemResourcesHaveChanged indicates to FE that the font properties need to be coerced
        // when creating a new root element
        internal static bool SystemResourcesHaveChanged;

        // SystemResourcesAreChanging is used by FE when coercing the font properties to determine
        // if it should return the current system metric or the value passed to the coerce callback
        [ThreadStatic]
        internal static bool SystemResourcesAreChanging;

        // Events supporting ResourceDictionaryDiagnostics
        internal static event EventHandler<ResourceDictionaryLoadedEventArgs> ThemedDictionaryLoaded;
        internal static event EventHandler<ResourceDictionaryUnloadedEventArgs> ThemedDictionaryUnloaded;
        internal static event EventHandler<ResourceDictionaryLoadedEventArgs> GenericDictionaryLoaded;

        #endregion
    }
    
    internal class DeferredResourceReference : DeferredReference
    {
        #region Constructor

        internal DeferredResourceReference(ResourceDictionary dictionary, object key)
        {
            _dictionary = dictionary;
            _keyOrValue = key;
        }

        #endregion Constructor

        #region Methods

        internal override object GetValue(BaseValueSourceInternal valueSource)
        {
            // If the _value cache is invalid fetch the value from
            // the dictionary else just retun the cached value
            if (_dictionary != null)
            {
                bool canCache;
                object value  = _dictionary.GetValue(_keyOrValue, out canCache);
                if (canCache)
                {
                    // Note that we are replacing the _keyorValue field
                    // with the value and deleting the _dictionary field.
                    _keyOrValue = value;
                    RemoveFromDictionary();
                }

                // Freeze if this value originated from a style or template
                bool freezeIfPossible =
                    valueSource == BaseValueSourceInternal.ThemeStyle ||
                    valueSource == BaseValueSourceInternal.ThemeStyleTrigger ||
                    valueSource == BaseValueSourceInternal.Style ||
                    valueSource == BaseValueSourceInternal.TemplateTrigger ||
                    valueSource == BaseValueSourceInternal.StyleTrigger ||
                    valueSource == BaseValueSourceInternal.ParentTemplate ||
                    valueSource == BaseValueSourceInternal.ParentTemplateTrigger;

                // This is to freeze values produced by deferred
                // references within styles and templates
                if (freezeIfPossible)
                {
                    StyleHelper.SealIfSealable(value);
                }

                // tell any listeners (e.g. ResourceReferenceExpressions)
                // that the value has been inflated
                OnInflated();

                return value;
            }

            return _keyOrValue;
        }

        // Tell the listeners that we're inflated.
        private void OnInflated()
        {
            if (_inflatedList != null)
            {
                foreach (ResourceReferenceExpression listener in _inflatedList)
                {
                    listener.OnDeferredResourceInflated(this);
                }
            }
        }

        // Gets the type of the value it represents
        internal override Type GetValueType()
        {
            if (_dictionary != null)
            {
                // Take a peek at the element type of the ElementStartRecord
                // within the ResourceDictionary's deferred content.
                bool found;
                return _dictionary.GetValueType(_keyOrValue, out found);
            }
            else
            {
                return _keyOrValue != null ? _keyOrValue.GetType() : null;
            }
        }

        // remove this DeferredResourceReference from its ResourceDictionary
        internal virtual void RemoveFromDictionary()
        {
            if (_dictionary != null)
            {
                _dictionary.DeferredResourceReferences.Remove(this);
                _dictionary = null;
            }
        }

        internal virtual void AddInflatedListener(ResourceReferenceExpression listener)
        {
            if (_inflatedList == null)
            {
                _inflatedList = new WeakReferenceList(this);
            }
            _inflatedList.Add(listener);
        }

        internal virtual void RemoveInflatedListener(ResourceReferenceExpression listener)
        {
            Debug.Assert(_inflatedList != null);

            if (_inflatedList != null)
            {
                _inflatedList.Remove(listener);
            }
        }

        #endregion Methods

        #region Properties

        internal virtual object Key
        {
            get { return _keyOrValue; }
        }

        internal ResourceDictionary Dictionary
        {
            get { return _dictionary; }
            set { _dictionary = value; }
        }

        internal virtual object Value
        {
            get { return _keyOrValue; }
            set { _keyOrValue = value; }
        }

        internal virtual bool IsUnset
        {
            get { return false; }
        }

        internal bool IsInflated
        {
            get { return (_dictionary == null); }
        }

        #endregion Properties

        #region Data

        private ResourceDictionary _dictionary;
        protected object _keyOrValue;
        private WeakReferenceList _inflatedList;

        #endregion Data
    }

    internal class DeferredAppResourceReference : DeferredResourceReference
    {
        #region Constructor

        internal DeferredAppResourceReference(ResourceDictionary dictionary, object resourceKey)
            : base(dictionary, resourceKey)
        {
        }

        #endregion Constructor

        #region Methods

        internal override object GetValue(BaseValueSourceInternal valueSource)
        {
            lock (((ICollection)Application.Current.Resources).SyncRoot)
            {
                return base.GetValue(valueSource);
            }
        }

        // Gets the type of the value it represents
        internal override Type GetValueType()
        {
            lock (((ICollection)Application.Current.Resources).SyncRoot)
            {
                return base.GetValueType();
            }
        }

        #endregion Methods
    }

    internal class DeferredThemeResourceReference : DeferredResourceReference
    {
        #region Constructor

        internal DeferredThemeResourceReference(ResourceDictionary dictionary, object resourceKey, bool canCacheAsThemeResource)
            :base(dictionary, resourceKey)
        {
            _canCacheAsThemeResource = canCacheAsThemeResource;
        }

        #endregion Constructor

        #region Methods

        internal override object GetValue(BaseValueSourceInternal valueSource)
        {
            lock (SystemResources.ThemeDictionaryLock)
            {
                // If the value cache is invalid fetch the value from
                // the dictionary else just retun the cached value
                if (Dictionary != null)
                {
                    bool canCache;
                    object key = Key;
                    object value;

                    SystemResources.IsSystemResourcesParsing = true;

                    try
                    {
                        value = Dictionary.GetValue(key, out canCache);
                        if (canCache)
                        {
                            // Note that we are replacing the _keyorValue field
                            // with the value and deleting the _dictionary field.
                            Value = value;
                            Dictionary = null;
                        }
                    }
                    finally
                    {
                        SystemResources.IsSystemResourcesParsing = false;
                    }

                    // Only cache keys that would be located by FindResourceInternal
                    if ((key is Type || key is ResourceKey) && _canCacheAsThemeResource && canCache)
                    {
                        SystemResources.CacheResource(key, value, false /*isTraceEnabled*/);
                    }

                    return value;
                }

                return Value;
            }
        }

        // Gets the type of the value it represents
        internal override Type GetValueType()
        {
            lock (SystemResources.ThemeDictionaryLock)
            {
                return base.GetValueType();
            }
        }

        // remove this DeferredResourceReference from its ResourceDictionary
        internal override void RemoveFromDictionary()
        {
            // DeferredThemeResourceReferences are never added to the dictionary's
            // list of deferred references, so they don't need to be removed.
        }

        #endregion Methods

        private bool _canCacheAsThemeResource;
    }

    /// <summary>
    /// This signifies a DeferredResourceReference that is used as a place holder
    /// for the front loaded StaticResource within a deferred content section.
    /// </summary>
    internal class DeferredResourceReferenceHolder : DeferredResourceReference
    {
        #region Constructor

        internal DeferredResourceReferenceHolder(object resourceKey, object value)
            :base(null, null)
        {
            _keyOrValue = new object[]{resourceKey, value};
        }

        #endregion Constructor

        #region Methods

        internal override object GetValue(BaseValueSourceInternal valueSource)
        {
            return Value;
        }

        // Gets the type of the value it represents
        internal override Type GetValueType()
        {
            object value = Value;
            return value != null ? value.GetType() : null;
        }

        #endregion Methods

        #region Properties

        internal override object Key
        {
            get { return ((object[])_keyOrValue)[0]; }
        }

        internal override object Value
        {
            get { return ((object[])_keyOrValue)[1]; }
            set { ((object[])_keyOrValue)[1] = value; }
        }

        internal override bool IsUnset
        {
            get { return Value == DependencyProperty.UnsetValue; }
        }

        #endregion Properties
    }

}




