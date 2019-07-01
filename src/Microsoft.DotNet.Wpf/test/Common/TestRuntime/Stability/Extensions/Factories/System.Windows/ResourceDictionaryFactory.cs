// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class ResourceDictionaryFactory : DiscoverableFactory<ResourceDictionary>
    {
        #region Public Members

        public List<Freezable> Items { get; set; }

        #endregion

        #region Override Members

        public override ResourceDictionary Create(DeterministicRandom random)
        {
            ResourceDictionary dictionary = new ResourceDictionary();

            AddFreezableEntries(dictionary);

            AddCLRTypeEntries(dictionary, random);

            return dictionary;
        }

        #endregion

        #region Private Members

        private void AddFreezableEntries(ResourceDictionary dictionary)
        {
            if (Items == null)
            {
                return;
            }

            for (int i = 0; i < Items.Count; i++)
            {
                //Freeze Freezable item.
                Freezable item = Items[i];
                if (item.CanFreeze)
                {
                    item.Freeze();
                }
                else
                {
                    item = null;
                }

                AddEntry(dictionary, item);
            }
        }

        private void AddCLRTypeEntries(ResourceDictionary dictionary, DeterministicRandom random)
        {
            AddEntry(dictionary, random.Next());
            AddEntry(dictionary, random.NextBool());
            AddEntry(dictionary, random.NextDouble());
        }

        private void AddEntry(ResourceDictionary dictionary, object value)
        {
            //Find a unique key.
            string key = "";
            do
            {
                key = Guid.NewGuid().ToString("N");
            } while (dictionary.Contains(key));

            dictionary.Add(key, value);
        }

        #endregion
    }
}
