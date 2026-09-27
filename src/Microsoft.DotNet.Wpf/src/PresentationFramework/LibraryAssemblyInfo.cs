// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PresentationFramework.Fluent.Tests, PublicKey=00000000000000000400000000000000")]
// The unit test assembly is signed with the shared WCP key, not with the ECMA placeholder key.
[assembly: InternalsVisibleTo("PresentationFramework.Tests, PublicKey=" + MS.Internal.PresentationFramework.BuildInfo.WCP_PUBLIC_KEY_STRING)]
