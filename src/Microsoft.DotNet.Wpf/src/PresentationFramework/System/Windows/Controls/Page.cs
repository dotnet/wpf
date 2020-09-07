// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implements the Avalon Page class
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;

using System.Windows.Controls.Primitives;
using System.Windows.Navigation;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Documents;

using MS.Internal.AppModel;
using MS.Internal.KnownBoxes;
using MS.Internal;
using MS.Utility;

//In order to avoid generating warnings about unknown message numbers and 
//unknown pragmas when compiling your C# source code with the actual C# compiler, 
//you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace System.Windows.Controls 
{  
    #region Page Class
  
    /// <summary>
    /// Public class Page
    /// </summary>
    [ContentProperty("Content")]
    public class Page : FrameworkElement, IWindowService, IAddChild
    {
        #region Constructors
        //----------------------------------------------
        //
        // Constructors
        //
        //----------------------------------------------        
        static Page()
        {
            // We use IWindowService change notifications to propagate the cached values to the Window
            Window.IWindowServiceProperty.OverrideMetadata(
                    typeof(Page), 
                    new FrameworkPropertyMetadata(new PropertyChangedCallback(_OnWindowServiceChanged)));

            // (Window/NavigationWindow and Page should not be focusable)
            // This makes Page non-focusable.  If FocusedElement is set on the Page, focus would
            // go to that element, otherwise, it will be null.  Page taking focus doesn't make 
            // sense, instead, it should be forwared to some more meaningful element in the tree.
            FocusableProperty.OverrideMetadata(typeof(Page), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
            
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Page), new FrameworkPropertyMetadata(typeof(Page)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(Page));
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Page()
        {
            // Initialize the _templateCache to the default value for TemplateProperty. 
            // If the default value is non-null then wire it to the current instance.
            PropertyMetadata metadata = TemplateProperty.GetMetadata(DependencyObjectType);
            ControlTemplate defaultValue = (ControlTemplate) metadata.DefaultValue;
            if (defaultValue != null)
            {
                OnTemplateChanged(this, new DependencyPropertyChangedEventArgs(TemplateProperty, metadata, null, defaultValue));
            }
        }
        #endregion Constructors
           
        #region IAddChild
        /// <summary>
        ///     Adds a child. This is called by the parser
        /// </summary>
        /// <param name="obj"></param>
        void IAddChild.AddChild(Object obj)
        {
             VerifyAccess();

             // if content is the first child or being cleared, set directly
             if (Content == null || obj == null)
             {
                 Content = obj;
             }
             else
             {
                 throw new InvalidOperationException(SR.Get(SRID.PageCannotHaveMultipleContent));
             }             
        }

        ///<summary>
        ///     This method is called by the parser when text appears under the tag in markup.
        ///     By default Page does not support text; calling this method has no effect.
        ///</summary>
        ///<param name="str">
        ///     Text to add as a child.
        ///</param>
        void IAddChild.AddText (string str)
        {            
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(str, this);
        }
        #endregion IAddChild

        #region LogicalTree
        /// <summary>
        ///     Returns enumerator to logical children
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                VerifyAccess();
                return new SingleChildEnumerator(Content);
            }
        }
        #endregion LogicalTree       

        #region Public Properties        

        /// <summary>
        ///     The DependencyProperty for the Content property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty ContentProperty = 
                ContentControl.ContentProperty.AddOwner(
                        typeof(Page), 
                        new FrameworkPropertyMetadata(new PropertyChangedCallback(OnContentChanged)));

        /// <summary>
        ///     Content of the Page
        /// </summary>
        /// <remarks>
        ///     Page only supports one child
        /// </remarks>
        public Object Content
        {
            get
            {
                VerifyAccess();
                return GetValue(ContentProperty);
            }
            set
            {
                VerifyAccess();
                SetValue(ContentProperty, value);
            }
        }

        // All these properties are implemented
        // as "bound" to the window service.  For 
        // example, getting WindowTitle will return Window.Title,
        // setting WindowTitle will set Window.Title. 
        //
       
        string IWindowService.Title
        { 
            get
            {
                VerifyAccess();
                if (WindowService == null)
                {
#pragma warning disable 6503
                    throw new InvalidOperationException(SR.Get(SRID.CannotQueryPropertiesWhenPageNotInTreeWithWindow));
#pragma warning restore 6503
                }
                return WindowService.Title;
            }
            
            set
            {
                VerifyAccess();
                if (WindowService == null)
                {
                    PageHelperObject._windowTitle = value;
                    PropertyIsSet(SetPropertyFlags.WindowTitle);
                }
                else if (_isTopLevel == true) // only top level page can set this property
                {
                    WindowService.Title = value;
                    PropertyIsSet(SetPropertyFlags.WindowTitle);
                }
            }
        }

        /// <summary>
        ///    Proxy for Window Title property
        /// </summary>
        [Localizability(LocalizationCategory.Title)]
        public string WindowTitle
        {
            get
            {
                VerifyAccess();
                return ((IWindowService)this).Title;
            }

            set
            {
                VerifyAccess();
                ((IWindowService)this).Title = value;
            }
        }

        internal bool ShouldJournalWindowTitle()
        {
            return IsPropertySet(SetPropertyFlags.WindowTitle);
        }

        /// <summary>
        /// Bound to IWindowService property
        /// </summary>
        double IWindowService.Height
        {
            get
            {
                VerifyAccess();
                if (WindowService == null)
                {
#pragma warning disable 6503
                    throw new InvalidOperationException(SR.Get(SRID.CannotQueryPropertiesWhenPageNotInTreeWithWindow));
#pragma warning restore 6503
                }                
                return WindowService.Height;
            }

            set
            {
                VerifyAccess();
                if (WindowService == null)
                {
                    PageHelperObject._windowHeight = value;
                    PropertyIsSet(SetPropertyFlags.WindowHeight);
                }
                else if (_isTopLevel == true)// only top level page can set this property
                {
                    if (!WindowService.UserResized)
                    {
                      WindowService.Height = value;
                    }
                    PropertyIsSet(SetPropertyFlags.WindowHeight);
                }
            }
        }

        /// <summary>
        ///     Proxy to Window.Height property
        /// </summary>
        public double WindowHeight
        {
            get
            {
                VerifyAccess();
                return ((IWindowService)this).Height;
            }

            set
            {
                VerifyAccess();
                ((IWindowService)this).Height = value;
            }
        }
        
        /// <summary>
        ///     Proxy to Window.Width property
        /// </summary>
        double IWindowService.Width
        {
            get
            {
                VerifyAccess();
                if (WindowService == null)
                {
#pragma warning disable 6503
                    throw new InvalidOperationException(SR.Get(SRID.CannotQueryPropertiesWhenPageNotInTreeWithWindow));
#pragma warning restore 6503
                }                
                return WindowService.Width;
            }

            set
            {
                VerifyAccess();
                if (WindowService == null)
                {
                    PageHelperObject._windowWidth = value;
                    PropertyIsSet(SetPropertyFlags.WindowWidth);
                }
                else if (_isTopLevel == true) // only top level page can set this property
                {
                    if (!WindowService.UserResized)
                    {
                      WindowService.Width = value;
                    }
                    PropertyIsSet(SetPropertyFlags.WindowWidth);
                }
            }
        }

        /// <summary>
        /// Bound to IWindowService.Width property
        /// </summary>
        public double WindowWidth
        {
            get
            {
                VerifyAccess();
                return ((IWindowService)this).Width;
            }

            set
            {
                VerifyAccess();
                ((IWindowService)this).Width = value;
            }
        }

        /// <summary>
        ///     The DependencyProperty for the Background property.
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty =
                Panel.BackgroundProperty.AddOwner(
                        typeof(Page),
                        new FrameworkPropertyMetadata(
                                Panel.BackgroundProperty.GetDefaultValue(typeof(Panel)),
                                FrameworkPropertyMetadataOptions.None));

        /// <summary>
        ///     An object that describes the background.
        /// </summary>
        [Category("Appearance")]
        public Brush Background
        {
            get { return (Brush) GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }
        

        /// <summary>
        ///     The DependencyProperty for the Title property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                "Title", typeof(string), typeof(Page),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnTitleChanged)));

        /// <summary>
        ///     An object that describes the Title.
        /// </summary>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // If the Title has changed we want to set the flag.
        static private void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Page)d).PropertyIsSet(SetPropertyFlags.Title);
        }

        /// <summary>
        /// Determines whether to show the default navigation UI.
        /// </summary>
        public bool ShowsNavigationUI
        {
            get
            {
                VerifyAccess();
                if (WindowService == null)
                {
#pragma warning disable 6503
                    throw new InvalidOperationException(SR.Get(SRID.CannotQueryPropertiesWhenPageNotInTreeWithWindow));
#pragma warning restore 6503
                }

                // Return false if it is not NavigationWindow.
                NavigationWindow navWin = WindowService as NavigationWindow;
                if (navWin != null)
                {
                    return navWin.ShowsNavigationUI;
                }
                else
                {
                    return false;
                }
            }

            set
            {
                VerifyAccess();
                if (WindowService == null)
                {
                    PageHelperObject._showsNavigationUI = value;
                    PropertyIsSet(SetPropertyFlags.ShowsNavigationUI);
                }
                else if (_isTopLevel == true) // only top level page can set this property
                {
                    SetShowsNavigationUI(value);
                    PropertyIsSet(SetPropertyFlags.ShowsNavigationUI);
                }
            }
        }

        /// <summary>
        ///     The DependencyProperty for the KeepAlive property.
        /// </summary>
        public static readonly DependencyProperty KeepAliveProperty =
                JournalEntry.KeepAliveProperty.AddOwner(typeof(Page));

        /// <summary>
        ///     An object that describes the KeepAlive status of the Page.
        /// </summary>
        public bool KeepAlive
        {
            get
            {
                return JournalEntry.GetKeepAlive(this);
            }

            set
            {
                JournalEntry.SetKeepAlive(this, value);
            }
        }

        /// <summary>
        /// NavigationServiceProperty
        /// </summary>
        public NavigationService NavigationService
        {
            get 
            {
                return NavigationService.GetNavigationService(this);
            }
        }

        /// <summary>
        ///     The DependencyProperty for the Foreground property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Font Color
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty =
                TextElement.ForegroundProperty.AddOwner(typeof(Page));

        /// <summary>
        ///     An brush that describes the foreground color which is used by
        ///     via inheritance.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public Brush Foreground
        {
            get { return (Brush) GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the FontFamily property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty =
                TextElement.FontFamilyProperty.AddOwner(typeof(Page));

        /// <summary>
        ///     The font family of the desired font which is used via inheritance
        /// </summary>
        [Bindable(true), Category("Appearance")]
        [Localizability(
            LocalizationCategory.Font,
            Modifiability = Modifiability.Unmodifiable
        )]
        public FontFamily FontFamily
        {
            get { return (FontFamily) GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the FontSize property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font Size
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty =
                TextElement.FontSizeProperty.AddOwner(typeof(Page));

        /// <summary>
        ///     The size of the desired font which will be used via inheritance.
        /// </summary>
        [TypeConverter(typeof(FontSizeConverter))]
        [Bindable(true), Category("Appearance")]
        [Localizability(LocalizationCategory.None)] // FontSize is localizable        
        public double FontSize
        {
            get { return (double) GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// TemplateProperty
        /// </summary>
        public static readonly DependencyProperty TemplateProperty =
                Control.TemplateProperty.AddOwner(
                        typeof(Page), 
                        new FrameworkPropertyMetadata(
                                (ControlTemplate) null,  // default value
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnTemplateChanged)));


        /// <summary>
        /// Template Property
        /// </summary>
        public ControlTemplate Template
        {
            get { return _templateCache; }
            set { SetValue(TemplateProperty, value); }
        }

        // Internal Helper so the FrameworkElement could see this property
        internal override FrameworkTemplate TemplateInternal
        {
            get { return Template; }
        }
        
        // Internal Helper so the FrameworkElement could see the template cache
        internal override FrameworkTemplate TemplateCache
        {
            get { return _templateCache; }
            set { _templateCache = (ControlTemplate)value; }
        }

        // Internal helper so FrameworkElement could see call the template changed virtual
        internal override void OnTemplateChangedInternal(FrameworkTemplate oldTemplate, FrameworkTemplate newTemplate)
        {
            OnTemplateChanged((ControlTemplate)oldTemplate, (ControlTemplate)newTemplate);
        }

        // Property invalidation callback invoked when TemplateProperty is changed
        private static void OnTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Page p = (Page) d;
            StyleHelper.UpdateTemplateCache(p, (FrameworkTemplate) e.OldValue, (FrameworkTemplate) e.NewValue, TemplateProperty);
        }

        /// <summary>
        ///     Template has changed
        /// </summary>
        /// <remarks>
        ///     When a Template changes, the VisualTree is removed. The new Template's 
        ///     VisualTree will be created when ApplyTemplate is called
        /// </remarks>
        /// <param name="oldTemplate">The old Template</param>
        /// <param name="newTemplate">The new Template</param>
        protected virtual void OnTemplateChanged(
            ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
        }
        
        #endregion Public Properties

        //---------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------

        //---------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------
        #region Protected Methods

        /// <summary>
        ///     Measurement override.
        /// </summary>
        /// <param name="constraint">
        ///     Sizing constraint.
        /// </param>
        protected override Size MeasureOverride(Size constraint)
        {
            VerifyAccess();
            int count = this.VisualChildrenCount;

            
            if (count > 0)
            {
                UIElement child = this.GetVisualChild(0) as UIElement;

                if (child != null)
                {
                    child.Measure(constraint);
                    return child.DesiredSize;
                }
            }

            return (new Size(0, 0));
        }

        /// <summary>
        ///     ArrangeOverride allows for the customization of the positioning of children.  
        /// </summary>
        /// <param name="arrangeBounds">
        ///     Measured size.
        /// </param>
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            VerifyAccess();
            int count = this.VisualChildrenCount;
          
            if (count > 0)
            {
                UIElement child = this.GetVisualChild(0) as UIElement;

                if (child != null)
                {
                    child.Arrange(new Rect(new Point(), arrangeBounds));
                }
            }
            return arrangeBounds;
        }

        /// <summary>
        /// OnVisualParentChanged is called when the parent of the Visual is changed.
        /// </summary>
        /// <param name="oldParent">Old parent or null if the Visual did not have a parent before.</param>
        protected internal sealed override void OnVisualParentChanged(DependencyObject oldParent)
        {
            VerifyAccess();
            base.OnVisualParentChanged(oldParent);

            // When Page is added to a tree, it can only be the root element of Window's or Frame's Content. 
            // In code, it means you can only add Page to a tree via Window.Content = Page, Frame.Content = Page, 
            // or equivalent such as navigation.
            // So we only allow a Page to be parented by a Visual (parent) in the following
            // cases:
            // 1. The visual parent is null.
            // 2. The logical parent is Window (Frame's Content is not its logical child).
            // 3. When the logical parent is not Window, it can only be the case of Page inside of a Frame. We can verify that
            //    by checking the Content of the Page's NavigationService is the Page itself. 
            //    The NavigationService dp and its Content property both are set before logical and/or visual Parent change.
            // 4. Exception should be thrown in any other situations, except for the v1 compatibility case below.

            Visual visualParent = VisualTreeHelper.GetParent(this) as Visual;

            // Need to check whether visual parent is null first, because if the app caught the exception as a result of setting illegal parent, 
            // and it removes the Page from the wrongly set parent, the visual link is removed first before the logical link. 
            // As a result when OnVisualParentChanged is fired, visualParent is null while the logical Parent is still the old one; Parent getter
            // here will return the illegal one. 

            if ((visualParent == null) || 
                (Parent is Window) || 
                ((NavigationService != null) && (NavigationService.Content == this)))
            {
                return;
            }

            // NOTE (Huwang 03/09/2007): The code below walks up the TemplatedParent chain until it finds the first Frame or Window. It does not 
            // check whether Window.Content or Frame.Content is Page. So it allows the scenario where Page can be in any element’s template and 
            // be parented by any element as long as the template is nested inside a Window or Frame, as demoed below
            //
            // <Window>
            //    <Window.Template>
            //         ...
            //         <Button>
            //            <Button.Template>
            //               ...
            //              <DockPanel>
            //               ...
            //               <Page>
            //         ...
            //
            // This is not what we intend to establish and support. But we discovered this after shipping V1. We need to maintain this behavior until
            // V4.0 when we will have an opportunity to do BC. Will file a bug for V4.0.

            bool isParentValid = false;
            // Don't worry about FCE since FCE is not a visual
            FrameworkElement feParent = visualParent as FrameworkElement;
            if (feParent != null)
            {
                DependencyObject parent = feParent as DependencyObject;

                // walk the StyledParent chain
                while ((feParent != null) && (feParent.TemplatedParent != null))
                {
                    parent = feParent.TemplatedParent;
                    // don't care if this cast fails to null b/c StyledParent
                    // are supposed to be FE or FCE and FCE is not part of the
                    // visual tree.
                    feParent = parent as FrameworkElement;

                    // (Page throws InvalidOperationException when 
                    // navigated to by a Frame which is part of the style visual 
                    // tree of an element)

                    // We need this here for the case when Frame is in the style 
                    // of an element
                    if (feParent is Frame)
                    {
                        break;
                    }
                }

                if ((parent is Window) || (parent is Frame))
                {
                    isParentValid = true;
                }
            }

            if (isParentValid == false)
            {
                throw new InvalidOperationException(SR.Get(SRID.ParentOfPageMustBeWindowOrFrame));
            }
        }

        #endregion Protected Methods
                
        //---------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------
        #region Private Methods

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Page page = (Page) d;
            page.OnContentChanged(e.OldValue, e.NewValue);
        }

        private void OnContentChanged(object oldContent, object newContent)
        {
            RemoveLogicalChild(oldContent);
            AddLogicalChild(newContent);
        }
                
        private static void _OnWindowServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Page p = d as Page;
            Debug.Assert( p != null, "DependencyObject must be of type Page." );

            p.OnWindowServiceChanged(e.NewValue as IWindowService);
        }
        
        /// <summary>
        ///     When IWindowService is changed, it means that this control is either placed into
        ///     a window's visual tree or taken out.  If we are in a new Window's visual tree and this
        ///     was a top level Page in the old Window, want to unhook events from the old window.  
        ///     Additionally, if this Page is a top level Page in the new Window's visual tree, we hook 
        ///     the events up to the new window.  Moreover, we also want to propagate the cached properties 
        ///     for top level Pages, otherwise, we clear the cache.
        ///     NOTE: the property values are propaged to the first window Page is attached to.  For
        ///     subsequent windows these properties are not propagated.
        /// </summary>
        private void OnWindowServiceChanged(IWindowService iws)
        {            
            _currentIws = iws;
            DetermineTopLevel();

            if (_currentIws != null)
            {
                if (_isTopLevel == true)
                {
                    PropagateProperties();
                }
            }            
        }

        private void DetermineTopLevel()
        {
            FrameworkElement feParent = this.Parent as FrameworkElement;

            if ((feParent != null) && (feParent.InheritanceBehavior == InheritanceBehavior.Default))
            {
                _isTopLevel = true;
            }
            else
            {
                _isTopLevel = false;
            }
        }
               
        private void PropagateProperties()
        {
            Debug.Assert(_currentIws != null, "_currentIws cannot be null here. Caller should always verify it");

            if (_pho == null)
            {
                return;
            }

            if (IsPropertySet(SetPropertyFlags.WindowTitle))
            {
                _currentIws.Title = PageHelperObject._windowTitle;
            }
            if (IsPropertySet(SetPropertyFlags.WindowHeight) && (! _currentIws.UserResized))
            {
                _currentIws.Height = PageHelperObject._windowHeight;
            }

            if (IsPropertySet(SetPropertyFlags.WindowWidth) && (! _currentIws.UserResized))
            {
                _currentIws.Width = PageHelperObject._windowWidth;
            }

            if (IsPropertySet(SetPropertyFlags.ShowsNavigationUI))
            {
                SetShowsNavigationUI(PageHelperObject._showsNavigationUI);
            }
        }

        bool IWindowService.UserResized
        {
            get 
            {
                Invariant.Assert(_currentIws != null, "_currentIws cannot be null here.");
                return _currentIws.UserResized; 
            }
        }

        private void SetShowsNavigationUI(bool showsNavigationUI)
        {
            NavigationWindow navWin = _currentIws as NavigationWindow;
            if (navWin != null)
            {
                navWin.ShowsNavigationUI = showsNavigationUI;
            }
        }

        private bool IsPropertySet(SetPropertyFlags property)
        {
            return (_setPropertyFlags & property) != 0;
        }

        private void PropertyIsSet(SetPropertyFlags property)
        {
            _setPropertyFlags |= property;
        }

        #region ShouldSerialize methods
        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeWindowTitle()
        {
            return IsPropertySet(SetPropertyFlags.WindowTitle);
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeWindowHeight()
        {
            return IsPropertySet(SetPropertyFlags.WindowHeight);
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public  bool ShouldSerializeWindowWidth()
        {
            return IsPropertySet(SetPropertyFlags.WindowWidth);
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public  bool ShouldSerializeTitle()
        {
            return IsPropertySet(SetPropertyFlags.Title);
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public  bool ShouldSerializeShowsNavigationUI()
        {
            return IsPropertySet(SetPropertyFlags.ShowsNavigationUI);
        }
        #endregion

        #endregion Private Methods

        //---------------------------------------------------
        //
        // Private Properties
        //
        //---------------------------------------------------
        #region Private Properties
        /// <summary>
        /// Returns IWindowService for the window page is hosted in.
        /// </summary>
        private IWindowService WindowService
        {
            get
            {
                return _currentIws;
            }
        }

        private PageHelperObject PageHelperObject
        {
            get
            {
                if (_pho == null)
                {
                    _pho = new PageHelperObject();
                }
                return _pho;
            }
        }
        
        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 19; }
        }

        #endregion Private Properties

        //----------------------------------------------
        //
        // Private Fields
        //
        //----------------------------------------------
        #region Private Fields
        private IWindowService              _currentIws;
        private PageHelperObject            _pho;
        private SetPropertyFlags            _setPropertyFlags = SetPropertyFlags.None;
        private bool                        _isTopLevel;
        private ControlTemplate             _templateCache;
        #endregion Private Fields
        
        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default 
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey

        #endregion Page Class
    }

    class PageHelperObject
    {
        //----------------------------------------------
        //
        // Internal Fields
        //
        //----------------------------------------------
        #region Internal Fields
        
        //we start to cache these properties because the window is not necessarly available 
        //when the values are set. Also we need to be able to tell whether a property has been
        //set per property so that we will know which one is set. We have a bool variable per property 
        //for that purpose. 
        internal String                         _text;
        internal String                         _windowTitle;
        internal double                         _windowHeight;
        internal double                         _windowWidth;
        internal bool                           _showsNavigationUI;
        #endregion Internal Fields
    }

    [Flags]
    internal enum SetPropertyFlags : byte
    {
        WindowTitle         = 0x01,
        WindowHeight        = 0x02,
        WindowWidth         = 0x04,
        Title               = 0x08,
        ShowsNavigationUI   = 0x10,

        None                = 0x00,
    }
}



