// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Utility class for getting a lazily evaluated value.
    /// </summary>
    /// <remarks>
    /// The class is not thread-safe.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    internal class Lazy<T>
    {
        private Func<T> getValue;
        private T value;
        private bool gotValue;

        /// <summary>
        /// Lazy constructor.
        /// </summary>
        /// <remarks>
        /// The getValue callback will be called (at most) once, the
        /// first time the Value property is accessed.
        /// </remarks>
        /// <param name="getValue">Function for evaluating the value.</param>
        public Lazy(Func<T> getValue)
        {
            Debug.Assert(getValue != null);
            this.getValue = getValue;
            this.gotValue = false;
        }

        /// <summary>
        /// Non-lazy constructor.
        /// </summary>
        /// <param name="value"></param>
        public Lazy(T value)
        {
            this.value = value;
            this.gotValue = true;
        }

        /// <summary>
        /// Get the value.
        /// </summary>
        public T Value
        {
            get
            {
                if (!this.gotValue)
                {
                    this.value = getValue();
                    this.getValue = null; // no reason to hold on to reference
                    this.gotValue = true;
                }
                return this.value;
            }
        }
    }
}