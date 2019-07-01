// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.CustomTypes
{
    /// <summary>
    /// Custom ContentControl with a new property 'ExtraContent' that allows storage of any other disconnected tree.
    /// </summary>
    public class CustomContentControl : ContentControl
    {
        /// <summary>
        /// Default constructor
        /// </summary>    
        public CustomContentControl() { }

        /// <summary>
        /// ExtraContentProperty
        /// </summary>
        public static readonly DependencyProperty ExtraContentProperty = DependencyProperty.Register("ExtraContent", typeof(object), typeof(CustomContentControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// ExtraContent
        /// </summary>
        public object ExtraContent
        {
            get { return GetValue(ExtraContentProperty); }
            set { SetValue(ExtraContentProperty, value); }
        }
    }
}
