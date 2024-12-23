// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;

namespace System.Xaml.Tests.Common;

public class CustomType : TypeDelegator
{
    public CustomType(Type delegatingType) : base(delegatingType)
    {
    }

    public Optional<Assembly> AssemblyResult { get; set; }
    public override Assembly Assembly => AssemblyResult.Or(base.Assembly);

    public Optional<IList<CustomAttributeData>> GetCustomAttributesDataResult { get; set; }
    public override IList<CustomAttributeData> GetCustomAttributesData()
    {
        return GetCustomAttributesDataResult.Or(typeImpl.GetCustomAttributesData);
    }

    public Optional<Type?> DeclaringTypeResult { get; set; }
    public override Type? DeclaringType => DeclaringTypeResult.Or(typeImpl.DeclaringType);


    public Optional<ConstructorInfo?> GetConstructorResult { get; set; }
    protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
    {
        return GetConstructorResult.Or(base.GetConstructorImpl, bindingAttr, binder, callConvention, types, modifiers);
    }

    public Optional<EventInfo[]> GetEventsResult { get; set; }
    public override EventInfo[] GetEvents(BindingFlags bindingAttr)
    {
        return GetEventsResult.Or(typeImpl.GetEvents, bindingAttr);
    }

    public Optional<Type?[]?> GetGenericParameterConstraintsResult { get; set; }
    public override Type[] GetGenericParameterConstraints()
    {
        return GetGenericParameterConstraintsResult.Or(typeImpl.GetGenericParameterConstraints)!;
    }

    public Optional<Type?[]?> GetInterfacesResult { get; set; }
    public override Type[] GetInterfaces()
    {
        return GetInterfacesResult.Or(typeImpl.GetInterfaces)!;
    }

    public Optional<MemberInfo[]> GetMemberResult { get; set; }
    public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
    {
        return GetMemberResult.Or(typeImpl.GetMember, name, type, bindingAttr);
    }

    public Optional<PropertyInfo[]> GetPropertiesResult { get; set; }
    public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
    {
        return GetPropertiesResult.Or(typeImpl.GetProperties, bindingAttr);
    }

    public Optional<bool> IsGenericParameterResult { get; set; }
    public override bool IsGenericParameter => IsGenericParameterResult.Or(typeImpl.IsGenericParameter);

    public Optional<Type> UnderlyingSystemTypeResult { get; set; }
    public override Type UnderlyingSystemType => UnderlyingSystemTypeResult.Or(typeImpl.UnderlyingSystemType);
}
