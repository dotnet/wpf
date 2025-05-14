// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The Stylus struct used to store Stylus cursor information.
    /// </summary>
    internal struct StylusDeviceInfo
    {
        public string   CursorName;
        public int      CursorId;
        public bool     CursorInverted;
        public StylusButtonCollection ButtonCollection;
    }
}

