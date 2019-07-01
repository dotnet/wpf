// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Test.VariationGeneration
{
    

    /// <summary>
    /// A single value in the model
    /// </summary>
    internal class ValueCombination
    {
        public ValueCombination(ValueCombination combination)
        {
            this.parameterToValueMap = new Dictionary<int, int>(combination.ParameterToValueMap);
            this.State = combination.State;
            keys = new KeyCollection(parameterToValueMap.Keys);
        }

        public ValueCombination(IList<int> values, ParameterInteraction interaction)
        {
            if (values.Count != interaction.Parameters.Count)
            {
                throw new ArgumentOutOfRangeException("values", "values and interaction must be the same length.");
            }

            this.parameterToValueMap = new Dictionary<int, int>(interaction.Parameters.Count);
            for (int i = 0; i < values.Count; i++)
            {
                parameterToValueMap[interaction.Parameters[i]] = values[i];
            }
            State = ValueCombinationState.Uncovered;
            keys = new KeyCollection(parameterToValueMap.Keys);
        }

        public ValueCombinationState State { get; set; }

        private Dictionary<int, int> parameterToValueMap;
        /// <summary>
        /// Dictionary that maps a parameter to its value
        /// </summary>
        public IDictionary<int, int> ParameterToValueMap { get { return parameterToValueMap; } }
        

        public IEnumerable<int> Keys { get { return keys; } }

        public double Weight { get; set; }
        public object Tag { get; set; }

        private KeyCollection keys;

        private class KeyCollection : IEnumerable<int>
        {
            IEnumerator<int> enumerator;

            public KeyCollection(IEnumerable<int> keys)
            {
                enumerator = keys.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                enumerator.Reset();
                return enumerator;
            }

            public IEnumerator<int> GetEnumerator()
            {
                enumerator.Reset();
                return enumerator;
            }
        }
    }
}
