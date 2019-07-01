// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using Microsoft.Test.Stability.Core;
using System.Collections;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Interface for Discoverable Factories
    /// </summary>
    public abstract class DiscoverableFactory : IDiscoverableObject
    {
        /// <summary>
        /// Indicates if the desired type can be produced by this factory
        /// </summary>
        /// <param name="desiredType"></param>
        /// <returns></returns>
        public abstract bool CanCreate(Type desiredType);

        /// <summary>
        /// Creates an Object of the desired type
        /// </summary>
        /// <param name="desiredtype">The desired type to produce</param>
        /// <param name="random"></param>
        /// <returns></returns>
        public abstract Object Create(Type desiredtype, DeterministicRandom random);
    }


    public abstract class DiscoverableFactory<ProducedType> : DiscoverableFactory
    {

        public override bool CanCreate(Type desiredType)
        {
            return desiredType == typeof(ProducedType) || typeof(ProducedType).IsSubclassOf(desiredType);
        }

        public override sealed Object Create(Type desiredtype, DeterministicRandom random)
        {
            //validate args
            return Create(random);
        }

        public abstract ProducedType Create(DeterministicRandom random);
    }

    /// <summary>
    /// This creates a collection of objects
    /// </summary>
    /// <typeparam name="CollectionType"></typeparam>
    /// <typeparam name="MemberType"></typeparam>
    public abstract class DiscoverableCollectionFactory<CollectionType, MemberType> : DiscoverableFactory where CollectionType : IList<MemberType>, new()
    {
        public virtual List<MemberType> ContentList { get; set; }

        public override bool CanCreate(Type desiredType)
        {
            return typeof(CollectionType) == desiredType;
        }

        public override sealed object Create(Type desiredtype, DeterministicRandom random)
        {
            IList collection = (IList)Activator.CreateInstance(typeof(CollectionType));
            if (ContentList != null)
            {
                foreach (MemberType local in ContentList)
                {
                    // Some collections doesn't allow null. 
                    if(local != null)
                    {
                        collection.Add(local);
                    }
                }
            }
            return (CollectionType)collection;
        }
    }
}
