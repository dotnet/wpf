// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Test.Stability.Core
{
    /// <summary>
    /// Provides Repeatable/Deterministic Sequence of random numerical values
    /// </summary>
    public class DeterministicRandom : IDisposable
    {

        #region Private Data

        private Random random;
        private bool isDisposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Generates a new Deterministic Random object
        /// </summary>
        /// <param name="seed">Random Seed for generating numbers</param>
        /// <param name="iteration">Iteration step</param>
        public DeterministicRandom(int seed, int contextId, int iteration)
        {
            isDisposed = false;

            //The following is not a very clever algorithm for seeding on two integers.
            //We are taking the first value from an externally seeded random object 
            //to offset our iteration's random seed
            Random temp = new Random(seed);
            random = new Random(iteration*temp.Next()+contextId*temp.Next());
        }

        #endregion

        #region Public Members



        /// <summary>
        /// Returns a nonnegative random number.
        /// </summary>
        /// <returns></returns>
        public int Next()
        {
            CheckPrecondition();
            return random.Next();
        }

        /// <summary>
        /// Returns a nonnegative random number less than the specified maximum.
        /// </summary>
        /// <param name="max"></param>
        /// <returns></returns>
        public int Next(int max)
        {
            CheckPrecondition();
            return random.Next(max);
        }

        /// <summary>
        /// Returns a random number between 0.0 and 1.0.
        /// </summary>
        /// <returns></returns>
        public double NextDouble()
        {
            CheckPrecondition();
            return random.NextDouble();
        }

        /// <summary>
        /// Gets a random item in a list
        /// </summary>
        /// <param name="list">The list you want to get an item from</param>
        /// <returns>A random item in the specified list</returns>
        public T NextItem<T>(List<T> list)
        {
            CheckPrecondition();
            if (list == null)
                throw new ArgumentNullException("list");
            if (list.Count == 0)
                throw new ArgumentException("The list does not contain any values", "list");

            return list[Next(list.Count)];
        }

        /// <summary>
        /// Gets a random value from an enumeration
        /// </summary>
        /// <typeparam name="enumClass">The type of enumeration</typeparam>
        /// <returns>a randomly selected value from the enumeration</returns>
        public enumClass NextEnum<enumClass>()
        {
            CheckPrecondition();
            //Run-time type checking (Generics where keyword explicitly does not support enum filtering)
            //Ideally this would be done at compile time
            Type enumType = typeof(enumClass);
            if (enumType.BaseType != typeof(Enum))
            {
                throw new ArgumentException("enumClass can only be an enum.");
            }

            Array enumValues = Enum.GetValues(enumType);
            return (enumClass)enumValues.GetValue(Next(enumValues.Length));
        }

        /// <summary>
        /// Gets a random value from a collection of Static Properties
        /// </summary>
        /// <typeparam name="CollectionType"></typeparam>
        /// <typeparam name="ElementType"></typeparam>
        /// <returns></returns>
        public ElementType NextStaticProperty<CollectionType, ElementType>()
        {
            return NextStaticProperty<ElementType>(typeof(CollectionType));
        }

        /// <summary>
        /// Gets a random value from a collection of Static Properties
        /// </summary>
        /// <typeparam name="ElementType"></typeparam>
        /// <returns></returns>
        public ElementType NextStaticProperty<ElementType>(Type CollectionType)
        {
            MethodAttributes requisiteAttributes = MethodAttributes.Static | MethodAttributes.Public;
            PropertyInfo[] propertyCollection = CollectionType.GetProperties();
            PropertyInfo propertyInfo = propertyCollection[random.Next(propertyCollection.Length)];
            if (propertyInfo.PropertyType != typeof(ElementType))
            {
                throw new InvalidOperationException("Test Bug - The Type: " + CollectionType + " does not contain a pure collection of " + typeof(ElementType));
            }
            MethodInfo methodInfo = propertyInfo.GetGetMethod();
            if ((methodInfo != null) && ((methodInfo.Attributes & requisiteAttributes) == requisiteAttributes))
            {
                return (ElementType)propertyInfo.GetValue(null, null);
            }
            else
            {
                throw new InvalidOperationException("Test Bug - The collection of types contained in " + CollectionType + " are not exclusively Public Static objects.");
            }
        }

        /// <summary>
        /// Gets a random value from a collection of Public Static Fields.
        /// </summary>
        /// <typeparam name="CollectionType"></typeparam>
        /// <typeparam name="ElementType"></typeparam>
        /// <returns></returns>
        public ElementType NextStaticField<CollectionType, ElementType>()
        {
            return NextStaticField<ElementType>(typeof(CollectionType));
        }

        /// <summary>
        /// Gets a random value from a collection of Public Static Fields.
        /// </summary>
        /// <typeparam name="ElementType"></typeparam>
        /// <param name="CollectionType"></param>
        /// <returns></returns>
        public ElementType NextStaticField<ElementType>(Type CollectionType)
        {
            FieldInfo[] fieldCollection = CollectionType.GetFields();
            if(fieldCollection == null || fieldCollection.Length < 1)
            {
                throw new InvalidOperationException("Test Bug - The Type: " + CollectionType + " does not contain fields.");
            }

            FieldInfo fieldInfo = fieldCollection[random.Next(fieldCollection.Length)];
            if (fieldInfo.FieldType != typeof(ElementType))
            {
                throw new InvalidOperationException("Test Bug - The Type: " + CollectionType + " does not contain a pure collection of " + typeof(ElementType));
            }

            FieldAttributes requisiteAttributes = FieldAttributes.Public | FieldAttributes.Static;
            if ((fieldInfo.Attributes & requisiteAttributes) == requisiteAttributes)
            {
                return (ElementType)fieldInfo.GetValue(null);
            }
            else
            {
                throw new InvalidOperationException("Test Bug - The collection of types contained in " + CollectionType + " are not exclusively Public Static objects.");
            }
        }

        /// <summary>
        /// Provides 50-50 odds of true/false
        /// </summary>
        /// <returns></returns>
        public bool NextBool()
        {
            this.CheckPrecondition();
            return (this.random.NextDouble() > 0.5);
        }

        /// <summary>
        /// Disables the random object instance to control the lifespan of randomness.
        /// </summary>
        public void Dispose()
        {
            isDisposed = true;
        }

        #endregion

        #region Private Members

        private void CheckPrecondition()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("DeterministicRandom", "This DeterministicRandom object is no longer active.It should no longer be used at this point in the lifespan. Check if this object was held indirectly from the State.");
            }
        }

        #endregion

    }
}
