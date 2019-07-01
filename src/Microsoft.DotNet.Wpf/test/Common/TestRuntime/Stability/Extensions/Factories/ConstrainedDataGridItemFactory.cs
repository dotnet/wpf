// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(ConstrainedDataGridItem))]
    class ConstrainedDataGridItemFactory : DiscoverableFactory<ConstrainedDataGridItem>
    {
        public Boolean Boolean { get; set; }
        public Byte Byte { get; set; }
        public Byte[] ByteArray { get; set; }
        public Int32 Int32 { get; set; }
        public Int64 Int64 { get; set; }
        public Double Double { get; set; }
        public Decimal Decimal { get; set; }
        public Single Single { get; set; }
        public Char Char { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public String String { get; set; }
        public Object Object { get; set; }
        public Enumerations Enumerations { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Uri Uri { get; set; }        

        public override ConstrainedDataGridItem Create(DeterministicRandom random)
        {     
            ConstrainedDataGridItem item = new ConstrainedDataGridItem();
            item.BooleanProp = Boolean;
            item.ByteProp = Byte;
            item.ByteArrayProp = ByteArray;
            item.Int32Prop = Int32;
            item.Int64Prop = Int64;
            item.DoubleProp = Double;
            item.DecimalProp = Decimal;
            item.SingleProp = Single; 
            item.CharProp = Char;
            item.StringProp = String;
            item.ObjectProp = Object;
            item.EnumerationsProp = Enumerations;
            item.UriProp = Uri;
            return item;
        }
    }
}
