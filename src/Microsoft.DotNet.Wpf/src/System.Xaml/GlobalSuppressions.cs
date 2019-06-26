// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

#region Microsoft.Design Suppressions
[assembly: SuppressMessage("Microsoft.Design", "CA1064:Exceptions should be public", Justification = "Exception is internal only", Scope = "type", Target = "~T:MS.Internal.Xaml.Parser.GenericTypeNameParser.TypeNameParserException")]
[assembly: SuppressMessage("Microsoft.Design", "CA1032:Implement standard exception constructors", Justification = "Exception is internal only", Scope = "type", Target = "~T:MS.Internal.Xaml.Parser.GenericTypeNameParser.TypeNameParserException")]
[assembly: SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Scope = "type", Target = "System.Xaml.Schema.XamlCollectionKind", Justification = "Our measurements showed this type provided improved spacial complexity.")]
[assembly: SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Scope = "type", Target = "System.Xaml.XamlNodeType", Justification = "Our measurements showed this type provided improved spacial complexity.")]
[assembly: SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes", Scope = "type", Target = "System.Xaml.Schema.ReferenceEqualityTuple`2<T1,T2>", Justification = "Internal Only")]
[assembly: SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes", Scope = "type", Target = "System.Xaml.Schema.ReferenceEqualityTuple`3<T1,T2,T3>", Justification = "Internal Only")]
[assembly: SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not changing public API surface for .NET Core 3 release", Scope = "member", Target = "~M:System.Xaml.XamlWriter.System#IDisposable#Dispose")]
[assembly: SuppressMessage("Microsoft.Design", "CA1031:Do not catch general exception types", Justification = "Don't want render thread to crash", Scope = "member", Target = "~M:System.Xaml.XamlBackgroundReader.XamlReaderThreadStart(System.Object)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1031:Do not catch general exception types", Justification = "Finalizers shouldn't throw", Scope = "member", Target = "~M:System.Xaml.XamlSchemaContext.Finalize")]
#endregion

#region Microsoft.Performance suppressions
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "MS.Internal.Xaml.Context.ObjectWriterFrame")]

// Need this public Ctor Override that takes an InnerExcepetion.
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Xaml.XamlParseException.#.ctor(MS.Internal.Xaml.Context.XamlParserContext,System.String,System.Exception)")]

[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.EventTrace.#EasyTraceEvent(MS.Internal.Xaml.EventTrace+Keyword,MS.Internal.Xaml.EventTrace+Event)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.EventTrace.#EasyTraceEvent(MS.Internal.Xaml.EventTrace+Keyword,MS.Internal.Xaml.EventTrace+Event,System.Object)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.EventTrace.#EasyTraceEvent(MS.Internal.Xaml.EventTrace+Keyword,MS.Internal.Xaml.EventTrace+Event,System.Object,System.Object)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.EventTrace.#EasyTraceEvent(MS.Internal.Xaml.EventTrace+Keyword,MS.Internal.Xaml.EventTrace+Event,System.Object,System.Object,System.Object)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.EventTrace.#EasyTraceEvent(MS.Internal.Xaml.EventTrace+Keyword,MS.Internal.Xaml.EventTrace+Level,MS.Internal.Xaml.EventTrace+Event)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.EventTrace.#EasyTraceEvent(MS.Internal.Xaml.EventTrace+Keyword,MS.Internal.Xaml.EventTrace+Level,MS.Internal.Xaml.EventTrace+Event,System.Object)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.EventTrace.#EasyTraceEvent(MS.Internal.Xaml.EventTrace+Keyword,MS.Internal.Xaml.EventTrace+Level,MS.Internal.Xaml.EventTrace+Event,System.Object,System.Object)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.TraceProvider.#get_Keywords()", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.TraceProvider.#get_Level()", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.TraceProvider.#get_MatchAllKeywords()", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.TraceProvider.#TraceEvent(MS.Internal.Xaml.EventTrace+Event,MS.Internal.Xaml.EventTrace+Keyword,MS.Internal.Xaml.EventTrace+Level)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.TraceProvider.#TraceEvent(MS.Internal.Xaml.EventTrace+Event,MS.Internal.Xaml.EventTrace+Keyword,MS.Internal.Xaml.EventTrace+Level,System.Object)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.Context.ObjectWriterContext.#get_ParentInstanceRegisteredName()", Justification = "We need the setter, and write-only properties are bad practice")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Internal.Xaml.Context.ObjectWriterContext.#get_ParentKey()", Justification = "We need the setter, and write-only properties are bad practice")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "MS.Win32.ClassicEtw.#GetTraceLoggerHandle(MS.Win32.ClassicEtw+WNODE_HEADER*)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Xaml.ReflectionHelper.#GetAlreadyLoadedAssembly(System.String)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Xaml.ReflectionHelper.#GetCustomAttributeData(System.Collections.Generic.IList`1<System.Reflection.CustomAttributeData>,System.Type,System.Type&,System.Boolean,System.Boolean)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Xaml.ReflectionHelper.#GetCustomAttributeData(System.Reflection.MemberInfo,System.Type,System.Type&)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Xaml.ReflectionHelper.#GetTypeConverterAttributeData(System.Reflection.MemberInfo,System.Type&)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Xaml.ReflectionHelper.#IsInternalType(System.Type)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Xaml.ReflectionHelper.#ResetCacheForAssembly(System.String)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Xaml.SR.#get_ResourceManager()", Justification = "Auto-generated")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Xaml.XmlCompatibilityReader.#.ctor(System.Xml.XmlReader,System.Collections.Generic.IEnumerable`1<System.String>)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Xaml.XmlCompatibilityReader.#.ctor(System.Xml.XmlReader,System.Xaml.IsXmlNamespaceSupportedCallback,System.Collections.Generic.IEnumerable`1<System.String>)", Justification = "Shared source file")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Xaml.XmlCompatibilityReader.#get_Encoding()", Justification = "Shared source file")]

// This is a debug-only method, we should mark it as Conditional("DEBUG")
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Xaml.XamlNode.#IsEof_Helper(System.Xaml.XamlNodeType,System.Object)", Justification = "Fix doesn't meet Ask Mode bar")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Xaml.XamlObjectWriter.#get_ObjectWriterContext()", Justification = "Fix doesn't meet Ask Mode bar - Bug 773900")]

// New since v4 RTM:

//this is used by subclasses, bad FxCop detection
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "System.Xaml.MS.Impl.FrugalObjectList`1+Compacter")]
#endregion

#region Microsoft.Naming Suppressions
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope = "member", Target = "System.Xaml.XamlType.#IsWhitespaceSignificantCollectionCore", MessageId = "Whitespace", Justification = "Add Whitespace to the dictionary if we already shipped, and it seems good.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope = "member", Target = "System.Xaml.XamlType.#IsWhitespaceSignificantCollection", MessageId = "Whitespace", Justification = "Add Whitespace to the dictionary if we already shipped, and it seems good.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope = "member", Target = "System.Xaml.XamlType.#TrimSurroundingWhitespace", MessageId = "Whitespace", Justification = "Add Whitespace to the dictionary if we already shipped, and it seems good.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope = "member", Target = "System.Xaml.XamlType.#TrimSurroundingWhitespaceCore", MessageId = "Whitespace", Justification = "Add Whitespace to the dictionary if we already shipped, and it seems good.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope = "member", Target = "System.Xaml.Schema.XaslType.#IsWhitespaceSignificantCollection", MessageId = "Whitespace", Justification = "Add Whitespace to the dictionary if we already shipped, and it seems good.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope = "member", Target = "System.Xaml.Schema.XaslType.#TrimSurroundingWhitespace", MessageId = "Whitespace", Justification = "Add Whitespace to the dictionary if we already shipped, and it seems good.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "System.Xaml.XamlReader.#IsEof", MessageId = "Eof", Justification = "Review Eof")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Scope = "type", Target = "System.Xaml.XamlNodeQueue", Justification = "This is unnecessarily limiting.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "System.Windows.Markup.PropertyDefinition.#Type", Justification = "Makes sense for our problem domain.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "System.Xaml.XamlMember.#Type", Justification = "Makes sense for our problem domain.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "System.Xaml.XamlReader.#Type", Justification = "Makes sense for our problem domain.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Scope = "member", Target = "System.Xaml.XamlReader.#Namespace", MessageId = "Namespace", Justification = "Works for our problem domain.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope = "member", Target = "System.Xaml.XamlType.#LookupIsWhitespaceSignificantCollection()", MessageId = "Whitespace", Justification = "Back compat.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope = "member", Target = "System.Xaml.XamlType.#LookupTrimSurroundingWhitespace()", MessageId = "Whitespace", Justification = "Back compat.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "SubClass", Scope = "member", Target = "System.Xaml.XamlLanguage.#SubClass", Justification = "Needs to match the capitalization used in XAML syntax.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace", Scope = "type", Target = "System.Windows.Markup.TrimSurroundingWhitespaceAttribute", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace", Scope = "type", Target = "System.Windows.Markup.WhitespaceSignificantCollectionAttribute", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "System.Windows.Markup.ArrayExtension.#Type", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "System.Windows.Markup.TypeExtension.#Type", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "nameSpace", Scope = "member", Target = "System.Windows.Markup.RootNamespaceAttribute.#.ctor(System.String)", Justification = "Inherited from Base.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "System.Windows.Markup.NameScopePropertyAttribute.#Type", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Uid", Scope = "type", Target = "System.Windows.Markup.UidPropertyAttribute")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Uid", Scope = "member", Target = "System.Xaml.XamlLanguage.#Uid")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Namescope", Scope = "member", Target = "System.Xaml.XamlObjectWriterSettings.#RegisterNamesOnExternalNamescope")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Uids", Scope = "member", Target = "System.Xaml.XamlReaderSettings.#IgnoreUidsOnPropertyElements")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arity", Scope = "member", Target = "System.Xaml.XamlSchemaContext.#SupportMarkupExtensionsWithDuplicateArity")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arity", Scope = "member", Target = "System.Xaml.XamlSchemaContextSettings.#SupportMarkupExtensionsWithDuplicateArity")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "Arity", Scope = "resource", Target = "ExceptionStringTable.resources")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#String")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Double")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Int32")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Object")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Char")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Single")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Int16")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Int64")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Decimal")]
#endregion

#region Microsoft.Usage Suppressions
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.ThreeItemList`1.Promote(System.Xaml.MS.Impl.ThreeItemList`1<T>):System.Void")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.ThreeItemList`1.SetCount(System.Int32):System.Void")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.ThreeItemList`1.Promote(System.Xaml.MS.Impl.FrugalListBase`1<T>):System.Void")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.ArrayItemList`1.Promote(System.Xaml.MS.Impl.SixItemList`1<T>):System.Void")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.ArrayItemList`1.SetCount(System.Int32):System.Void")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.SingleItemList`1.SetCount(System.Int32):System.Void")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.SixItemList`1.Promote(System.Xaml.MS.Impl.SixItemList`1<T>):System.Void")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.SixItemList`1.Promote(System.Xaml.MS.Impl.ThreeItemList`1<T>):System.Void")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.SixItemList`1.SetCount(System.Int32):System.Void")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.SixItemList`1.Promote(System.Xaml.MS.Impl.FrugalListBase`1<T>):System.Void")]
#endregion

#region Microsoft.Reliablity Suppressions
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope = "member", Target = "System.Xaml.Schema.ClrNamespace.#ParseClrNamespaceUri(System.String)", MessageId = "System.Reflection.Assembly.LoadWithPartialName", Justification = "Back compat.")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFile", Scope = "member", Target = "System.Xaml.ReflectionHelper.#LoadAssemblyHelper(System.String,System.String)")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadWithPartialName", Scope = "member", Target = "System.Xaml.XamlSchemaContext.#ResolveAssembly(System.String)", Justification = "Need to support load of assemblies from GAC by short name.")]
#endregion
