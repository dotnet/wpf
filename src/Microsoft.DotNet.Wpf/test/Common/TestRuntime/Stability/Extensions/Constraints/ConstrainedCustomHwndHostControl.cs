// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class ConstrainedHwndHostControlRecord : ConstrainedDataSource
    {
        #region Override Members

        public override object GetData(DeterministicRandom random)
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = new CustomHwndHostControl.HwndHostControlRecord();
                    }
                }
            }

            return instance;
        }

        #endregion

        #region Public Members

        public ConstrainedHwndHostControlRecord() { }

        #endregion

        #region Private Data

        private static object lockObject = new object();

        private CustomHwndHostControl.HwndHostControlRecord instance;

        #endregion
    }
}
