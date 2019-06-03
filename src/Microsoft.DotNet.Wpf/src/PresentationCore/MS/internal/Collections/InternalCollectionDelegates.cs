// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Event handlers used internally to track changes to our
//              generated collection classes.
//              
//

namespace MS.Internal.Collections
{
    internal delegate void ItemInsertedHandler(object sender, object item);
    internal delegate void ItemRemovedHandler(object sender, object item);
}

