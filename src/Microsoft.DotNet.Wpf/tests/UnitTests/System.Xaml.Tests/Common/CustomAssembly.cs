// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;

namespace System.Xaml.Tests.Common;

public class CustomAssembly : Assembly
{
    protected Assembly DelegatingAssembly { get; }

    public CustomAssembly(Assembly delegatingAssembly)
    {
        DelegatingAssembly = delegatingAssembly;
    }

    public Optional<string?> FullNameResult { get; set; }
    public override string? FullName => FullNameResult.Or(DelegatingAssembly.FullName);

    public Optional<bool> IsDynamicResult { get; set; }
    public override bool IsDynamic => IsDynamicResult.Or(DelegatingAssembly.IsDynamic);

    public Optional<bool> ReflectionOnlyResult { get; set; }
    public override bool ReflectionOnly => ReflectionOnlyResult.Or(DelegatingAssembly.ReflectionOnly);

    public Optional<IList<CustomAttributeData>> GetCustomAttributesDataResult { get; set; }
    public override IList<CustomAttributeData> GetCustomAttributesData()
    {
        return GetCustomAttributesDataResult.Or(DelegatingAssembly.GetCustomAttributesData);
    }

    public Optional<Dictionary<Type, object?[]?>> GetCustomAttributesMap { get; set; }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        if (GetCustomAttributesMap.HasValue)
        {
            object?[]? result;
            if (GetCustomAttributesMap.Value.TryGetValue(attributeType, out result))
            {
                return result!;
            }
        }

        return DelegatingAssembly.GetCustomAttributes(attributeType, inherit);
    }

    public override bool Equals(object? o) => DelegatingAssembly.Equals(o);

    public override int GetHashCode() => DelegatingAssembly.GetHashCode();
}
