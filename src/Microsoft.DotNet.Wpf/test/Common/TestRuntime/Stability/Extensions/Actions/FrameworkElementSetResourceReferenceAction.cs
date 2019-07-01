// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Reflection;
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class FrameworkElementSetResourceReferenceAction : SimpleDiscoverableAction
    {
        #region Public Members

        public FrameworkElement Element { get; set; }

        public bool IsApplyTemplate { get; set; }

        public double ResourceValue { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            if (IsApplyTemplate)
            {
                Element.ApplyTemplate();
            }
            else
            {
                FrameworkElement parent = Element.Parent as FrameworkElement;
                if (parent != null && GetInheritanceBehavior(Element) == InheritanceBehavior.Default && GetInheritanceBehavior(parent) == InheritanceBehavior.Default)
                {
                    ResourceDictionary resources = parent.Resources;

                    ResourceValue *= 10000.0;
                    resources["MyStressResource"] = ResourceValue;
                    Element.SetResourceReference(FrameworkElement.HeightProperty, "MyStressResource");

                    object loadedValue = Element.FindResource("MyStressResource");
                    if ((double)loadedValue != ResourceValue)
                    {
                        throw new Exception(string.Format("Loaded MyStressResource value:{0} is not the same as the original value:{1}.", (double)loadedValue, ResourceValue));
                    }
                }
            }
        }

        #endregion

        #region Private Members

        private InheritanceBehavior GetInheritanceBehavior(FrameworkElement element)
        {
            Type type = typeof(FrameworkElement);
            PropertyInfo propInfo = type.GetProperty("InheritanceBehavior", BindingFlags.NonPublic | BindingFlags.Instance);
            object val = propInfo.GetValue(element, new object[0]);
            return (InheritanceBehavior)val;
        }

        #endregion
    }
}
