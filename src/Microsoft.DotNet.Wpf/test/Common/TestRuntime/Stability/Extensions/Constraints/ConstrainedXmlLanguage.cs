// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using System.Collections;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class ConstrainedXmlLanguage : ConstrainedDataSource
    {
        public ConstrainedXmlLanguage()
        {
        }

        public override object GetData(DeterministicRandom random)
        {
            List<string> ietfLanguageTags = new List<string>();

            foreach (CultureInfo cultureInfo in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                ietfLanguageTags.Add(cultureInfo.IetfLanguageTag);
            }

            return XmlLanguage.GetLanguage(random.NextItem<string>(ietfLanguageTags));
        }
    }
}
