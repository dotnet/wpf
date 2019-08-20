// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
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
            string s = SR.Get(value);
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
