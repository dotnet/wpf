// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: 
//              
//

using MS.Internal;
using MS.Internal.Media;
using MS.Internal.Media3D;
using System;
using System.Diagnostics;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Security;
using System.Windows.Media.Composition;
using System.Windows.Markup;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     ModelVisual3D is a Visual3D which draws the given Model3D.
    ///     ModelVisual3D is usable from Xaml.
    /// </summary>
    [ContentProperty("Children")]
    public class ModelVisual3D : Visual3D, IAddChild
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        
        /// <summary>
        ///     Default ctor
        /// </summary>
        public ModelVisual3D()
        {
            _children = new Visual3DCollection(this);
        }
        
        #endregion Constructors
        
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///   Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: 
        ///       During this virtual call it is not valid to modify the Visual tree. 
        /// </summary>
        protected sealed override Visual3D GetVisual3DChild(int index)
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
        protected sealed override int Visual3DChildrenCount
        {           
            get { return _children.Count; }
        }
        
        void IAddChild.AddChild(Object value)
        {
            if( value == null )
            {
                throw new System.ArgumentNullException("value");
            }
            
            Visual3D visual3D = value as Visual3D;

            if (visual3D == null)
            {
                throw new System.ArgumentException(SR.Get(SRID.Collection_BadType, this.GetType().Name, value.GetType().Name, typeof(Visual3D).Name));
            }

            Children.Add(visual3D);
        }

        void IAddChild.AddText(string text)
        {
            // The only text we accept is whitespace, which we ignore.
            foreach (char c in text)
            {
                if (!Char.IsWhiteSpace(c))
                {
                    throw new System.InvalidOperationException(SR.Get(SRID.AddText_Invalid, this.GetType().Name));
                }
            }
        }

        /// <summary>
        ///     Children of this Visual3D
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Visual3DCollection Children
        {
            get
            {
                VerifyAPIReadOnly();

                return _children;
            }
        }

        /// <summary>
        ///    DependencyProperty which backs the ModelVisual3D.Content property.
        /// </summary>
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(
                    "Content",
                    /* propertyType = */ typeof(Model3D),
                    /* ownerType = */ typeof(ModelVisual3D),
                    new PropertyMetadata(ContentPropertyChanged),
                    (ValidateValueCallback) delegate { return MediaContext.CurrentMediaContext.WriteAccessEnabled; });

        private static void ContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ModelVisual3D owner = ((ModelVisual3D) d);

            // if it's not a subproperty change, then we need to change the protected Model property of Visual3D
            if (!e.IsASubPropertyChange)
            {
                owner.Visual3DModel = (Model3D)e.NewValue;
            }
        }
        
        /// <summary>
        ///     The Model3D to render
        /// </summary>
        public Model3D Content
        {
            get
            {
                return (Model3D) GetValue(ContentProperty);
            }

            set
            {
                SetValue(ContentProperty, value);
            }
        }


        /// For binary compatability we need to keep the Transform DP and proprety here
        public static new readonly DependencyProperty TransformProperty = Visual3D.TransformProperty;

        /// <summary>
        ///     Transform for this Visual3D.
        /// </summary>
        public new Transform3D Transform
        {
            get
            {
                return (Transform3D) GetValue(TransformProperty);
            }

            set
            {
                SetValue(TransformProperty, value);
            }
        }
        
        #endregion Public Methods
        
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        
        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------        

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        
        #region Private Fields

        private readonly Visual3DCollection _children;

        #endregion Private Fields    

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------
        
        #region Internal Fields

        #endregion Internal Fields
    }
}

