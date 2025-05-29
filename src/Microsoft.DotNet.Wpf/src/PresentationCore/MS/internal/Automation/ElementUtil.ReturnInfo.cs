// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MS.Internal.Automation;

/// <summary>
/// Utility class for working with <see cref="AutomationPeer"/>.
/// </summary>
internal static partial class ElementUtil
{
    /// <summary>
    /// Wraps the return value and exception of an asynchronous operation.
    /// </summary>
    /// <typeparam name="TReturn"></typeparam>
    [StructLayout(LayoutKind.Auto)]
    private readonly struct ReturnInfo<TReturn>
    {
        /// <summary>
        /// The exception that was thrown during the operation, if any.
        /// </summary>
        public Exception? StoredException { get; init; }

        /// <summary>
        /// Gets a value indicating whether the operation has been completed or timed out.
        /// </summary>
        public bool Completed { get; init; }

        /// <summary>
        /// The return value of the operation, if it completed successfully.
        /// </summary>
        public TReturn Value { get; init; }

        /// <summary>
        /// Creates a new instance of <see cref="ReturnInfo{TReturn}"/> with the specified exception.
        /// </summary>
        /// <param name="exception">The exception that was thrown during the operation.</param>
        /// <returns>Returns a <see cref="ReturnInfo{TReturn}"/> object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReturnInfo<TReturn> FromException(Exception exception)
        {
            return new ReturnInfo<TReturn>
            {
                StoredException = exception,
                Completed = true,
                Value = default!
            };
        }

        /// <summary>
        /// Creates a new instance of <see cref="ReturnInfo{TReturn}"/> with the specified value.
        /// </summary>
        /// <param name="value">The return value of the operation.</param>
        /// <returns>Returns a <see cref="ReturnInfo{TReturn}"/> object.</returns>
        public static ReturnInfo<TReturn> FromResult(TReturn value)
        {
            return new ReturnInfo<TReturn>
            {
                StoredException = null,
                Completed = true,
                Value = value
            };
        }
    }
}
