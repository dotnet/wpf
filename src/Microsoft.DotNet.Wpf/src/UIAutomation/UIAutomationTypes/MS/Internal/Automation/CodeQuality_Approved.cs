// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "System.Windows.Automation.TextPatternIdentifiers.OverlineStyleAttribute" )]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "System.Windows.Automation.TextPatternIdentifiers.OverlineColorAttribute" )]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "System.Windows.Automation.AutomationElementIdentifiers.IsOffscreenProperty" )]
// Approved by avsec.
[module: SuppressMessage("Microsoft.MSInternal", "CA900:AptcaAssembliesShouldBeReviewed")]
// The rule doesn't apply to non-public namespaces
[module: SuppressMessage("Microsoft.MSInternal", "CA904:DeclareTypesInMicrosoftOrSystemNamespace", Scope = "namespace", Target = "MS.Internal")]
[module: SuppressMessage("Microsoft.MSInternal", "CA904:DeclareTypesInMicrosoftOrSystemNamespace", Scope = "namespace", Target = "MS.Win32")]
// Reviewed by WinFX team and this case is OK
[module: SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Scope = "type", Target = "System.Windows.Automation.Text.FlowDirections")]
// Reviewed by atgarch and these enums are OK
[module: SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames", Scope = "type", Target = "System.Windows.Automation.TreeScope")]
[module: SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames", Scope = "type", Target = "System.Windows.Automation.SupportedTextSelection")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "System.Windows.Rect.Offset(System.Windows.Rect,System.Double,System.Double):System.Windows.Rect")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "System.Windows.Rect.Offset(System.Double,System.Double):System.Void")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "System.Windows.Point.Offset(System.Double,System.Double):System.Void")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "System.Windows.Automation.Text.CapStyle.Unicase")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "ExceptionStringTable.resources", MessageId = "classname")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "ExceptionStringTable.resources", MessageId = "imagename")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "StringTable.resources", MessageId = "dataitem")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "StringTable.resources", MessageId = "datagrid")]
// Reviewed and these are correctly suffixed
[module: SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Scope = "type", Target = "System.Windows.Automation.AutomationTextAttribute")]
[module: SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes", Scope = "type", Target = "System.Windows.Automation.AutomationIdentifier")]
// Not required for non-public code
[module: SuppressMessage("Microsoft.MSInternal", "CA905:SystemAndMicrosoftNamespacesRequireApproval", Scope = "namespace", Target = "System.Windows.Automation")]
[module: SuppressMessage("Microsoft.MSInternal", "CA905:SystemAndMicrosoftNamespacesRequireApproval", Scope = "namespace", Target = "System.Windows.Automation.Provider")]
[module: SuppressMessage("Microsoft.MSInternal", "CA905:SystemAndMicrosoftNamespacesRequireApproval", Scope = "namespace", Target = "System.Windows.Input")]
[module: SuppressMessage("Microsoft.MSInternal", "CA905:SystemAndMicrosoftNamespacesRequireApproval", Scope = "namespace", Target = "System.Windows.Automation.Text")]
[module: SuppressMessage("Microsoft.MSInternal", "CA905:SystemAndMicrosoftNamespacesRequireApproval", Scope = "namespace", Target = "System.Windows.Automation")]

// The method uses a large switch as a covertion table.  Safe to exclude.
[module: SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope="member", Target="System.Windows.Input.KeyInterop.KeyFromVirtualKey(System.Int32):System.Windows.Input.Key")]
[module: SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope="member", Target="System.Windows.Input.KeyInterop.VirtualKeyFromKey(System.Windows.Input.Key):System.Int32")]

//**************************************************************************************************************************
// Non hyphenated words are Microsoft style.
//***************************************************************************************************************************
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ExceptionStringTable.resources", MessageId="nonenabled")]
