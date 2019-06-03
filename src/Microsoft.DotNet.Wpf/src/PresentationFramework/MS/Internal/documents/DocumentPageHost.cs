// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Provides a view port for a page of content for a DocumentPage.
//

using System;
using System.Windows;           // UIElement
using System.Windows.Media;     // Visual

namespace MS.Internal.Documents
{
    /// <summary> 
    /// Provides a view port for a page of content for a DocumentPage.
    /// </summary>
    internal class DocumentPageHost : FrameworkElement
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary> 
        /// Create an instance of a DocumentPageHost.
        /// </summary>
        internal DocumentPageHost()
            : base()
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        internal static void DisconnectPageVisual(Visual pageVisual)
        {
            // There might be a case where a visual associated with a page was 
            // inserted to a visual tree before. It got removed later, but GC did not
            // destroy its parent yet. To workaround this case always check for the parent
            // of page visual and disconnect it, when necessary.
            Visual currentParent = VisualTreeHelper.GetParent(pageVisual) as Visual;
            if (currentParent != null)
            {
                ContainerVisual pageVisualHost = currentParent as ContainerVisual;
                if (pageVisualHost == null)
                    throw new ArgumentException(SR.Get(SRID.DocumentPageView_ParentNotDocumentPageHost), "pageVisual");
                DocumentPageHost docPageHost = VisualTreeHelper.GetParent(pageVisualHost) as DocumentPageHost;
                if (docPageHost == null)
                    throw new ArgumentException(SR.Get(SRID.DocumentPageView_ParentNotDocumentPageHost), "pageVisual");
                docPageHost.PageVisual = null;
            }
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Root of visual subtree hosted by this DocumentPageHost.
        /// </summary>
        internal Visual PageVisual
        {
            get
            {
                return _pageVisual;
            }
            set
            {
                ContainerVisual pageVisualHost;
                if (_pageVisual != null)
                {
                    pageVisualHost = VisualTreeHelper.GetParent(_pageVisual) as ContainerVisual;
                    Invariant.Assert(pageVisualHost != null);
                    pageVisualHost.Children.Clear();
                    this.RemoveVisualChild(pageVisualHost);
                }
                _pageVisual = value;
                if (_pageVisual != null)
                {
                    pageVisualHost = new ContainerVisual();
                    this.AddVisualChild(pageVisualHost);
                    pageVisualHost.Children.Add(_pageVisual);
                    pageVisualHost.SetValue(FlowDirectionProperty, FlowDirection.LeftToRight);
                }
            }
        }

        /// <summary>
        /// Internal cached offset.
        /// </summary>
        internal Point CachedOffset;

        #endregion Internal Properties

        #region VisualChildren
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
            if (index != 0 || _pageVisual == null)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }
            return VisualTreeHelper.GetParent(_pageVisual) as Visual;
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
            get { return _pageVisual != null ? 1 : 0; }
        }

        #endregion VisualChildren

        private Visual _pageVisual;
    }
}



