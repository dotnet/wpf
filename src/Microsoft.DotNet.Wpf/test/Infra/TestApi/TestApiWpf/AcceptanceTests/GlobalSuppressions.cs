// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project. 
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc. 
//
// To add a suppression to this file, right-click the message in the 
// Error List, point to "Suppress Message(s)", and click 
// "In Project Suppression File". 
// You do not need to add suppressions to this file manually. 

// AutomatedApplicationTestBase.#.ctor() calls a protected virtual method for test initialization.  
// Each subclass does custom initialization here and is the general pattern for xunit.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Scope = "member", Target = "Microsoft.Test.AcceptanceTests.AutomatedApplicationTestBase.#.ctor()")]

// Using xUnit Assert.Throws pattern here.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults", Scope = "member", Target = "Microsoft.Test.AcceptanceTests.WpfOutOfProcUnitTests.#ApplicationSettingsValidationTest()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults", Scope = "member", Target = "Microsoft.Test.AcceptanceTests.WpfInProcSeparateThreadCreateUnitTest.#ApplicationSettingsValidationTest()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults", Scope = "member", Target = "Microsoft.Test.AcceptanceTests.WpfInProcSameThreadUnitTests.#ApplicationSettingsValidationTest()")]

// Testing that an internal property is set correctly
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Type.InvokeMember", Scope = "member", Target = "Microsoft.Test.AcceptanceTests.AutomatedApplicationTestBase.#StartTestHelper()")]

