// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Markup;
using System.Xaml.Schema;

namespace System.Xaml.Tests.Common;

public class SubXamlType : XamlType
{
    public SubXamlType(string? unknownTypeNamespace, string? unknownTypeName, IList<XamlType?>? typeArguments, XamlSchemaContext schemaContext) : base(unknownTypeNamespace, unknownTypeName, typeArguments, schemaContext) { }

    public SubXamlType(Type? underlyingType, XamlSchemaContext? schemaContext) : base(underlyingType, schemaContext) { }

    public SubXamlType(string? typeName, IList<XamlType?>? typeArguments, XamlSchemaContext? schemaContext) : base(typeName, typeArguments, schemaContext) { }

    public XamlMember LookupAliasedPropertyEntry(XamlDirective directive) => LookupAliasedProperty(directive);

    public IEnumerable<XamlMember> LookupAllAttachableMembersEntry() => LookupAllAttachableMembers();

    public IEnumerable<XamlMember> LookupAllMembersEntry() => LookupAllMembers();

    public IList<XamlType> LookupAllowedContentTypesEntry() => LookupAllowedContentTypes();

    public XamlMember LookupAttachableMemberEntry(string name) => LookupAttachableMember(name);

    public XamlType LookupBaseTypeEntry() => LookupBaseType();

    public XamlCollectionKind LookupCollectionKindEntry() => LookupCollectionKind();

    public bool LookupConstructionRequiresArgumentsEntry() => LookupConstructionRequiresArguments();

    public XamlMember LookupContentPropertyEntry() => LookupContentProperty();

    public IList<XamlType> LookupContentWrappersEntry() => LookupContentWrappers();

    public ICustomAttributeProvider LookupCustomAttributeProviderEntry() => LookupCustomAttributeProvider();

    public XamlValueConverter<XamlDeferringLoader> LookupDeferringLoaderEntry() => LookupDeferringLoader();

    public XamlTypeInvoker LookupInvokerEntry() => LookupInvoker();

    public bool LookupIsAmbientEntry() => LookupIsAmbient();

    public bool LookupIsConstructibleEntry() => LookupIsConstructible();

    public bool LookupIsMarkupExtensionEntry() => LookupIsMarkupExtension();

    public bool LookupIsNameScopeEntry() => LookupIsNameScope();

    public bool LookupIsNullableEntry() => LookupIsNullable();

    public bool LookupIsPublicEntry() => LookupIsPublic();

    public bool LookupIsUnknownEntry() => LookupIsUnknown();

    public bool LookupIsWhitespaceSignificantCollectionEntry() => LookupIsWhitespaceSignificantCollection();

    public bool LookupIsXDataEntry() => LookupIsXData();

    public XamlType LookupItemTypeEntry() => LookupItemType();

    public XamlType LookupKeyTypeEntry() => LookupKeyType();

    public XamlType LookupMarkupExtensionReturnTypeEntry() => LookupMarkupExtensionReturnType();

    public XamlMember LookupMemberEntry(string name, bool skipReadOnlyCheck) => LookupMember(name, skipReadOnlyCheck);

    public IList<XamlType> LookupPositionalParametersEntry(int parameterCount) => LookupPositionalParameters(parameterCount);

    public EventHandler<XamlSetMarkupExtensionEventArgs> LookupSetMarkupExtensionHandlerEntry() => LookupSetMarkupExtensionHandler();

    public EventHandler<XamlSetTypeConverterEventArgs> LookupSetTypeConverterHandlerEntry() => LookupSetTypeConverterHandler();

    public XamlValueConverter<TypeConverter> LookupTypeConverterEntry() => LookupTypeConverter();

    public Type LookupUnderlyingTypeEntry() => LookupUnderlyingType();

    public bool LookupUsableDuringInitializationEntry() => LookupUsableDuringInitialization();

    public bool LookupTrimSurroundingWhitespaceEntry() => LookupTrimSurroundingWhitespace();

    public XamlValueConverter<ValueSerializer> LookupValueSerializerEntry() => LookupValueSerializer();
}
