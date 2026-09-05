// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;

namespace System.Windows.Markup.Tests;

public class InternalTypeHelperTests
{
    [Fact]
    public void Ctor_Default()
    {
        // Verify doesn't throw.
        new SubInternalTypeHelper();
    }

    private class SubInternalTypeHelper : InternalTypeHelper
    {
        protected internal override void AddEventHandler(EventInfo eventInfo, object target, Delegate handler)
            => throw new NotImplementedException();

        protected internal override Delegate CreateDelegate(Type delegateType, object target, string handler)
            => throw new NotImplementedException();

        protected internal override object CreateInstance(Type type, CultureInfo culture)
            => throw new NotImplementedException();

        protected internal override object GetPropertyValue(PropertyInfo propertyInfo, object target, CultureInfo culture)
            => throw new NotImplementedException();

        protected internal override void SetPropertyValue(PropertyInfo propertyInfo, object target, object value, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
