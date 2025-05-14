// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: Interface for the asynchronous data dispatcher.
//
// Specs:       Asynchronous Data Model.mht
//

namespace MS.Internal.Data
{
    /// <summary> Interface for the asynchronous data dispatcher. </summary>
    internal interface IAsyncDataDispatcher
    {
        /// <summary> Add a request to the dispatcher's queue </summary>
        void AddRequest(AsyncDataRequest request);      // thread-safe

        /// <summary> Cancel all requests in the dispatcher's queue </summary>
        void CancelAllRequests();                       // thread-safe
    }
}
