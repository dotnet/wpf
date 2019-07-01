// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// DoubleAnimate TranslateTransform3D OffsetXProperty, OffsetYProperty, OffsetZProperty.
    /// </summary>
    public class DoubleAnimateTranslateTransform3DAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Viewport3D Viewport3D { get; set; }

        public Properties PropertyToAnimate { get; set; }

        /// <summary/>
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public DoubleAnimation DoubleAnimation { get; set; }

        /// <summary/>
        public HandoffBehavior HandoffBehavior { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            TranslateTransform3D TranslateTransform3D = GetTranslateTransform3DFromViewport3D(Viewport3D);

            switch (PropertyToAnimate)
            {
                case Properties.OffsetXProperty:
                    TranslateTransform3D.BeginAnimation(TranslateTransform3D.OffsetXProperty, DoubleAnimation, HandoffBehavior);
                    break;
                case Properties.OffsetYProperty:
                    TranslateTransform3D.BeginAnimation(TranslateTransform3D.OffsetYProperty, DoubleAnimation, HandoffBehavior);
                    break;
                case Properties.OffsetZProperty:
                    TranslateTransform3D.BeginAnimation(TranslateTransform3D.OffsetZProperty, DoubleAnimation, HandoffBehavior);
                    break;
            }
        }

        public override bool CanPerform()
        {
            return (GetTranslateTransform3DFromViewport3D(Viewport3D) != null);
        }

        #endregion

        public enum Properties
        {
            OffsetXProperty,
            OffsetYProperty,
            OffsetZProperty
        }

        /// <summary>
        /// Get TranslateTransform3D from Viewport3D, since get from ObjectTree directly will takes too much time and resource
        /// Work around bug 24204: http://vstfdevdiv:8080/web/wi.aspx?pcguid=22f9acc9-569a-41ff-b6ac-fac1b6370209&id=24204
        private TranslateTransform3D GetTranslateTransform3DFromViewport3D(Viewport3D Viewport3D)
        {
            if (Viewport3D != null && Viewport3D.Children.Count != 0)
            {
                foreach (Visual3D visual in Viewport3D.Children)
                {
                    Transform3D transform3D = ((Visual3D)(visual)).Transform;
                    if (transform3D != null && transform3D.GetType() == typeof(TranslateTransform3D))
                    {
                        return transform3D as TranslateTransform3D;
                    }
                }
            }

            return null;
        }
    }
}
