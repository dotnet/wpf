// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Markup.Localizer
{
    /// <summary>
    /// The EventArgs for the BamlLocalizer.ErrorNotify event. 
    /// </summary>
    public class BamlLocalizerErrorNotifyEventArgs : EventArgs
    {        BamlLocalizableResourceKey _key;    // The key of the localizable resources related to the error 
        BamlLocalizerError       _error;    // The error code. 
        
        internal BamlLocalizerErrorNotifyEventArgs(BamlLocalizableResourceKey key, BamlLocalizerError error)
        {
            _key = key; 

            _error = error;
        }

        /// <summary>
        /// The key of the BamlLocalizableResource related to the error
        /// </summary>
        public BamlLocalizableResourceKey Key { get { return _key; } }

        /// <summary>
        /// The error encountered by BamlLocalizer
        /// </summary>        
        public BamlLocalizerError Error { get { return _error; } }        
    }
}
