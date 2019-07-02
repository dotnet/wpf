// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.Threading;

    /// <summary>
    /// Object hosting the result of an asynchronous operation
    /// </summary>
    public class ImageComparisonResult : IAsyncResult
    {
        private int _index;
        private bool _isCompleted;
        private System.Threading.WaitHandle _waitHandle;
        private bool _succeeded;

        internal ImageComparisonResult(int index, WaitHandle waitHandle)
        {
            _index = index;
            _waitHandle = waitHandle;
        }

        /// <summary>
        /// Get the result of the operation. True for success, false for failure.
        /// </summary>
        public bool Succeeded
        {
            get { return _succeeded; }
            internal set { _succeeded = value; }
        }

        /// <summary>
        /// Get the index of the operation; unique ID so user can identify what enqueued operation failed
        /// </summary>
        public int Index
        {
            get { return _index; }
            internal set { _index = value; }
        }

        /// <summary>
        /// Inform the user if the operation has completed or was stopped by a failure.
        /// </summary>
        public bool IsCompleted
        {
            get { return _isCompleted; }
            internal set { _isCompleted = value; }
        }

        /// <summary>
        /// Report if the operation has been done synchrounously or not
        /// </summary>
        bool IAsyncResult.CompletedSynchronously
        {
            get { return _waitHandle == null; }
        }
        /// <summary>
        /// A unique ID that identify each operation
        /// </summary>
        object IAsyncResult.AsyncState
        {
            get { return _index; }
        }
        /// <summary>
        /// Retrieve the WaitHandle (null if synchronous operation) associated with the Result
        /// </summary>
        System.Threading.WaitHandle IAsyncResult.AsyncWaitHandle
        {
            get { return _waitHandle; }
        }
    }
}
