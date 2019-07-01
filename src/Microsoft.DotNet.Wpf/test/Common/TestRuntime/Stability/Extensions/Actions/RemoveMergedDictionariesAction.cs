// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.ObjectModel;
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public abstract class RemoveMergedDictionariesAction : SimpleDiscoverableAction
    {
        #region Public Members

        public int RemoveIndex { get; set; }

        public int RemoveMethod { get; set; }

        #endregion

        #region Protected Members

        protected void RemoveMergedDictionaries(Collection<ResourceDictionary> target)
        {
            RemoveIndex %= target.Count;
            switch (RemoveMethod % 3)
            {
                case 0:
                    target.RemoveAt(RemoveIndex);
                    break;
                case 1:
                    ResourceDictionary removeItem = target[RemoveIndex];
                    target.Remove(removeItem);
                    break;
                case 2:
                    target.Clear();
                    break;
            }
        }

        #endregion
    }
}
