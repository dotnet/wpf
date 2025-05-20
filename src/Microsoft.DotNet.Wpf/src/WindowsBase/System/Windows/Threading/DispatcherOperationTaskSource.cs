// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Windows.Threading;

/// <summary>
/// DispatcherOperation uses this class to access a TaskCompletionSource<T> without being a generic itself.
/// </summary>
internal abstract class DispatcherOperationTaskSource
{
    public abstract void Initialize(DispatcherOperation operation);
    public abstract Task GetTask();
    public abstract void SetCanceled();
    public abstract void SetResult(object result);
    public abstract void SetException(Exception exception);
}

internal sealed class DispatcherOperationTaskSource<TResult> : DispatcherOperationTaskSource
{
    private TaskCompletionSource<TResult> _taskCompletionSource;

    /// <summary>
    /// Create the underlying TaskCompletionSource and set the DispatcherOperation as the Task's AsyncState.
    /// </summary>
    /// <param name="operation"></param>
    public override void Initialize(DispatcherOperation operation)
    {
        Debug.Assert(_taskCompletionSource is null);

        _taskCompletionSource = new TaskCompletionSource<TResult>(new DispatcherOperationTaskMapping(operation));
    }

    public override Task GetTask()
    {
        Debug.Assert(_taskCompletionSource is not null);

        return _taskCompletionSource.Task;
    }

    public override void SetCanceled()
    {
        Debug.Assert(_taskCompletionSource is not null);

        _taskCompletionSource.SetCanceled();
    }

    public override void SetResult(object result)
    {
        Debug.Assert(_taskCompletionSource is not null);

        _taskCompletionSource.SetResult((TResult)result);
    }

    public void SetResult(TResult result)
    {
        Debug.Assert(_taskCompletionSource is not null);

        _taskCompletionSource.SetResult(result);
    }

    public override void SetException(Exception exception)
    {
        Debug.Assert(_taskCompletionSource is not null);

        _taskCompletionSource.SetException(exception);
    }
}
