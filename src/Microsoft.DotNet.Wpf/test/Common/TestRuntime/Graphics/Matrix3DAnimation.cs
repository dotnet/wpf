// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics
{
    /// <summary>
    /// This summary has not been prepared yet. NOSUMMARY - pantal07
    /// </summary>
    public class Matrix3DAnimation : AnimationTimeline
    {
        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public Matrix3DAnimation(Matrix3D from, Matrix3D to)
        {
            this.from = from;
            this.to = to;
            this.RepeatBehavior = RepeatBehavior.Forever;
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public Matrix3DAnimation(Matrix3D from, Matrix3D to, Duration duration)
            : this(from, to)
        {
            this.Duration = duration;
        }

        /// <summary>
        /// This only exists to support the new Cloning pattern: "CreateInstanceCore" -> "CloneCore"
        /// </summary>
        private Matrix3DAnimation()
        {
        }

        /// <summary/>
        protected override Freezable CreateInstanceCore()
        {
            return new Matrix3DAnimation();
        }

        /// <summary/>
        protected override void CloneCore(Freezable freezable)
        {
            base.CloneCore(freezable);

            Matrix3DAnimation original = (Matrix3DAnimation)freezable;
            this.from = original.from;
            this.to = original.to;
        }

        /// <summary/>
        protected override void GetAsFrozenCore(Freezable freezable)
        {
            base.GetAsFrozenCore(freezable);

            Matrix3DAnimation original = (Matrix3DAnimation)freezable;
            this.from = original.from;
            this.to = original.to;
        }

        /// <summary/>
        protected override void GetCurrentValueAsFrozenCore(Freezable freezable)
        {
            base.GetCurrentValueAsFrozenCore(freezable);

            Matrix3DAnimation original = (Matrix3DAnimation)freezable;
            this.from = original.from;
            this.to = original.to;
        }

        /// <summary/>
        public new Matrix3DAnimation GetAsFrozen()
        {
            return (Matrix3DAnimation)base.GetAsFrozen();
        }

        /// <summary/>
        public override object GetCurrentValue(object defaultOriginValue, object baseValue, AnimationClock clock)
        {
            double progress = (double)clock.CurrentProgress;

            Matrix3D newValue = new Matrix3D();
            newValue.M11 = from.M11 + (to.M11 - from.M11) * progress;
            newValue.M12 = from.M12 + (to.M12 - from.M12) * progress;
            newValue.M13 = from.M13 + (to.M13 - from.M13) * progress;
            newValue.M14 = from.M14 + (to.M14 - from.M14) * progress;
            newValue.M21 = from.M21 + (to.M21 - from.M21) * progress;
            newValue.M22 = from.M22 + (to.M22 - from.M22) * progress;
            newValue.M23 = from.M23 + (to.M23 - from.M23) * progress;
            newValue.M24 = from.M24 + (to.M24 - from.M24) * progress;
            newValue.M31 = from.M31 + (to.M31 - from.M31) * progress;
            newValue.M32 = from.M32 + (to.M32 - from.M32) * progress;
            newValue.M33 = from.M33 + (to.M33 - from.M33) * progress;
            newValue.M34 = from.M34 + (to.M34 - from.M34) * progress;
            newValue.OffsetX = from.OffsetX + (to.OffsetX - from.OffsetX) * progress;
            newValue.OffsetY = from.OffsetY + (to.OffsetY - from.OffsetY) * progress;
            newValue.OffsetZ = from.OffsetZ + (to.OffsetZ - from.OffsetZ) * progress;
            newValue.M44 = from.M44 + (to.M44 - from.M44) * progress;

            return newValue;
        }

        /// <summary/>
        public override Type TargetPropertyType { get { return typeof(Matrix3D); } }

        private Matrix3D from;
        private Matrix3D to;
    }
}
