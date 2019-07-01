// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Microsoft.Test.VariationGeneration
{
    /// <summary>
    /// Represents a single value in a parameter.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public class ParameterValue<T> : ParameterValueBase
    {
        /// <summary>
        /// Initializes a new value with the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public ParameterValue(T value) : this(value, null, 1.0)
        {
        }

        /// <summary>
        /// Initializes a new value with the specified value and an expected result.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="tag">A user defined tag.</param>
        public ParameterValue(T value, object tag) : this(value, tag, 1.0)
        {
        }

        /// <summary>
        /// Initializes a new value with the specified value and weight.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="weight">The weight of the value.</param>
        public ParameterValue(T value, double weight) : this(value, null, weight)
        {
        }

        /// <summary>
        /// Initializes a new value with the specified value, tag, and weight.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="tag">A user-defined tag.</param>
        /// <param name="weight">The weight of the value.</param>
        public ParameterValue(T value, object tag, double weight)
        {
            Value = value;
            Tag = tag;
            Weight = weight;
        }

        /// <summary>
        /// The actual value.
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Returns the value that this ParameterValue represents.
        /// </summary>
        /// <returns>The value.</returns>
        public override object GetValue()
        {
            return Value;
        }
    }
}
