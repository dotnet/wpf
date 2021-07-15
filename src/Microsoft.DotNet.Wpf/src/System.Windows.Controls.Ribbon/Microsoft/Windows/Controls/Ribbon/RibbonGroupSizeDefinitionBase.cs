// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    public abstract class RibbonGroupSizeDefinitionBase : Freezable
    {
        #region Public Properties

        public bool IsCollapsed
        {
            get { return (bool)GetValue(IsCollapsedProperty); }
            set { SetValue(IsCollapsedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsCollapsed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCollapsedProperty =
            DependencyProperty.Register("IsCollapsed", typeof(bool), typeof(RibbonGroupSizeDefinitionBase), new FrameworkPropertyMetadata(false));

        #endregion
    }
}