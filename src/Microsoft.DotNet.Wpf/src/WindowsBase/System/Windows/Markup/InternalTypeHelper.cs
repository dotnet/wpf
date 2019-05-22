// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   Defines a class that Provides methods used internally by the BamlReader to access
//   allowed internal types, properties and events in Partial Trust. The markup compiler
//   will generate a subclass of this class that provides an appropriate implementation
//   in the user's code context.
//
//

using System;
using System.Windows;
using System.Reflection;
using System.ComponentModel;
using System.Globalization;

namespace System.Windows.Markup
{
    /// <summary>
    /// Class that provides methods used internally by the BamlReader to access allowed
    /// internal types, properties and events in Partial Trust. The markup compiler
    /// will generate a subclass of this class that provides an appropriate implementation
    /// in the user's code context. 
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class InternalTypeHelper
    {
        /// <summary>
        /// Default InternalTypeHelper constructor
        /// </summary>
        protected InternalTypeHelper()
        {
        }

        /// <summary>
        /// Called by the BamlReader to create an internal Type.
        /// </summary>
        protected internal abstract object CreateInstance(Type type, CultureInfo culture);

        /// <summary>
        /// Called by the BamlReader to set an internal property value on a target object.
        /// </summary>
        protected internal abstract object GetPropertyValue(PropertyInfo propertyInfo, object target, CultureInfo culture);

        /// <summary>
        /// Called by the BamlReader to get an internal property value on a target object.
        /// </summary>
        protected internal abstract void SetPropertyValue(PropertyInfo propertyInfo, object target, object value, CultureInfo culture);

        /// <summary>
        /// Called by the BamlReader to create an event delegate on a non-public handler method.
        /// </summary>
        protected internal abstract Delegate CreateDelegate(Type delegateType, object target, string handler);

        /// <summary>
        /// Called by the BamlReader to attach an event handler delegate to an internal event.
        /// </summary>
        protected internal abstract void AddEventHandler(EventInfo eventInfo, object target, Delegate handler);
    }
}
