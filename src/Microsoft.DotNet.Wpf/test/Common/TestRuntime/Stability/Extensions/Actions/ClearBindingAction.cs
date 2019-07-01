// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action get a FrameworkElement which has binding, then remove the binding.
    /// </summary>
    public class ClearBindingAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public FrameworkElement FrameworkElement { get; set; }

        public bool IsRemoveAll { get; set; }

        public int RemoveIndex { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            if (IsRemoveAll)
            {
                BindingOperations.ClearAllBindings(FrameworkElement);
            }
            else
            {
                List<DependencyProperty> propertyList = PropertyForBinding.GetPropertiesForBinding(FrameworkElement);
                if (propertyList != null && propertyList.Count > 0)
                {
                    RemoveIndex %= propertyList.Count;
                    BindingOperations.ClearBinding(FrameworkElement, propertyList[RemoveIndex]);
                }
            }
        }

        #endregion
    }
}
