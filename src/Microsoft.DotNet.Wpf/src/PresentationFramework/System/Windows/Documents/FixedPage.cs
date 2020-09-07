// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements the FixedPage element
// Spec FixedPanelPage.mht
//

namespace System.Windows.Documents
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO.Packaging;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents.DocumentStructures;
    using System.Windows.Input;
    using System.Windows.Markup;
    using System.Windows.Media;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using MS.Internal;
    using MS.Internal.Documents;
    using MS.Internal.Utility;
    
    using BuildInfo = MS.Internal.PresentationFramework.BuildInfo;

    //=====================================================================
    /// <summary>
    /// FixedPage is the container element for a metafile that represents 
    /// a single page of portable, high-fidelity content.
    /// 
    /// As an object that represents a static page of content, the primary 
    /// usage scenario for a FixedPage is inside a FixedDocument, a control 
    /// that is specialized to represent FixedPages to the pagination architecture.  
    /// The secondary scenario is to place a FixedPage inside a generic paginating 
    /// control such as the FlowDocument; for this scenario, the FixedPage is configured 
    /// to automatically set page breaks at the beginning and end of its content.
    /// </summary>
    [ContentProperty("Children")]
    public sealed class FixedPage : FrameworkElement, IAddChildInternal, IFixedNavigate, IUriContext
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        static FixedPage()
        {
            FrameworkPropertyMetadata metadata = new FrameworkPropertyMetadata(FlowDirection.LeftToRight, FrameworkPropertyMetadataOptions.AffectsParentArrange);
            metadata.CoerceValueCallback = new CoerceValueCallback(CoerceFlowDirection);
            FlowDirectionProperty.OverrideMetadata(typeof(FixedPage), metadata);
            // This puts the origin always at the top left of the page and prevents mirroring unless this is overridden.
        }

        /// <summary>
        ///     Default FixedPage constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public FixedPage() : base()
        {
            Init();
        }
        #endregion Constructors


        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------
        
        #region Public Methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() 
        {
            return new System.Windows.Automation.Peers.FixedPageAutomationPeer(this);
        }

        /// <summary>
        /// Responds to mouse wheel event, used to update debug visuals.
        /// MouseWheelEvent handler, initializes the context menu.
        /// </summary>
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
#if DEBUG        
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                int delta = e.Delta;
                e.Handled = true;

                if (delta > 0)
                {
                    _drawDebugVisual--;
                }
                else
                {
                    _drawDebugVisual++;
                }
                    
                _drawDebugVisual = _drawDebugVisual % (int)DrawDebugVisual.LastOne;

                if (_drawDebugVisual < 0)
                {
                    _drawDebugVisual += (int)DrawDebugVisual.LastOne;
                }

                InvalidateVisual();

                //
                // For container, the first child of element is always a Path with Fill.
                //
                if (_uiElementCollection.Count != 0)
                {
                    Path path = _uiElementCollection[0] as Path;
                    if (path != null)
                    {
                        if (_drawDebugVisual == 0)
                        {
                            path.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            path.Visibility = Visibility.Hidden;
                        }
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Override from UIElement
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            // Draw background in rectangle inside border.
            Brush background = this.Background;
            if (background != null)
            {
                dc.DrawRectangle(background,
                                 null,
                                 new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            }
#if DEBUG

            AdornerLayer al = AdornerLayer.GetAdornerLayer(this);

            if (al != null)
            {
                Adorner[] adorners = al.GetAdorners(this);
                if (adorners != null && adorners.Length > 0)
                {
                    al.Update(this);
                }
            }

#endif
        }

        ///<summary>
        /// This method is called to Add the object as a child of the Panel.  This method is used primarily
        /// by the parser.
        ///</summary>
        /// <exception cref="ArgumentNullException">value is NULL.</exception>
        /// <exception cref="ArgumentException">value is not of type UIElement.</exception>        
        ///<param name="value">
        /// The object to add as a child; it must be a UIElement.
        ///</param>
        /// <ExternalAPI/>
        void IAddChild.AddChild (Object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            UIElement uie = value as UIElement;

            if (uie == null)
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(UIElement)), "value");
            }

            Children.Add(uie);
        }

        ///<summary>
        /// This method is called by the parser when text appears under the tag in markup.
        /// As default Panels do not support text, calling this method has no effect if the
        /// text is all whitespace.  Passing non-whitespace text throws an exception.
        ///</summary>
        /// <exception cref="ArgumentException">text contains non-whitespace text.</exception>
        ///<param name="text">
        /// Text to add as a child.
        ///</param>
        void IAddChild.AddText (string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }

        /// <summary>
        /// Reads the attached property Left from the given element.
        /// </summary>
        /// <exception cref="ArgumentNullException">element is NULL.</exception>
        /// <param name="element">The element from which to read the Left attached property.</param>
        /// <returns>The property's Length value.</returns>
        /// <seealso cref="Canvas.LeftProperty" />
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        [AttachedPropertyBrowsableForChildren()]
        public static double GetLeft(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            return (double)element.GetValue(LeftProperty);
        }

        /// <summary>
        /// Writes the attached property Left to the given element.
        /// </summary>
        /// <exception cref="ArgumentNullException">element is NULL.</exception>
        /// <param name="element">The element to which to write the Left attached property.</param>
        /// <param name="length">The Length to set</param>
        /// <seealso cref="Canvas.LeftProperty" />
        public static void SetLeft(UIElement element, double length)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            element.SetValue(LeftProperty, length);
        }

        /// <summary>
        /// Reads the attached property Top from the given element.
        /// </summary>
        /// <exception cref="ArgumentNullException">element is NULL.</exception>
        /// <param name="element">The element from which to read the Top attached property.</param>
        /// <returns>The property's Length value.</returns>
        /// <seealso cref="Canvas.TopProperty" />
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        [AttachedPropertyBrowsableForChildren()]
        public static double GetTop(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            return (double)element.GetValue(TopProperty);
        }

        /// <summary>
        /// Writes the attached property Top to the given element.
        /// </summary>
        /// <exception cref="ArgumentNullException">element is NULL.</exception>
        /// <param name="element">The element to which to write the Top attached property.</param>
        /// <param name="length">The Length to set</param>
        /// <seealso cref="Canvas.TopProperty" />
        public static void SetTop(UIElement element, double length)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            element.SetValue(TopProperty, length);
        }

        /// <summary>
        /// Reads the attached property Right from the given element.
        /// </summary>
        /// <exception cref="ArgumentNullException">element is NULL.</exception>
        /// <param name="element">The element from which to read the Right attached property.</param>
        /// <returns>The property's Length value.</returns>
        /// <seealso cref="Canvas.RightProperty" />
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        [AttachedPropertyBrowsableForChildren()]
        public static double GetRight(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            return (double)element.GetValue(RightProperty);
        }

        /// <summary>
        /// Writes the attached property Right to the given element.
        /// </summary>
        /// <exception cref="ArgumentNullException">element is NULL.</exception>
        /// <param name="element">The element to which to write the Right attached property.</param>
        /// <param name="length">The Length to set</param>
        /// <seealso cref="Canvas.RightProperty" />
        public static void SetRight(UIElement element, double length)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            element.SetValue(RightProperty, length);
        }

        /// <summary>
        /// Reads the attached property Bottom from the given element.
        /// </summary>
        /// <exception cref="ArgumentNullException">element is NULL.</exception>
        /// <param name="element">The element from which to read the Bottom attached property.</param>
        /// <returns>The property's Length value.</returns>
        /// <seealso cref="Canvas.BottomProperty" />
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        [AttachedPropertyBrowsableForChildren()]
        public static double GetBottom(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            return (double)element.GetValue(BottomProperty);
        }

        /// <summary>
        /// Writes the attached property Bottom to the given element.
        /// </summary>
        /// <exception cref="ArgumentNullException">element is NULL.</exception>
        /// <param name="element">The element to which to write the Bottom attached property.</param>
        /// <param name="length">The Length to set</param>
        /// <seealso cref="Canvas.BottomProperty" />
        public static void SetBottom(UIElement element, double length)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            element.SetValue(BottomProperty, length);
        }

        /// <summary>
        /// Reads the attached property NavigateUri from the given element.
        /// </summary>
        /// <exception cref="ArgumentNullException">element is NULL.</exception>
        /// <remarks>Should be kept here for compatibility since the attached property has moved from FixedPage to Hyperlink.</remarks>
        [AttachedPropertyBrowsableForChildren()]
        public static Uri GetNavigateUri(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            return (Uri)element.GetValue(NavigateUriProperty);
        }

        /// <summary>
        /// Writes the attached property NavigateUri to the given element.
        /// </summary>
        /// <exception cref="ArgumentNullException">element is NULL.</exception>
        /// <remarks>Should be kept here for compatibility since the attached property has moved from FixedPage to Hyperlink.</remarks>
        public static void SetNavigateUri(UIElement element, Uri uri)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            element.SetValue(NavigateUriProperty, uri);
        }

        #endregion
        
        #region IUriContext
        /// <summary>
        /// <see cref="IUriContext.BaseUri" />
        /// </summary>
        Uri IUriContext.BaseUri
        {
            get { return (Uri) GetValue(BaseUriHelper.BaseUriProperty); }
            set { SetValue(BaseUriHelper.BaseUriProperty, value); }
        }

        /// <summary>
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                return this.Children.GetEnumerator();
            }
        }
        
        
        #endregion IUriContext

        //--------------------------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------
        
        #region Public Properties

        /// <summary>
        /// Returns a UIElementCollection of children for user to add/remove children manually
        /// Returns null if Panel is data-bound (no manual control of children is possible,
        /// the associated ItemsControl completely overrides children)
        /// Note: the derived Panel classes should never use this collection for any
        /// internal purposes! They should use Children instead, because Children
        /// is always present and either is a mirror of public Children collection (in case of Direct Panel)
        /// or is generated from data binding.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public UIElementCollection Children
        {
            get
            {
                if(_uiElementCollection == null) //nobody used it yet
                {
                    _uiElementCollection = CreateUIElementCollection(this);
                }

                return _uiElementCollection;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty PrintTicketProperty =
                DependencyProperty.RegisterAttached(
                        "PrintTicket", 
                        typeof(object), 
                        typeof(FixedPage),
                        new FrameworkPropertyMetadata((object)null));
                                                  
        /// <summary>
        /// Get/Set PrintTicket Property
        /// </summary>
        public object PrintTicket
        {
            get { return GetValue(PrintTicketProperty); }
            set { SetValue(PrintTicketProperty,value); }
        }
        
        /// <summary>
        /// The Background property defines the brush used to fill the area between borders.
        /// </summary>
        public Brush Background
        {
            get { return (Brush) GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }
        
        /// <summary>
        /// DependencyProperty for <see cref="Background" /> property.
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty =
                Panel.BackgroundProperty.AddOwner(
                        typeof(FixedPage),
                        new FrameworkPropertyMetadata((Brush)Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// This is the dependency property registered for the Canvas' Left attached property.
        /// 
        /// The Left property is read by a Canvas on its children to determine where to position them.
        /// The child's offset from this property does not have an effect on the Canvas' own size.
        /// If you want offset to affect size, set the child's Margin property instead.
        /// Conflict between the Left and Right properties is resolved in favor of Left.
        /// Percentages are with respect to the Canvas' size.
        /// </summary>
        /// <seealso cref="FrameworkElement.Margin" />
        public static readonly DependencyProperty LeftProperty =
                DependencyProperty.RegisterAttached(
                        "Left", 
                        typeof(double), 
                        typeof(FixedPage),
                        new FrameworkPropertyMetadata(Double.NaN, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        /// <summary>
        /// This is the dependency property registered for the Canvas' Top attached property.
        /// 
        /// The Top property is read by a Canvas on its children to determine where to position them.
        /// The child's offset from this property does not have an effect on the Canvas' own size.
        /// If you want offset to affect size, set the child's Margin property instead.
        /// Conflict between the Top and Bottom properties is resolved in favor of Top.
        /// Percentages are with respect to the Canvas' size.
        /// </summary>
        /// <seealso cref="FrameworkElement.Margin" />
        public static readonly DependencyProperty TopProperty =
                DependencyProperty.RegisterAttached(
                        "Top", 
                        typeof(double), 
                        typeof(FixedPage),
                        new FrameworkPropertyMetadata(Double.NaN, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        /// <summary>
        /// This is the dependency property registered for the Canvas' Right attached property.
        /// 
        /// The Right property is read by a Canvas on its children to determine where to position them.
        /// The child's offset from this property does not have an effect on the Canvas' own size.
        /// If you want offset to affect size, set the child's Margin property instead.
        /// Conflict between the Left and Right properties is resolved in favor of Right.
        /// Percentages are with respect to the Canvas' size.
        /// </summary>
        /// <seealso cref="FrameworkElement.Margin" />
        public static readonly DependencyProperty RightProperty = 
                DependencyProperty.RegisterAttached(
                        "Right", 
                        typeof(double), 
                        typeof(FixedPage),
                        new FrameworkPropertyMetadata(Double.NaN, FrameworkPropertyMetadataOptions.AffectsParentArrange));


        /// <summary>
        /// This is the dependency property registered for the Canvas' Bottom attached property.
        /// 
        /// The Bottom property is read by a Canvas on its children to determine where to position them.
        /// The child's offset from this property does not have an effect on the Canvas' own size.
        /// If you want offset to affect size, set the child's Margin property instead.
        /// Conflict between the Top and Bottom properties is resolved in favor of Bottom.
        /// Percentages are with respect to the Canvas' size.
        /// </summary>
        /// <seealso cref="FrameworkElement.Margin" />
        public static readonly DependencyProperty BottomProperty =
                DependencyProperty.RegisterAttached(
                        "Bottom", 
                        typeof(double), 
                        typeof(FixedPage),
                        new FrameworkPropertyMetadata(Double.NaN, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        /// <summary>
        ///     The DependencyProperty for the ContentBox property.
        /// </summary>
        public Rect ContentBox
        {
            get { return (Rect) GetValue(ContentBoxProperty); }
            set { SetValue(ContentBoxProperty, value); }
        }
        
        /// <summary>
        ///     The DependencyProperty for the ContentBox property.
        /// </summary>
        public static readonly DependencyProperty ContentBoxProperty =
                DependencyProperty.Register(
                        "ContentBox",
                        typeof(Rect),
                        typeof(FixedPage),
                        new FrameworkPropertyMetadata(Rect.Empty));

        /// <summary>
        ///     The DependencyProperty for the BleedBox property.
        /// </summary>
        public Rect BleedBox
        {
            get { return (Rect) GetValue(BleedBoxProperty); }
            set { SetValue(BleedBoxProperty, value); }
        }
        
        /// <summary>
        ///     The DependencyProperty for the BleedBox property.
        /// </summary>
        public static readonly DependencyProperty BleedBoxProperty =
                DependencyProperty.Register(
                        "BleedBox",
                        typeof(Rect),
                        typeof(FixedPage),
                        new FrameworkPropertyMetadata(Rect.Empty));

        /// <summary>
        /// Contains the target URI to navigate when a hyperlink is clicked
        /// </summary>
        public static readonly DependencyProperty NavigateUriProperty =
                        DependencyProperty.RegisterAttached(
                        "NavigateUri", 
                        typeof(Uri), 
                        typeof(FixedPage),
                        new FrameworkPropertyMetadata(
                                (Uri) null,
                                new PropertyChangedCallback(Hyperlink.OnNavigateUriChanged),
                                new CoerceValueCallback(Hyperlink.CoerceNavigateUri)));

        #endregion

        protected internal override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);

            if (oldParent == null)
            {
                HighlightVisual highlightVisual = HighlightVisual.GetHighlightVisual(this);

                AdornerLayer al = AdornerLayer.GetAdornerLayer(this);        

                if (highlightVisual == null && al != null)
                {
                    //Get Page Content
                    PageContent pc = LogicalTreeHelper.GetParent(this) as PageContent;
                    if (pc != null)
                    {
                        //Get FixedDocument
                        FixedDocument doc = LogicalTreeHelper.GetParent(pc) as FixedDocument;
                        if (doc != null)
                        {
                            if (al != null)
                            {
                                //The Text Selection adorner must have predefined ZOrder MaxInt/2, 
                                //we assign the ZOrder to annotation adorners respectively
                                int zOrder = System.Int32.MaxValue / 2; 
                                al.Add(new HighlightVisual(doc, this),zOrder);
                            }
                        }
                    }
                }
#if DEBUG
                DebugVisualAdorner debugVisualAd = DebugVisualAdorner.GetDebugVisual(this);
                if (debugVisualAd == null && al != null)
                {
                    al.Add(new DebugVisualAdorner(this), System.Int32.MaxValue / 4);
                }
#endif                
            }
        }

        private static object CoerceFlowDirection(DependencyObject page, Object flowDirection)
        {
            return FlowDirection.LeftToRight;
        }

        internal static Uri GetLinkUri(IInputElement element, Uri inputUri)
        {
            DependencyObject dpo = element as DependencyObject;
            Debug.Assert(dpo != null, "GetLinkUri shouldn't be called for non-DependencyObjects.");

            if (inputUri != null)
            {
                //
                // First remove the fragment, this is to prevent escape in file URI case, for example,
                // if the inputUri = "..\..\myFile.xaml#fragment", without removing the fragment first,
                // the absoluteUri would be "file:///...../myFile.xaml%23fragment", note # is escaped to
                // %23.
                // If indeed the file contains # such as "This#File.xaml", it should set 
                // FixedPage.NavigateUri="This%23File.xaml"
                //

                //
                // Copy from BindUriHelper.GetFragment STARTS.
                // It should have a version return #, otherwise, you can 
                // not tell betweeen myFile.xaml and myFile.xaml#
                //
                Uri workuri = inputUri;
                if (inputUri.IsAbsoluteUri == false)
                {
                    // this is a relative uri, and Fragement() doesn't work with relative uris.  The base uri is completley irrelevant 
                    // here and will never affect the returned fragment, but the method requires something to be there.  Therefore, 
                    // we will use "http://microsoft.com" as a convenient substitute.
                    workuri = new Uri(new Uri("http://microsoft.com/"), inputUri);
                }
                // Copy from BindUriHelper.GetFragment ENDS.

                // the fragmene will include # sign
                String fragment = workuri.Fragment;

                int fragmentLength = (fragment == null) ? 0 : fragment.Length;
                if (fragmentLength != 0)
                {
                    String inputUriString = inputUri.ToString();
                    String inputUriStringWithoutFragment = inputUriString.Substring(0, inputUriString.IndexOf('#'));
                    inputUri = new Uri(inputUriStringWithoutFragment, UriKind.RelativeOrAbsolute);

                    //Only Check for the startpart uri if the hyperlink is relative, else it's not part of the package
                    if (inputUri.IsAbsoluteUri == false)
                    {
                        String startPartUriString = GetStartPartUriString(dpo);
                        if (startPartUriString != null)
                        {
                            inputUri = new Uri(startPartUriString, UriKind.RelativeOrAbsolute);
                        }
                    }
                }
                //
                // Resolve to absolute URI
                //
                Uri baseUri = BaseUriHelper.GetBaseUri(dpo);
                Uri absoluteUri = BindUriHelper.GetUriToNavigate(dpo, baseUri, inputUri);
                if (fragmentLength != 0)
                {
                    StringBuilder absoluteUriString = new StringBuilder(absoluteUri.ToString());
                    absoluteUriString.Append(fragment);
                    absoluteUri = new Uri(absoluteUriString.ToString(), UriKind.RelativeOrAbsolute);
                }

                return absoluteUri;
            }
            return null;
        }
        //--------------------------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------------------------

        //--------------------------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------------------------
        
        #region Protected Methods


        /// <summary>
        /// Gets the Visual children count.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                if (_uiElementCollection == null)
                {
                    return 0;
                }
                else
                {
                    return _uiElementCollection.Count;
                }
            }
        }

        /// <summary>
        /// Gets the Visual child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (_uiElementCollection == null)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }
            return _uiElementCollection[index];            
        }

        /// <summary>
        /// Creates a new UIElementCollection. Panel-derived class can create its own version of
        /// UIElementCollection -derived class to add cached information to every child or to
        /// intercept any Add/Remove actions (for example, for incremental layout update)
        /// </summary>
        private UIElementCollection CreateUIElementCollection(FrameworkElement logicalParent)
        {
            return new UIElementCollection(this, logicalParent);
        }


        /// <summary>
        /// Updates DesiredSize of the Canvas.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <remarks>
        /// Canvas measures each of its children accounting for any of their FrameworkElement properties.
        /// Children will be passed either the parent's constraint less any margin on the child or their
        /// explicitly specified (Min/Max)Width/Height properties.
        /// 
        /// If it has enough space, Canvas will return a size large enough to accommodate all children's 
        /// desired sizes and margins.  Children's Top/Left/Bottom/Right properties are not considered in
        /// this method.  If it does not have enough space to accommodate its children in a dimension, this
        /// will simply return the full constraint in that dimension.
        /// </remarks>
        /// <param name="constraint">Constraint size is an "upper limit" that Canvas should not exceed.</param>
        /// <returns>Canvas' desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Size childConstraint = new Size(Double.PositiveInfinity, Double.PositiveInfinity);
            
            foreach (UIElement child in Children)
            {
                child.Measure(childConstraint);
            }

            return new Size();
        }
        
        /// <summary>
        /// Canvas computes a position for each of its children taking into account their margin and
        /// attached Canvas properties: Top, Left, Bottom, and Right.  If specified, Top or Left take
        /// priority over Bottom or Right.
        /// 
        /// Canvas will also arrange each of its children.
        /// </summary>
        /// <param name="arrangeSize">Size that Canvas will assume to position children.</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            //Canvas arranges children at their DesiredSize.
            //This means that Margin on children is actually respected and added
            //to the size of layout partition for a child. 
            //Therefore, is Margin is 10 and Left is 20, the child's ink will start at 30.
            
            foreach (UIElement child in Children)
            {
                double x = 0;
                double y = 0;
                

                //Compute offset of the child:
                //If Left is specified, then Right is ignored
                //If Left is not specified, then Right is used
                //If both are not there, then 0
                double left = GetLeft(child);
                if(!DoubleUtil.IsNaN(left)) 
                {
                    x = left; 
                }
                else
                {
                    double right = GetRight(child);

                    if(!DoubleUtil.IsNaN(right)) 
                    {
                        x = arrangeSize.Width - child.DesiredSize.Width - right;
                    }
                }
                
                double top = GetTop(child);
                if(!DoubleUtil.IsNaN(top)) 
                {
                    y = top; 
                }
                else
                {
                    double bottom = GetBottom(child);

                    if(!DoubleUtil.IsNaN(bottom)) 
                    {
                        y = arrangeSize.Height - child.DesiredSize.Height - bottom;
                    }
                }
                
                child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
            }
            return arrangeSize;
        }


        #endregion

        //--------------------------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------------------------

        #region Internal methods

        void IFixedNavigate.NavigateAsync(string elementID)
        {
            FixedHyperLink.NavigateToElement(this, elementID);
        }


        UIElement IFixedNavigate.FindElementByID(string elementID, out FixedPage rootFixedPage)
        {
            UIElement uiElementRet = null;
            rootFixedPage = this;

            // We need iterate through the PageContentCollect first.
            UIElementCollection elementCollection = this.Children;
            UIElement uiElement;
            DependencyObject  node ;

            for (int i = 0, n = elementCollection.Count; i < n; i++)
            {
                uiElement = elementCollection[i];

                node = LogicalTreeHelper.FindLogicalNode(uiElement, elementID);
                if (node != null)
                {
                    uiElementRet = node as UIElement;
                    break;
                }
            }

            return uiElementRet;
        }

        // Create a FixedNode representing this glyphs or image
        internal FixedNode CreateFixedNode(int pageIndex, UIElement e)
        {
            // Should we check here to make sure this is a selectable element?
            Debug.Assert(e != null);
            return _CreateFixedNode(pageIndex, e);
        }

        internal Glyphs GetGlyphsElement(FixedNode node)
        {
            return GetElement(node) as Glyphs;
        }

        // FixedNode represents a leaf node. It contains a path 
        // from root to the leaf in the form of child index.
        //  [Level1 ChildIndex] [Level2 ChildIndex] [Level3 ChildIndex]...
        internal DependencyObject GetElement(FixedNode node)
        {
            int currentLevelIndex = node[1];
            if (!(currentLevelIndex >= 0 && currentLevelIndex <= this.Children.Count))
            {
                return null;
            }

#if DEBUG
            if (node.ChildLevels > 1)
            {
                DocumentsTrace.FixedFormat.FixedDocument.Trace(string.Format("FixedPage.GetUIElement {0} is nested element", node));
            }
#endif

            DependencyObject element = this.Children[currentLevelIndex];
            for (int level = 2; level <= node.ChildLevels; level++)
            {
                // Follow the path if necessary
                currentLevelIndex = node[level];
                if (element is Canvas)
                {
                    // Canvas is a known S0 grouping element.
                    // Boundary Node only would appear in first level!
                    Debug.Assert(currentLevelIndex >= 0 && currentLevelIndex <= ((Canvas)element).Children.Count);
                    element = ((Canvas)element).Children[currentLevelIndex];
                }
                else 
                {
                    DocumentsTrace.FixedFormat.FixedDocument.Trace(string.Format("FixedPage.GeElement {0} is non S0 grouping element in L[{1}]!", node, level));
                    IEnumerable currentChildrens = LogicalTreeHelper.GetChildren((DependencyObject)element);
                    if (currentChildrens == null)
                    {
                        DocumentsTrace.FixedFormat.FixedDocument.Trace(string.Format("FixedPage.GetElement {0} is NOT a grouping element in L[{1}]!!!", node, level));
                        return null;
                    }

                    // We have no uniform way to do random access for this element. 
                    // This should never happen for S0 conforming document. 
                    int childIndex = -1;
                    IEnumerator itor = currentChildrens.GetEnumerator();
                    while (itor.MoveNext())
                    {
                        childIndex++;
                        if (childIndex == currentLevelIndex)
                        {
                            element = (DependencyObject)itor.Current;
                            break;
                        }
                    }
                }
            }

#if DEBUG
            if (!(element is Glyphs))
            {
                DocumentsTrace.FixedFormat.FixedDocument.Trace(string.Format("FixedPage.GetElement{0} is non-Glyphs", node));
            }
#endif
            return element;
        }

#endregion Internal Methods

        #region Internal Properties

        internal String StartPartUriString
        {
            get
            {
                return _startPartUriString;
            }
            set
            {
                _startPartUriString = value;
            }
        }

        private String _startPartUriString;
#if DEBUG
        internal FixedPageStructure FixedPageStructure
        {
            get 
            {
                return _fixedPageStructure;
            }
            set 
            {
                _fixedPageStructure = value;
            }

        }

        internal int DrawDebugVisualSelection
        {
            get
            {
                return _drawDebugVisual;
            }
        }

#endif

#endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // private Properties
        //
        //---------------------------------------------------------------------
        private UIElementCollection _uiElementCollection;

        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        #region Private Methods

        private void Init()
        {
            if (XpsValidatingLoader.DocumentMode)
            {
                this.InheritanceBehavior = InheritanceBehavior.SkipAllNext;
            }
        }

        internal StoryFragments GetPageStructure()
        {
            StoryFragments sf;

            sf = FixedDocument.GetStoryFragments(this);

            return sf;
        }

        internal int[] _CreateChildIndex(DependencyObject e)
        {
            ArrayList childPath = new ArrayList();
            while (e != this)
            {
                DependencyObject parent = LogicalTreeHelper.GetParent(e);
                int childIndex = -1;
                if (parent is FixedPage)
                {
                    childIndex = ((FixedPage)parent).Children.IndexOf((UIElement)e);
                }
                else if (parent is Canvas)
                {
                    childIndex = ((Canvas)parent).Children.IndexOf((UIElement)e);
                }
                else
                {
                    IEnumerable currentChildrens = LogicalTreeHelper.GetChildren(parent);
                    Debug.Assert(currentChildrens != null);

                    // We have no uniform way to do random access for this element. 
                    // This should never happen for S0 conforming document. 
                    IEnumerator itor = currentChildrens.GetEnumerator();
                    while (itor.MoveNext())
                    {
                        childIndex++;
                        if (itor.Current == e)
                        {
                            break;
                        }
                    }
                }
                childPath.Insert(0, childIndex);
                e = parent;
            }
            while (e != this) ;

            return (int[])childPath.ToArray(typeof(int));
        }

        // Making this function private and only expose the versions
        // that take S0 elements as parameter. 
        private FixedNode _CreateFixedNode(int pageIndex, UIElement e)
        {
            return FixedNode.Create(pageIndex, _CreateChildIndex(e));
        }

        /// <summary>
        /// This function walks the logical tree for the FixedDocumentSequence and then
        /// retrieves its URI.  We use this Uri for fragment navigation during a FixedPage.OnClick
        /// event.
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private static String GetStartPartUriString(DependencyObject current)
        {
            //1) Get FixedPage from DependcyObject's InheritanceObject property

            DependencyObject obj = current;
            FixedPage fixedPage = current as FixedPage;
            while (fixedPage == null && obj != null)
            {
                obj = obj.InheritanceParent;
                fixedPage = obj as FixedPage;
            }

            if (fixedPage == null)
            {
                return null;
            }

            //2) Check if fixedPage StartPartUri is null
            if (fixedPage.StartPartUriString == null)
            {
                //3) Walk Logical Tree to the FixedDocumentSequence
                DependencyObject parent = LogicalTreeHelper.GetParent(current);
                while (parent != null)
                {
                    FixedDocumentSequence docSequence = parent as FixedDocumentSequence;
                    if (docSequence != null)
                    {
                        //4) Retrieve DocumentSequence Uri
                        Uri startPartUri = ((IUriContext)docSequence).BaseUri;
                        if (startPartUri != null)
                        {
                            String startPartUriString = startPartUri.ToString();

                            // If there is a fragment we need to strip it off
                            String fragment = startPartUri.Fragment;

                            int fragmentLength = (fragment == null) ? 0 : fragment.Length;
                            if (fragmentLength != 0)
                            {
                                fixedPage.StartPartUriString = startPartUriString.Substring(0, startPartUriString.IndexOf('#'));
                            }
                            else
                            {
                                fixedPage.StartPartUriString = startPartUri.ToString();
                            }
                        }
                        break;
                    }
                    parent = LogicalTreeHelper.GetParent(parent);
                }

                //If we don't have a starting part Uri, assign fixedPage.StartPartUriString to Empty so
                //we don't try to look it up again.
                if (fixedPage.StartPartUriString == null)
                {
                    fixedPage.StartPartUriString = String.Empty;
                }
            }

            if (fixedPage.StartPartUriString == String.Empty)
            {
                return null;
            }
            else
            {
                return fixedPage.StartPartUriString;
            }
        }

        #endregion

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------


#if DEBUG
        private FixedPageStructure _fixedPageStructure;
        //
        // The debugging features: set to true will draw the bounding box for each
        // line from the analyzed layout results.
        // 
        private int _drawDebugVisual = 0;

#endif
    }

#if DEBUG

    internal sealed class DebugVisualAdorner: Adorner
    {
        internal DebugVisualAdorner(FixedPage page) : base(page)
        {
            _fixedPage = page;
        }
        
        override protected void OnRender(DrawingContext dc)
        {
            if (_fixedPage.DrawDebugVisualSelection == (int) DrawDebugVisual.None)
            {
                return;
            }

            FixedPageStructure pageStructure = _fixedPage.FixedPageStructure;            
            Debug.Assert(pageStructure != null);
            
            if (_fixedPage.DrawDebugVisualSelection == (int) DrawDebugVisual.Glyphs)
            {
                if (pageStructure.FixedNodes != null)
                {
                    _RenderMarkupOrder(dc, pageStructure.FixedNodes);
                }
            }
            else if (_fixedPage.DrawDebugVisualSelection == (int) DrawDebugVisual.Lines)
            {
                pageStructure.RenderLines(dc);
            }
            else
            {
                if (pageStructure.FixedSOMPage != null)
                {
                    pageStructure.FixedSOMPage.Render(dc, null, (DrawDebugVisual) _fixedPage.DrawDebugVisualSelection);
                }
                else
                {
                    //Explicit document structure
                    FlowNode[] pageNodes = _fixedPage.FixedPageStructure.FlowNodes;
                    int flowOrder = 0;

                    foreach (FlowNode node in pageNodes)
                    {
                        if (node != null && node.FixedSOMElements != null)
                        {
                            foreach (FixedSOMElement somElement in node.FixedSOMElements)
                            {
                                somElement.Render(dc, flowOrder.ToString(), (DrawDebugVisual) _fixedPage.DrawDebugVisualSelection);
                                flowOrder++;
                            }
                        }
                    }
                }
            }
        }

        internal static DebugVisualAdorner GetDebugVisual(FixedPage page)
        {
            AdornerLayer al = AdornerLayer.GetAdornerLayer(page);
            DebugVisualAdorner debugVisualAd;

            if (al == null)
            {
                return null;
            }

            Adorner[] adorners = al.GetAdorners(page);

            if (adorners != null)
            {
                foreach (Adorner ad in adorners)
                {
                    debugVisualAd = ad as DebugVisualAdorner;
                    if (debugVisualAd != null)
                    {
                        return debugVisualAd;
                    }
                }
            }

            return null;
        }        

        private void _RenderMarkupOrder(DrawingContext dc, List<FixedNode> markupOrder)
        {
            int order = 0;
            foreach (FixedNode node in markupOrder)
            {
                DependencyObject ob = _fixedPage.GetElement(node);
                Glyphs glyphs = ob as Glyphs;
                Path path = ob as Path;
                if (glyphs != null)
                {
                    GlyphRun glyphRun = glyphs.ToGlyphRun();
                    Rect alignmentBox = glyphRun.ComputeAlignmentBox();
                    alignmentBox.Offset(glyphs.OriginX, glyphs.OriginY);    
                    GeneralTransform transform = glyphs.TransformToAncestor(_fixedPage);
                    alignmentBox = transform.TransformBounds(alignmentBox);
                    
                    Pen pen = new Pen(Brushes.Green, 1);
                    dc.DrawRectangle(null, pen , alignmentBox);
                    _RenderLabel(dc, order.ToString(), alignmentBox);

                    ++order;
                }
                else if (path != null)
                {
                    Geometry renderGeom = path.RenderedGeometry;
                    Pen backgroundPen = new Pen(Brushes.Black,1);
                    dc.DrawGeometry(null, backgroundPen, renderGeom);
                    _RenderLabel(dc, order.ToString(), renderGeom.Bounds);
                    ++order;
                }
            }
        }

        private void _RenderLabel(DrawingContext dc, string label, Rect boundingRect)
        {
            FormattedText ft = new FormattedText(label, 
                                        System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS,
                                        FlowDirection.LeftToRight,
                                        new Typeface("Arial"), 
                                        10,
                                        Brushes.White,
                                        GetDpi().PixelsPerDip);
            Point labelLocation = new Point(boundingRect.Left-25, (boundingRect.Bottom + boundingRect.Top)/2 - 10);
            Geometry geom = ft.BuildHighlightGeometry(labelLocation);
            Pen backgroundPen = new Pen(Brushes.Black,1);
            dc.DrawGeometry(Brushes.Black, backgroundPen, geom);
            dc.DrawText(ft, labelLocation);
            
        }

        private FixedPage _fixedPage;
        
    }
    
    

    internal enum DrawDebugVisual
    {
        None = 0,
        Glyphs = 1,
        Lines = 2,
        TextRuns = 3,
        Paragraphs = 4,
        Groups = 5,
        LastOne = 6
    }
#endif
}


