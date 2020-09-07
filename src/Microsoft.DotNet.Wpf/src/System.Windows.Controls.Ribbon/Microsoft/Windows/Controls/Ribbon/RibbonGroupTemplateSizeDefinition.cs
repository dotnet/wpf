// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System.Windows;
using System.Windows.Markup;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    [ContentProperty("ContentTemplate")]
    public class RibbonGroupTemplateSizeDefinition : RibbonGroupSizeDefinitionBase
    {
        #region Public Properties

        public DataTemplate ContentTemplate
        {
            get { return (DataTemplate)GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContentTemplateProperty =
            DependencyProperty.Register("ContentTemplate",
                typeof(DataTemplate),
                typeof(RibbonGroupTemplateSizeDefinition),
                new FrameworkPropertyMetadata(null));

        #endregion

        #region Freezable

        protected override Freezable CreateInstanceCore()
        {
            return new RibbonGroupTemplateSizeDefinition();
        }

        #endregion
    }
}