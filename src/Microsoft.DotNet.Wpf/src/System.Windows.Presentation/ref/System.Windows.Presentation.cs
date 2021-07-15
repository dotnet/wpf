// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace System.Windows.Threading
{
    public static partial class DispatcherExtensions
    {
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static System.Windows.Threading.DispatcherOperation BeginInvoke(this System.Windows.Threading.Dispatcher dispatcher, System.Action action) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static System.Windows.Threading.DispatcherOperation BeginInvoke(this System.Windows.Threading.Dispatcher dispatcher, System.Action action, System.Windows.Threading.DispatcherPriority priority) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static void Invoke(this System.Windows.Threading.Dispatcher dispatcher, System.Action action) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static void Invoke(this System.Windows.Threading.Dispatcher dispatcher, System.Action action, System.TimeSpan timeout) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static void Invoke(this System.Windows.Threading.Dispatcher dispatcher, System.Action action, System.TimeSpan timeout, System.Windows.Threading.DispatcherPriority priority) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static void Invoke(this System.Windows.Threading.Dispatcher dispatcher, System.Action action, System.Windows.Threading.DispatcherPriority priority) { }
    }
    public static partial class TaskExtensions
    {
        public static System.Windows.Threading.DispatcherOperationStatus DispatcherOperationWait(this System.Threading.Tasks.Task @this) { throw null; }
        public static System.Windows.Threading.DispatcherOperationStatus DispatcherOperationWait(this System.Threading.Tasks.Task @this, System.TimeSpan timeout) { throw null; }
        public static bool IsDispatcherOperationTask(this System.Threading.Tasks.Task @this) { throw null; }
    }
}
