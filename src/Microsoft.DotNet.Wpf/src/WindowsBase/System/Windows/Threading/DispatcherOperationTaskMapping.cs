// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace System.Windows.Threading
{
    // An internal class used to store the operation that a task is
    // associated with.  Being internal helps prevent the implementation
    // detail that we store the DispatcherOperation in the async state of the
    // Task object from becoming a public API.  Instead, users can use the
    // Task extensions to officially get this information.
    internal class DispatcherOperationTaskMapping
    {
        public DispatcherOperationTaskMapping(DispatcherOperation operation)
        {
            Operation = operation;
        }
        
        public DispatcherOperation Operation {get; private set;}
    }
}

