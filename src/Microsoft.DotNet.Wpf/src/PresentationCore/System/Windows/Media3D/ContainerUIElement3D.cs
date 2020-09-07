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
using System.Windows.Automation.Peers;
using System.Windows.Media.Composition;
using System.Windows.Markup;
    
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     ContainerUIElement3D is a UIElement3D which contains children of type Visual3D.
    ///     It does not set the Visual3DModel property.
    /// </summary>
    [ContentProperty("Children")]
    public sealed class ContainerUIElement3D : UIElement3D
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
        public ContainerUIElement3D()
        {
            _children = new Visual3DCollection(this);
        }
        
        #endregion Constructors
        
        /// <summary>
        ///   Derived class must implement to support Visual3D children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisual3DChildrenCount-1.
        ///
        ///    By default a Visual3D does not have any children.
        ///
        ///  Remark: 
        ///       During this virtual call it is not valid to modify the Visual tree. 
        /// </summary>
        protected override Visual3D GetVisual3DChild(int index)
        {            
            //Visual3DCollection does the range check for index
            return _children[index];
        }
        
        /// <summary>
        ///  Derived classes override this property to enable the Visual3D code to enumerate 
        ///  the Visual3D children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual3D does not have any children.
        ///
        ///  Remark: During this virtual method the Visual tree must not be modified.
        /// </summary>        
        protected override int Visual3DChildrenCount
        {           
            get { return _children.Count; }
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
        /// Called by the Automation infrastructure when AutomationPeer
        /// is requested for this element.
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new UIElement3DAutomationPeer(this);
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        
        #region Private Fields

        private readonly Visual3DCollection _children;

        #endregion Private Fields    
    }
}


