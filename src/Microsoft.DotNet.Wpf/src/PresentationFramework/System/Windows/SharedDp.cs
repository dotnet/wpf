// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



/***************************************************************************\
*
*
*
* Structure that holds information about a DependencyProperty that is to
* be shared between multiple instantiations of this template.
* (See OptimizedTemplateContent)
*
\***************************************************************************/

namespace System.Windows
{
    internal class SharedDp
    {
        internal SharedDp(
            DependencyProperty dp,
            object             value,
            string             elementName)
        {
            Dp = dp;
            Value = value;
            ElementName = elementName;
        }
        
        internal DependencyProperty Dp;
        internal object             Value;
        internal string             ElementName;
    }

}         


