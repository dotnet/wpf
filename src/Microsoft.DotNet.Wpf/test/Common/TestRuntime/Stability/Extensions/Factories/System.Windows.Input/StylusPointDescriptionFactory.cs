// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create StylusPointDescription.
    /// </summary>
    internal class StylusPointDescriptionFactory : DiscoverableFactory<StylusPointDescription>
    {
        #region Override Members

        /// <summary>
        /// Create a StylusPointDescription.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override StylusPointDescription Create(DeterministicRandom random)
        {
            List<StylusPointPropertyInfo> fullPropertyInfos = new List<StylusPointPropertyInfo>();
            AddPropertyInfos(fullPropertyInfos);
            StylusPointDescription stylusPointDescription = new StylusPointDescription(fullPropertyInfos);
            return stylusPointDescription;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// StylusPointDescription must contain at least X, Y and NormalPressure in that order.
        /// Any StylusPointPropertyInfos that represent buttons must be placed at the end of the collection.
        /// </summary>
        private void AddPropertyInfos(List<StylusPointPropertyInfo> fullPropertyInfos)
        {
            //Add X, Y and NormalPressure properties first.
            fullPropertyInfos.Add(new StylusPointPropertyInfo(StylusPointProperties.X));
            fullPropertyInfos.Add(new StylusPointPropertyInfo(StylusPointProperties.Y));
            fullPropertyInfos.Add(new StylusPointPropertyInfo(StylusPointProperties.NormalPressure));
        }

        #endregion
    }
}
