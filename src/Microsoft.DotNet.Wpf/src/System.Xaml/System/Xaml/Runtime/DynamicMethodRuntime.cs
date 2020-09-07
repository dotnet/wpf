// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Xaml;
using System.Xaml.MS.Impl;
using System.Xaml.Permissions;
using System.Xaml.Schema;

namespace MS.Internal.Xaml.Runtime
{

    // Perf notes:
    // - Consider caching some bounded number of ctor/factory binding lookups, similar to what
    //   Activator.CreateInstance does.

    // This class enables us to access non-public members exactly as if we were compiled into the
    // LocalAssembly or LocalType. It does this by using the overloads of DynamicMethod.ctor that
    // take ownerType/ownerModule. We assert FullTrust to emit the dynamic methods; after that,
    // no special permissions are needed to invoke the methods.

    internal class DynamicMethodRuntime : ClrObjectRuntime
    {
        const BindingFlags BF_AllInstanceMembers = 
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags BF_AllStaticMembers = 
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        static MethodInfo s_GetTypeFromHandleMethod;
        static MethodInfo s_InvokeMemberMethod;

        delegate void PropertySetDelegate(object target, object value);
        delegate object PropertyGetDelegate(object target);
        delegate object FactoryDelegate(object[] args);
        delegate Delegate DelegateCreator(Type delegateType, object target, string methodName);

        Assembly _localAssembly;

        Type _localType;
        
        XamlSchemaContext _schemaContext;

        // We cache based on MemberInfo instead of XamlMember for two reasons:
        // 1. Equivalent XamlMembers can actually have different MemberInfos. Caching based on
        //    XamlMember equivalence would introduce functional differences between ClrObjectRuntime
        //    and DynamicMethodRuntime.
        // 2. In the typical case where the MemberInfo is a Runtime Member, it is never GCed, and so
        //    we don't have to worry that we're keeping it rooted. If this ever becomes a concern,
        //    we can switch to a ConditionalWeakTable here.

        Dictionary<MethodInfo, PropertyGetDelegate> _propertyGetDelegates;
        Dictionary<MethodInfo, PropertySetDelegate> _propertySetDelegates;
        Dictionary<MethodBase, FactoryDelegate> _factoryDelegates;
        Dictionary<Type, object> _converterInstances;
        Dictionary<Type, DelegateCreator> _delegateCreators;
        DelegateCreator _delegateCreatorWithoutHelper;

        private Dictionary<MethodInfo, PropertyGetDelegate> PropertyGetDelegates
        {
            get
            {
                if (_propertyGetDelegates == null)
                {
                    _propertyGetDelegates = new Dictionary<MethodInfo, PropertyGetDelegate>();
                }
                return _propertyGetDelegates;
            }
        }

        private Dictionary<MethodInfo, PropertySetDelegate> PropertySetDelegates
        {
            get
            {
                if (_propertySetDelegates == null)
                {
                    _propertySetDelegates = new Dictionary<MethodInfo, PropertySetDelegate>();
                }
                return _propertySetDelegates;
            }
        }

        private Dictionary<MethodBase, FactoryDelegate> FactoryDelegates
        {
            get
            {
                if (_factoryDelegates == null)
                {
                    _factoryDelegates = new Dictionary<MethodBase, FactoryDelegate>();
                }
                return _factoryDelegates;
            }
        }

        private Dictionary<Type, object> ConverterInstances
        {
            get
            {
                if (_converterInstances == null)
                {
                    _converterInstances = new Dictionary<Type, object>();
                }
                return _converterInstances;
            }
        }

        private Dictionary<Type, DelegateCreator> DelegateCreators
        {
            get
            {
                if (_delegateCreators == null)
                {
                    _delegateCreators = new Dictionary<Type, DelegateCreator>();
                }
                return _delegateCreators;
            }
        }

        internal DynamicMethodRuntime(XamlRuntimeSettings settings, XamlSchemaContext schemaContext,
            XamlAccessLevel accessLevel)
            : base(settings, true /*isWriter*/)
        {
            Debug.Assert(schemaContext != null);
            Debug.Assert(accessLevel != null);
            _schemaContext = schemaContext;
            _localAssembly = Assembly.Load(accessLevel.AssemblyAccessToAssemblyName);
            if (accessLevel.PrivateAccessToTypeName != null)
            {
                _localType = _localAssembly.GetType(accessLevel.PrivateAccessToTypeName, true /*throwOnError*/);
            }
        }

        public override TConverterBase GetConverterInstance<TConverterBase>(XamlValueConverter<TConverterBase> ts)
        {
            Type clrType = ts.ConverterType;
            if (clrType == null)
            {
                return null;
            }
            object result;
            if (!ConverterInstances.TryGetValue(clrType, out result))
            {
                result = CreateInstanceWithCtor(clrType, null);
                ConverterInstances.Add(clrType, result);
            }
            return (TConverterBase)result;
        }

        //CreateFromValue is expected to convert the provided value via any applicable converter (on property or type) or provide the original value if there is no converter
        public override object CreateFromValue(
                                    ServiceProviderContext serviceContext,
                                    XamlValueConverter<TypeConverter> ts, object value,
                                    XamlMember property)
        {
            if (ts == BuiltInValueConverter.Event)
            {
                string valueString = value as string;
                if (valueString != null)
                {
                    object rootObject;
                    Type delegateType;
                    EventConverter.GetRootObjectAndDelegateType(serviceContext, out rootObject, out delegateType);

                    return CreateDelegate(delegateType, rootObject, valueString);
                }
            }
            return base.CreateFromValue(serviceContext, ts, value, property);
        }

        protected override Delegate CreateDelegate(Type delegateType, object target, string methodName)
        {
            DelegateCreator creator;
            Type targetType = target.GetType();
            if (!DelegateCreators.TryGetValue(targetType, out creator))
            {
                creator = CreateDelegateCreator(targetType);
                DelegateCreators.Add(targetType, creator);
            }
            return creator.Invoke(delegateType, target, methodName);
        }

        protected override object CreateInstanceWithCtor(XamlType xamlType, object[] args)
        {
            return CreateInstanceWithCtor(xamlType.UnderlyingType, args);
        }

        private object CreateInstanceWithCtor(Type type, object[] args)
        {
            ConstructorInfo ctor = null;
            if (args == null || args.Length == 0)
            {
                ctor = type.GetConstructor(BF_AllInstanceMembers, null, Type.EmptyTypes, null);
            }
            if (ctor == null)
            {
                // We go down this path even if there are no args, because we might match a params array
                ConstructorInfo[] ctors = type.GetConstructors(BF_AllInstanceMembers);
                // This method throws if it can't find a match, so ctor will never be null
                ctor = (ConstructorInfo)BindToMethod(BF_AllInstanceMembers, ctors, args);
            }
            FactoryDelegate factoryDelegate;
            if (!FactoryDelegates.TryGetValue(ctor, out factoryDelegate))
            {
                factoryDelegate = CreateFactoryDelegate(ctor);
                FactoryDelegates.Add(ctor, factoryDelegate);
            }
            return factoryDelegate.Invoke(args);
        }

        protected override object InvokeFactoryMethod(Type type, string methodName, object[] args)
        {
            MethodInfo factory = GetFactoryMethod(type, methodName, args, BF_AllStaticMembers);
            FactoryDelegate factoryDelegate;
            if (!FactoryDelegates.TryGetValue(factory, out factoryDelegate))
            {
                factoryDelegate = CreateFactoryDelegate(factory);
                FactoryDelegates.Add(factory, factoryDelegate);
            }
            return factoryDelegate.Invoke(args);
        }

        protected override object GetValue(XamlMember member, object obj)
        {
            MethodInfo getter = member.Invoker.UnderlyingGetter;
            if (getter == null)
            {
                throw new NotSupportedException(SR.Get(SRID.CantGetWriteonlyProperty, member));
            }

            PropertyGetDelegate getterDelegate;
            if (!PropertyGetDelegates.TryGetValue(getter, out getterDelegate))
            {
                getterDelegate = CreateGetDelegate(getter);
                PropertyGetDelegates.Add(getter, getterDelegate);
            }
            return getterDelegate.Invoke(obj);
        }

        protected override void SetValue(XamlMember member, object obj, object value)
        {
            MethodInfo setter = member.Invoker.UnderlyingSetter;
            if (setter == null)
            {
                throw new NotSupportedException(SR.Get(SRID.CantSetReadonlyProperty, member));
            }

            PropertySetDelegate setterDelegate;
            if (!PropertySetDelegates.TryGetValue(setter, out setterDelegate))
            {
                setterDelegate = CreateSetDelegate(setter);
                PropertySetDelegates.Add(setter, setterDelegate);
            }
            setterDelegate.Invoke(obj, value);
        }

        private DelegateCreator CreateDelegateCreator(Type targetType)
        {
            const BindingFlags helperFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            // We are relying on WPF-generated delegate helper for now
            // Expected signature: internal Delegate _CreateDelegate(Type delegateType, string handler)
            // If it's not present, we just return a wrapper around Delegate.CreateDelegate, which
            // will fail if we the _localAssembly doesn't have RestrictedMemberAccess permission
            MethodInfo helper = targetType.GetMethod(KnownStrings.CreateDelegateHelper,
                helperFlags, null, new Type[] { typeof(Type), typeof(string) }, null);
            if (helper == null)
            {
                if (_delegateCreatorWithoutHelper == null)
                {
                    _delegateCreatorWithoutHelper = CreateDelegateCreatorWithoutHelper();
                }
                return _delegateCreatorWithoutHelper;
            }

            DynamicMethod dynamicMethod = CreateDynamicMethod(targetType.Name + "DelegateHelper",
                typeof(Delegate), typeof(Type), typeof(object), typeof(string));
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            // We have to emit an indirect call through reflection to avoid the helper getting
            // inlined into the dynamic method, and potentially executing without access to private
            // members on the target type.
            Emit_LateBoundInvoke(ilGenerator, targetType, KnownStrings.CreateDelegateHelper, 
                helperFlags | BindingFlags.InvokeMethod, 1, 0, 2);
            Emit_CastTo(ilGenerator, typeof(Delegate));
            ilGenerator.Emit(OpCodes.Ret);

            return (DelegateCreator)dynamicMethod.CreateDelegate(typeof(DelegateCreator));
        }

        private DelegateCreator CreateDelegateCreatorWithoutHelper()
        {
            DynamicMethod dynamicMethod = CreateDynamicMethod("CreateDelegateHelper",
                typeof(Delegate), typeof(Type), typeof(object), typeof(string));
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Ldarg_2);
            MethodInfo method = typeof(Delegate).GetMethod(KnownStrings.CreateDelegate,
                BindingFlags.Static | BindingFlags.Public, null, 
                new Type[] { typeof(Type), typeof(object), typeof(string) }, null);
            ilGenerator.Emit(OpCodes.Call, method);
            ilGenerator.Emit(OpCodes.Ret);

            return (DelegateCreator)dynamicMethod.CreateDelegate(typeof(DelegateCreator));
        }

        private FactoryDelegate CreateFactoryDelegate(ConstructorInfo ctor)
        {
            DynamicMethod dynamicMethod = CreateDynamicMethod(ctor.DeclaringType.Name + "Ctor", 
                typeof(object), typeof(object[]));
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            LocalBuilder[] locals = LoadArguments(ilGenerator, ctor);
            ilGenerator.Emit(OpCodes.Newobj, ctor);
            UnloadArguments(ilGenerator, locals);
            ilGenerator.Emit(OpCodes.Ret);
            
            return (FactoryDelegate)dynamicMethod.CreateDelegate(typeof(FactoryDelegate));
        }

        private FactoryDelegate CreateFactoryDelegate(MethodInfo factory)
        {
            DynamicMethod dynamicMethod = CreateDynamicMethod(factory.Name + "Factory", 
                typeof(object), typeof(object[]));
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
            
            LocalBuilder[] locals = LoadArguments(ilGenerator, factory);
            ilGenerator.Emit(OpCodes.Call, factory);
            Emit_BoxIfValueType(ilGenerator, factory.ReturnType);
            UnloadArguments(ilGenerator, locals);
            ilGenerator.Emit(OpCodes.Ret);

            return (FactoryDelegate)dynamicMethod.CreateDelegate(typeof(FactoryDelegate));
        }

        // load arguments from object[] args onto the evaluation stack
        private LocalBuilder[] LoadArguments(ILGenerator ilGenerator, MethodBase method)
        {
            ParameterInfo[] args = method.GetParameters();
            if (args.Length == 0)
            {
                return null;
            }

            // We need to handle vararg matches (and optional parameters?)
            
            ParameterInfo[] parameters = method.GetParameters();
            Type[] paramTypes = new Type[parameters.Length];
            LocalBuilder[] locals = new LocalBuilder[paramTypes.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                Type paramType = parameters[i].ParameterType;

                ilGenerator.Emit(OpCodes.Ldarg_0);
                Emit_ConstInt(ilGenerator, i);
                ilGenerator.Emit(OpCodes.Ldelem_Ref);

                if (paramType.IsByRef)
                {
                    Type elementType = paramType.GetElementType();
                    Emit_CastTo(ilGenerator, elementType);

                    // load the arg into a variable so we can pass it by ref
                    locals[i] = ilGenerator.DeclareLocal(elementType);
                    ilGenerator.Emit(OpCodes.Stloc, locals[i]);
                    ilGenerator.Emit(OpCodes.Ldloca_S, locals[i]);
                }
                else
                {
                    Emit_CastTo(ilGenerator, paramType);
                }
            }
            return locals;
        }

        // update object[] args from any variables passed by ref in LoadArguments
        private void UnloadArguments(ILGenerator ilGenerator, LocalBuilder[] locals)
        {
            if (locals == null)
            {
                return;
            }
            for (int i = 0; i < locals.Length; i++)
            {
                if (locals[i] != null)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    Emit_ConstInt(ilGenerator, i);
                    ilGenerator.Emit(OpCodes.Ldloc, locals[i]);
                    Emit_BoxIfValueType(ilGenerator, locals[i].LocalType);
                    ilGenerator.Emit(OpCodes.Stelem_Ref);
                }
            }
        }

        // The methods below don't properly handle non-Runtime reflection classes
        
        // Note that CreateGetDelegate fails verification for value types (and probably shouldn't)
        private PropertyGetDelegate CreateGetDelegate(MethodInfo getter)
        {
            DynamicMethod dynamicMethod = CreateDynamicMethod(getter.Name + "Getter", 
                typeof(object), typeof(object));
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            Type targetType = getter.IsStatic ? getter.GetParameters()[0].ParameterType : GetTargetType(getter);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            Emit_CastTo(ilGenerator, targetType);
            
            Emit_Call(ilGenerator, getter);
            Emit_BoxIfValueType(ilGenerator, getter.ReturnType);
            ilGenerator.Emit(OpCodes.Ret);

            return (PropertyGetDelegate)dynamicMethod.CreateDelegate(typeof(PropertyGetDelegate));
        }

        // Note that CreateSetDelegate fails verification for value types (and probably shouldn't)
        private PropertySetDelegate CreateSetDelegate(MethodInfo setter)
        {
            DynamicMethod dynamicMethod = CreateDynamicMethod(setter.Name + "Setter", 
                typeof(void), typeof(object), typeof(object));
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            ParameterInfo[] parameters = setter.GetParameters();
            Type targetType = setter.IsStatic ? parameters[0].ParameterType : GetTargetType(setter);
            Type valueType = setter.IsStatic ? parameters[1].ParameterType : parameters[0].ParameterType;

            ilGenerator.Emit(OpCodes.Ldarg_0);
            Emit_CastTo(ilGenerator, targetType);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            Emit_CastTo(ilGenerator, valueType);

            Emit_Call(ilGenerator, setter);
            ilGenerator.Emit(OpCodes.Ret);

            return (PropertySetDelegate)dynamicMethod.CreateDelegate(typeof(PropertySetDelegate));
        }

        private DynamicMethod CreateDynamicMethod(string name, Type returnType, params Type[] argTypes)
        {
            if (_localType != null)
            {
                return new DynamicMethod(name, returnType, argTypes, _localType);
            }
            else
            {
                return new DynamicMethod(name, returnType, argTypes, _localAssembly.ManifestModule);
            }
        }

        private Type GetTargetType(MethodInfo instanceMethod)
        {
            Type declaringType = instanceMethod.DeclaringType;
            // Derived classes are not allowed to access protected members on instances of their base classes.
            // So if it it's an inherited protected member, we need to cast to the local type.
            if (_localType != null && _localType != declaringType && declaringType.IsAssignableFrom(_localType))
            {
                if (instanceMethod.IsFamily || instanceMethod.IsFamilyAndAssembly)
                {
                    return _localType;
                }
                if (instanceMethod.IsFamilyOrAssembly)
                {
                    // This is a non-security-critical check; we're attempting to do the right thing here,
                    // but the real security check happens in the CLR when we execute the dynamic method.
                    bool areInternalsVisible = _schemaContext.AreInternalsVisibleTo(
                        declaringType.Assembly, _localType.Assembly);
                    if (!areInternalsVisible)
                    {
                        return _localType;
                    }
                }
            }
            // Otherwise just cast to the declaring type of the member
            return declaringType;
        }

        private static void Emit_Call(ILGenerator ilGenerator, MethodInfo method)
        {
            OpCode callType = (method.IsStatic || method.DeclaringType.IsValueType) ? OpCodes.Call : OpCodes.Callvirt;
            ilGenerator.Emit(callType, method);
        }

        private static void Emit_CastTo(ILGenerator ilGenerator, Type toType)
        {
            if (toType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, toType);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Castclass, toType);
            }
        }

        private static void Emit_BoxIfValueType(ILGenerator ilGenerator, Type type)
        {
            if (type.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, type);
            }
        }

        private static void Emit_ConstInt(ILGenerator ilGenerator, int value)
        {
            switch (value)
            {
                case -1:
                    ilGenerator.Emit(OpCodes.Ldc_I4_M1);
                    return;
                case 0:
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    ilGenerator.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    ilGenerator.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    ilGenerator.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    ilGenerator.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    ilGenerator.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    ilGenerator.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    ilGenerator.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    ilGenerator.Emit(OpCodes.Ldc_I4_8);
                    return;
                default:
                    ilGenerator.Emit(OpCodes.Ldc_I4, value);
                    return;
            }
        }

        private void Emit_LateBoundInvoke(ILGenerator ilGenerator, Type targetType, string methodName,
            BindingFlags bindingFlags, short targetArgNum, params short[] paramArgNums)
        {
            //Emits: typeof(targetType).InvokeMember(
            //           methodName, bindingFlags, null, ldarg_targetArgNum, 
            //           new object[] { ldarg_paramArgNums });

            Emit_TypeOf(ilGenerator, targetType);
            ilGenerator.Emit(OpCodes.Ldstr, methodName);
            Emit_ConstInt(ilGenerator, (int)bindingFlags);
            ilGenerator.Emit(OpCodes.Ldnull);
            ilGenerator.Emit(OpCodes.Ldarg, targetArgNum);

            LocalBuilder args = ilGenerator.DeclareLocal(typeof(object[]));
            Emit_ConstInt(ilGenerator, paramArgNums.Length);
            ilGenerator.Emit(OpCodes.Newarr, typeof(object));
            ilGenerator.Emit(OpCodes.Stloc, args); // args = new object[]

            for (int i = 0; i < paramArgNums.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Ldloc, args);
                Emit_ConstInt(ilGenerator, i);
                ilGenerator.Emit(OpCodes.Ldarg, paramArgNums[i]);
                // Assuming all arguments are reference types
                ilGenerator.Emit(OpCodes.Stelem_Ref); // args[i] = ldarg_paramArgNums[i]
            }
            ilGenerator.Emit(OpCodes.Ldloc, args);

            if (s_InvokeMemberMethod == null)
            {
                s_InvokeMemberMethod = typeof(Type).GetMethod(KnownStrings.InvokeMember,
                    new Type[] { typeof(string), typeof(BindingFlags), typeof(Binder), typeof(object), typeof(object[]) });
            }
            ilGenerator.Emit(OpCodes.Callvirt, s_InvokeMemberMethod);
        }

        private void Emit_TypeOf(ILGenerator ilGenerator, Type type)
        {
            ilGenerator.Emit(OpCodes.Ldtoken, type);
            if (s_GetTypeFromHandleMethod == null)
            {
                s_GetTypeFromHandleMethod = typeof(Type).GetMethod(
                    KnownStrings.GetTypeFromHandle, BindingFlags.Public | BindingFlags.Static,
                    null, new Type[] { typeof(RuntimeTypeHandle) }, null);
            }
            ilGenerator.Emit(OpCodes.Call, s_GetTypeFromHandleMethod);
        }
    }
}
