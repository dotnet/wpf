// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Markup
{
    internal static class RestrictiveXamlContext
    {
        internal static readonly DependencyProperty IsRestrictivelyLoadedProperty =
            DependencyProperty.RegisterAttached(
                "IsRestrictivelyLoaded",
                typeof(bool),
                typeof(RestrictiveXamlContext),
                new PropertyMetadata(false));

        internal static bool GetIsRestrictivelyLoaded(DependencyObject element)
        {
            if (element == null)
            {
                return false;
            }

            return (bool)element.GetValue(IsRestrictivelyLoadedProperty);
        }

        internal static void SetIsRestrictivelyLoaded(DependencyObject element, bool value)
        {
            element?.SetValue(IsRestrictivelyLoadedProperty, value);
        }
    }
}
