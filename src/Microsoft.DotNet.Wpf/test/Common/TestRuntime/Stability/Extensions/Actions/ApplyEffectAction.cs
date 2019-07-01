// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media.Effects;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class ApplyBitmapEffectAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public UIElement Target { get; set; }

        public BitmapEffect BitmapEffect { get; set; }
        public BitmapEffectInput BitmapEffectInput { get; set; }

        public override void Perform()
        {
            //V1 & V2 Effects can not co-exist.
            Target.Effect = null;

#pragma warning disable 0618
            Target.BitmapEffect = BitmapEffect;
            Target.BitmapEffectInput = BitmapEffectInput;
#pragma warning restore 0618
        }
    }

    public class ApplyEffectAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public FrameworkElement Target { get; set; }

        public Effect Effect { get; set; }

        public override bool CanPerform()
        {
            return (Effect != null) && (Target != null);
        }

        public override void Perform()
        {
#pragma warning disable 0618
            Target.BitmapEffect = null;
            Target.BitmapEffectInput = null;
#pragma warning restore 0618

            //V1 & V2 Effects can not co-exist.
            Target.Effect = Effect;
        }
    }
}
