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
    /// Wraps the exception from an asynchronous operation, tracking whether the operation has completed or timed out.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    private readonly struct ActionInfo
    {
        /// <summary>
        /// The exception that was thrown during the operation, if any.
        /// </summary>
        internal Exception? StoredException { get; init; }

        /// <summary>
        /// Gets a value indicating whether the operation has been completed or timed out.
        /// </summary>
        internal bool HasCompleted { get; init; }

        /// <summary>
        /// Creates a new instance of <see cref="ActionInfo"/> with the specified exception.
        /// </summary>
        /// <param name="exception">The exception that was thrown during the operation.</param>
        /// <returns>Returns a <see cref="ActionInfo"/> object.</returns>
        internal static ActionInfo FromException(Exception exception)
        {
            return new ActionInfo
            {
                StoredException = exception,
                HasCompleted = true
            };
        }

        /// <summary>
        /// Returns a singleton instance of <see cref="ActionInfo"/> signalizing successful completion of the operation.
        /// </summary>
        /// <returns>Returns a <see cref="ActionInfo"/> object.</returns>
        internal static ActionInfo Completed { get; } = new ActionInfo
        {
            StoredException = null,
            HasCompleted = true
        };
    }
}
