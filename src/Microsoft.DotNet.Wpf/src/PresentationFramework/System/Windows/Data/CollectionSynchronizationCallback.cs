// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Delegate used to synchronize access to multi-threaded collections.
//
// See spec at Cross-thread Collections.docx
//

using System;
using System.Collections;

namespace System.Windows.Data
{
    ///<summary>
    /// An application that wishes to allow WPF to participate in synchronized
    /// (multi-threaded) access to a collection can register a callback matching
    /// the CollectionSynchronizationCallback delegate.   WPF will then invoke
    /// the callback to access the collection.
    ///</summary>
    ///<param name="collection"> The collection that the caller intends to access. </param>
    ///<param name="context"> An object supplied by the application at registration
    ///     time.  See BindingOperations.EnableCollectionSynchronization.  </param>
    ///<param name="accessMethod"> The method that performs the caller's desired access. </param>
    ///<param name="writeAccess"/> True if the caller needs write access to the collection,
    ///     false if the caller needs only read access. </param>
    ///<notes>
    /// The method supplied by the application should do the following steps:
    ///     1. Determine the synchronization mechanism used by the application
    ///         to govern access to the given collection.   The context object
    ///         can be used to help with this step.
    ///     2. Ensure the desired access to the collection.  This is read-access
    ///         or write-access, depending on the value of writeAccess.
    ///     3. Invoke the access method.
    ///     4. Release the access to the collection, if appropriate.
    ///</notes>

    public delegate void CollectionSynchronizationCallback(
        IEnumerable collection,
        object      context,
        Action      accessMethod,
        bool        writeAccess
        );
}
