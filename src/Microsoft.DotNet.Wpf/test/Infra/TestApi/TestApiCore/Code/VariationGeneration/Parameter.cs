// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Test.VariationGeneration
{
    /// <summary>
    /// Represents a single variable and its values in the model whose values are of the specified type.
    /// Combinations of these values are used in the combinatorial generation of variations by the <see cref="Model{T}"/>.
    /// </summary>
    /// <remarks>
    /// Exhaustively testing all possible inputs to any nontrivial software component is generally not possible
    /// because of the enormous number of variations. Combinatorial testing is one approach to achieve high coverage
    /// with a much smaller set of variations. Pairwise, the most common combinatorial strategy, tests every possible 
    /// pair of values. Higher orders of combinations (three-wise, four-wise, and so on) can also be used for higher coverage
    /// at the expense of more variations. See <a href="http://pairwise.org">Pairwise Testing</a> and 
    /// <a href="http://www.pairwise.org/docs/pnsqc2006/PNSQC%20140%20-%20Jacek%20Czerwonka%20-%20Pairwise%20Testing%20-%20BW.pdf">
    /// Pairwise Testing in Real World</a> for more resources.
    /// </remarks>
    /// <typeparam name="T">The type of values that represent this parameter.</typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type", Target = "Microsoft.Test.VariationGeneration.Parameter", Justification = "The suggested name ParameterCollection is confusing.")]
    public class Parameter<T> : ParameterBase, IList<ParameterValue<T>>
    {
        /// <summary>
        /// Initializes a new instance of the parameter class using the specified name.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        public Parameter(string name) : base(name)
        {
        }

        /// <summary>
        /// Returns the value of this parameter in this variation.
        /// </summary>
        /// <param name="v">The variation.</param>
        /// <returns>The value.</returns>
        public T GetValue(Variation v)
        {
            return (T)v[Name];
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="Parameter{T}"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="Parameter{T}"/>.</param>
        /// <returns>The index of the item (if the item is found in the list); otherwise, -1.</returns>
        public int IndexOf(ParameterValue<T> item)
        {
            return values.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item into the <see cref="Parameter{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index where the item should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="Parameter{T}"/>.</param>
        public void Insert(int index, ParameterValue<T> item)
        {
            values.Insert(index, item);
        }

        /// <summary>
        /// Removes the <see cref="Parameter{T}"/> value at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item that should be removed.</param>
        public void RemoveAt(int index)
        {
            values.RemoveAt(index);
        }

        /// <summary>
        /// Gets or sets the value at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public ParameterValue<T> this[int index]
        {
            get
            {
                return values[index];
            }
            set
            {
                values[index] = value;
            }
        }

        /// <summary>
        /// Adds a value to the <see cref="Parameter{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="Parameter{T}"/>.</param>
        public void Add(ParameterValue<T> item)
        {
            values.Add(item);
        }

        /// <summary>
        /// Adds a value to the <see cref="Parameter{T}"/>. This value is wrapped in a <see cref="ParameterValue{T}"/>.
        /// </summary>
        /// <param name="item">The value to wrap and add.</param>
        public void Add(T item)
        {
            values.Add(new ParameterValue<T>(item));
        }

        /// <summary>
        /// Removes all values from the <see cref="Parameter{T}"/>.
        /// </summary>
        public void Clear()
        {
            values.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="Parameter{T}"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="Parameter{T}"/>.</param>
        /// <returns>true if the value is found in the <see cref="Parameter{T}"/>; otherwise, false.</returns>
        public bool Contains(ParameterValue<T> item)
        {
            return values.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="Parameter{T}"/> to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from <see cref="Parameter{T}"/>. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in the array where copying begins.</param>
        public void CopyTo(ParameterValue<T>[] array, int arrayIndex)
        {
            values.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns false.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific value from the <see cref="Parameter{T}"/>.
        /// </summary>
        /// <param name="item">The value to remove from the <see cref="Parameter{T}"/>.</param>
        /// <returns>true if the value was successfully removed from the <see cref="Parameter{T}"/>; otherwise, false. This method also returns false if the value is not found in the original <see cref="Parameter{T}"/>.</returns>
        public bool Remove(ParameterValue<T> item)
        {
            return values.Remove(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="Parameter{T}"/>.
        /// </summary>
        /// <returns>An IEnumerator(T) that can be used to iterate through the collection.</returns>
        public IEnumerator<ParameterValue<T>> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        /// <summary>
        /// The number of values in this parameter.
        /// </summary>
        public override int Count
        {
            get { return values.Count; }
        }

        /// <summary>
        /// Returns the item at the given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public override object GetAt(int index)
        {
            return values[index];
        }

        private List<ParameterValue<T>> values = new List<ParameterValue<T>>();
    }
}
