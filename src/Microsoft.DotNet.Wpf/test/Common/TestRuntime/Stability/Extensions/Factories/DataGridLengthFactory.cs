// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    [TargetTypeAttribute(typeof(DataGridLength))]
    class DataGridLengthFactory : DiscoverableFactory<DataGridLength>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double Value { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 SwitchIndex { get; set; }

        public override DataGridLength Create(DeterministicRandom random)
        {
            DataGridLength dataGridLength = new DataGridLength();
            switch (SwitchIndex)
            {
                case 0:
                    dataGridLength = new DataGridLength(Value, random.NextEnum<DataGridLengthUnitType>());
                    break;
                case 1:
                    dataGridLength = DataGridLength.Auto;
                    break;
                case 2:
                    dataGridLength = DataGridLength.SizeToCells;
                    break;
                case 3:
                    dataGridLength = DataGridLength.SizeToHeader;
                    break;
            }
            return dataGridLength;
        }
    }
#endif
}
