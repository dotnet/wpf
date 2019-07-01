// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action get a FrameworkElement which has binding, then change the binding source values.
    /// </summary>
    [TargetTypeAttribute(typeof(ModifyBindingSourceValuesAction))]
    public class ModifyBindingSourceValuesAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public CLRDataItem DataItem { get; set; }

        public int RandomInt { get; set; }

        public float RandomFloat { get; set; }

        public double RandomDouble { get; set; }

        public bool RandomBool { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public string RandomString { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            DataItem.ModifyData(RandomString, RandomInt, RandomBool, RandomDouble, RandomFloat);
        }

        #endregion
    }
}
