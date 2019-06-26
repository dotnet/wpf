// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace BamlTestClasses40
{
    public partial class ResourceResolution_OverWrite : Border
    {
        public ResourceResolution_OverWrite()
        {
            InitializeComponent();

            ResourceDictionary dict = this.Resources;

            dict["Test0"] = "Overwritten0";
            dict["Test1"] = "Overwritten1";
            dict["Test2"] = "Overwritten2";
            dict["Test3"] = "Overwritten3";
            dict["Test4"] = "Overwritten4";
        }
    }
}
