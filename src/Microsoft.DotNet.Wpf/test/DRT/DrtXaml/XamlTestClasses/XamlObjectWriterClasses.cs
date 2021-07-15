// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows.Markup;

namespace Test
{
    [DictionaryKeyPropertyAttribute("DKPProperty")]
    public class DKPClass
    {
        private string s = "DKPKey";

        public string DKPProperty { get { return s; } }
    }
}