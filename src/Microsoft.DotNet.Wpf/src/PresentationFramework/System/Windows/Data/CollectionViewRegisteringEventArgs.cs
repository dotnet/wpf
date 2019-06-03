// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Arguments to the CollectionViewRegistering event (see BindingOperations).
//
// See spec at Cross-thread Collections.docx
//

using System;

namespace System.Windows.Data
{
    public class CollectionViewRegisteringEventArgs : EventArgs
    {
        internal CollectionViewRegisteringEventArgs(CollectionView view)
        {
            _view = view;
        }

        public CollectionView CollectionView
        {
            get { return _view; }
        }

        CollectionView _view;
    }
}
