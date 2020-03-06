// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System.Collections.Specialized;
using System.Windows;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    public class RibbonControlSizeDefinitionCollection : FreezableCollection<RibbonControlSizeDefinition>
    {
        #region Freezable

        protected override Freezable CreateInstanceCore()
        {
            return new RibbonControlSizeDefinitionCollection();
        }

        #endregion
    }
}