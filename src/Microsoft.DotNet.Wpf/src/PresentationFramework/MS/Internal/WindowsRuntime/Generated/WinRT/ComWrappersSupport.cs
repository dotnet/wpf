// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Linq.Expressions;
using WinRT.Interop;

#if !NETSTANDARD2_0
using ComInterfaceEntry = System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry;
#endif

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{
    internal static partial class ComWrappersSupport
    {
        private readonly static ConcurrentDictionary<string, Func<IInspectable, object>> TypedObjectFactoryCache = new ConcurrentDictionary<string, Func<IInspectable, object>>();

        private readonly static Guid IID_IAgileObject = Guid.Parse("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90");

        static ComWrappersSupport()
        {
            PlatformSpecificInitialize();
        }

        static partial void PlatformSpecificInitialize();

        public static TReturn MarshalDelegateInvoke<TDelegate, TReturn>(IntPtr thisPtr, Func<TDelegate, TReturn> invoke)
            where TDelegate : class, Delegate
        {
            var target_invoke = FindObject<TDelegate>(thisPtr);
            if (target_invoke != null)
            {
                return invoke(target_invoke);
            }
            return default;
        }

        public static void MarshalDelegateInvoke<T>(IntPtr thisPtr, Action<T> invoke)
            where T : class, Delegate
        {
            var target_invoke = FindObject<T>(thisPtr);
            if (target_invoke != null)
            {
                invoke(target_invoke);
            }
        }

        public static bool TryUnwrapObject(object o, out IObjectReference objRef)
        {
            // The unwrapping here needs to be in exact type match in case the user
            // has implemented a WinRT interface or inherited from a WinRT class
            // in a .NET (non-projected) type.

            if (o is Delegate del)
            {
                return TryUnwrapObject(del.Target, out objRef);
            }

            Type type = o.GetType();
            ObjectReferenceWrapperAttribute objRefWrapper = type.GetCustomAttribute<ObjectReferenceWrapperAttribute>();
            if (objRefWrapper is object)
            {
                objRef = (IObjectReference)type.GetField(objRefWrapper.ObjectReferenceField, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).GetValue(o);
                return true;
            }

            ProjectedRuntimeClassAttribute projectedClass = type.GetCustomAttribute<ProjectedRuntimeClassAttribute>();

            if (projectedClass is object)
            {
                return TryUnwrapObject(
                    type.GetProperty(projectedClass.DefaultInterfaceProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).GetValue(o),
                    out objRef);
            }

            objRef = null;
            return false;
        }

        public static IObjectReference GetObjectReferenceForInterface(IntPtr externalComObject)
        {
            using var unknownRef = ObjectReference<IUnknownVftbl>.FromAbi(externalComObject);

            if (unknownRef.TryAs<IUnknownVftbl>(IID_IAgileObject, out var agileRef) >= 0)
            {
                agileRef.Dispose();
                return unknownRef.As<IUnknownVftbl>();
            }
            else
            {
                return new ObjectReferenceWithContext<IUnknownVftbl>(
                    unknownRef.GetRef(),
                    Context.GetContextCallback());
            }
        }

        public static List<ComInterfaceEntry> GetInterfaceTableEntries(object obj)
        {
            var entries = new List<ComInterfaceEntry>();
            var interfaces = obj.GetType().GetInterfaces();
            foreach (var iface in interfaces)
            {
                if (Projections.IsTypeWindowsRuntimeType(iface))
                {
                    var ifaceAbiType = iface.FindHelperType();
                    entries.Add(new ComInterfaceEntry
                    {
                        IID = GuidGenerator.GetIID(ifaceAbiType),
                        Vtable = (IntPtr)ifaceAbiType.FindVftblType().GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
                    });
                }

                if (iface.IsConstructedGenericType
                    && Projections.TryGetCompatibleWindowsRuntimeTypeForVariantType(iface, out var compatibleIface))
                {
                    var compatibleIfaceAbiType = compatibleIface.FindHelperType();
                    entries.Add(new ComInterfaceEntry
                    {
                        IID = GuidGenerator.GetIID(compatibleIfaceAbiType),
                        Vtable = (IntPtr)compatibleIfaceAbiType.FindVftblType().GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
                    });
                }
            }

            if (obj is Delegate)
            {
                entries.Add(new ComInterfaceEntry
                {
                    IID = GuidGenerator.GetIID(obj.GetType()),
                    Vtable = (IntPtr)obj.GetType().GetHelperType().GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
                });
            }
            // Add IAgileObject to all CCWs
            entries.Add(new ComInterfaceEntry
            {
                IID = IID_IAgileObject,
                Vtable = IUnknownVftbl.AbiToProjectionVftblPtr
            });
            return entries;
        }

        public static (InspectableInfo inspectableInfo, List<ComInterfaceEntry> interfaceTableEntries) PregenerateNativeTypeInformation(object obj)
        {
            var interfaceTableEntries = GetInterfaceTableEntries(obj);
            var iids = new Guid[interfaceTableEntries.Count];
            for (int i = 0; i < interfaceTableEntries.Count; i++)
            {
                iids[i] = interfaceTableEntries[i].IID;
            }

            Type type = obj.GetType();

            if (type.FullName.StartsWith("ABI."))
            {
                type = Projections.FindCustomPublicTypeForAbiType(type) ?? type.Assembly.GetType(type.FullName.Substring("ABI.".Length)) ?? type;
            }

            return (
                new InspectableInfo(type, iids),
                interfaceTableEntries);
        }

        internal class InspectableInfo
        {
            public Guid[] IIDs { get; }
            // WPF's usage of WinRT doesn't create any ccws, so we can return an empty string.
            public string RuntimeClassName => "";

            public InspectableInfo(Type type, Guid[] iids)
            {
                IIDs = iids;
            }

        }
    }
}
