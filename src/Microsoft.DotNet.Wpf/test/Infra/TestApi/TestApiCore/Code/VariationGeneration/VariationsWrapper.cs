// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Test.VariationGeneration
{
    /// <summary>
    /// Wrapper around IEnumerable that creates list of T instances
    /// and assigns those instances properties based on list of generic
    /// variations and reflection mapping metadata.
    /// <remarks>
    /// The wrapper is used to translate list of generic Variations generated
    /// by the model and containing key\value pairs of parameter name\value
    /// to list of strongly type variation definition instances.
    /// </remarks>
    /// </summary>
    internal class VariationsWrapper<T> : IEnumerable<T> where T : new()
    {
        IEnumerable<Variation> variations;
        Dictionary<string, PropertyInfo> propertiesMap;

        /// <summary>
        /// Initializes a single object from a variation.
        /// </summary>
        /// <param name="variation">The source variation.</param>
        /// <returns>The initialized object.</returns>
        public T AssignParameterValues(Variation variation)
        {
            T value = new T();
            foreach (string parameterName in variation.Keys)
            {
                if (propertiesMap.ContainsKey(parameterName))
                {
                    PropertyInfo propertyInfo = propertiesMap[parameterName];
                    propertyInfo.SetValue(value, variation[parameterName], null);
                }
            }
            return value;
        }

        /// <summary>
        /// Initializes new wrapper.
        /// </summary>
        /// <param name="propertiesMap">Map of properties to use when mapping 
        /// generic variation object to strongly typed T instance.</param>
        /// <param name="variations">List of generic variations to wrap.</param>
        public VariationsWrapper(Dictionary<string, PropertyInfo> propertiesMap, IEnumerable<Variation> variations)
        {
            this.variations = variations;
            this.propertiesMap = propertiesMap;
        }

        #region IEnumerable implementation
        public IEnumerator<T> GetEnumerator()
        {
            foreach (Variation variation in variations)
            {
                yield return AssignParameterValues(variation);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            foreach (Variation variation in variations)
            {
                yield return AssignParameterValues(variation);
            }
        }
        #endregion
    }
}
