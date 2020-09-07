// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace System.Windows.Threading
{
    // DispatcherOperation uses this class to access a TaskCompletionSource<T>
    // without being a generic iteself.
    internal abstract class DispatcherOperationTaskSource
    {
        public abstract void Initialize(DispatcherOperation operation);
        public abstract Task GetTask();
        public abstract void SetCanceled();
        public abstract void SetResult(object result);
        public abstract void SetException(Exception exception);
    }

    internal class DispatcherOperationTaskSource<TResult> : DispatcherOperationTaskSource
    {
        // Create the underlying TaskCompletionSource and set the
        // DispatcherOperation as the Task's AsyncState.
        public override void Initialize(DispatcherOperation operation)
        {
            if(_taskCompletionSource != null)
            {
                throw new InvalidOperationException();
            }
            
            _taskCompletionSource = new TaskCompletionSource<TResult>(new DispatcherOperationTaskMapping(operation));
        }

        public override Task GetTask()
        {
            if(_taskCompletionSource == null)
            {
                throw new InvalidOperationException();
            }

            return _taskCompletionSource.Task;
        }
        
        public override void SetCanceled()
        {
            if(_taskCompletionSource == null)
            {
                throw new InvalidOperationException();
            }

            _taskCompletionSource.SetCanceled();
        }
        
        public override void SetResult(object result)
        {
            if(_taskCompletionSource == null)
            {
                throw new InvalidOperationException();
            }

            _taskCompletionSource.SetResult((TResult)result);
        }
        
        public override void SetException(Exception exception)
        {
            if(_taskCompletionSource == null)
            {
                throw new InvalidOperationException();
            }

            _taskCompletionSource.SetException(exception);
        }

        private TaskCompletionSource<TResult> _taskCompletionSource;
    }
}
