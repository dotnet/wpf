// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Windows
{
    internal static class ExceptionExtensions
    {
        /// <summary>
        ///  Returns <see langword="true"/> if the exception is an exception that isn't recoverable and/or a likely
        ///  bug in our implementation.
        /// </summary>
        public static bool IsCriticalException(this Exception ex)
            => ex is NullReferenceException
                or StackOverflowException
                or OutOfMemoryException
                or ThreadAbortException
                or IndexOutOfRangeException
                or AccessViolationException;
    }
}
