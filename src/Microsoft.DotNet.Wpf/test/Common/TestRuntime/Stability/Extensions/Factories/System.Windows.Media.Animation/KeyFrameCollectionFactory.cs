// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// This factory create a tpye of KeyFrameCollection when specified the KeyFrame type
    /// </summary>
    internal class KeyFrameCollectionFactory<KeyFrameCollection, KeyFrameType> : DiscoverableFactory where KeyFrameCollection : IList, new()
    {
        public virtual List<KeyFrameType> ContentList { get; set; }

        public override bool CanCreate(Type desiredType)
        {
            return typeof(KeyFrameCollection) == desiredType;
        }

        public override sealed object Create(Type desiredtype, DeterministicRandom random)
        {
            IList collection = (IList)Activator.CreateInstance(typeof(KeyFrameCollection));
            if (ContentList != null)
            {
                foreach (KeyFrameType local in ContentList)
                {
                    // Some KeyFrameCollection doesn't allow null. 
                    if (local != null)
                    {
                        collection.Add(local);
                    }
                }
            }
            return (KeyFrameCollection)collection;
        }
    }
}
