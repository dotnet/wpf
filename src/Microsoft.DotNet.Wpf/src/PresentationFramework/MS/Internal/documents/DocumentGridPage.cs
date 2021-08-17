// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: DocumentGridPage displays a graphical representation of an
//              DocumentPaginator page including drop-shadow 
//              and is used by DocumentGrid.
//


using MS.Internal.Annotations.Anchoring;
using MS.Win32;
using System.Threading;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Shapes;

using System.Windows.Input;
using System.Windows.Media;
using System;
using System.Collections;
using System.Diagnostics;

namespace MS.Internal.Documents
{
    /// <summary> 
    /// 
    /// </summary>
    internal class DocumentGridPage : FrameworkElement, IDisposable
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary> 
        /// Standard constructor.
        /// </summary>
        public DocumentGridPage(DocumentPaginator paginator) : base()
        {
            _paginator = paginator;

            //Attach the GetPageCompleted event handler to the paginator so we can
            //use it to keep track of whether our page has been loaded yet.
            _paginator.GetPageCompleted += new GetPageCompletedEventHandler(OnGetPageCompleted);

            //Set up the static elements of our Visual Tree.
            Init();
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The DocumentPage for the displayed page.
        /// </summary>
        public DocumentPage DocumentPage
        {
            get
            {
                CheckDisposed();
                return _documentPageView.DocumentPage;
            }
        }

        /// <summary>
        /// The page number displayed.
        /// </summary>
        public int PageNumber
        {
            get
            {
                CheckDisposed();
                return _documentPageView.PageNumber;
            }
            set
            {
                CheckDisposed();
                if (_documentPageView.PageNumber != value)
                {
                    _documentPageView.PageNumber = value;
                }
            }
        }

        /// <summary>
        /// The DocumentPageView displayed by this DocumentGridPage
        /// </summary>
        public DocumentPageView DocumentPageView
        {
            get
            {
                CheckDisposed();
                return _documentPageView;
            }
        }

        /// <summary>
        /// Whether to show the border and drop shadow around the page.
        /// </summary>
        /// <value></value>
        public bool ShowPageBorders
        {
            get
            {
                CheckDisposed();
                return _showPageBorders;
            }

            set
            {
                CheckDisposed();
                if (_showPageBorders != value)
                {
                    _showPageBorders = value;
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// Whether the requested page is loaded.
        /// </summary>
        public bool IsPageLoaded
        {
            get
            {
                CheckDisposed();
                return _loaded;
            }
        }

        //-------------------------------------------------------------------
        //
        //  Public Events
        //
        //-------------------------------------------------------------------

        #region Public Events
        /// <summary>
        /// Fired when the document is finished paginating.
        /// </summary>
        public event EventHandler PageLoaded;
        #endregion Public Events

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods
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
            CheckDisposed();

            // count is either 0 or 1
            if (VisualChildrenCount != 0)
            {
                switch (index)
                {
                    case 0:
                        return _documentContainer;
                    default:
                        throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
                }
            }

            throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
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
            get
            {
                if (!_disposed && hasAddedChildren)
                    return 1;
                else
                    return 0;
            }
        }


        /// <summary>
        /// Content measurement.
        /// </summary>
        /// <param name="availableSize">Available size that parent can give to the child. This is soft constraint.</param>
        /// <returns>The DocumentGridPage's desired size.</returns>
        protected override sealed Size MeasureOverride(Size availableSize)
        {
            CheckDisposed();

            if (!hasAddedChildren)
            {
                this.AddVisualChild(_documentContainer);

                hasAddedChildren = true;
            }

            //Show / hide the border and drop shadow based on the current
            //state of the ShowPageBorders property.
            if (ShowPageBorders)
            {
                var key = new ComponentResourceKey(typeof(FrameworkElement), "DocumentGridPageContainerWithBorder");
                _documentContainer.SetCurrentValue(StyleProperty, TryFindResource(key));
            }
            else
            {
                _documentContainer.SetCurrentValue(StyleProperty, TryFindResource(typeof(ContentControl)));
            }

            //Measure our children.
            _documentContainer.Measure(availableSize);

            //Set the Page Zoom on the DocumentPageView to the ratio between our measure constraint
            //and the actual size of the page; this will cause the DPV to scale our page appropriately.
            if (DocumentPage.Size != Size.Empty && DocumentPage.Size.Width != 0.0)
            {
                _documentPageView.SetPageZoom(availableSize.Width / DocumentPage.Size.Width);
            }

            return availableSize;
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="arrangeSize">The final size that element should use to arrange itself and its children.</param>
        protected override sealed Size ArrangeOverride(Size arrangeSize)
        {
            CheckDisposed();

            //Arrange the page container, no offset.
            _documentContainer.Arrange(new Rect(new Point(0.0, 0.0), arrangeSize));

            base.ArrangeOverride(arrangeSize);
            return arrangeSize;
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Creates the static members of DocumentGridPage's Visual Tree.
        /// </summary>
        private void Init()
        {
            //Create the DocumentPageView, which will display our
            //content.
            _documentPageView = new DocumentPageView();
            _documentPageView.ClipToBounds = true;
            _documentPageView.StretchDirection = StretchDirection.Both;
            _documentPageView.PageNumber = int.MaxValue;

            //Create the content control that contains the page content.
            _documentContainer = new ContentControl();
            _documentContainer.Content = _documentPageView;

            _loaded = false;
        }

        /// <summary>
        /// Handles the GetPageCompleted event raised by the DocumentPaginator.
        /// At this point, we'll set the _loaded flag to indicate the page is loaded
        /// and fire the PageLoaded event.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">Details about this event.</param>
        private void OnGetPageCompleted(object sender, GetPageCompletedEventArgs e)
        {
            //If the GetPageCompleted action completed successfully
            //and is our page then we'll set the flag and fire the event.
            if (!_disposed &&
                e != null &&
                !e.Cancelled &&
                e.Error == null &&
                e.PageNumber != int.MaxValue &&
                e.PageNumber == this.PageNumber &&
                e.DocumentPage != null &&
                e.DocumentPage != DocumentPage.Missing)
            {
                _loaded = true;

                if (PageLoaded != null)
                {
                    PageLoaded(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        protected void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                //Detach the GetPageCompleted event from the content.
                if (_paginator != null)
                {
                    _paginator.GetPageCompleted -= new GetPageCompletedEventHandler(OnGetPageCompleted);
                    _paginator = null;
                }

                //Dispose our DocumentPageView.
                IDisposable dpv = _documentPageView as IDisposable;
                if (dpv != null)
                {
                    dpv.Dispose();
                }
            }
        }

        /// <summary>
        /// Checks if the instance is already disposed, throws if so.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(DocumentPageView).ToString());
            }
        }

        #endregion Private Methods

        #region IDisposable Members

        /// <summary>
        /// Dispose the object.
        /// </summary>
        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);

            this.Dispose();
        }

        #endregion IDisposable Members

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields
        private bool hasAddedChildren;
        private DocumentPaginator _paginator;
        private DocumentPageView _documentPageView;
        private ContentControl _documentContainer;

        private bool _showPageBorders;
        private bool _loaded;

        private bool _disposed;

        #endregion Private Fields
    }
}


