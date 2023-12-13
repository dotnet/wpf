﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BamlTestClasses40
{
    public partial class ResourceResolution_Insert : Border
    {
        public ResourceResolution_Insert()
        {
            InitializeComponent();

            _stackPanel.Resources["Test0"] = "Error!  Resource inserted at runtime";
            _stackPanel.Resources["Test1"] = "Error!  Resource inserted at runtime";
            _stackPanel.Resources["Test2"] = "Error!  Resource inserted at runtime";
            _stackPanel.Resources["Test3"] = "Error!  Resource inserted at runtime";
            _stackPanel.Resources["Test4"] = "Error!  Resource inserted at runtime";
        }
    }
}
