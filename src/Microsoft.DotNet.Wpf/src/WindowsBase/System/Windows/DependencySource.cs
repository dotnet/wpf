// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace System.Windows
{
    /// <summary>
    ///     Represents dependency scope of an <see cref="Expression"/>
    /// </summary>
    /// <remarks>
    ///     Expressions are responsible for propagating invalidation to
    ///     dependents when a property changes. The property that changes is
    ///     known as the "source".
    /// </remarks>
    internal sealed class DependencySource
    {
        /// <summary>
        ///     Dependency source construction
        /// </summary>
        /// <param name="d">DependencyObject source</param>
        /// <param name="dp">Property source</param>
        public DependencySource(DependencyObject d, DependencyProperty dp)
        {
            _d = d;
            _dp = dp;
        }

        /// <summary>
        ///     DependencyObject source
        /// </summary>
        public DependencyObject DependencyObject
        {
            get { return _d; }
        }

        /// <summary>
        ///     Property source
        /// </summary>
        public DependencyProperty DependencyProperty
        {
            get { return _dp; }
        }

        private DependencyObject _d;
        private DependencyProperty _dp;
    }
}
