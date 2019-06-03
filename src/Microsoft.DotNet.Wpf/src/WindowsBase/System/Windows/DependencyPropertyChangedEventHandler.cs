// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows
{
    /// <summary>
    ///     Represents the method that will handle the event raised when a
    ///     DependencyProperty is changed on a DependencyObject.
    /// </summary>
    public delegate void DependencyPropertyChangedEventHandler(object sender, DependencyPropertyChangedEventArgs e);
}

