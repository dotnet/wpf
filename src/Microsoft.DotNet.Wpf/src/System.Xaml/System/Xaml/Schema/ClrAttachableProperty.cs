﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Markup;

#if SILVERLIGHTXAML
using MS.Internal.Xaml.MS.Impl;
#else
using System.Xaml.MS.Impl;
#endif

#if SILVERLIGHTXAML
namespace MS.Internal.Xaml.Schema
#else
namespace System.Xaml.Schema
#endif
{
    [DebuggerDisplay("{Name}")]
    class ClrAttachedProperty : ClrProperty
    {
        public readonly MethodInfo ClrBindingGetterMethodInfo;
        public readonly MethodInfo ClrBindingSetterMethodInfo;
        private Type _systemTypeOfProperty = null;
        internal ClrAttachedProperty(string name, MethodInfo getter, MethodInfo setter, XamlType declaringType)
            : base(name, declaringType)
        {
            Debug.Assert(getter is not null || setter is not null);

            if (getter is null && setter is null)
            {
                throw new XamlSchemaException(SR.Format(SR.SetOnlyProperty, declaringType.Name, name));
            }

            _isPublic = (getter is not null) ? getter.IsPublic : setter.IsPublic;
            _isReadOnly = (setter is null);
            _isStatic = false;
            _isAttachable = true;
            _isEvent = false;

            ClrBindingGetterMethodInfo = getter;
            ClrBindingSetterMethodInfo = setter;
        }

        protected override Type LookupSystemTypeOfProperty()
        {
            if (_systemTypeOfProperty is null)
            {
                _systemTypeOfProperty = PrivateLookupSystemTypeOfProperty;
            }
            return _systemTypeOfProperty;
        }

        private Type PrivateLookupSystemTypeOfProperty
        {
            get 
            {
                if (ClrBindingGetterMethodInfo is not null)
                {
                    return ClrBindingGetterMethodInfo.ReturnType;
                }
                else
                {
                    ParameterInfo[] pis = ClrBindingSetterMethodInfo.GetParameters();
                    if (pis.Length > 1)
                    {
                        return ClrBindingSetterMethodInfo.GetParameters()[1].ParameterType;
                    }
                    else
                    {
                        throw new XamlSchemaException(SR.Format(SR.IncorrectSetterParamNum, ClrBindingSetterMethodInfo.Name, 2));
                    }
                }
            }
        }

        protected override object[] LookupCustomAttributes(Type attrType)
        {
            if (ClrBindingGetterMethodInfo is not null)
            {
                return ClrBindingGetterMethodInfo.GetCustomAttributes(attrType, true);
            }
            return ClrBindingSetterMethodInfo.GetCustomAttributes(attrType, true);
        }

        protected override XamlType LookupTargetType()
        {
            MethodInfo mi = (ClrBindingGetterMethodInfo is not null)
                ? ClrBindingGetterMethodInfo 
                : ClrBindingSetterMethodInfo;
            ParameterInfo[] parameters = mi.GetParameters();
            Type paramType = parameters[0].ParameterType;
            XamlType targetType = SchemaContext.GetXamlType(paramType);
            return targetType;
        }
    }
}
