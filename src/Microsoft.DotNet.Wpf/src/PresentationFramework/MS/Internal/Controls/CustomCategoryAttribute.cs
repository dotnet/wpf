// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using MS.Internal;
using System.Windows;
using System.Diagnostics;
using System.Globalization;

namespace System.Windows
{
    /// <summary>
    ///     Attribute which specifies additional category strings which can be localized:
    ///     Accessibility, Content, Navigation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
    internal sealed class CustomCategoryAttribute : CategoryAttribute
    {
        internal CustomCategoryAttribute(string name) : base(name)
        {
            Debug.Assert("Content".Equals(name, StringComparison.Ordinal) 
                      || "Accessibility".Equals(name, StringComparison.Ordinal) 
                      || "Navigation".Equals(name, StringComparison.Ordinal));
        }

        protected override string GetLocalizedString(string value)
        {
            // Return a localized version of the custom category
            if (String.Compare(value, "Content", StringComparison.Ordinal) == 0)
                return SR.Get(SRID.DesignerMetadata_CustomCategory_Content);
            else if(String.Compare(value, "Accessibility", StringComparison.Ordinal) == 0)
                return SR.Get(SRID.DesignerMetadata_CustomCategory_Accessibility);
            else /*if(String.Compare(value, "Navigation", StringComparison.Ordinal) == 0)*/
                return SR.Get(SRID.DesignerMetadata_CustomCategory_Navigation);
        }
    }
}
