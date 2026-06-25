// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: Delegate for a TextChangedEvent fired on a TextContainer.
//

namespace System.Windows.Documents
{
    /// <summary>
    ///  The TextChangedEventHandler delegate is called with TextContainerChangedEventArgs every time
    ///  content is added to or removed from the TextContainer
    /// </summary>
    internal delegate void TextContainerChangedEventHandler(object sender, TextContainerChangedEventArgs e);
}
