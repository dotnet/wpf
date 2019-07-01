// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create StylusPoint.
    /// </summary>
    internal class StylusPointFactory : DiscoverableFactory<StylusPoint>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a StylusPointDescription to initialize a StylusPoint.
        /// </summary>
        public StylusPointDescription StylusPointDescription { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Creaet a StylusPoint.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override StylusPoint Create(DeterministicRandom random)
        {
            double x = random.NextDouble() * 500;
            double y = random.NextDouble() * 500;
            //pressureFactor must be [0,1]
            float pressureFactor = (float)random.NextDouble();

            if (StylusPointDescription == null)
            {
                return new StylusPoint(x, y, pressureFactor);
            }

            int[] additionalValues = CreateAdditionalValues(StylusPointDescription.GetStylusPointProperties(), random);
            if (additionalValues == null)
            {
                return new StylusPoint(x, y, pressureFactor);
            }

            return new StylusPoint(x, y, pressureFactor, StylusPointDescription, additionalValues);
        }

        #endregion

        #region Private Members

        /// <summary>
        /// The values in additionalValues that correspond to button properties must be 0 or 1.
        ///  -and-
        /// The number of values in additionalValues match the number of properties in stylusPointDescription minus 3.
        /// </summary>
        private int[] CreateAdditionalValues(IList<StylusPointPropertyInfo> properties, DeterministicRandom random)
        {
            if (StylusPointDescription == null)
            {
                return null;
            }

            int arrayCount = StylusPointDescription.PropertyCount - 3;
            int[] additionalValues;
            if (arrayCount < 0)
            {
                return null;
            }

            additionalValues = new int[arrayCount];

            for (int i = 0; i < arrayCount; i++)
            {
                if (properties[i].IsButton)
                {
                    additionalValues[i] = random.Next(2);
                }
                else
                {
                    additionalValues[i] = random.Next();
                }
            }

            return additionalValues;
        }

        #endregion
    }
}
