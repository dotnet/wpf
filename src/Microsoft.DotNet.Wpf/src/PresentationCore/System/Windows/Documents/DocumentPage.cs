// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Base for pages returned from the Paginator.GetPage. 
//
//

using System.Windows.Media;     // Visual

namespace System.Windows.Documents
{
    /// <summary>
    /// Base for pages returned from the Paginator.GetPage.
    /// </summary>
    public class DocumentPage : IDisposable
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="visual">The visual representation of this page.</param>
        public DocumentPage(Visual visual)
        {
            _visual = visual;
            _pageSize = Size.Empty;
            _bleedBox = Rect.Empty;
            _contentBox = Rect.Empty;
        }

        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="visual">The visual representation of this page.</param>
        /// <param name="pageSize">The size of the page, including margins.</param>
        /// <param name="bleedBox">The bounds the ink drawn by the page.</param>
        /// <param name="contentBox">The bounds of the content within the page.</param>
        public DocumentPage(Visual visual, Size pageSize, Rect bleedBox, Rect contentBox)
        {
            _visual = visual;
            _pageSize = pageSize;
            _bleedBox = bleedBox;
            _contentBox = contentBox;
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Dispose the page.
        /// </summary>
        public virtual void Dispose()
        {
            _visual = null;
            _pageSize = Size.Empty;
            _bleedBox = Rect.Empty;
            _contentBox = Rect.Empty;
            OnPageDestroyed(EventArgs.Empty);
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The visual representation of this page.
        /// </summary>
        public virtual Visual Visual
        {
            get { return _visual;  }
        }

        /// <summary>
        /// The size of the page, including margins.
        /// </summary>
        public virtual Size Size
        {
            get
            {
                if (_pageSize == Size.Empty && _visual != null)
                {
                    return VisualTreeHelper.GetContentBounds(_visual).Size;
                }
                return _pageSize;
            }
        }

        /// <summary>
        /// The bounds the ink drawn by the page, relative to the page size.
        /// </summary>
        public virtual Rect BleedBox
        {
            get
            {
                if (_bleedBox == Rect.Empty)
                {
                    return new Rect(this.Size);
                }
                return _bleedBox;
            }
        }

        /// <summary>
        /// The bounds of the content within the page, relative to the page size.
        /// </summary>
        public virtual Rect ContentBox
        {
            get
            {
                if (_contentBox == Rect.Empty)
                {
                    return new Rect(this.Size);
                }
                return _contentBox;
            }
        }

        /// <summary>
        /// Static representation of a non-existent page.
        /// </summary>
        public static readonly DocumentPage Missing = new MissingDocumentPage();

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Public Events
        //
        //-------------------------------------------------------------------

        #region Public Events

        /// <summary>
        /// Called when the Visual for this page has been destroyed and 
        /// can no longer be used for display.
        /// </summary>
        public event EventHandler PageDestroyed;

        #endregion Public Events

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Override for subclasses that wish to add logic when this event is fired.
        /// </summary>
        /// <param name="e">Event arguments for the PageDestroyed event.</param>
        protected void OnPageDestroyed(EventArgs e)
        {
            if (this.PageDestroyed != null)
            {
                this.PageDestroyed(this, e);
            }
        }

        /// <summary>
        /// Sets the visual representation of this page.
        /// </summary>
        /// <param name="visual">The visual representation of this page.</param>
        protected void SetVisual(Visual visual)
        {
            _visual = visual;
        }

        /// <summary>
        /// Sets the size of the page.
        /// </summary>
        /// <param name="size">The size of the page, including margins.</param>
        protected void SetSize(Size size)
        {
            _pageSize = size;
        }

        /// <summary>
        /// Sets the bounds the ink drawn by the page.
        /// </summary>
        /// <param name="bleedBox">The bounds the ink drawn by the page, relative to the page size.</param>
        protected void SetBleedBox(Rect bleedBox)
        {
            _bleedBox = bleedBox;
        }

        /// <summary>
        /// Sets the bounds of the content within the page.
        /// </summary>
        /// <param name="contentBox">The bounds of the content within the page, relative to the page size.</param>
        protected void SetContentBox(Rect contentBox)
        {
            _contentBox = contentBox;
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Protected Fields
        //
        //-------------------------------------------------------------------

        #region Protected Fields

        /// <summary>
        /// The visual representation of this page.
        /// </summary>
        private Visual _visual;

        /// <summary>
        /// The size of the page, including margins.
        /// </summary>
        private Size _pageSize;

        /// <summary>
        /// The bounds the ink drawn by the page, relative to the page size.
        /// </summary>
        private Rect _bleedBox;

        /// <summary>
        /// The bounds of the content within the page, relative to the page size.
        /// </summary>
        private Rect _contentBox;

        #endregion Protected Fields

        //-------------------------------------------------------------------
        //
        //  Missing
        //
        //-------------------------------------------------------------------

        #region Missing

        private sealed class MissingDocumentPage : DocumentPage
        {
            public MissingDocumentPage() : base(null, Size.Empty, Rect.Empty, Rect.Empty) { }
            public override void Dispose() {}
        }

        #endregion Missing
    }
}
