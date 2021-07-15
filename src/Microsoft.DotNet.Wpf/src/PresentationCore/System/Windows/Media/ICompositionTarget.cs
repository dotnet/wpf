// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      Definition of the ICompositionTarget interface used to register
//      composition targets with the MediaContext.
//

using System;
using System.Diagnostics;
using System.Windows.Media.Composition;

namespace System.Windows.Media
{
    /// <summary>
    /// With this interface we register CompositionTargets with the
    /// MediaContext.
    /// </summary>
    internal interface ICompositionTarget : IDisposable
    {
        void Render(bool inResize, DUCE.Channel channel);
        void AddRefOnChannel(DUCE.Channel channel, DUCE.Channel outOfBandChannel);
        void ReleaseOnChannel(DUCE.Channel channel, DUCE.Channel outOfBandChannel);
    }
}

