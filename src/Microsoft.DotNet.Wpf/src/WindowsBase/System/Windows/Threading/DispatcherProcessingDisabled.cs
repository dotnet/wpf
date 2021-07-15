// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Threading
{
    /// <summary>
    ///     A structure that allows for dispatcher processing to be
    ///     enabled after a call to Dispatcher.DisableProcessing.
    /// </summary>
    public struct DispatcherProcessingDisabled : IDisposable
    {
        /// <summary>
        ///     Reenable processing in the dispatcher.
        /// </summary>
        public void Dispose()
        {
            if(_dispatcher != null)
            {
                _dispatcher.VerifyAccess();
                
                _dispatcher._disableProcessingCount--;
                _dispatcher = null;
            }
        }

        /// <summary>
        ///     Checks whether this object is equal to another
        ///     DispatcherProcessingDisabled object.
        /// </summary>
        /// <param name="obj">
        ///     Object to compare with.
        /// </param>
        /// <returns>
        ///     Returns true when the object is equal to the specified object,
        ///     and false otherwise.
        /// </returns>
        public override bool Equals(object obj)
        {
            if ((null == obj) || !(obj is DispatcherProcessingDisabled))
                return false;

            return (this._dispatcher == ((DispatcherProcessingDisabled)obj)._dispatcher);
        }

        /// <summary>
        /// Compute hash code for this object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode( )
        {
            return base.GetHashCode();
        }

        /// <summary>
        ///     Compare two DispatcherProcessingDisabled instances for equality.
        /// </summary>
        /// <param name="left">
        ///     left operand
        /// </param>
        /// <param name="right">
        ///     right operand
        /// </param>
        /// <returns>
        ///     Whether or not two operands are equal.
        /// </returns>
        public static bool operator ==(DispatcherProcessingDisabled left, DispatcherProcessingDisabled right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///     Compare two DispatcherProcessingDisabled instances for inequality.
        /// </summary>
        /// <param name="left">
        ///     left operand
        /// </param>
        /// <param name="right">
        ///     right operand
        /// </param>
        /// <returns>
        ///     Whether or not two operands are equal.
        /// </returns>
        public static bool operator !=(DispatcherProcessingDisabled left, DispatcherProcessingDisabled right)
        {
            return !(left.Equals(right));
        }

        internal Dispatcher _dispatcher; // set by Dispatcher
    }
}

