// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//

using System.ComponentModel;
using MS.Internal.PresentationCore;

namespace MS.Internal
{
    /// <summary>
    ///     Allows for localizing custom categories.
    /// </summary>
    /// <remarks>
    ///     This class could be shared amongst any of the assemblies if desired.
    /// </remarks>
    internal sealed class CustomCategoryAttribute : CategoryAttribute
    {
        internal CustomCategoryAttribute() : base()
        {
        }

        internal CustomCategoryAttribute(string category) : base(category)
        {
        }

        protected override string GetLocalizedString(string value)
        {
            string s = SR.GetResourceString(value);
            if (s != null)
            {
                return s;
            }
            else
            {
                return value;
            }
        }
    }
}
