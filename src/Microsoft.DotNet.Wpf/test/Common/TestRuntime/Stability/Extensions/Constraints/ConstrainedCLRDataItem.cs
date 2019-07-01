// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.CustomTypes;
using Microsoft.Test.Text;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class ConstrainedCLRDataItem : ConstrainedDataSource
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
                        instance = new CLRDataItem();
                        StringProperties strProperties = new StringProperties();
                        strProperties.MinNumberOfCodePoints = 0;
                        strProperties.MaxNumberOfCodePoints = 200;
// Fixed build break + notified LXuan.  Turns out to not be a 3.5/4.0 issue but rather a plain old build break.
//                        strProperties.UnicodeRange = new UnicodeRange(0x0000, 0xffff);
                        string stringValue = StringFactory.GenerateRandomString(strProperties, random.Next());
                        instance.ModifyData(stringValue, random.Next(10), random.NextBool(), random.NextDouble(), (float)random.NextDouble());
                    }
                }
            }

            return instance;
        }

        #endregion

        #region Public Members

        public ConstrainedCLRDataItem() { }

        #endregion

        #region Private Data

        private static object lockObject = new object();

        private CLRDataItem instance;

        #endregion
    }
}
