// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Viewport3D element is a UIElement that contains a 3D
// scene and a camera that defines the scene's projection into the 2D
// rectangle of the control.
//

using MS.Internal;
using MS.Internal.KnownBoxes;
using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Markup;
using System.Windows.Threading;

namespace System.Windows.Controls
{
    /// <summary>
    ///     The Viewport3D provides the Camera and layout bounds
    ///     required to project the Visual3Ds into 2D.  The Viewport3D
    ///     is the bridge between 2D visuals and 3D.
    /// </summary>
    [ContentProperty("Children")]
    [Localizability(LocalizationCategory.NeverLocalize)]
    public class Viewport3D : FrameworkElement, IAddChild
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        static Viewport3D()
        {
            // Viewport3Ds do not really have a "Desired Bounds" since the
            // camera frustum is scaled to the width of the viewport rect.
            // As a result, layout never imposes a clip on Viewport3D.
            //
            // This makes it very easy to render Visual3Ds outside of the
            // layout bounds.  Most framework users find this surprising.
            // As a result, we clip Viewport3Ds to bounds by default.
            ClipToBoundsProperty.OverrideMetadata(typeof(Viewport3D), new PropertyMetadata(BooleanBoxes.TrueBox));
        }

        /// <summary>
        /// Viewport3D Constructor
        /// </summary>
        public Viewport3D()
        {
            _viewport3DVisual = new Viewport3DVisual();

            // The value for the Camera property and the Children property on Viewport3D
            // will also be the value for these properties on the Viewport3DVisual we
            // create as an internal Visual child.  This then will cause these values to
            // be shared, which will break property inheritance, dynamic resource references
            // and databinding.  To prevent this, we mark the internal
            // Viewport3DVisual.CanBeInheritanceContext to be false, allowing Camera and
            // Children to only pick up value context from the Viewport3D (this).
            _viewport3DVisual.CanBeInheritanceContext = false;

            this.AddVisualChild(_viewport3DVisual);

            // XamlSerializer does not support RO DPs
            //
            //  The XamlSerializer currently only serializes locally set properties.  To
            //  work around this we intentionally promote our ReadOnly Children
            //  property to locally set.
            //
            SetValue(ChildrenPropertyKey, _viewport3DVisual.Children);

            _viewport3DVisual.SetInheritanceContextForChildren(this);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public properties
        //
        //------------------------------------------------------
        #region Properties

        /// <summary>
        ///     The DependencyProperty for the Camera property.
        /// </summary>
        public static readonly DependencyProperty CameraProperty =
            Viewport3DVisual.CameraProperty.AddOwner(
                typeof(Viewport3D),
                new FrameworkPropertyMetadata(
                    FreezableOperations.GetAsFrozen(new PerspectiveCamera()),
                    new PropertyChangedCallback(OnCameraChanged)));

        private static void OnCameraChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Viewport3D owner = (Viewport3D) d;

            if (!e.IsASubPropertyChange)
            {
                owner._viewport3DVisual.Camera = (Camera) e.NewValue;
            }
        }

        /// <summary>
        /// Camera for viewing scene.  If no camera is specified, then a default will be provided.
        /// </summary>
        public Camera Camera
        {
            get { return (Camera) GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        private static readonly DependencyPropertyKey ChildrenPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "Children",
                typeof(Visual3DCollection),
                typeof(Viewport3D),
                new FrameworkPropertyMetadata((object) null));

        /// <summary>
        /// The 3D children of the Viewport3D
        /// </summary>
        public static readonly DependencyProperty ChildrenProperty = ChildrenPropertyKey.DependencyProperty;

        /// <summary>
        /// The 3D children of this Viewport3D.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Visual3DCollection Children
        {
            get { return (Visual3DCollection) GetValue(ChildrenProperty); }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Protected methods
        //
        //------------------------------------------------------

        #region Protected methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new Viewport3DAutomationPeer(this);
        }

        /// <summary>
        /// Viewport3D arranges its children to fill whatever it is given.
        /// </summary>
        /// <param name="finalSize">Size that Viewport3D will assume.</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Rect newBounds = new Rect(new Point(), finalSize);
            _viewport3DVisual.Viewport = newBounds;

            return finalSize;
        }

        /// <summary>
        ///   Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark:
        ///       During this virtual call it is not valid to modify the Visual tree.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            //added in the constructor so 1 children always exist
            switch(index)
            {
                case 0:
                    return _viewport3DVisual;

                default:
                    throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }
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
        protected override int VisualChildrenCount
        {
            //children are added in the constructor
            get { return 1; }
        }
        #endregion Protected methods

        //------------------------------------------------------
        //
        //  Private fields
        //
        //------------------------------------------------------

        #region Private Fields

        private readonly Viewport3DVisual _viewport3DVisual;

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  IAddChild implementation
        //
        //------------------------------------------------------

        void IAddChild.AddChild(Object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            Visual3D visual3D = value as Visual3D;

            if (visual3D == null)
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(Visual3D)), "value");
            }

            Children.Add(visual3D);
        }

        void IAddChild.AddText(string text)
        {
            // The only text we accept is whitespace, which we ignore.
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }
    }
}


