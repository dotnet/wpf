// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
// Description:  
//      InternalDispatchObject facilitates implementing a COM dispinterface using a non-public 
//      corresponding managed interface definition (of type ComInterfaceType.InterfaceIsIDispatch). 
//      (For example, we use this to implement DWebBrowserEvents2 without making it a public interface
//      type in the framework.)
//
//      If a public managed interface definition is available, CLR's built-in implementation of
//      IDispatch suffices. It maps IDispatch calls to Reflection calls. But reflecting on non-public
//      members causes a demand for ReflectionPermission/MemberAccess against the caller of the 
//      Reflection API. In a partial-trust AppDomain, and oddly only with some particular transitions 
//      between managed-native-managed stack frames, this demand fails. Given that this occurs with
//      substantially equivalent callstacks in terms of CAS (Asserts) and managed<->native transitions,
//      there is probably an obscure CLR bug somewhere at the intersection of its IDispatch 
//      implementation, Reflection and CAS. And there seems to be no explicit suggestion in 
//      documentation that this scenario is supported in the first place.
//
//      By implementing IReflect, any object can provide its own reflection implementation. And it 
//      turns out CLR's IDispatch "front end" will happily use such an implementation. We don't need
//      to explicitly assert any ReflectionPermission in this arrangement as long as we are targeting
//      an internally visible interface type. (The built-in reflection system performs the equivalent
//      of a LinkDemand against the caller of the reflection API.) This, however, creates a potentially
//      dangerous security situation.
//
//      CAUTION!!! Do not expose any InternalDispatchObject-derived object instance to untrusted code,
//          unless the implementation of the dispinterface is entirely safe. Such objects should be 
//          passed only to native code, to be invoked via IDispatch.
//
//          Because IReflect is a public interface and has no (link)demands on it and because 
//          InternalDispatchObject's internal invocation of relection satisfies the reflection system's
//          ReflectionPermission demand, the implementaiton of the given dispinterface effectively
//          becomes publicly accessible. (Putting a LinkDemand on InternalDispatchObject.InvokeMember()
//          would have no effect because it can be called via IReflect, and LinkDemands are evaluated
//          based on static type info. And putting a full Demand would defeat the whole purpose of
//          InternalDispatchObject.)
//


using System;
using System.Security;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;

namespace MS.Internal.Interop
{
internal abstract class InternalDispatchObject<IDispInterface> : IReflect
{
    /// <summary>
    /// DISPID->MethodInfo map used by InvokeMember()
    /// </summary>
    private Dictionary<int, MethodInfo> _dispId2MethodMap;

    protected InternalDispatchObject()
    {
        // Populate _dispId2MethodMap with the MethodInfos for the interface, keyed by DISPID.
        // There is no support for properties (yet).
        MethodInfo[] methods = typeof(IDispInterface).GetMethods();
        _dispId2MethodMap = new Dictionary<int, MethodInfo>(methods.Length);
        foreach (MethodInfo method in methods)
        {
            int dispid = ((DispIdAttribute[])method.GetCustomAttributes(typeof(DispIdAttribute), false))[0].Value;
            _dispId2MethodMap[dispid] = method;
        }
    }

    FieldInfo IReflect.GetField(string name, BindingFlags bindingAttr)
    {
        throw new NotImplementedException();
    }
    FieldInfo[] IReflect.GetFields(BindingFlags bindingAttr)
    {
        return null;
    }
    MemberInfo[] IReflect.GetMember(string name, BindingFlags bindingAttr)
    {
        throw new NotImplementedException();
    }
    MemberInfo[] IReflect.GetMembers(BindingFlags bindingAttr)
    {
        throw new NotImplementedException();
    }
    MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr)
    {
        throw new NotImplementedException();
    }
    MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
    {
        throw new NotImplementedException();
    }
    MethodInfo[] IReflect.GetMethods(BindingFlags bindingAttr)
    {
        // It doesn't help to return typeof(IDispInterface).GetMethods(), because the CLR's IDispatch layer
        // ignores non-visible MethodInfos (DispatchInfo::SynchWithManagedView(), in clr\src\VM\DispatchInfo.cpp).
        // For all "unknown" DISPIDs, IReflect.InvokeMember() is passed a method name like "[DISPID=<id>]", 
        // which we parse.
        return null;
    }
    PropertyInfo[] IReflect.GetProperties(BindingFlags bindingAttr)
    {
        return null;
    }
    PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
    {
        throw new NotImplementedException();
    }
    PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr)
    {
        throw new NotImplementedException();
    }

    object IReflect.InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters)
    {
        // We never expect to get a real method name here--see the explanation in GetMethods().
        if (name.StartsWith("[DISPID=", StringComparison.OrdinalIgnoreCase))
        {
            int dispid = int.Parse(name.Substring(8, name.Length-9), CultureInfo.InvariantCulture);
            MethodInfo method;
            if (_dispId2MethodMap.TryGetValue(dispid, out method))
            {
                return method.Invoke(this, invokeAttr, binder, args, culture);
            }
        }
        // This exception can be thrown if IDispInterface doesn't declare a method with a particular DISPID, 
        // but the real COM interface has it and the event source is trying to invoke it. For a call coming via
        // the native IDispatch, such an error is usually ignorable.
        throw new MissingMethodException(GetType().Name, name);
    }
    
    Type IReflect.UnderlyingSystemType
    {
        get { return typeof(IDispInterface); }
    }
};
}//namespace
