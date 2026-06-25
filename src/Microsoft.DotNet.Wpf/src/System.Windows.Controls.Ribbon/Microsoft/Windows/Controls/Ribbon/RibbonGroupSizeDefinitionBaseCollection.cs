// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#region Using declarations

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    #endregion

    /// <summary>
    ///   Provides a familiar name for XAML usage for a collection of RibbonGroupSizeDefinitions.
    /// </summary>
    public class RibbonGroupSizeDefinitionBaseCollection : FreezableCollection<RibbonGroupSizeDefinitionBase>
    {
        #region Freezable

        protected override Freezable CreateInstanceCore()
        {
            return new RibbonGroupSizeDefinitionBaseCollection();
        }

        #endregion
    }
}