// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public abstract class ResourceAddEntryAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Freezable Freezable { get; set; }

        #endregion

        #region Protected Members

        protected void ResourceAddEntry(ResourceDictionary dictionary)
        {
            //Find a unique key.
            string key = "";
            do
            {
                key = Guid.NewGuid().ToString("N");
            } while (dictionary.Contains(key));

            dictionary.Add(key, Freezable);
        }

        #endregion
    }
}
