// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows
{
    /// <summary>
    /// Provides extension methods relating to control focus.
    /// </summary>
    internal static class FocusExtensions
    {
        /// <summary>
        /// Checks if the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if the element can be focused.</returns>
        public static bool CanFocus(this UIElement e)
        {
            return e.Focusable && e.IsVisible && (e.IsEnabled || e.FocusableWhenNotEnabled);
        }

        /// <summary>
        /// Checks if the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if the element can be focused.</returns>
        public static bool CanFocus(this UIElement3D e)
        {
            return e.Focusable && e.IsVisible && (e.IsEnabled || e.FocusableWhenNotEnabled);
        }

        /// <summary>
        /// Checks if the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if the element can be focused.</returns>
        public static bool CanFocus(this ContentElement e)
        {
            return e.Focusable && (e.IsEnabled || e.FocusableWhenNotEnabled);
        }
    }
}
