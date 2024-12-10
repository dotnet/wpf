// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

[assembly: CLSCompliant(false)]

[assembly: InternalsVisibleTo($"PresentationBuildTasks, PublicKey={PublicKeys.MicrosoftShared}")]
[assembly: InternalsVisibleTo($"PresentationCore, PublicKey={PublicKeys.MicrosoftShared}")]
[assembly: InternalsVisibleTo($"PresentationFramework, PublicKey={PublicKeys.MicrosoftShared}")]
[assembly: InternalsVisibleTo($"PresentationUI, PublicKey={PublicKeys.MicrosoftShared}")]
[assembly: InternalsVisibleTo($"ReachFramework, PublicKey={PublicKeys.MicrosoftShared}")]
[assembly: InternalsVisibleTo($"System.Windows.Controls.Ribbon, PublicKey={PublicKeys.Ecma}")]
[assembly: InternalsVisibleTo($"System.Windows.Input.Manipulations, PublicKey={PublicKeys.Ecma}")]
[assembly: InternalsVisibleTo($"System.Windows.Presentation, PublicKey={PublicKeys.Ecma}")]
[assembly: InternalsVisibleTo($"System.Xaml, PublicKey={PublicKeys.Ecma}")]
[assembly: InternalsVisibleTo($"UIAutomationClient, PublicKey={PublicKeys.MicrosoftShared}")]
[assembly: InternalsVisibleTo($"UIAutomationClientSideProviders, PublicKey={PublicKeys.Ecma}")]
[assembly: InternalsVisibleTo($"UIAutomationProvider, PublicKey={PublicKeys.MicrosoftShared}")]
[assembly: InternalsVisibleTo($"UIAutomationTypes, PublicKey={PublicKeys.MicrosoftShared}")]
[assembly: InternalsVisibleTo($"WindowsBase, PublicKey={PublicKeys.MicrosoftShared}")]
[assembly: InternalsVisibleTo($"WindowsFormsIntegration, PublicKey={PublicKeys.MicrosoftShared}")]

[assembly: InternalsVisibleTo($"System.Windows.Primitives.Tests, PublicKey={PublicKeys.Open}")]
[assembly: InternalsVisibleTo($"PresentationCore.Tests, PublicKey={PublicKeys.Open}")]
