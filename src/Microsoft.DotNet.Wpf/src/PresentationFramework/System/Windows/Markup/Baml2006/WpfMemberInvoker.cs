// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xaml.Schema;
using System.Reflection;

namespace System.Windows.Baml2006
{
    internal class WpfMemberInvoker : XamlMemberInvoker
    {
        WpfXamlMember _member;
        bool _hasShouldSerializeMethodBeenLookedup = false;
        MethodInfo _shouldSerializeMethod = null;

        public WpfMemberInvoker(WpfXamlMember member) : base(member)
        {
            _member = member;
        }

        public override void SetValue(object instance, object value)
        {
             DependencyObject dObject = instance as DependencyObject;
             if (dObject is not null)
             {
                 if (_member.DependencyProperty is not null)
                 {
                     dObject.SetValue(_member.DependencyProperty, value);
                     return;
                 }
                 else if (_member.RoutedEvent is not null)
                 {
                     Delegate handler = value as Delegate;
                     if (handler is not null)
                     {
                         UIElement.AddHandler(dObject, _member.RoutedEvent, handler);
                         return;
                     }
                 }
             }

             base.SetValue(instance, value);
        }

        public override object GetValue(object instance)
        {
            DependencyObject dObject = instance as DependencyObject;
            if (dObject is not null && _member.DependencyProperty is not null)
            {
                object result = dObject.GetValue(_member.DependencyProperty);
                if (result is not null)
                {
                    return result;
                }
                // Getter fallback: see comment on WpfXamlMember.AsContentProperty
                if (!_member.ApplyGetterFallback || _member.UnderlyingMember is null)
                {
                    return result;
                }
            }

            return base.GetValue(instance);
        }

        public override ShouldSerializeResult ShouldSerializeValue(object instance)
        {
            // Look up the ShouldSerializeMethod
            if (!_hasShouldSerializeMethodBeenLookedup)
            {
                Type declaringType = _member.UnderlyingMember.DeclaringType;
                string methodName = $"ShouldSerialize{_member.Name}";
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
                Type[] args = new Type[] { typeof(DependencyObject) };
                if (_member.IsAttachable)
                {
                    _shouldSerializeMethod = declaringType.GetMethod(methodName, flags, null, args, null);
                }
                else
                {
                    flags |= BindingFlags.Instance;
                    _shouldSerializeMethod = declaringType.GetMethod(methodName, flags, null, args, null);
                }

                _hasShouldSerializeMethodBeenLookedup = true;
            }        

            // Invoke the method if we found one
            if (_shouldSerializeMethod is not null)
            {
                bool result;
                var args = new object[] { instance as DependencyObject };
                if (_member.IsAttachable)
                {
                    result = (bool)_shouldSerializeMethod.Invoke(null, args);
                }
                else
                {
                    result = (bool)_shouldSerializeMethod.Invoke(instance, args);
                }

                return result ? ShouldSerializeResult.True : ShouldSerializeResult.False;
            }

            DependencyObject dObject = instance as DependencyObject;
            if (dObject is not null && _member.DependencyProperty is not null)
            {
                // Call DO's ShouldSerializeProperty to see if the property is set.
                // If the property is unset, the property should not be serialized
                bool isPropertySet = dObject.ShouldSerializeProperty(_member.DependencyProperty);

                if (!isPropertySet)
                {
                    return ShouldSerializeResult.False;
                }               
            }

            return base.ShouldSerializeValue(instance);
        }
    }
}
