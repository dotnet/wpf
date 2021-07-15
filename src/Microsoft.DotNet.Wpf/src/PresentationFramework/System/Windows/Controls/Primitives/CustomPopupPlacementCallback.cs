// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     Delegate used to provide custom positioning for a Popup.
    /// </summary>
    /// <param name="popupSize">The size of the Popup.</param>
    /// <param name="targetSize">The size of the PlacementTarget.</param>
    /// <param name="offset">The pre-computed offset based on HorizontalOffset and VerticalOffset.</param>
    /// <returns>
    ///     In priority order, the possible primary positions that the popup should be placed, relative to
    ///     the PlacementTarget. Each of these positions will be scored based on how much of the Popup is
    ///     on-screen. If the most on-screen position doesn't allow the popup to be completely on-screen,
    ///     then the Popup will be nudged to be fully-onscreen as best as possible.
    /// </returns>
    public delegate CustomPopupPlacement[] CustomPopupPlacementCallback(Size popupSize, Size targetSize, Point offset);
}
