// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public abstract class ResourceRemoveEntryAction : SimpleDiscoverableAction
    {
        #region Public Members

        public int RemoveIndex { get; set; }

        public bool IsRemoveAll { get; set; }

        #endregion

        #region Protected Members

        protected void ResourceRemoveEntry(ResourceDictionary dictionary)
        {
            if (dictionary.Count == 0)
            {
                return;
            }

            if (IsRemoveAll)
            {
                dictionary.Clear();
            }
            else
            {
                RemoveIndex %= dictionary.Count;
                //Get key at RemoveIndex.
                object key = null;
                int count = 0;
                foreach (object item in dictionary.Keys)
                {
                    if (count == RemoveIndex)
                    {
                        key = item;
                        break;
                    }
                    count++;
                }

                if (key != null)
                {
                    dictionary.Remove(key);
                }
            }
        }

        #endregion
    }
}
