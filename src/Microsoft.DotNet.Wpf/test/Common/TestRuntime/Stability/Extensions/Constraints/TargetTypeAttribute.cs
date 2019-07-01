// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    //TODO: Auto-Detect this on Generic'ed Factories

    /// <summary>
    /// TargetTypeAttribute defines the targeted WPF type of an object consuming constrained data
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TargetTypeAttribute : Attribute
    {
        private Type target;

        /// <summary>
        /// Indicates the target type
        /// </summary>
        /// <param name="target"></param>
        public TargetTypeAttribute(Type target)
        {
            this.target = target;
        }

        internal static Type FindTarget(Type consumer)
        {
            foreach (object attribute in consumer.GetCustomAttributes(typeof(TargetTypeAttribute), true))
            {
                TargetTypeAttribute targetAttribute = attribute as TargetTypeAttribute;
                if (targetAttribute != null)
                {
                    return targetAttribute.target;
                }
            }
            throw new InvalidOperationException("No constraint Target is defined for the consumer Type :" + consumer);
        }
    }
}
