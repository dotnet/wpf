// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MS.Internal
{

    internal enum LocalizationDirectivesToLocFile
    {
        None,            // No Localization file generated.
        CommentsOnly,    // Put the Localization comments to .loc file.
        All,             // Put the localization comments and attributes to .loc file.
        Unknown
    }
}

