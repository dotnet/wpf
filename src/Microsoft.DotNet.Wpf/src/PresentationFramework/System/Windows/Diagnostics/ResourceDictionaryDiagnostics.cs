// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//  ResourceDictionary diagnostics API
//      i.   Enables enumeration of generic and themed ResourceDictionary instances.
//      ii.  Notifies listeners when generic or themed ResourceDictionary instances
//           get loaded
//      iii. Notifies listeners when themed ResourceDictionary instances get
//           unloaded (generic ResourceDictionary instances are never unloaded)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Security;
using System.Windows;
using System.Windows.Threading;
using MS.Internal;
using MS.Internal.Utility;

namespace System.Windows.Diagnostics
{
    /// <summary>
    /// Enables enumeration of generic and themed <see cref="ResourceDictionary"/> instances, and provides
    /// a notification infrastructure for listening to loading and unloading of ResourceDictionary instances.
    /// </summary>
    /// <remarks>
    /// This type supports the .NET Framework infrastructure and is not intended to be used directly
    /// from application code.
    /// </remarks>
    public static class ResourceDictionaryDiagnostics
    {
        // Calls critical method IsEnvironmentVariableSet, but doesn't expose data
        static ResourceDictionaryDiagnostics()
        {
            IsEnabled = VisualDiagnostics.IsEnabled &&
                VisualDiagnostics.IsEnvironmentVariableSet(null, XamlSourceInfoHelper.XamlSourceInfoEnvironmentVariable);

            // internal property, not visible to user
            IgnorableProperties.Add(typeof(ResourceDictionary).GetProperty("DeferrableContent"));
        }

        #region Theme and Generic resource dictionaries

        /// <summary>
        /// When a managed debugger is attached, enumerates all instances of
        /// themed <see cref="ResourceDictionary"/> instances loaded
        /// by the application.
        /// </summary>
        public static IEnumerable<ResourceDictionaryInfo> ThemedResourceDictionaries
        {
            get
            {
                if (!IsEnabled)
                {
                    return ResourceDictionaryDiagnostics.EmptyResourceDictionaryInfos;
                }

                return SystemResources.ThemedResourceDictionaries;
            }
        }

        /// <summary>
        /// When a managed debugger is attached, enumerates all instances of
        /// generic <see cref="ResourceDictionary"/> instances loaded
        /// by the application.
        /// </summary>
        public static IEnumerable<ResourceDictionaryInfo> GenericResourceDictionaries
        {
            get
            {
                if (!IsEnabled)
                {
                    return ResourceDictionaryDiagnostics.EmptyResourceDictionaryInfos;
                }

                return SystemResources.GenericResourceDictionaries;
            }
        }

        /// <summary>
        /// Occurs when a managed debugger is attached, and a themed <see cref="ResourceDictionary"/> is loaded
        /// by the application.
        /// </summary>
        public static event EventHandler<ResourceDictionaryLoadedEventArgs> ThemedResourceDictionaryLoaded
        {
            add
            {
                if (IsEnabled)
                {
                    SystemResources.ThemedDictionaryLoaded += value;
                }
            }

            remove
            {
                SystemResources.ThemedDictionaryLoaded -= value;
            }
        }

        /// <summary>
        /// Occurs when a managed debugger is attached, and a themed <see cref="ResourceDictionary"/> is unloaded
        /// from the application.
        /// </summary>
        public static event EventHandler<ResourceDictionaryUnloadedEventArgs> ThemedResourceDictionaryUnloaded
        {
            add
            {
                if (IsEnabled)
                {
                    SystemResources.ThemedDictionaryUnloaded += value;
                }
            }

            remove
            {
                SystemResources.ThemedDictionaryUnloaded -= value;
            }
        }

        /// <summary>
        /// Occurs when a managed debugger is attached, and a generic <see cref="ResourceDictionary"/> is loaded
        /// by the application.
        /// </summary>
        public static event EventHandler<ResourceDictionaryLoadedEventArgs> GenericResourceDictionaryLoaded
        {
            add
            {
                if(IsEnabled)
                {
                    SystemResources.GenericDictionaryLoaded += value;
                }
            }

            remove
            {
                SystemResources.GenericDictionaryLoaded -= value;
            }
        }

        #endregion

        #region Find ResourceDictionaries created from a given source Uri

        public static IEnumerable<ResourceDictionary> GetResourceDictionariesForSource(Uri uri)
        {
            if (!IsEnabled || _dictionariesFromUri == null)
            {
                return EmptyResourceDictionaries;
            }

            lock (_dictionariesFromUriLock)
            {
                List<WeakReference<ResourceDictionary>> list;
                if (!_dictionariesFromUri.TryGetValue(uri, out list) || list.Count == 0)
                {
                    return EmptyResourceDictionaries;
                }

                List<ResourceDictionary> result = new List<ResourceDictionary>(list.Count);
                List<WeakReference<ResourceDictionary>> toRemove = null;

                foreach (WeakReference<ResourceDictionary> wr in list)
                {
                    ResourceDictionary rd;
                    if (wr.TryGetTarget(out rd))
                    {
                        result.Add(rd);
                    }
                    else
                    {
                        // stale entry - mark for removal (can't remove while iterating over list)
                        if (toRemove == null)
                        {
                            toRemove = new List<WeakReference<ResourceDictionary>>();
                        }
                        toRemove.Add(wr);
                    }
                }

                // if we found stale entries, remove them now
                if (toRemove != null)
                {
                    RemoveEntries(uri, list, toRemove);
                }

                return result.AsReadOnly();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddResourceDictionaryForUri(Uri uri, ResourceDictionary rd)
        {
            if (uri != null && IsEnabled)
            {
                AddResourceDictionaryForUriImpl(uri, rd);
            }
        }

        private static void AddResourceDictionaryForUriImpl(Uri uri, ResourceDictionary rd)
        {
            lock (_dictionariesFromUriLock)
            {
                List<WeakReference<ResourceDictionary>> list;

                if (_dictionariesFromUri == null)
                {
                    _dictionariesFromUri = new Dictionary<Uri, List<WeakReference<ResourceDictionary>>>();
                }

                if (!_dictionariesFromUri.TryGetValue(uri, out list))
                {
                    list = new List<WeakReference<ResourceDictionary>>(1);
                    _dictionariesFromUri.Add(uri, list);
                }

                list.Add(new WeakReference<ResourceDictionary>(rd));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RemoveResourceDictionaryForUri(Uri uri, ResourceDictionary rd)
        {
            if (uri != null && IsEnabled)
            {
                RemoveResourceDictionaryForUriImpl(uri, rd);
            }
        }

        private static void RemoveResourceDictionaryForUriImpl(Uri uri, ResourceDictionary rd)
        {
            lock (_dictionariesFromUriLock)
            {
                if (_dictionariesFromUri != null)
                {
                    List<WeakReference<ResourceDictionary>> list;
                    if (_dictionariesFromUri.TryGetValue(uri, out list))
                    {
                        List<WeakReference<ResourceDictionary>> toRemove = new List<WeakReference<ResourceDictionary>>();
                        foreach (WeakReference<ResourceDictionary> wr in list)
                        {
                            ResourceDictionary target;
                            if (!wr.TryGetTarget(out target) || target == rd)
                            {
                                toRemove.Add(wr);
                            }
                        }

                        RemoveEntries(uri, list, toRemove);
                    }
                }
            }
        }

        private static void RemoveEntries(Uri uri,
            List<WeakReference<ResourceDictionary>> list,
            List<WeakReference<ResourceDictionary>> toRemove)
        {
            foreach (WeakReference<ResourceDictionary> wr in toRemove)
            {
                list.Remove(wr);
            }

            if (list.Count == 0)
            {
                _dictionariesFromUri.Remove(uri);
            }
        }

        private static Dictionary<Uri, List<WeakReference<ResourceDictionary>>> _dictionariesFromUri;
        private static object _dictionariesFromUriLock = new object();
        private static IReadOnlyCollection<ResourceDictionary> EmptyResourceDictionaries
            => Array.Empty<ResourceDictionary>();

        #endregion

        #region Find owners of ResourceDictionary

        public static IEnumerable<FrameworkElement> GetFrameworkElementOwners(ResourceDictionary dictionary)
        {
            return GetOwners<FrameworkElement>(dictionary.FrameworkElementOwners, EmptyFrameworkElementList);
        }

        public static IEnumerable<FrameworkContentElement> GetFrameworkContentElementOwners(ResourceDictionary dictionary)
        {
            return GetOwners<FrameworkContentElement>(dictionary.FrameworkContentElementOwners, EmptyFrameworkContentElementList);
        }

        public static IEnumerable<Application> GetApplicationOwners(ResourceDictionary dictionary)
        {
            return GetOwners<Application>(dictionary.ApplicationOwners, EmptyApplicationList);
        }

        private static IEnumerable<T> GetOwners<T>(WeakReferenceList list, IEnumerable<T> emptyList)
            where T : DispatcherObject
        {
            if (!IsEnabled || list == null || list.Count == 0)
            {
                return emptyList;
            }

            List<T> result = new List<T>(list.Count);
            foreach (Object o in list)
            {
                T owner = o as T;
                if (owner != null)
                {
                    result.Add(owner);
                }
            }

            return result.AsReadOnly();
        }

        private static IReadOnlyCollection<FrameworkElement> EmptyFrameworkElementList
            => Array.Empty<FrameworkElement>();
        private static IReadOnlyCollection<FrameworkContentElement> EmptyFrameworkContentElementList
            => Array.Empty<FrameworkContentElement>();
        private static IReadOnlyCollection<Application> EmptyApplicationList
            => Array.Empty<Application>();

        #endregion

        #region Notify when static resource references are resolved

        public static event EventHandler<StaticResourceResolvedEventArgs> StaticResourceResolved;

        internal static bool HasStaticResourceResolvedListeners
        {
            get { return IsEnabled && (StaticResourceResolved != null); }
        }

        internal static bool ShouldIgnoreProperty(object targetProperty)
        {
            return IgnorableProperties.Contains(targetProperty);
        }

        internal static LookupResult RequestLookupResult(StaticResourceExtension requester)
        {
            // requests can be nested - e.g. when one request resolves to a
            // resource whose value is deferred content that gets inflated.
            // So we need a (thread static) stack of active requests.
            if (_lookupResultStack == null)
            {
                _lookupResultStack = new Stack<LookupResult>();
            }

            LookupResult result = new LookupResult(requester);
            _lookupResultStack.Push(result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RecordLookupResult(object key, ResourceDictionary rd)
        {
            if (IsEnabled && _lookupResultStack != null)
            {
                RecordLookupResultImpl(key, rd);
            }
        }

        private static void RecordLookupResultImpl(object key, ResourceDictionary rd)
        {
            if (_lookupResultStack.Count > 0)
            {
                // get the active request
                LookupResult result = _lookupResultStack.Peek();

                // if the resolution is for some other key, ignore it
                // (this can happen when inflating deferred content)
                if (!Object.Equals(result.Requester.ResourceKey, key))
                    return;

                // simple case - no deferred content, no lookup optimizations
                if (result.Requester.GetType() == typeof(StaticResourceExtension))
                {
                    Debug.Assert(result.Dictionary == null || result.Dictionary == rd,
                                "simple lookup resolved more than once");
                    result.Key = key;
                    result.Dictionary = rd;
                    return;
                }

                // the requester is a derived class of StaticResourceExtension
                // (notably StaticResourceHolder, the only built-in derived class).
                // This introduces complications:
                //      1. The class caches the result of its lookup the first time,
                //          and returns the cached value thereafter.  This avoids
                //          the lookup, and thus also avoids RD's call to this method.
                //      2. The initial lookup can happen twice, the second time
                //          to cope with deferred content including a closer definition
                //          (see StaticResourceExtension.FindResourceInDeferredContent).

                // For (2), retain the latest recorded result.
                result.Key = key;
                result.Dictionary = rd;

                // There's a potential bug here, with no fix/workaround of reasonable
                // cost.  The two lookups cited in (2) will both resolve with the
                // right request at the top of the stack - good.  But if there is
                // a subsequent lookup for the same key from some other
                // (non-StaticResourceExtension) source made while the request is
                // still on top, its result will get recorded here - bad, it might
                // yield the wrong dictionary.
                // This would have to be a complex situation involving deferred
                // content with nested dictionaries providing alternative definitions
                // for the same key, and with that "subsequent lookup".
                // I can't get it to happen (especially the "subsequent lookup"
                // part), but I'm not 100% certain it can't.  But as it would
                // only impact diagnostics, and that only in a limited way, it's
                // not worth any more effort.
            }
        }

        internal static void RevertRequest(StaticResourceExtension requester, bool success)
        {
            LookupResult result = _lookupResultStack.Pop();
            Debug.Assert(Object.Equals(result.Requester, requester), "Reverting a request for the wrong requester");

            // if the request failed, nothing more to do
            if (!success)
                return;

            // simple case - no deferred content, no lookup optimizations
            if (result.Requester.GetType() == typeof(StaticResourceExtension))
            {
                Debug.Assert(result.Dictionary != null, "simple lookup resolved incorrectly");
                return;
            }

            // complex case (see (1) in remarks in RecordLookupResultImpl above)
            // Maintain a cache mapping requester to the dictionary it used.
            // Use weak references to avoid memory leaks.
            if (_resultCache == null)
            {
                _resultCache = new Dictionary<WeakReferenceKey<StaticResourceExtension>, WeakReference<ResourceDictionary>>();
            }

            WeakReferenceKey<StaticResourceExtension> wrKey = new WeakReferenceKey<StaticResourceExtension>(requester);
            WeakReference<ResourceDictionary> wrDict;
            ResourceDictionary cachedDict = null;

            bool found = _resultCache.TryGetValue(wrKey, out wrDict);
            if (found)
            {
                wrDict.TryGetTarget(out cachedDict);
            }

            if (result.Dictionary != null)
            {
                // The first time the class requests a value, cache the dictionary
                if (cachedDict != null)
                {
                    Debug.Assert(cachedDict == result.Dictionary, "conflicting dictionaries for a StaticResource reference");
                }

                _resultCache[wrKey] = new WeakReference<ResourceDictionary>(result.Dictionary);
            }
            else
            {
                // for subsequent (optimized) requests, use the dictionary from the cache
                Debug.Assert(cachedDict != null, "no dictionary found for StaticResource reference");
                result.Key = requester.ResourceKey;
                result.Dictionary = cachedDict;
            }
        }

        internal static void OnStaticResourceResolved(object targetObject, object targetProperty, LookupResult result)
        {
            EventHandler<StaticResourceResolvedEventArgs> handler = StaticResourceResolved;
            if (handler != null && result.Dictionary != null)
            {
                handler(null, new StaticResourceResolvedEventArgs(
                                    targetObject,
                                    targetProperty,
                                    result.Dictionary,
                                    result.Key));
            }

            RequestCacheCleanup(targetObject);
        }

        private static void RequestCacheCleanup(object targetObject)
        {
            DispatcherObject d;
            Dispatcher dispatcher;

            // ignore if cleanup is not needed, already requested, or not requestable
            if (_resultCache == null ||
                _cleanupOperation != null ||
                (d = targetObject as DispatcherObject) == null ||
                (dispatcher = d.Dispatcher) == null)
            {
                return;
            }

            // request a cleanup at low priority
            _cleanupOperation = dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)DoCleanup);
        }

        // purge the cache of stale entries
        private static void DoCleanup()
        {
            _cleanupOperation = null;

            List<WeakReferenceKey<StaticResourceExtension>> toRemove = null;
            foreach (KeyValuePair<WeakReferenceKey<StaticResourceExtension>, WeakReference<ResourceDictionary>>
                        kvp in _resultCache)
            {
                ResourceDictionary dict;
                if (kvp.Key.Item == null || !kvp.Value.TryGetTarget(out dict))
                {
                    if (toRemove == null)
                    {
                        toRemove = new List<WeakReferenceKey<StaticResourceExtension>>();
                    }
                    toRemove.Add(kvp.Key);
                }
            }

            if (toRemove != null)
            {
                foreach (WeakReferenceKey<StaticResourceExtension> wrKey in toRemove)
                {
                    _resultCache.Remove(wrKey);
                }
            }
        }

        private static List<object> IgnorableProperties = new List<object>();

        // these are [ThreadStatic] because two threads can be resolving StaticResource simultaneously
        [ThreadStatic]
        private static Stack<LookupResult> _lookupResultStack;
        [ThreadStatic]
        private static Dictionary<WeakReferenceKey<StaticResourceExtension>, WeakReference<ResourceDictionary>>
                _resultCache;
        [ThreadStatic]
        private static DispatcherOperation _cleanupOperation;

        internal class LookupResult
        {
            public StaticResourceExtension Requester { get; set; }
            public Object Key { get; set; }
            public ResourceDictionary Dictionary { get; set; }
            public LookupResult(StaticResourceExtension requester) { Requester = requester; }
        }

        #endregion

        internal static bool IsEnabled { get; private set; }

        private static readonly ReadOnlyCollection<ResourceDictionaryInfo> EmptyResourceDictionaryInfos
            = new List<ResourceDictionaryInfo>().AsReadOnly();
    }
}
