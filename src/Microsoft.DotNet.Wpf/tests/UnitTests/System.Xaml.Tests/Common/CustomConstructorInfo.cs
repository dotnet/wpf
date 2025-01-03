// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;

namespace System.Xaml.Tests.Common;

public class CustomConstructorInfo : ConstructorInfo
{
    protected ConstructorInfo DelegatingConstructor { get; }

    public CustomConstructorInfo(ConstructorInfo delegatingConstructor)
    {
        DelegatingConstructor = delegatingConstructor;
    }

    public Optional<MethodAttributes> AttributesResult { get; set; }
    public override MethodAttributes Attributes => AttributesResult.Or(DelegatingConstructor.Attributes);

    public Optional<Type> DeclaringTypeResult { get; set; }
    public override Type DeclaringType => DeclaringTypeResult.Or(DelegatingConstructor.DeclaringType!);

    public Optional<MethodImplAttributes> GetMethodImplementationFlagsResult { get; set; }
    public override MethodImplAttributes GetMethodImplementationFlags() => GetMethodImplementationFlagsResult.Or(DelegatingConstructor.GetMethodImplementationFlags);

    public Optional<ParameterInfo[]> GetParametersResult { get; set; }
    public override ParameterInfo[] GetParameters() => GetParametersResult.Or(DelegatingConstructor.GetParameters);

    public Optional<bool> IsSecurityCriticalResult { get; set; }
    public override bool IsSecurityCritical => IsSecurityCriticalResult.Or(DelegatingConstructor.IsSecurityCritical);

    public Optional<bool> IsSecuritySafeCriticalResult { get; set; }
    public override bool IsSecuritySafeCritical => IsSecuritySafeCriticalResult.Or(DelegatingConstructor.IsSecuritySafeCritical);

    public Optional<bool> IsSecurityTransparentResult { get; set; }
    public override bool IsSecurityTransparent => IsSecurityTransparentResult.Or(DelegatingConstructor.IsSecurityTransparent);

    public Optional<RuntimeMethodHandle> MethodHandleResult { get; set; }
    public override RuntimeMethodHandle MethodHandle => MethodHandleResult.Or(DelegatingConstructor.MethodHandle);

    public Optional<MemberTypes> MemberTypeResult { get; set; }
    public override MemberTypes MemberType => MemberTypeResult.Or(DelegatingConstructor.MemberType);

    public Optional<string> NameResult { get; set; }
    public override string Name => NameResult.Or(DelegatingConstructor.Name);

    public Optional<Type> ReflectedTypeResult { get; set; }
    public override Type ReflectedType => ReflectedTypeResult.Or(DelegatingConstructor.ReflectedType!);

    public override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
    {
        return DelegatingConstructor.Invoke(invokeAttr, binder, parameters, culture);
    }

    public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
    {
        return DelegatingConstructor.Invoke(obj, invokeAttr, binder, parameters, culture);
    }

    public override object[] GetCustomAttributes(bool inherit)
    {
        return DelegatingConstructor.GetCustomAttributes(inherit);
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        return DelegatingConstructor.GetCustomAttributes(attributeType, inherit);
    }

    public override bool IsDefined(Type attributeType, bool inherit)
    {
        return DelegatingConstructor.IsDefined(attributeType, inherit);
    }

    public override bool Equals(object? obj) => DelegatingConstructor.Equals(obj);

    public override int GetHashCode() => DelegatingConstructor.GetHashCode();
}
