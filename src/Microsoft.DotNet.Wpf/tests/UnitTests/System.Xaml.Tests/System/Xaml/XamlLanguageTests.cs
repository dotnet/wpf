// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Markup;
using System.Xaml.Schema;
using Xunit;

namespace System.Xaml.Tests;

public class XamlLanguageTests
{
    [Fact]
    public void XamlNamespaces_Get_ReturnsExpected()
    {
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, XamlLanguage.XamlNamespaces);
    }

    [Fact]
    public void XmlNamespaces_Get_ReturnsExpected()
    {
        Assert.Equal(new string[] { "http://www.w3.org/XML/1998/namespace" }, XamlLanguage.XmlNamespaces);
    }

    [Fact]
    public void Array_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Array;
        Assert.Same(type, XamlLanguage.Array);
        Assert.Equal("ArrayExtension", type.Name);
        Assert.Equal(typeof(ArrayExtension), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Member_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Member;
        Assert.Same(type, XamlLanguage.Member);
        Assert.Equal("Member", type.Name);
        Assert.Equal(typeof(MemberDefinition), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Null_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Null;
        Assert.Same(type, XamlLanguage.Null);
        Assert.Equal("NullExtension", type.Name);
        Assert.Equal(typeof(NullExtension), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Property_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Property;
        Assert.Same(type, XamlLanguage.Property);
        Assert.Equal("Property", type.Name);
        Assert.Equal(typeof(PropertyDefinition), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Reference_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Reference;
        Assert.Same(type, XamlLanguage.Reference);
        Assert.Equal("Reference", type.Name);
        Assert.Equal(typeof(Reference), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Static_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Static;
        Assert.Same(type, XamlLanguage.Static);
        Assert.Equal("StaticExtension", type.Name);
        Assert.Equal(typeof(StaticExtension), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Type_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Type;
        Assert.Same(type, XamlLanguage.Type);
        Assert.Equal("TypeExtension", type.Name);
        Assert.Equal(typeof(TypeExtension), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void String_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.String;
        Assert.Same(type, XamlLanguage.String);
        Assert.Equal("String", type.Name);
        Assert.Equal(typeof(string), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Double_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Double;
        Assert.Same(type, XamlLanguage.Double);
        Assert.Equal("Double", type.Name);
        Assert.Equal(typeof(double), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Int32_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Int32;
        Assert.Same(type, XamlLanguage.Int32);
        Assert.Equal("Int32", type.Name);
        Assert.Equal(typeof(int), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Boolean_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Boolean;
        Assert.Same(type, XamlLanguage.Boolean);
        Assert.Equal("Boolean", type.Name);
        Assert.Equal(typeof(bool), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void XData_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.XData;
        Assert.Same(type, XamlLanguage.XData);
        Assert.Equal("XData", type.Name);
        Assert.Equal(typeof(XData), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Object_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Object;
        Assert.Same(type, XamlLanguage.Object);
        Assert.Equal("Object", type.Name);
        Assert.Equal(typeof(object), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Char_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Char;
        Assert.Same(type, XamlLanguage.Char);
        Assert.Equal("Char", type.Name);
        Assert.Equal(typeof(char), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Single_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Single;
        Assert.Same(type, XamlLanguage.Single);
        Assert.Equal("Single", type.Name);
        Assert.Equal(typeof(float), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Byte_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Byte;
        Assert.Same(type, XamlLanguage.Byte);
        Assert.Equal("Byte", type.Name);
        Assert.Equal(typeof(byte), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Int16_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Int16;
        Assert.Same(type, XamlLanguage.Int16);
        Assert.Equal("Int16", type.Name);
        Assert.Equal(typeof(short), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Int64_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Int64;
        Assert.Same(type, XamlLanguage.Int64);
        Assert.Equal("Int64", type.Name);
        Assert.Equal(typeof(long), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Decimal_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Decimal;
        Assert.Same(type, XamlLanguage.Decimal);
        Assert.Equal("Decimal", type.Name);
        Assert.Equal(typeof(decimal), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void Uri_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.Uri;
        Assert.Same(type, XamlLanguage.Uri);
        Assert.Equal("Uri", type.Name);
        Assert.Equal(typeof(Uri), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void TimeSpan_Get_ReturnsExpected()
    {
        XamlType type = XamlLanguage.TimeSpan;
        Assert.Same(type, XamlLanguage.TimeSpan);
        Assert.Equal("TimeSpan", type.Name);
        Assert.Equal(typeof(TimeSpan), type.UnderlyingType);
        Assert.Null(type.TypeArguments);
        Assert.Equal(XamlLanguage.XamlNamespaces[0], type.PreferredXamlNamespace);
    }

    [Fact]
    public void AllTypes_Get_ReturnsExpected()
    {
        ReadOnlyCollection<XamlType> types = XamlLanguage.AllTypes;
        Assert.Same(types, XamlLanguage.AllTypes);
        Assert.Equal(new XamlType[] { XamlLanguage.Array, XamlLanguage.Member, XamlLanguage.Null, XamlLanguage.Property, XamlLanguage.Reference, XamlLanguage.Static, XamlLanguage.Type, XamlLanguage.String, XamlLanguage.Double, XamlLanguage.Int16, XamlLanguage.Int32, XamlLanguage.Int64, XamlLanguage.Boolean, XamlLanguage.XData, XamlLanguage.Object, XamlLanguage.Char, XamlLanguage.Single, XamlLanguage.Byte, XamlLanguage.Decimal, XamlLanguage.Uri, XamlLanguage.TimeSpan }, types);
    }

    [Fact]
    public void Arguments_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Arguments;
        Assert.Same(directive, XamlLanguage.Arguments);
        Assert.Equal("Arguments", directive.Name);
        Assert.Equal("List", directive.Type.Name);
        Assert.Equal(typeof(List<object>), directive.Type.UnderlyingType);
        Assert.Equal(new Type[] { typeof(object) }, directive.Type.TypeArguments.Select(t => t.UnderlyingType));
        Assert.Equal(AllowedMemberLocations.Any, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void AsyncRecords_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.AsyncRecords;
        Assert.Same(directive, XamlLanguage.AsyncRecords);
        Assert.Equal("AsyncRecords", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void Class_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Class;
        Assert.Same(directive, XamlLanguage.Class);
        Assert.Equal("Class", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void ClassModifier_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.ClassModifier;
        Assert.Same(directive, XamlLanguage.ClassModifier);
        Assert.Equal("ClassModifier", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void Code_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Code;
        Assert.Same(directive, XamlLanguage.Code);
        Assert.Equal("Code", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void ConnectionId_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.ConnectionId;
        Assert.Same(directive, XamlLanguage.ConnectionId);
        Assert.Equal("ConnectionId", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Any, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void FactoryMethod_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.FactoryMethod;
        Assert.Same(directive, XamlLanguage.FactoryMethod);
        Assert.Equal("FactoryMethod", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Any, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void FieldModifier_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.FieldModifier;
        Assert.Same(directive, XamlLanguage.FieldModifier);
        Assert.Equal("FieldModifier", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void Items_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Items;
        Assert.Same(directive, XamlLanguage.Items);
        Assert.Equal("_Items", directive.Name);
        Assert.Equal("List", directive.Type.Name);
        Assert.Equal(typeof(List<object>), directive.Type.UnderlyingType);
        Assert.Equal(new Type[] { typeof(object) }, directive.Type.TypeArguments.Select(t => t.UnderlyingType));
        Assert.Equal(AllowedMemberLocations.Any, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void Initialization_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Initialization;
        Assert.Same(directive, XamlLanguage.Initialization);
        Assert.Equal("_Initialization", directive.Name);
        Assert.Equal("Object", directive.Type.Name);
        Assert.Equal(typeof(object), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Any, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void Key_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Key;
        Assert.Same(directive, XamlLanguage.Key);
        Assert.Equal("Key", directive.Name);
        Assert.Equal("Object", directive.Type.Name);
        Assert.Equal(typeof(object), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Any, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void Members_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Members;
        Assert.Same(directive, XamlLanguage.Members);
        Assert.Equal("Members", directive.Name);
        Assert.Equal("List", directive.Type.Name);
        Assert.Equal(typeof(List<MemberDefinition>), directive.Type.UnderlyingType);
        Assert.Equal(new Type[] { typeof(MemberDefinition) }, directive.Type.TypeArguments.Select(t => t.UnderlyingType));
        Assert.Equal(AllowedMemberLocations.MemberElement, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void ClassAttributes_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.ClassAttributes;
        Assert.Same(directive, XamlLanguage.ClassAttributes);
        Assert.Equal("ClassAttributes", directive.Name);
        Assert.Equal("List", directive.Type.Name);
        Assert.Equal(typeof(List<Attribute>), directive.Type.UnderlyingType);
        Assert.Equal(new Type[] { typeof(Attribute) }, directive.Type.TypeArguments.Select(t => t.UnderlyingType));
        Assert.Equal(AllowedMemberLocations.MemberElement, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void Name_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Name;
        Assert.Same(directive, XamlLanguage.Name);
        Assert.Equal("Name", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void PositionalParameters_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.PositionalParameters;
        Assert.Same(directive, XamlLanguage.PositionalParameters);
        Assert.Equal("_PositionalParameters", directive.Name);
        Assert.Equal("List", directive.Type.Name);
        Assert.Equal(typeof(List<object>), directive.Type.UnderlyingType);
        Assert.Equal(new Type[] { typeof(object) }, directive.Type.TypeArguments.Select(t => t.UnderlyingType));
        Assert.Equal(AllowedMemberLocations.Any, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void Shared_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Shared;
        Assert.Same(directive, XamlLanguage.Shared);
        Assert.Equal("Shared", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void Subclass_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Subclass;
        Assert.Same(directive, XamlLanguage.Subclass);
        Assert.Equal("Subclass", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void SynchronousMode_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.SynchronousMode;
        Assert.Same(directive, XamlLanguage.SynchronousMode);
        Assert.Equal("SynchronousMode", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void TypeArguments_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.TypeArguments;
        Assert.Same(directive, XamlLanguage.TypeArguments);
        Assert.Equal("TypeArguments", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void Uid_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Uid;
        Assert.Same(directive, XamlLanguage.Uid);
        Assert.Equal("Uid", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void UnknownContent_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.UnknownContent;
        Assert.Same(directive, XamlLanguage.UnknownContent);
        Assert.Equal("_UnknownContent", directive.Name);
        Assert.Equal("Object", directive.Type.Name);
        Assert.Equal(typeof(object), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.MemberElement, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void Base_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Base;
        Assert.Same(directive, XamlLanguage.Base);
        Assert.Equal("base", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://www.w3.org/XML/1998/namespace" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void Lang_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Lang;
        Assert.Same(directive, XamlLanguage.Lang);
        Assert.Equal("lang", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://www.w3.org/XML/1998/namespace" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void Space_Get_ReturnsExpected()
    {
        XamlDirective directive = XamlLanguage.Space;
        Assert.Same(directive, XamlLanguage.Space);
        Assert.Equal("space", directive.Name);
        Assert.Equal("String", directive.Type.Name);
        Assert.Equal(typeof(string), directive.Type.UnderlyingType);
        Assert.Null(directive.Type.TypeArguments);
        Assert.Equal(AllowedMemberLocations.Attribute, directive.AllowedLocation);
        Assert.Equal(new string[] { "http://www.w3.org/XML/1998/namespace" }, directive.GetXamlNamespaces());
    }

    [Fact]
    public void AllDirectives_Get_ReturnsExpected()
    {
        ReadOnlyCollection<XamlDirective> directives = XamlLanguage.AllDirectives;
        Assert.Same(directives, XamlLanguage.AllDirectives);
        Assert.Equal(new XamlDirective[] { XamlLanguage.Arguments, XamlLanguage.AsyncRecords, XamlLanguage.Class, XamlLanguage.Code, XamlLanguage.ClassModifier, XamlLanguage.ConnectionId, XamlLanguage.FactoryMethod, XamlLanguage.FieldModifier, XamlLanguage.Key, XamlLanguage.Initialization, XamlLanguage.Items, XamlLanguage.Members, XamlLanguage.ClassAttributes, XamlLanguage.Name, XamlLanguage.PositionalParameters, XamlLanguage.Shared, XamlLanguage.Subclass, XamlLanguage. SynchronousMode, XamlLanguage.TypeArguments, XamlLanguage.Uid, XamlLanguage.UnknownContent, XamlLanguage.Base, XamlLanguage.Lang, XamlLanguage.Space }, directives);
    }
}
