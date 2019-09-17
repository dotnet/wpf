// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Xaml.Schema
{
    static class SafeReflectionInvoker
    {
#if PARTIALTRUST
        private static bool s_UseDynamicAssembly = false;   // true when we use the dynamic assembly approach (i.e. in partial-trust)
        private static object lockObject = new Object();    // for synchronizing the assembly-building step

        // delegate types for the wrapping methods
        private delegate Delegate CreateDelegate1Delegate(Type delegateType, Type targetType, string methodName);
        private delegate Delegate CreateDelegate2Delegate(Type delegateType, object target, string methodName);
        private delegate object CreateInstanceDelegate(Type type, object[] arguments);
        private delegate object InvokeMethodDelegate(MethodInfo method, object instance, object[] args);

        // wrapping delegates
        private static CreateDelegate1Delegate s_CreateDelegate1;
        private static CreateDelegate2Delegate s_CreateDelegate2;
        private static CreateInstanceDelegate s_CreateInstance;
        private static InvokeMethodDelegate s_InvokeMethod;

        private static bool UseDynamicAssembly()
        {
            return s_UseDynamicAssembly;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining |
                                                    System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
        private static void CreateDynamicAssembly()
        {

            // 1. Create the transparent methods, each wrapping a call to a reflection method,
            //    and cache a delegate to each method.
            Type[] parameterTypes;      // signature of the reflection method
            Type[] wrappedParameterTypes; // signature of the wrapping method (when different)
            MethodInfo mi;              // descriptor for the reflection method
            DynamicMethod method;       // wrapping method
            ILGenerator il;             // wrapping method's generator

            // 1a. Delegate.CreateDelegate( Type, Type, String )
            parameterTypes = new Type[] { typeof(Type), typeof(Type), typeof(String) };
            mi = typeof(Delegate).GetMethod("CreateDelegate", parameterTypes);

            method = new DynamicMethod( "CreateDelegate", typeof(Delegate), parameterTypes );
            method.DefineParameter(1, ParameterAttributes.In, "delegateType");
            method.DefineParameter(2, ParameterAttributes.In, "targetType");
            method.DefineParameter(3, ParameterAttributes.In, "methodName");

            il = method.GetILGenerator(5);
            il.Emit(OpCodes.Ldarg_0);               // push delegateType
            il.Emit(OpCodes.Ldarg_1);               // push targetType
            il.Emit(OpCodes.Ldarg_2);               // push methodName
            il.EmitCall(OpCodes.Call, mi, null);    // call Delegate.CreateDelegate
            il.Emit(OpCodes.Ret);                   // return the result

            s_CreateDelegate1 = (CreateDelegate1Delegate)method.CreateDelegate(typeof(CreateDelegate1Delegate));

            // 1b. Delegate.CreateDelegate( Type, Object, String )
            parameterTypes = new Type[] { typeof(Type), typeof(Object), typeof(String) };
            mi = typeof(Delegate).GetMethod("CreateDelegate", parameterTypes);

            method = new DynamicMethod( "CreateDelegate", typeof(Delegate), parameterTypes );
            method.DefineParameter(1, ParameterAttributes.In, "delegateType");
            method.DefineParameter(2, ParameterAttributes.In, "target");
            method.DefineParameter(3, ParameterAttributes.In, "methodName");

            il = method.GetILGenerator(5);
            il.Emit(OpCodes.Ldarg_0);               // push delegateType
            il.Emit(OpCodes.Ldarg_1);               // push target
            il.Emit(OpCodes.Ldarg_2);               // push methodName
            il.EmitCall(OpCodes.Call, mi, null);    // call Delegate.CreateDelegate
            il.Emit(OpCodes.Ret);                   // return the result

            s_CreateDelegate2 = (CreateDelegate2Delegate)method.CreateDelegate(typeof(CreateDelegate2Delegate));

            // 1c. Activator.CreateInstance( Type, Object[] )
            parameterTypes = new Type[] { typeof(Type), typeof(Object[]) };
            mi = typeof(Activator).GetMethod("CreateInstance", parameterTypes);

            method = new DynamicMethod( "CreateInstance", typeof(Object), parameterTypes );
            method.DefineParameter(1, ParameterAttributes.In, "type");
            method.DefineParameter(2, ParameterAttributes.In, "arguments");

            il = method.GetILGenerator(4);
            il.Emit(OpCodes.Ldarg_0);               // push type
            il.Emit(OpCodes.Ldarg_1);               // push arguments
            il.EmitCall(OpCodes.Call, mi, null);    // call Activator.CreateInstance
            il.Emit(OpCodes.Ret);                   // return the result

            s_CreateInstance = (CreateInstanceDelegate)method.CreateDelegate(typeof(CreateInstanceDelegate));

            // 1d. MethodInfo.Invoke(object, args)
            parameterTypes = new Type[] { typeof(Object), typeof(Object[]) };
            wrappedParameterTypes = new Type[] { typeof(MethodInfo), typeof(Object), typeof(Object[]) };
            mi = typeof(MethodInfo).GetMethod("Invoke", parameterTypes);

            method = new DynamicMethod( "InvokeMethod", typeof(Object), wrappedParameterTypes );
            method.DefineParameter(1, ParameterAttributes.In, "method");
            method.DefineParameter(2, ParameterAttributes.In, "instance");
            method.DefineParameter(3, ParameterAttributes.In, "args");

            il = method.GetILGenerator(5);
            il.Emit(OpCodes.Ldarg_0);               // push method
            il.Emit(OpCodes.Ldarg_1);               // push instance
            il.Emit(OpCodes.Ldarg_2);               // push args
            il.EmitCall(OpCodes.Callvirt, mi, null); // call method.Invoke
            il.Emit(OpCodes.Ret);                   // return the result

            s_InvokeMethod = (InvokeMethodDelegate)method.CreateDelegate(typeof(InvokeMethodDelegate));
        }
#endif

        static readonly Assembly SystemXaml = typeof(SafeReflectionInvoker).Assembly;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Retained per servicing policy.")]
        public static bool IsInSystemXaml(Type type)
        {
            if (type.Assembly == SystemXaml)
            {
                return true;
            }
            if (type.IsGenericType)
            {
                foreach (Type typeArg in type.GetGenericArguments())
                {
                    if (IsInSystemXaml(typeArg))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        // ^^^^^----- End of unused members.  -----^^^^^

        internal static Delegate CreateDelegate(Type delegateType, Type targetType, string methodName)
        {
#if PARTIALTRUST
            return UseDynamicAssembly() ? s_CreateDelegate1(delegateType, targetType, methodName)
                                        : CreateDelegateCritical(delegateType, targetType, methodName);
#else
            return CreateDelegateCritical(delegateType, targetType, methodName);
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static Delegate CreateDelegateCritical(Type delegateType, Type targetType, string methodName)
        {
            return Delegate.CreateDelegate(delegateType, targetType, methodName);
        }

        internal static Delegate CreateDelegate(Type delegateType, object target, string methodName)
        {
#if PARTIALTRUST
            return UseDynamicAssembly() ? s_CreateDelegate2(delegateType, target, methodName)
                                        : CreateDelegateCritical(delegateType, target, methodName);
#else
            return CreateDelegateCritical(delegateType, target, methodName);
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static Delegate CreateDelegateCritical(Type delegateType, object target, string methodName)
        {
            return Delegate.CreateDelegate(delegateType, target, methodName);
        }

        internal static object CreateInstance(Type type, object[] arguments)
        {
#if PARTIALTRUST
            return UseDynamicAssembly() ? s_CreateInstance(type, arguments)
                                        : CreateInstanceCritical(type, arguments);
#else
            return CreateInstanceCritical(type, arguments);
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static object CreateInstanceCritical(Type type, object[] arguments)
        {
            return Activator.CreateInstance(type, arguments);
        }

        internal static object InvokeMethod(MethodInfo method, object instance, object[] args)
        {
#if PARTIALTRUST
            return UseDynamicAssembly() ? s_InvokeMethod(method, instance, args)
                                        : InvokeMethodCritical(method, instance, args);
#else
            return InvokeMethodCritical(method, instance, args);
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static object InvokeMethodCritical(MethodInfo method, object instance, object[] args)
        {
            return method.Invoke(instance, args);
        }

        // vvvvv---- Unused members.  Servicing policy is to retain these anyway.  -----vvvvv
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Retained per servicing policy.")]
        internal static bool IsSystemXamlNonPublic(MethodInfo method)
        {
            Type declaringType = method.DeclaringType;
            if (IsInSystemXaml(declaringType) && (!method.IsPublic || !declaringType.IsVisible))
            {
                return true;
            }
            if (method.IsGenericMethod)
            {
                foreach (Type typeArg in method.GetGenericArguments())
                {
                    if (IsInSystemXaml(typeArg) && !typeArg.IsVisible)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        // ^^^^^----- End of unused members.  -----^^^^^
    }
}
