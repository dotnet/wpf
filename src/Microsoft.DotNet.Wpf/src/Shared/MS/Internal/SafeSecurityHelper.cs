// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

// Purpose:  Helper functions that require elevation but are safe to use.

using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
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

        /// <summary>
        ///     This function iterates through the assemblies loaded in the current
        ///     AppDomain and finds one that has the same assembly name passed in.
        /// </summary>
        internal static Assembly GetLoadedAssembly(AssemblyName assemblyName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Version reqVersion = assemblyName.Version;
            CultureInfo reqCulture = assemblyName.CultureInfo;
            byte[] reqKeyToken = assemblyName.GetPublicKeyToken();

            for (int i = assemblies.Length - 1; i >= 0; i--)
            {
                AssemblyName curAsmName = GetAssemblyName(assemblies[i]);
                Version curVersion = curAsmName.Version;
                CultureInfo curCulture = curAsmName.CultureInfo;
                byte[] curKeyToken = curAsmName.GetPublicKeyToken();

                if (string.Equals(curAsmName.Name, assemblyName.Name, StringComparison.InvariantCultureIgnoreCase) &&
                     (reqVersion is null || reqVersion.Equals(curVersion)) &&
                     (reqCulture is null || reqCulture.Equals(curCulture)) &&
                     (reqKeyToken is null || IsSameKeyToken(reqKeyToken, curKeyToken)))
                {
                    return assemblies[i];
                }
            }

            return null;
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
