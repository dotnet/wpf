// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections;
using System.Collections.Specialized;

namespace Microsoft.Test.Stability.Extensions.State
{
    /// <summary>
    /// Dictionary of Typed Lists provides essential services for managing multiple instances of value objects associated with a 
    /// given key. This is relevant for managing state of non-"surface area"-able objects.
    /// </summary>
    [Serializable]
    public class TypedListDictionary
    {
        #region Private Data

        private IDictionary dictionary = new HybridDictionary();

        #endregion

        #region Public Members

        /// <summary>
        /// Adds an object into the list corresponding to the specified type bucket
        /// </summary>
        /// <param name="t">Key type</param>
        /// <param name="o">value to be added to list</param>
        public void Add(Type t, Object o)
        {
            ArrayList list = dictionary[t] as ArrayList;
            if (list == null)
            {
                list = new ArrayList();
                dictionary.Add(t, list);
            }
            list.Add(o);
        }

        /// <summary>
        /// Removes an object from the list corresponding to the specified type bucket
        /// </summary>
        /// <param name="t">Key type</param>
        /// <param name="o">Value to be removed from list</param>
        /// <returns>true if object was successfully removed</returns>
        public bool Remove(Type t, Object o)
        {
            ArrayList list = dictionary[t] as ArrayList;
            if ((list == null) || (!list.Contains(o)))
            {
                return false;
            }
            else
            {
                list.Remove(o);
                return true;
            }
        }

        /// <summary>
        /// Return a readonly proxy to the list so that callers cannot bypass
        /// the add api and currupt the state of the dictionary
        /// </summary>
        /// <param name="t">Type key for retrieval</param>
        /// <returns>a readonly list of Objects of requested type</returns>
        public ArrayList GetObjectsOfType(Type t)
        {
            return ArrayList.ReadOnly(((ArrayList)dictionary[t]));
        }

        /// <summary>
        /// Clear the contents of the dictionary
        /// </summary>
        public void Clear()
        {
            dictionary.Clear();
        }

        /// <summary>
        /// Indicates if an object is stored within the Dictionary
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public bool Contains(Type inputType, Object o)
        {
            bool result = false;
            ArrayList l = dictionary[inputType] as ArrayList;
            if (l != null && l.Contains(o))
            {
                result = true;
            }
            return result;
        }

        #endregion
    }
}
