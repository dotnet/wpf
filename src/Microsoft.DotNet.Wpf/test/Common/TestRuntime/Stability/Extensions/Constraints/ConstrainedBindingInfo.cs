// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    /// <summary>
    /// Class Constraints StressBindingInfo for ItemsBinding.
    /// </summary>
    public class ConstrainedBindingInfo : ConstrainedDataSource
    {
        #region Private Data

        private static object lockObject = new object();

        private StressBindingInfo instance;

        #endregion

        #region Public Members

        public ConstrainedBindingInfo() { }

        #endregion

        #region Override Members

        public override object GetData(DeterministicRandom random)
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = new StressBindingInfo(random);
                    }
                }
            }

            return instance;
        }

        #endregion
    }
}
