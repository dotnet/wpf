// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//
//  Option flags that can be set in an Avalon application container file.
//
//  Every compound file (container) that contains an Avalon application is
//  stamped with a GUID that identifies the file as containing an Avalon
//  application. The top DWORD of the GUID (the Data1 member of the GUID
//  structure) is treated as a set of bit flags that specify various options
//  in how the container is to be launched.
//
//  Neither the base GUID nor any of its option-flagged variants is ever used
//  by COM. It's not registered in the registry, and CoCreateInstance is never
//  called on it. It's really just a magic number identifying this container as
//  an Avalon application, together with a set of option flags.
//
//
//


using System;

using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    #region ContainerFlags enumeration

    /// <summary></summary>
    [FriendAccessAllowed] // Built into Base, used by Framework.
    [Flags]
    internal enum ContainerFlags
    {
        /// <summary></summary>
        HostInBrowser = 0x01,

        /// <summary></summary>
        Writable      = 0x02,

        /// Remove this after transition to Metro
        /// 0x04 is skipped just in case a new flag needs to be added before we have chance to remove Metro
        Metro = 0x08,

        /// <summary></summary>
        ExecuteInstrumentation = 0x010
    }

    #endregion ContainerFlags enumeration
}
