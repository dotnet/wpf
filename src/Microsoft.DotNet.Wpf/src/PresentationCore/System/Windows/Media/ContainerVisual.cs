// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//              Created it.

using System;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Diagnostics;
using System.Collections;
using MS.Internal;
using System.Resources;
using System.Runtime.InteropServices;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// A ContainerVisual is a Container for other Visuals.
    /// </summary>
    public class ContainerVisual : Visual
    {
        /// <summary>
        /// Ctor ContainerVisual.
        /// </summary>
        public ContainerVisual()
        {
            _children = new VisualCollection(this);
        }

        // ------------------------------------------------------------------------------------------
        // Publicly re-exposed VisualTreeHelper interfaces.
        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        public VisualCollection Children
        {
            get
            { 
                VerifyAPIReadOnly();

                return _children;
            }
        }

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        public DependencyObject Parent
        {
            get { return base.VisualParent; }
        }

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        public Geometry Clip
        {
            get { return base.VisualClip; }
            set { base.VisualClip = value; }
        }

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        public double Opacity
        {
            get { return base.VisualOpacity; }
            set { base.VisualOpacity = value; }
        }

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        public Brush OpacityMask
        {
            get { return base.VisualOpacityMask;  }
            set { base.VisualOpacityMask = value; }
        }

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        public CacheMode CacheMode
        {
            get { return base.VisualCacheMode; }
            set { base.VisualCacheMode = value; }
        }
        
        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        public BitmapEffect BitmapEffect
        {
            get { return base.VisualBitmapEffect; }
            set { base.VisualBitmapEffect = value; }
        }


        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        public BitmapEffectInput BitmapEffectInput
        {
            get { return base.VisualBitmapEffectInput; }
            set { base.VisualBitmapEffectInput = value; }
        }


        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        public Effect Effect
        {
            get { return base.VisualEffect; }
            set { base.VisualEffect = value; }
        }

        /// <summary>
        /// Gets or sets X- (vertical) guidelines on this Visual.
        /// </summary>
        [DefaultValue(null)]
        public DoubleCollection XSnappingGuidelines
        {
            get { return base.VisualXSnappingGuidelines; }
            set { base.VisualXSnappingGuidelines = value; }
        }

        /// <summary>
        /// Gets or sets Y- (vertical) guidelines on this Visual.
        /// </summary>
        [DefaultValue(null)]
        public DoubleCollection YSnappingGuidelines
        {
            get { return base.VisualYSnappingGuidelines; }
            set { base.VisualYSnappingGuidelines = value; }
        }        

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        new public HitTestResult HitTest(Point point)
        {
            return base.HitTest(point);
        }

        /// <summary>
        /// Re-exposes the Visual base class's corresponding VisualTreeHelper implementation as public method.
        /// </summary>
        new public void HitTest(HitTestFilterCallback filterCallback, HitTestResultCallback resultCallback, HitTestParameters hitTestParameters)
        {
            base.HitTest(filterCallback, resultCallback, hitTestParameters);
        }

        /// <summary>
        /// VisualContentBounds returns the bounding box for the contents of this Visual.
        /// </summary>
        public Rect ContentBounds
        {
            get
            {
                return base.VisualContentBounds;
            }
        }

        /// <summary>
        /// Gets or sets the Transform property.
        /// </summary>
        public Transform Transform
        {
            get
            {
                return base.VisualTransform;
            }
            set
            {
                base.VisualTransform = value;
            }            
        }


        /// <summary>
        /// Gets or sets the Offset property.
        /// </summary>
        public Vector Offset
        {
            get
            {
                return base.VisualOffset;
            }
            set
            {
                base.VisualOffset = value;
            }
        }       

        /// <summary>
        /// DescendantBounds returns the union of all of the content bounding
        /// boxes for all of the descendants of the current visual, but not including
        /// the contents of the current visual.
        /// </summary>
        public Rect DescendantBounds
        {
            get
            {
                return base.VisualDescendantBounds;
            }
        }

        // ------------------------------------------------------------------------------------------
        // Protected methods
        // ------------------------------------------------------------------------------------------

        /// <summary>
        ///   Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: 
        ///       During this virtual call it is not valid to modify the Visual tree. 
        /// </summary>
        protected sealed override Visual GetVisualChild(int index)
        {            
            //VisualCollection does the range check for index
            return _children[index];
        }
        
        /// <summary>
        ///  Derived classes override this property to enable the Visual code to enumerate 
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: During this virtual method the Visual tree must not be modified.
        /// </summary>        
        protected sealed override int VisualChildrenCount
        {           
            get 
            { 
                return _children.Count; 
            }
        }

        // ------------------------------------------------------------------------------------------
        // Private fields
        // ------------------------------------------------------------------------------------------

        private readonly VisualCollection _children;
    }
}


