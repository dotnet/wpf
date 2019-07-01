// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;


namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(Pen))]
    class PenFactory : DiscoverableFactory<Pen>
    {
        public Brush Brush { get; set; }
        public PenLineCap DashCap { get; set; }
        public DashStyle DashStyle { get; set; }
        public PenLineCap EndLineCap { get; set; }
        public PenLineJoin LineJoin { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double MiterLimit { get; set; }
        public PenLineCap StartLineCap { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double Thickness { get; set; }

        public override Pen Create(DeterministicRandom random)
        {
            Pen pen = new Pen(this.Brush, this.Thickness);
            pen.StartLineCap = this.StartLineCap;
            pen.EndLineCap = this.EndLineCap;
            pen.DashCap = this.DashCap;
            pen.LineJoin = this.LineJoin;
            pen.MiterLimit = this.MiterLimit;
            pen.DashStyle = this.DashStyle;
            return pen;
        }
    }
}
