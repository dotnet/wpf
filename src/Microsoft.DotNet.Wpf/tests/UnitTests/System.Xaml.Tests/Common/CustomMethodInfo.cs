// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;

namespace System.Xaml.Tests.Common;

public class CustomMethodInfo : MethodInfo
{
    protected MethodInfo DelegatingMethod { get; }

    public CustomMethodInfo(MethodInfo delegatingMethod)
    {
        DelegatingMethod = delegatingMethod;
    }

    public Optional<MethodAttributes> AttributesResult { get; set; }
    public override MethodAttributes Attributes => AttributesResult.Or(DelegatingMethod.Attributes);

    public Optional<Type?> DeclaringTypeResult { get; set; }
    public override Type DeclaringType => DeclaringTypeResult.Or(DelegatingMethod.DeclaringType)!;

    public Optional<MethodInfo> GetBaseDefinitionResult { get; set; }
    public override MethodInfo GetBaseDefinition() => GetBaseDefinitionResult.Or(DelegatingMethod.GetBaseDefinition);

    public Optional<MethodImplAttributes> GetMethodImplementationFlagsResult { get; set; }
    public override MethodImplAttributes GetMethodImplementationFlags() => GetMethodImplementationFlagsResult.Or(DelegatingMethod.GetMethodImplementationFlags);

    public Optional<ParameterInfo[]> GetParametersResult { get; set; }
    public override ParameterInfo[] GetParameters() => GetParametersResult.Or(DelegatingMethod.GetParameters);

    public Optional<RuntimeMethodHandle> MethodHandleResult { get; set; }
    public override RuntimeMethodHandle MethodHandle => MethodHandleResult.Or(DelegatingMethod.MethodHandle);

    public Optional<MemberTypes> MemberTypeResult { get; set; }
    public override MemberTypes MemberType => MemberTypeResult.Or(DelegatingMethod.MemberType);

    public Optional<string> NameResult { get; set; }
    public override string Name => NameResult.Or(DelegatingMethod.Name);

    public Optional<Type> ReflectedTypeResult { get; set; }
    public override Type ReflectedType => ReflectedTypeResult.Or(DelegatingMethod.ReflectedType!);

    public Optional<ParameterInfo> ReturnParameterResult { get; set; }
    public override ParameterInfo ReturnParameter => ReturnParameterResult.Or(DelegatingMethod.ReturnParameter);

    public Optional<Type?> ReturnTypeResult { get; set; }
    public override Type ReturnType => ReturnTypeResult.Or(DelegatingMethod.ReturnType)!;

    public Optional<ICustomAttributeProvider> ReturnTypeCustomAttributesResult { get; set; }
    public override ICustomAttributeProvider ReturnTypeCustomAttributes => ReturnTypeCustomAttributesResult.Or(DelegatingMethod.ReturnTypeCustomAttributes);

    public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
    {
        return DelegatingMethod.Invoke(obj, invokeAttr, binder, parameters, culture);
    }

    public override object[] GetCustomAttributes(bool inherit)
    {
        return DelegatingMethod.GetCustomAttributes(inherit);
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        return DelegatingMethod.GetCustomAttributes(attributeType, inherit);
    }

    public override bool IsDefined(Type attributeType, bool inherit)
    {
        return DelegatingMethod.IsDefined(attributeType, inherit);
    }

    public override bool Equals(object? obj) => DelegatingMethod.Equals(obj);

    public override int GetHashCode() => DelegatingMethod.GetHashCode();
}
