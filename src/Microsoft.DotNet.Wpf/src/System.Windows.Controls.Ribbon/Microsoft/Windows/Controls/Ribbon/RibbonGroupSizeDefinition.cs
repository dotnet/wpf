// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    #region Using declarations

    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Markup;

    #endregion

    /// <summary>
    ///   The RibbonGroupSizeDefinition class is used to designate the sizes of Ribbon controls
    ///   within a RibbonGroup, as well as the visual 'collapsed' state of the RibbonGroup.
    /// </summary>
    [ContentProperty("ControlSizeDefinitions")]
    public class RibbonGroupSizeDefinition : RibbonGroupSizeDefinitionBase
    {
        #region Constructors

        public RibbonGroupSizeDefinition()
        {
            ControlSizeDefinitions = new RibbonControlSizeDefinitionCollection();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the ControlSizeDefinitions property. This ordered list of
        ///     ControlSizeDefinitions is representative of a size configuration for the RibbonGroup.
        /// </summary>
        public RibbonControlSizeDefinitionCollection ControlSizeDefinitions
        {
            get { return (RibbonControlSizeDefinitionCollection)GetValue(ControlSizeDefinitionsProperty); }
            set { SetValue(ControlSizeDefinitionsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ControlSizeDefinitions.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ControlSizeDefinitionsProperty =
            DependencyProperty.Register("ControlSizeDefinitions", 
            typeof(RibbonControlSizeDefinitionCollection), 
            typeof(RibbonGroupSizeDefinition), 
            new FrameworkPropertyMetadata(null));        

        #endregion

        #region Freezable

        protected override Freezable CreateInstanceCore()
        {
            return new RibbonGroupSizeDefinition();
        }

        #endregion
    }
}