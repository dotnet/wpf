// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action get a FrameworkElement which can be binding, then set a binding to it.
    /// </summary>
    [TargetTypeAttribute(typeof(SetBindingAction))]
    public class SetBindingAction : DiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public FrameworkElement FrameworkElement { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent=true)]
        public Binding Binding { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public CLRDataItem CLRData { get; set; }

        public int RandomSelect { get; set; }

        public bool IsXMLBinding { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            //Select a DependencyPropery for binding
            int index = RandomSelect % dpList.Count;
            DependencyProperty propertyForBinding = dpList[index];
            if (IsXMLBinding)
            {
                XMLBinding(propertyForBinding);
            }
            else
            {
                CLRBinding(propertyForBinding);
            }
        }

        public override bool CanPerform()
        {
            dpList = PropertyForBinding.GetPropertiesForBinding(FrameworkElement);
            return dpList != null && dpList.Count > 0;
        }

        #endregion

        #region Private Data

        private List<DependencyProperty> dpList = new List<DependencyProperty>();

        private void CLRBinding(DependencyProperty propertyForBinding)
        {
            //Set the binding source
            Binding.Source = CLRData;

            //Set the binding path
            string path = "";
            if (propertyForBinding.PropertyType == typeof(int))
            {
                path = "IntegerValue";
            }
            if (propertyForBinding.PropertyType == typeof(bool))
            {
                path = "BooleanValue";
            }
            if (propertyForBinding.PropertyType == typeof(double))
            {
                path = "DoubleValue";
            }
            if (propertyForBinding.PropertyType == typeof(float))
            {
                path = "FloatValue";
            }
            if (propertyForBinding.PropertyType == typeof(string))
            {
                path = "StringValue";
            }

            Binding.Path = new PropertyPath(path);

            //Set Binding to the Element
            BindingOperations.SetBinding(FrameworkElement, propertyForBinding, Binding);
        }

        private void XMLBinding(DependencyProperty propertyForBinding)
        {
            //Set the binding source
            Binding.Source = CLRData.ToXmlDocument();

            //Set the binding path
            string xpath = "";
            if (propertyForBinding.PropertyType == typeof(string))
            {
                xpath = "CLRDataItem/StringValue";
            }
            if (propertyForBinding.PropertyType == typeof(int))
            {
                xpath = "CLRDataItem/IntegerValue";
            }
            if (propertyForBinding.PropertyType == typeof(bool))
            {
                xpath = "CLRDataItem/@BooleanValue";
            }
            if (propertyForBinding.PropertyType == typeof(double))
            {
                xpath = "CLRDataItem/@DoubleValue";
            }
            if (propertyForBinding.PropertyType == typeof(float))
            {
                xpath = "CLRDataItem/@FloatValue";
            }
            Binding.XPath = xpath;

            //Set Binding to the Element
            BindingOperations.SetBinding(FrameworkElement, propertyForBinding, Binding);
        }

        #endregion
    }
}
