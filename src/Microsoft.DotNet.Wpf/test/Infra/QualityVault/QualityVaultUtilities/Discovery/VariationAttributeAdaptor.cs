// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Test.Discovery
{
    /// <summary>
    /// Multiplies tests defined with a TestAttribute against VariationAttributes
    /// which specify various constructor parameters to those tests.
    /// </summary>
    public class VariationAttributeAdaptor : TestAttributeAdaptor
    {
        #region Protected Members

        /// <summary>
        /// Multiply TestInfo by VariationAttributes found on type's constructors.
        /// Create TestInfo from a TestAttribute, Type and default TestInfo.
        /// New test info will be added to tests List.
        /// </summary>
        protected override ICollection<TestInfo> BuildTestInfo(TestAttribute testAttribute, Type ownerType, TestInfo defaultTestInfo)
        {
            List<TestInfo> tests = new List<TestInfo>();
            TestInfo baseTestInfo = base.BuildTestInfo(testAttribute, ownerType, defaultTestInfo).First();

            // Search each constructor for variationAttributes
            foreach (ConstructorInfo constructorInfo in ownerType.GetConstructors())
            {
                IEnumerable<VariationAttribute> variationAttributes = constructorInfo.GetCustomAttributes(typeof(VariationAttribute), false).Cast<VariationAttribute>();
                tests.AddRange(Multiply(baseTestInfo, variationAttributes, ownerType));
            }

            // No variationAttributes found for the base test. Add it without variation information.
            if (tests.Count == 0)
            {
                tests.Add(baseTestInfo);
            }

            return tests;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Return a collection of TestInfos which are the base TestInfo multiplied against each VariationAttribute.
        /// </summary>
        /// <param name="baseTestInfo">Base TestInfo.</param>
        /// <param name="variationAttributes">Collection of attributes for each variation.</param>
        /// <param name="ownerType">Class upon which test is being discovered.</param>
        /// <returns>Collection of multiplied TestInfos.</returns>
        private IEnumerable<TestInfo> Multiply(TestInfo baseTestInfo, IEnumerable<VariationAttribute> variationAttributes, Type ownerType)
        {
            List<TestInfo> tests = new List<TestInfo>();

            foreach (VariationAttribute variationAttribute in variationAttributes)
            {
                // The difference between creating a TestInfo from a VariationAttribute instead of a TestAttribute is
                // mapping from ConstructorParameters into an entry in the DriverParameters PropertyBag, and
                // modifying the name based upon constructor parameters such that all TestInfos have unique names.
                // We can ask our base to construct a TestInfo for us, and make these two modifications after.
                TestInfo variationTestInfo = base.BuildTestInfo(variationAttribute, ownerType, baseTestInfo).First();
                variationTestInfo.DriverParameters["CtorParams"] = variationAttribute.ConstructorParameters;
                variationTestInfo.Name = variationTestInfo.Name + "(" + variationAttribute.ConstructorParameters + ")";
                tests.Add(variationTestInfo);
            }

            return tests;
        }

        #endregion
    }
}