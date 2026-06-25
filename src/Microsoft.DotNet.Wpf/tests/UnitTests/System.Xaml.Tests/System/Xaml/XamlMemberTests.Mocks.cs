// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Markup;
using System.Xaml.Schema;
using System.Xaml.Tests.Common;

namespace System.Xaml.Tests
{
    public partial class XamlMemberTests
    {
        public class SubXamlMember : XamlMember
        {
            public SubXamlMember(string name, XamlType declaringType, bool isAttachable) : base(name, declaringType, isAttachable) { }

            public SubXamlMember(PropertyInfo propertyInfo, XamlSchemaContext schemaContext) : base(propertyInfo, schemaContext) { }

            public SubXamlMember(PropertyInfo propertyInfo, XamlSchemaContext schemaContext, XamlMemberInvoker invoker) : base(propertyInfo, schemaContext, invoker) { }

            public SubXamlMember(string attachablePropertyName, MethodInfo? getter, MethodInfo? setter, XamlSchemaContext schemaContext) : base(attachablePropertyName, getter, setter, schemaContext) { }

            public SubXamlMember(string attachablePropertyName, MethodInfo? getter, MethodInfo? setter, XamlSchemaContext schemaContext, XamlMemberInvoker invoker) : base(attachablePropertyName, getter, setter, schemaContext, invoker) { }

            public SubXamlMember(EventInfo eventInfo, XamlSchemaContext schemaContext) : base(eventInfo, schemaContext) { }

            public SubXamlMember(EventInfo eventInfo, XamlSchemaContext schemaContext, XamlMemberInvoker invoker) : base(eventInfo, schemaContext, invoker) { }
            
            public SubXamlMember(string attachableEventName, MethodInfo adder, XamlSchemaContext schemaContext) : base(attachableEventName, adder, schemaContext) { }

            public SubXamlMember(string attachableEventName, MethodInfo adder, XamlSchemaContext schemaContext, XamlMemberInvoker invoker) : base(attachableEventName, adder, schemaContext, invoker) { }

            public ICustomAttributeProvider LookupCustomAttributeProviderEntry() => LookupCustomAttributeProvider();

            public XamlValueConverter<XamlDeferringLoader> LookupDeferringLoaderEntry() => LookupDeferringLoader();

            public IList<XamlMember> LookupDependsOnEntry() => LookupDependsOn();

            public XamlMemberInvoker LookupInvokerEntry() => LookupInvoker();
            
            public bool LookupIsAmbientEntry() => LookupIsAmbient();

            public bool LookupIsEventEntry() => LookupIsEvent();
            
            public bool LookupIsReadOnlyEntry() => LookupIsReadOnly();
            
            public bool LookupIsReadPublicEntry() => LookupIsReadPublic();

            public bool LookupIsUnknownEntry() => LookupIsUnknown();

            public bool LookupIsWriteOnlyEntry() => LookupIsWriteOnly();

            public bool LookupIsWritePublicEntry() => LookupIsWritePublic();
            
            public IReadOnlyDictionary<char,char> LookupMarkupExtensionBracketCharactersEntry() => LookupMarkupExtensionBracketCharacters();

            public XamlType LookupTargetTypeEntry() => LookupTargetType();

            public XamlType LookupTypeEntry() => LookupType();

            public XamlValueConverter<TypeConverter> LookupTypeConverterEntry() => LookupTypeConverter();

            public MethodInfo LookupUnderlyingGetterEntry() => LookupUnderlyingGetter();
            
            public MemberInfo LookupUnderlyingMemberEntry() => LookupUnderlyingMember();
    
            public MethodInfo LookupUnderlyingSetterEntry() => LookupUnderlyingSetter();

            public XamlValueConverter<ValueSerializer> LookupValueSerializerEntry() => LookupValueSerializer();
        }

        private class CustomXamlMember : SubXamlMember
        {
            public CustomXamlMember(string name, XamlType declaringType, bool isAttachable) : base(name, declaringType, isAttachable) { }

            public CustomXamlMember(PropertyInfo propertyInfo, XamlSchemaContext schemaContext) : base(propertyInfo, schemaContext) { }

            public CustomXamlMember(PropertyInfo propertyInfo, XamlSchemaContext schemaContext, XamlMemberInvoker invoker) : base(propertyInfo, schemaContext, invoker) { }

            public CustomXamlMember(string attachablePropertyName, MethodInfo getter, MethodInfo setter, XamlSchemaContext schemaContext) : base(attachablePropertyName, getter, setter, schemaContext) { }

            public CustomXamlMember(string attachablePropertyName, MethodInfo getter, MethodInfo setter, XamlSchemaContext schemaContext, XamlMemberInvoker invoker) : base(attachablePropertyName, getter, setter, schemaContext, invoker) { }

            public CustomXamlMember(EventInfo eventInfo, XamlSchemaContext schemaContext) : base(eventInfo, schemaContext) { }

            public CustomXamlMember(EventInfo eventInfo, XamlSchemaContext schemaContext, XamlMemberInvoker invoker) : base(eventInfo, schemaContext, invoker) { }

            public CustomXamlMember(string attachableEventName, MethodInfo adder, XamlSchemaContext schemaContext) : base(attachableEventName, adder, schemaContext) { }

            public CustomXamlMember(string attachableEventName, MethodInfo adder, XamlSchemaContext schemaContext, XamlMemberInvoker invoker) : base(attachableEventName, adder, schemaContext, invoker) { }
        
            public Optional<ICustomAttributeProvider?> LookupCustomAttributeProviderResult { get; set; }
            protected override ICustomAttributeProvider LookupCustomAttributeProvider()
            {
                return LookupCustomAttributeProviderResult.Or(base.LookupCustomAttributeProvider)!;
            }

            public Optional<IList<XamlMember>?> LookupDependsOnResult { get; set; }
            protected override IList<XamlMember> LookupDependsOn()
            {
                return LookupDependsOnResult.Or(base.LookupDependsOn)!;
            }

            public Optional<XamlValueConverter<XamlDeferringLoader>?> LookupDeferringLoaderResult { get; set; }
            protected override XamlValueConverter<XamlDeferringLoader> LookupDeferringLoader()
            {
                return LookupDeferringLoaderResult.Or(base.LookupDeferringLoader)!;
            }

            public Optional<XamlMemberInvoker?> LookupInvokerResult { get; set; }
            protected override XamlMemberInvoker LookupInvoker()
            {
                return LookupInvokerResult.Or(base.LookupInvoker)!;
            }

            public Optional<bool> LookupIsAmbientResult { get; set; }
            protected override bool LookupIsAmbient()
            {
                return LookupIsAmbientResult.Or(base.LookupIsAmbient);
            }

            public Optional<bool> LookupIsEventResult { get; set; }
            protected override bool LookupIsEvent()
            {
                return LookupIsEventResult.Or(base.LookupIsEvent);
            }

            public Optional<bool> LookupIsReadOnlyResult { get; set; }
            protected override bool LookupIsReadOnly()
            {
                return LookupIsReadOnlyResult.Or(base.LookupIsReadOnly);
            }

            public Optional<bool> LookupIsReadPublicResult { get; set; }
            protected override bool LookupIsReadPublic()
            {
                return LookupIsReadPublicResult.Or(base.LookupIsReadPublic);
            }

            public Optional<bool> LookupIsUnknownResult { get; set; }
            protected override bool LookupIsUnknown()
            {
                return LookupIsUnknownResult.Or(base.LookupIsUnknown);
            }

            public Optional<bool> LookupIsWriteOnlyResult { get; set; }
            protected override bool LookupIsWriteOnly()
            {
                return LookupIsWriteOnlyResult.Or(base.LookupIsWriteOnly);
            }

            public Optional<bool> LookupIsWritePublicResult { get; set; }
            protected override bool LookupIsWritePublic()
            {
                return LookupIsWritePublicResult.Or(base.LookupIsWritePublic);
            }

            public Optional<IReadOnlyDictionary<char,char>?> LookupMarkupExtensionBracketCharactersResult { get; set; }
            protected override IReadOnlyDictionary<char,char> LookupMarkupExtensionBracketCharacters()
            {
                return LookupMarkupExtensionBracketCharactersResult.Or(base.LookupMarkupExtensionBracketCharacters)!;
            }

            public Optional<XamlType?> LookupTargetTypeResult { get; set; }
            protected override XamlType LookupTargetType()
            {
                return LookupTargetTypeResult.Or(base.LookupTargetType)!;
            }

            public Optional<XamlType?> LookupTypeResult { get; set; }
            protected override XamlType LookupType()
            {
                return LookupTypeResult.Or(base.LookupType)!;
            }

            public Optional<XamlValueConverter<TypeConverter>?> LookupTypeConverterResult { get; set; }
            protected override XamlValueConverter<TypeConverter> LookupTypeConverter()
            {
                return LookupTypeConverterResult.Or(base.LookupTypeConverter)!;
            }

            public Optional<MethodInfo?> LookupUnderlyingGetterResult { get; set; }
            protected override MethodInfo LookupUnderlyingGetter()
            {
                return LookupUnderlyingGetterResult.Or(base.LookupUnderlyingGetter)!;
            }

            public Optional<MemberInfo?> LookupUnderlyingMemberResult { get; set; }
            protected override MemberInfo LookupUnderlyingMember()
            {
                return LookupUnderlyingMemberResult.Or(base.LookupUnderlyingMember)!;
            }

            public Optional<MethodInfo?> LookupUnderlyingSetterResult { get; set; }
            protected override MethodInfo LookupUnderlyingSetter()
            {
                return LookupUnderlyingSetterResult.Or(base.LookupUnderlyingSetter)!;
            }

            public Optional<XamlValueConverter<ValueSerializer>?> LookupValueSerializerResult { get; set; }
            protected override XamlValueConverter<ValueSerializer> LookupValueSerializer()
            {
                return LookupValueSerializerResult.Or(base.LookupValueSerializer)!;
            }

            public Optional<IList<string>?> GetXamlNamespacesResult { get; set; }
            public override IList<string> GetXamlNamespaces()
            {
                return GetXamlNamespacesResult.Or(base.GetXamlNamespaces)!;
            }
        }
    }
}
