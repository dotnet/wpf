// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Test.Elements
{
    public class NSGO : DependencyObject
    {
        public static List<string> GetStringList(DependencyObject obj)
        {
            return (List<string>)obj.GetValue(StringListProperty);
        }

        public static void SetStringList(DependencyObject obj, List<string> value)
        {
            obj.SetValue(StringListProperty, value);
        }

        // Using a DependencyProperty as the backing store for StringList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StringListProperty =
            DependencyProperty.RegisterAttached("StringList", typeof(List<string>), typeof(NSGO), new UIPropertyMetadata(new List<string>()));
    }
}
