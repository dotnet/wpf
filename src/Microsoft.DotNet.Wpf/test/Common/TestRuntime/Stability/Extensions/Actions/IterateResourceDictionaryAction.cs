// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections;
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public abstract class IterateResourceDictionaryAction : SimpleDiscoverableAction
    {
        #region Public Members

        public int CopyToIndex { get; set; }

        #endregion

        #region Protected Members

        protected void IterateResourceDictionary(ResourceDictionary dictionary)
        {
            LoopEnumerator(dictionary);
            LoopEnumerator(dictionary.Keys);
            LoopEnumerator(dictionary.Values);
            CopyDictionaryEntrys(dictionary);
            ReadDictionaryEntrys(dictionary);
        }

        #endregion

        #region Private Members

        private void LoopEnumerator(IEnumerable collection)
        {
            foreach (object item in collection)
            {
                object value = item;
            }
        }

        private void CopyDictionaryEntrys(ResourceDictionary dictionary)
        {
            int count = dictionary.Count;
            if (count == 0)
            {
                return;
            }

            CopyToIndex %= count;

            DictionaryEntry[] destiny = new DictionaryEntry[count * 2];
            dictionary.CopyTo(destiny, CopyToIndex);
        }

        private void ReadDictionaryEntrys(ResourceDictionary dictionary)
        {
            foreach (object key in dictionary.Keys)
            {
                object value = dictionary[key];
            }
        }

        #endregion
    }
}
