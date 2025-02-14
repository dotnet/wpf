// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Xaml.Schema
{
    [Flags]
    public enum AllowedMemberLocations
    {
        None = 0,
        Attribute = 1,
        MemberElement = 2,
        Any = Attribute | MemberElement,
    }
}
