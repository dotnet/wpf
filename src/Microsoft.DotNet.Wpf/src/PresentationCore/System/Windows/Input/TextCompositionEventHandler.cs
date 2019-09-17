// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: TextCompositionEventHandler delegate
//
//

using System;

namespace System.Windows.Input 
{
    /// <summary>
    ///     The delegate to use for handlers that receive TextCompositionEventArgs.
    /// </summary>
    /// <ExternalAPI Inherit="true"/>
    public delegate void TextCompositionEventHandler(object sender, TextCompositionEventArgs e);
}

