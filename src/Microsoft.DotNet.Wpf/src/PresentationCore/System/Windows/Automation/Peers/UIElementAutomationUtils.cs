// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Media;

namespace System.Windows.Automation.Peers;

/// <summary>
/// Provides utility methods shared by <see cref="UIElementAutomationPeer"/> and <see cref="UIElement3DAutomationPeer"/>.
/// </summary>
internal static class UIElementAutomationUtils
{
    /// <summary>
    /// Generic callback for the <see cref="Iterate{T}"/> function.
    /// </summary>
    internal delegate bool UIElementIteratorCallback<T>(ref T? capture, AutomationPeer peer);

    /// <summary>
    /// Iterates through the children of the given <paramref name="parent"/> and either attempts to create
    /// or retrieve the current <see cref="AutomationPeer"/> for the given <see cref="UIElement"/> / <see cref="UIElement3D"/>.
    /// </summary>
    internal static bool Iterate<T>(DependencyObject? parent, ref T? callbackParameter, UIElementIteratorCallback<T> callback)
    {
        bool done = false;

        if (parent is not null)
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childrenCount && !done; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                AutomationPeer? peer;

                if ((child is UIElement uiElement && (peer = uiElement.CreateAutomationPeer()) is not null) ||
                    (child is UIElement3D uiElement3D && (peer = uiElement3D.CreateAutomationPeer()) is not null))
                {
                    done = callback(ref callbackParameter, peer);
                }
                else
                {
                    done = Iterate(child, ref callbackParameter, callback);
                }
            }
        }

        return done;
    }
}
