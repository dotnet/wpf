// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#nullable disable// This file is used by Code Analysis to maintain SuppressMessage
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
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "MS.Internal.Xaml.Context.ObjectWriterFrame", Justification = "Non-Breaking")]

// New since v4 RTM:

// this is used by subclasses, bad FxCop detection
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "System.Xaml.MS.Impl.FrugalObjectList`1+Compacter", Justification = "Non-Breaking")]
#endregion

#region Microsoft.Naming Suppressions
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "System.Xaml.XamlReader.#IsEof", MessageId = "Eof", Justification = "Review Eof")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Scope = "type", Target = "System.Xaml.XamlNodeQueue", Justification = "This is unnecessarily limiting.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "System.Windows.Markup.PropertyDefinition.#Type", Justification = "Makes sense for our problem domain.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "System.Xaml.XamlMember.#Type", Justification = "Makes sense for our problem domain.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "System.Xaml.XamlReader.#Type", Justification = "Makes sense for our problem domain.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Scope = "member", Target = "System.Xaml.XamlReader.#Namespace", MessageId = "Namespace", Justification = "Works for our problem domain.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "System.Windows.Markup.ArrayExtension.#Type", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "System.Windows.Markup.TypeExtension.#Type", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "System.Windows.Markup.NameScopePropertyAttribute.#Type", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Uid", Scope = "type", Target = "System.Windows.Markup.UidPropertyAttribute", Justification = "Short for unique identifiers.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Uid", Scope = "member", Target = "System.Xaml.XamlLanguage.#Uid", Justification = "Short for unique identifiers.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Namescope", Scope = "member", Target = "System.Xaml.XamlObjectWriterSettings.#RegisterNamesOnExternalNamescope", Justification ="Will require changes in public API contract.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Uids", Scope = "member", Target = "System.Xaml.XamlReaderSettings.#IgnoreUidsOnPropertyElements", Justification = "Short for unique identifiers.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arity", Scope = "member", Target = "System.Xaml.XamlSchemaContext.#SupportMarkupExtensionsWithDuplicateArity", Justification ="Will require changes in public API contract.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arity", Scope = "member", Target = "System.Xaml.XamlSchemaContextSettings.#SupportMarkupExtensionsWithDuplicateArity", Justification ="Will require changes in public API contract.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "Arity", Scope = "resource", Target = "ExceptionStringTable.resources", Justification="Alligns with other instances of same resource.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#String", Justification="Will require changes in public API contract.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Double", Justification="Will require changes in public API contract.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Int32", Justification="Will require changes in public API contract.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Object", Justification="Will require changes in public API contract.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Char", Justification="Will require changes in public API contract.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Single", Justification="Will require changes in public API contract.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Int16", Justification="Will require changes in public API contract.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Int64", Justification="Will require changes in public API contract.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "System.Xaml.XamlLanguage.#Decimal", Justification="Will require changes in public API contract.")]
#endregion

#region Microsoft.Usage Suppressions
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.ThreeItemList`1.Promote(System.Xaml.MS.Impl.ThreeItemList`1<T>):System.Void", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.ThreeItemList`1.SetCount(System.Int32):System.Void", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.ThreeItemList`1.Promote(System.Xaml.MS.Impl.FrugalListBase`1<T>):System.Void", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.ArrayItemList`1.Promote(System.Xaml.MS.Impl.SixItemList`1<T>):System.Void", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.ArrayItemList`1.SetCount(System.Int32):System.Void", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.SingleItemList`1.SetCount(System.Int32):System.Void", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.SixItemList`1.Promote(System.Xaml.MS.Impl.SixItemList`1<T>):System.Void", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.SixItemList`1.Promote(System.Xaml.MS.Impl.ThreeItemList`1<T>):System.Void", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.SixItemList`1.SetCount(System.Int32):System.Void", Justification = "Kept for compatibility.")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "System.Xaml.MS.Impl.SixItemList`1.Promote(System.Xaml.MS.Impl.FrugalListBase`1<T>):System.Void", Justification = "Kept for compatibility.")]

#endregion
#region Microsoft.Reliablity Suppressions
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFile", Scope = "member", Target = "System.Xaml.ReflectionHelper.#LoadAssemblyHelper(System.String,System.String)", Justification = "Kept for compatibility.")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadWithPartialName", Scope = "member", Target = "System.Xaml.XamlSchemaContext.#ResolveAssembly(System.String)", Justification = "Need to support load of assemblies from GAC by short name.")]
#endregion
