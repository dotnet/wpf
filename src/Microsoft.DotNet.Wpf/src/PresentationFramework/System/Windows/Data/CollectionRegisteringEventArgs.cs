// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Arguments to the CollectionRegistering event (see BindingOperations).
//
// See spec at Cross-thread Collections.docx
//

using System;
using System.Collections;

namespace System.Windows.Data
{
    public class CollectionRegisteringEventArgs : EventArgs
    {
        internal CollectionRegisteringEventArgs(IEnumerable collection, object parent=null)
        {
            _collection = collection;
            _parent = parent;
        }

        public IEnumerable Collection { get { return _collection; } }

        public object Parent { get { return _parent; } }

        IEnumerable _collection;
        object _parent;
    }
}
