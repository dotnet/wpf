// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

// Purpose:  Helper functions that require elevation but are safe to use.

using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
#if WINDOWS_BASE || PRESENTATION_CORE || SYSTEM_XAML
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
#endif
#if SYSTEM_XAML
using TypeConverterHelper = System.Xaml.TypeConverterHelper;
#else
using Microsoft.Win32;
using MS.Win32;
using TypeConverterHelper = System.Windows.Markup.TypeConverterHelper;
#endif
#if PRESENTATIONFRAMEWORK
using System.Windows;
using System.Windows.Media;
#endif

// The SafeSecurityHelper class differs between assemblies and could not actually be
//  shared, so it is duplicated across namespaces to prevent name collision.
#if WINDOWS_BASE
namespace MS.Internal.WindowsBase
#elif PRESENTATION_CORE
namespace MS.Internal.PresentationCore
#elif PRESENTATIONFRAMEWORK
namespace MS.Internal.PresentationFramework
#elif REACHFRAMEWORK
namespace MS.Internal.ReachFramework
#elif DRT
namespace MS.Internal.Drt
#elif SYSTEM_XAML
namespace System.Xaml
#else
#error Class is being used from an unknown assembly.
#endif
{
    internal static partial class SafeSecurityHelper
    {
#if PRESENTATION_CORE
        ///<summary>
        /// Given a rectangle with coords in local screen coordinates.
        /// Return the rectangle in global screen coordinates.
        ///</summary>
        internal static void TransformLocalRectToScreen(HandleRef hwnd, ref NativeMethods.RECT rcWindowCoords)
        {
            int retval = MS.Internal.WindowsBase.NativeMethodsSetLastError.MapWindowPoints(hwnd , new HandleRef(null, IntPtr.Zero), ref rcWindowCoords, 2);
            int win32Err = Marshal.GetLastWin32Error();
            if(retval == 0 && win32Err != 0)
            {
                throw new System.ComponentModel.Win32Exception(win32Err);
            }
        }
#endif

#if PRESENTATIONFRAMEWORK
        internal static Point ClientToScreen(UIElement relativeTo, Point point)
        {
            GeneralTransform transform;
            PresentationSource source = PresentationSource.CriticalFromVisual(relativeTo);

            if (source == null)
            {
                return new Point(double.NaN, double.NaN);
            }
            transform = relativeTo.TransformToAncestor(source.RootVisual);
            Point ptRoot;
            transform.TryTransform(point, out ptRoot);
            Point ptClient = PointUtil.RootToClient(ptRoot, source);
            Point ptScreen = PointUtil.ClientToScreen(ptClient, source);

            return ptScreen;
        }
#endif // PRESENTATIONFRAMEWORK

#if WINDOWS_BASE || PRESENTATION_CORE || SYSTEM_XAML

        // Cache of Assembly -> AssemblyName, because calling new AssemblyName() is expensive.
        // If the assembly is static, the key is the assembly; if it's dynamic, the key is a WeakRefKey
        // pointing to the assembly, so we don't root collectible assemblies.
        //
        // This cache is bound (gated) by the number of assemblies in the appdomain.
        // We use a callback on GC to purge out collected assemblies, so we don't grow indefinitely.
        //
        private static Dictionary<object, AssemblyName> _assemblies; // get key via GetKeyForAssembly
        private static object syncObject = new object();
        private static bool _isGCCallbackPending;

        // PERF: Cache delegate for CleanupCollectedAssemblies to avoid allocating it each time.
        private static readonly WaitCallback _cleanupCollectedAssemblies = CleanupCollectedAssemblies;

        // Names an AssemblyLoadContext was asked for and could not resolve, so that it is
        // asked once rather than on every lookup. Keyed weakly so collectible contexts can unload.
        private static readonly ConditionalWeakTable<AssemblyLoadContext, ConcurrentDictionary<string, bool>> s_unresolvedNames = new();

        /// <summary>
        ///     Finds the assembly that a lookup for the given name refers to.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     An assembly with a given name and version can be loaded more than once, each copy
        ///     in its own <see cref="AssemblyLoadContext"/>, and each copy has its own type identity.
        ///     Picking the last loaded match from the whole process can therefore hand the caller
        ///     a copy that belongs to an unrelated context.
        ///     </para>
        ///     <para>
        ///     A lookup is made for a load context: the current contextual reflection context, or
        ///     the default context when none is established, which is where the caller's own
        ///     <see cref="Assembly.Load(AssemblyName)"/> fallback would resolve the name. That context
        ///     answers first, from the assemblies it holds or through its own resolution, wherever
        ///     the assembly it chooses lives. When it cannot resolve the name at all, and exactly
        ///     one context holds a copy, that copy is the answer. Otherwise the last loaded match
        ///     in the process is returned, as before.
        ///     </para>
        ///     <para>
        ///     An application with a single load context sees no difference: every copy is in the
        ///     default context, and its answer is the one the process-wide search gives.
        ///     </para>
        /// </remarks>
        internal static Assembly GetLoadedAssembly(AssemblyName assemblyName)
        {
            return GetAssemblyForCurrentContext(assemblyName) ?? GetLastLoadedAssembly(assemblyName);
        }

        /// <summary>
        ///     Finds the assembly the current load context means by the given name, or the only
        ///     loaded copy when that context cannot resolve it.
        /// </summary>
        /// <returns>
        ///     The assembly, or <see langword="null"/> when neither applies; unlike
        ///     <see cref="GetLoadedAssembly"/>, this never falls back to the process-wide search.
        /// </returns>
        internal static Assembly GetAssemblyForCurrentContext(AssemblyName assemblyName)
        {
            AssemblyLoadContext context = AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.Default;

            return GetAssemblyForContext(context, assemblyName) ?? GetOnlyLoadedAssembly(assemblyName);
        }

        /// <summary>
        ///     Finds the assembly a load context means by the given name: one it already holds,
        ///     or the one its own resolution chooses.
        /// </summary>
        private static Assembly GetAssemblyForContext(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            Assembly held = FindMatch(context.Assemblies, assemblyName);
            if (held is not null)
            {
                return held;
            }

            // A name the context has not loaded yet may still be one it ships. Treating it as
            // foreign is what hands the caller another context's copy, so the context is asked.
            ConcurrentDictionary<string, bool> unresolved = s_unresolvedNames.GetValue(context, static _ => new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase));
            string key = assemblyName.FullName;

            if (unresolved.ContainsKey(key))
            {
                return null;
            }

            try
            {
                return context.LoadFromAssemblyName(assemblyName);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is FileLoadException || ex is BadImageFormatException)
            {
                // The ways a context says the name is not one it can resolve. The runtime also
                // reports a Load override or Resolving handler that throws as a FileLoadException,
                // so a failing resolver falls back to the search below, as the callers' own
                // Assembly.Load fallbacks already did.
                unresolved.TryAdd(key, true);
                return null;
            }
        }

        /// <summary>
        ///     Returns the matching assembly when exactly one load context holds a copy of it.
        /// </summary>
        /// <remarks>
        ///     Several copies are left to the process-wide search, since a name alone cannot tell
        ///     which of them was meant. Nothing is remembered, as another copy can load at any time.
        /// </remarks>
        private static Assembly GetOnlyLoadedAssembly(AssemblyName assemblyName)
        {
            Assembly only = null;

            foreach (AssemblyLoadContext context in AssemblyLoadContext.All)
            {
                Assembly match = FindMatch(context.Assemblies, assemblyName);
                if (match is null)
                {
                    continue;
                }

                if (only is not null)
                {
                    return null;
                }

                only = match;
            }

            return only;
        }

        /// <summary>
        ///     Iterates through the assemblies loaded in the current AppDomain, newest first,
        ///     and finds one that has the same assembly name passed in.
        /// </summary>
        private static Assembly GetLastLoadedAssembly(AssemblyName assemblyName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = assemblies.Length - 1; i >= 0; i--)
            {
                if (IsMatch(assemblyName, GetAssemblyName(assemblies[i])))
                {
                    return assemblies[i];
                }
            }

            return null;
        }

        private static Assembly FindMatch(IEnumerable<Assembly> assemblies, AssemblyName assemblyName)
        {
            foreach (Assembly assembly in assemblies)
            {
                if (IsMatch(assemblyName, GetAssemblyName(assembly)))
                {
                    return assembly;
                }
            }

            return null;
        }

        private static bool IsMatch(AssemblyName requested, AssemblyName candidate)
        {
            Version reqVersion = requested.Version;
            CultureInfo reqCulture = requested.CultureInfo;
            byte[] reqKeyToken = requested.GetPublicKeyToken();

            return string.Equals(candidate.Name, requested.Name, StringComparison.InvariantCultureIgnoreCase) &&
                   (reqVersion is null || reqVersion.Equals(candidate.Version)) &&
                   (reqCulture is null || reqCulture.Equals(candidate.CultureInfo)) &&
                   (reqKeyToken is null || IsSameKeyToken(reqKeyToken, candidate.GetPublicKeyToken()));
        }

        private static AssemblyName GetAssemblyName(Assembly assembly)
        {
            object key = assembly.IsDynamic ? (object)new WeakRefKey(assembly) : assembly;
            lock (syncObject)
            {
                AssemblyName result;
                if (_assemblies is null)
                {
                    _assemblies = new Dictionary<object, AssemblyName>();
                }
                else
                {
                    if (_assemblies.TryGetValue(key, out result))
                    {
                        return result;
                    }
                }

                //
                // We use AssemblyName ctor here because GetName demands FileIOPermission
                // and does load more than just the required information.
                // Essentially we use AssemblyName just to help parsing the name, version, culture
                // and public key token from the assembly's name.
                //
                result = new AssemblyName(assembly.FullName);
                _assemblies.Add(key, result);
                if (assembly.IsDynamic && !_isGCCallbackPending)
                {
                    // Make sure we clean up the cache if/when this dynamic assembly is GCed
                    GCNotificationToken.RegisterCallback(_cleanupCollectedAssemblies, null);
                    _isGCCallbackPending  = true;
                }

                return result;
            }
        }

        // After a GC, clean up the weakrefs to any collected dynamic assemblies
        private static void CleanupCollectedAssemblies(object state) // dummy parameter required by WaitCallback definition
        {
            bool foundLiveDynamicAssemblies = false;
            List<object> keysToRemove = null;
            lock (syncObject)
            {
                foreach (object key in _assemblies.Keys)
                {
                    if (key is not WeakReference weakRef)
                    {
                        continue;
                    }

                    if (weakRef.IsAlive)
                    {
                        // There is a weak ref that is still alive, register another GC callback for next time
                        foundLiveDynamicAssemblies = true;
                    }
                    else
                    {
                        // The target has been collected, add it to our list of keys to remove
                        if (keysToRemove is null)
                        {
                            keysToRemove = new List<object>();
                        }

                        keysToRemove.Add(key);
                    }
                }

                if (keysToRemove is not null)
                {
                    foreach (object key in keysToRemove)
                    {
                        _assemblies.Remove(key);
                    }
                }

                if (foundLiveDynamicAssemblies)
                {
                    GCNotificationToken.RegisterCallback(_cleanupCollectedAssemblies, null);
                }
                else
                {
                    _isGCCallbackPending = false;
                }
            }
        }

#endif  // WINDOWS_BASE || PRESENTATION_CORE || SYSTEM_XAML

        //
        // Determine if two Public Key Tokens are the same.
        //
#if !REACHFRAMEWORK
#if PRESENTATIONFRAMEWORK || SYSTEM_XAML || PRESENTATION_CORE
        internal
#else
        private
#endif
        static bool IsSameKeyToken(byte[] reqKeyToken, byte[] curKeyToken)
        {
           bool isSame = false;

           if (reqKeyToken is null && curKeyToken is null)
           {
               // Both Key Tokens are not set, treat them as same.
               isSame = true;
           }
           else if (reqKeyToken is not null && curKeyToken is not null)
           {
               // Both KeyTokens are set.
               if (reqKeyToken.Length == curKeyToken.Length)
               {
                   isSame = true;

                   for (int i = 0; i < reqKeyToken.Length; i++)
                   {
                      if (reqKeyToken[i] != curKeyToken[i])
                      {
                         isSame = false;
                         break;
                      }
                   }
               }
           }

           return isSame;
        }
#endif //!REACHFRAMEWORK

        internal const string IMAGE = "image";
    }

#if WINDOWS_BASE || PRESENTATION_CORE || SYSTEM_XAML
    // for use as the key to a dictionary, when the "real" key is an object
    // that we should not keep alive by a strong reference.
    internal class WeakRefKey : WeakReference
    {
        public WeakRefKey(object target) : base(target)
        {
            Debug.Assert(target is not null);
            _hashCode = target.GetHashCode();
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object o)
        {
            WeakRefKey weakRef = o as WeakRefKey;
            if (weakRef is not null)
            {
                object target1 = Target;
                object target2 = weakRef.Target;

                if (target1 is not null && target2 is not null)
                {
                    return (target1 == target2);
                }
            }

            return base.Equals(o);
        }

        public static bool operator ==(WeakRefKey left, WeakRefKey right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(WeakRefKey left, WeakRefKey right)
        {
            return !(left == right);
        }

        private readonly int _hashCode;  // cache target's hashcode, lest it get GC'd out from under us
    }

    // This cleanup token will be immediately thrown away and as a result it will
    // (a couple of GCs later) make it into the finalization queue and when finalized
    // will kick off a thread-pool job that you can use to purge a weakref cache.
    internal class GCNotificationToken
    {
        private WaitCallback callback;
        private object state;

        private GCNotificationToken(WaitCallback callback, object state)
        {
            this.callback = callback;
            this.state = state;
        }

        ~GCNotificationToken()
        {
            // Schedule cleanup
            ThreadPool.QueueUserWorkItem(callback, state);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "See comment above")]
        internal static void RegisterCallback(WaitCallback callback, object state)
        {
            new GCNotificationToken(callback, state);
        }
    }
#endif
}
