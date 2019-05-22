// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Event args for the (internal) InheritedPropertyChanged event
//

using System;
using System.Windows;

namespace MS.Internal
{
    // Event args for the (internal) InheritedPropertyChanged event
    internal class InheritedPropertyChangedEventArgs : EventArgs
    {
        internal InheritedPropertyChangedEventArgs(ref InheritablePropertyChangeInfo info)
        {
            _info = info;
        }

        internal InheritablePropertyChangeInfo Info
        {
            get { return _info; }
        }

        private InheritablePropertyChangeInfo _info;
    }

    // Handler delegate for the (internal) InheritedPropertyChanged event
    internal delegate void InheritedPropertyChangedEventHandler(object sender, InheritedPropertyChangedEventArgs e);
}
