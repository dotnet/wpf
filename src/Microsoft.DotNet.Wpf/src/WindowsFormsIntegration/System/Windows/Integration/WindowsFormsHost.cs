// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using MS.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

using SD = System.Drawing;
using SW = System.Windows;
using SWC = System.Windows.Controls;
using SWF = System.Windows.Forms;
using SWM = System.Windows.Media;
using SWI = System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Input;

namespace System.Windows.Forms.Integration
{
    /// <summary>
    ///     An element that allows you to host a Windows Forms control on a
    ///     Windows Presentation Foundation page.
    /// </summary>
    [System.ComponentModel.DesignerCategory("code")]
    [ContentProperty("Child")]
    [DefaultEvent("ChildChanged")]
    public class WindowsFormsHost : HwndHost, IKeyboardInputSink
    {
        private SWM.Brush _cachedBackbrush;

        private HandleRef _hwndParent;
        private WinFormsAdapter _hostContainerInternal;

        #region Constructors
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static WindowsFormsHost()
        {
            //Keyboard tabbing support
            FocusableProperty.OverrideMetadata(typeof(WindowsFormsHost), new FrameworkPropertyMetadata(true));
            SWC.Control.IsTabStopProperty.OverrideMetadata(typeof(WindowsFormsHost), new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Initializes a new instance of the WindowsFormsHost class.
        /// </summary>
        public WindowsFormsHost()
            : base()
        {
            this._hostContainerInternal = new WinFormsAdapter(this);

            _propertyMap = new WindowsFormsHostPropertyMap(this);
        }
        #endregion

        #region Focus

        /// <summary>
        /// Notifies Avalon that focus has moved within this WindowsFormsHost.
        /// </summary>
        void NotifyFocusWithinHost()
        {
            DependencyObject focusScope = GetFocusScopeForElement(this);
            if (null != focusScope)
            { System.Windows.Input.FocusManager.SetFocusedElement(focusScope, this); }
        }

        /// <summary>
        ///     Handles Child.GotFocus event.
        /// </summary>
        private void OnChildGotFocus(object sender, EventArgs e)
        {
            SyncChildImeEnabledContext();
        }

        /// <summary>
        ///     Synchronizes the Winforms Child control ime context enble status.
        /// </summary>
        private void SyncChildImeEnabledContext()
        {
            if (SWF.ImeModeConversion.IsCurrentConversionTableSupported && this.Child != null && this.Child.IsHandleCreated)
            {
                Debug.WriteLineIf(HostUtils.ImeMode.Level >= TraceLevel.Info, "Inside SyncChildImeEnabledContext(), this = " + this);
                Debug.Indent();

                // Note: Do not attempt to set Child.ImeMode directly, it will be updated properly from the context.
                if (InputMethod.GetIsInputMethodEnabled(this))
                {
                    if (this.Child.ImeMode == ImeMode.Disable)
                    {
                        SWF.ImeContext.Enable(this.Child.Handle);
                    }
                }
                else
                {
                    if (this.Child.ImeMode != ImeMode.Disable)
                    {
                        SWF.ImeContext.Disable(this.Child.Handle);
                    }
                }

                Debug.Unindent();
            }
        }

        /// <summary>
        /// When implemented in a derived class, accesses the window process of the hosted child window.
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case NativeMethods.WM_CHILDACTIVATE:
                    _hostContainerInternal.HandleChildActivate();
                    break;
                case NativeMethods.WM_SETFOCUS:
                case NativeMethods.WM_MOUSEACTIVATE:
                    NotifyFocusWithinHost();
                    break;
                case NativeMethods.WM_GETOBJECT:
                    handled = true;
                    return OnWmGetObject(wParam, lParam);
            }
            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        private IntPtr OnWmGetObject(IntPtr wparam, IntPtr lparam)
        {
            IntPtr result = IntPtr.Zero;

            WindowsFormsHostAutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(this) as WindowsFormsHostAutomationPeer;
            if(peer != null)
            {
                // get the element proxy
                IRawElementProviderSimple el = peer.GetProvider();

                if (el != null)
                {
                    //This requires FullTrust but we already have it.
                    result = AutomationInteropProvider.ReturnRawElementProvider(Handle, wparam, lparam, el);
                }
            }
            return result;
        }

        private static DependencyObject GetFocusScopeForElement(DependencyObject element)
        {
            //Walk up the visual tree until we find an element that is a focus scope (or we run out of elements).
            //This will usually be the root Avalon Window.
            while (null != element && !FocusManager.GetIsFocusScope(element))
            { element = VisualTreeHelper.GetParent(element); }
            return element;
        }

        #endregion

        #region Layout

        private Vector _currentScale = new Vector(1.0, 1.0);

        /// <summary>
        ///     Scales the hosted Windows Forms control, and tracks the scale factor.
        /// </summary>
        /// <param name="newScale"></param>
        /// <returns>The new scale factor</returns>
        protected virtual Vector ScaleChild(Vector newScale)
        {
            if (newScale != _currentScale)
            {
                if (Child != null)
                {
                    Child.Scale(new System.Drawing.SizeF((float)(newScale.X / _currentScale.X), (float)(newScale.Y / _currentScale.Y)));
                }
            }
            Vector returnScale = newScale;
            returnScale.X = (newScale.X == 0) ? _currentScale.X : newScale.X;
            returnScale.Y = (newScale.Y == 0) ? _currentScale.Y : newScale.Y;
            return returnScale;
        }

        private void ScaleChild()
        {
            bool skewed;
            Vector newScale = HostUtils.GetScale(this, out skewed);
            if (skewed)
            {
                OnLayoutError();
            }
            _currentScale = ScaleChild(newScale);
        }

        private event EventHandler<LayoutExceptionEventArgs> _layoutError;
        /// <summary>
        /// Occurs when a layout error, such as a skew or rotation that WindowsFormsHost does not support,
        /// is encountered.
        /// </summary>
        public event EventHandler<LayoutExceptionEventArgs> LayoutError
        {
            add { _layoutError += value; }
            remove { _layoutError -= value; }
        }



        private void OnLayoutError()
        {
            InvalidOperationException exception = new InvalidOperationException(SR.Get(SRID.Host_CannotRotateWindowsFormsHost));
            LayoutExceptionEventArgs args = new LayoutExceptionEventArgs(exception);
            if (_layoutError != null)
            {
                _layoutError(this, args);
            }
            if (args.ThrowException)
            {
                throw exception;
            }
        }

        /// <internalonly>
        ///     Overrides the base class implementation of MeasureOverride to measure
        ///     the size of a WindowsFormsHost object and return the proper size to the layout engine.
        /// </internalonly>
        protected override SW.Size MeasureOverride(SW.Size constraint)
        {
            if (this.Visibility == Visibility.Collapsed || Child == null)
            {
                //Child takes up no space
                return new Size(0, 0);
            }

            ScaleChild();
            DpiScale dpi = GetDpi();

            SD.Size constraintSize = Convert.ConstraintToSystemDrawingSize(constraint, _currentScale, dpi.DpiScaleX, dpi.DpiScaleY);
            SD.Size preferredSize = Child.GetPreferredSize(constraintSize);
            System.Windows.Size returnSize = Convert.ToSystemWindowsSize(preferredSize, _currentScale, dpi.DpiScaleX, dpi.DpiScaleY);

            // Apply constraints to the preferred size
            returnSize.Width = Math.Min(returnSize.Width, constraint.Width);
            returnSize.Height = Math.Min(returnSize.Height, constraint.Height);

            return returnSize;
        }

        // Note: the WPF layout engine may call ArrangeOverride multiple times in succession
        private Size _priorConstraint;

        /// <summary>
        ///     When implemented in a derived class, positions child elements and determines a
        ///     size for a FrameworkElement-derived class.
        /// </summary>
        /// <param name="finalSize"></param>
        /// <returns></returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.Visibility == Visibility.Collapsed || Child == null)
            {
                //Child takes up no space
                return new Size(0, 0);
            }

            Vector originalScale = _currentScale;
            ScaleChild();
            DpiScale dpi = GetDpi();
            bool scaled = (_currentScale != originalScale);

            SD.Size targetSize = Convert.ConstraintToSystemDrawingSize(finalSize, _currentScale, dpi.DpiScaleX, dpi.DpiScaleY);
            if ((Child.Size != targetSize) && ((finalSize != _priorConstraint) || scaled))
            {
                _priorConstraint = finalSize;
                Child.Size = targetSize;
            }
            Size returnSize = Convert.ToSystemWindowsSize(Child.Size, _currentScale, dpi.DpiScaleX, dpi.DpiScaleY);
            returnSize.Width = Math.Min(returnSize.Width, finalSize.Width);
            returnSize.Height = Math.Min(returnSize.Height, finalSize.Height);
            if (HostContainerInternal.BackgroundImage != null)
            {
                _propertyMap.OnPropertyChanged(this, "Background", this.Background);
            }
            return returnSize;
        }
        #endregion Layout

        #region Containership

        /// <summary>
        /// Occurs when the Child property is set.
        /// </summary>
        public event EventHandler<ChildChangedEventArgs> ChildChanged;

        /// <summary>
        ///     Gets or sets the child control hosted by the WindowsFormsHost element.
        /// </summary>
        public Control Child
        {
            get
            {
                return _hostContainerInternal.Child;
            }
            set
            {
#pragma warning disable 1634, 1691
#pragma warning disable 56526
                Control oldChild = Child;
                SWF.Form form = value as SWF.Form;
                if (form != null)
                {
                    if (form.TopLevel)
                    {  //WinOS #1030878 - Can't host top-level forms
                        throw new ArgumentException(SR.Get(SRID.Host_ChildCantBeTopLevelForm));
                    }
                    else
                    {
                        form.ControlBox = false;
                    }
                }
                _hostContainerInternal.Child = value;
                if (Child != null)
                {
                    _propertyMap.ApplyAll();

                    Child.Margin = SWF.Padding.Empty;
                    Child.Dock = DockStyle.None;
                    Child.AutoSize = false;
                    Child.Location = SD.Point.Empty;
                    // clear the cached size
                    _priorConstraint = new Size(double.NaN, double.NaN);
                }
                OnChildChanged(oldChild);
#pragma warning restore 1634, 1691, 56526
            }
        }

        private void OnChildChanged(Control oldChild)
        {
            if (oldChild != null)
            {
                oldChild.GotFocus -= new EventHandler(this.OnChildGotFocus);
            }

            if (this.Child != null)
            {
                this.Child.GotFocus += new EventHandler(this.OnChildGotFocus);
            }

            if (ChildChanged != null)
            {
                ChildChanged(this, new ChildChangedEventArgs(oldChild));
            }


        }

        internal WinFormsAdapter HostContainerInternal
        {
            get
            {
                return _hostContainerInternal;
            }
        }

        #endregion Containership

        #region Rendering

        static Brush defaultBrush = SystemColors.WindowBrush;
        /// <summary>
        ///     Manually searches up the parent tree to find the first FrameworkElement that
        ///     has a non-null background, and returns that Brush.
        ///     If no parent is found with a brush, then this returns WindowBrush.
        /// </summary>
        /// <param name="dependencyObject"></param>
        /// <returns></returns>
        private static SWM.Brush FindBackgroundParent(SW.DependencyObject dependencyObject)
        {
            if (dependencyObject == null)
            {
                return defaultBrush;
            }

            Brush backgroundBrush = null;
            backgroundBrush = (Brush)dependencyObject.GetValue(SWC.Control.BackgroundProperty);

            if (backgroundBrush == null)
            {
                SW.FrameworkElement frameworkElement = dependencyObject as SW.FrameworkElement;
                if (frameworkElement != null)
                {
                    DependencyObject parentElement = VisualTreeHelper.GetParent(frameworkElement);
                    backgroundBrush = FindBackgroundParent(parentElement);
                }
            }

            return backgroundBrush ?? defaultBrush;
        }


        /// <summary>
        ///     This is called by the OnPaintBackground method of the host container.
        /// </summary>
        internal void PaintBackground()
        {
            SWM.Brush parentBrush = FindBackgroundParent(this);
            if (parentBrush != null)
            {
                if (_cachedBackbrush != parentBrush)
                {
                    _cachedBackbrush = parentBrush;
                    _propertyMap.OnPropertyChanged(this, "Background", parentBrush);
                }
            }
        }

        #endregion Rendering

        #region Keyboarding

        /// <summary>
        ///     Enables Windows Forms forms to function correctly when opened modelessly from
        ///     Windows Presentation Foundation.
        /// </summary>
        public static void EnableWindowsFormsInterop()
        {
            ApplicationInterop.EnableWindowsFormsInterop();
        }

        /// <internalonly>
        ///     Forwards focus from Windows Presentation Foundation to the hosted Windows Forms control.
        /// </internalonly>
        public virtual bool TabInto(SWI.TraversalRequest request)
        {
            return HostContainerInternal.FocusNext(request);
        }

        #endregion Keyboarding

        #region NativeWindow

        // In 4.0, we spun up a WinForms.NativeWindow to listen for WM_ACTIVATEAPP
        // messages.  This was part of a failed attempt to make focus-restoration
        // work correctly;   in 4.5 we solved the focus problems a different way
        // that didn't need the NativeWindow.  However, the NativeWindow had a
        // side-effect:  it swallows all exceptions.   For compat with 4.0, we
        // re-introduce the NativeWindow.  We don't need it to do anything - it's
        // only purpose is to swallow exceptions during WndProc so that 4.0 apps
        // don't crash. (Dev11 475347)

        private DummyNativeWindow _dummyNativeWindow;
        private class DummyNativeWindow : NativeWindow, IDisposable
        {
            WindowsFormsHost _host;
            public DummyNativeWindow(WindowsFormsHost host)
            { _host = host; }

            public void Dispose()
            { this.ReleaseHandle(); }
        }

        #endregion NativeWindow

        #region Window Handling & Misc
        /// <internalonly>
        ///     Overrides the base class implementation of BuildWindowCore to build the hosted
        ///     Windows Forms control.
        /// </internalonly>
        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            this.Loaded += new RoutedEventHandler(ApplyAllProperties);

            Debug.WriteLineIf(_traceHandle.TraceVerbose, String.Format(CultureInfo.CurrentCulture, "WindowsFormsHost({0}): BuildWindowCore (parent=0x{1:x8})", this.Name, hwndParent.Handle.ToInt32()));

            // for 4.0 compat, create a Winforms.NativeWindow to swallow exceptions during WndProc
            if (!CoreCompatibilityPreferences.TargetsAtLeast_Desktop_V4_5)
            {
                if (_dummyNativeWindow != null)
                {
                    _dummyNativeWindow.Dispose();
                }
                _dummyNativeWindow = new DummyNativeWindow(this);
                _dummyNativeWindow.AssignHandle(hwndParent.Handle);
            }

            _hwndParent = hwndParent;
            //For Keyboard interop
            ApplicationInterop.ThreadWindowsFormsHostList.Add(this);    //Keep track of this control, so it can get forwarded windows messages
            EnableWindowsFormsInterop();        //Start the forwarding of windows messages to all WFH controls on active windows

            UnsafeNativeMethods.SetParent(/* child = */ HostContainerInternal.Handle, /* parent = */ _hwndParent.Handle);
            return new HandleRef(HostContainerInternal, HostContainerInternal.Handle);
        }

        /// <internalonly>
        ///     Overrides the base class implementation of DestroyWindowCore to delete the
        ///     window containing this object.
        /// </internalonly>
        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            //For keyboard interop (remove this control from the list)
            //This line shouldn't be necessary since the list cleans itself, but it's good to be tidy.
            ApplicationInterop.ThreadWindowsFormsHostList.Remove(this);

            if (HostContainerInternal != null)
            {
                HostContainerInternal.Dispose();
            }
        }

        void ApplyAllProperties(object sender, RoutedEventArgs e)
        {
            _propertyMap.ApplyAll();
        }

        /// <internalonly>
        ///     Releases all resources used by the WindowsFormsHost element.
        /// </internalonly>
        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            finally
            {
                if (disposing)
                {
                    if (_hostContainerInternal != null)
                    {
                        try
                        {
                            if (_dummyNativeWindow != null)
                            {
                                _dummyNativeWindow.Dispose();
                            }
                            _hostContainerInternal.Dispose();
                            this.Loaded -= new RoutedEventHandler(ApplyAllProperties);
                        }
                        finally
                        {
                            if (Child != null)
                            {
                                Child.Dispose();
                            }
                        }
                    }
                }
            }
        }
        #endregion Window Handling & Misc

        #region Automation

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new WindowsFormsHostAutomationPeer(this);
        }

        #endregion Automation

        #region Property Mapping

        //Some of the properties that we would like to map don't exist on HwndHost,
        //so we add them here: TabIndex, Font (4x), Foreground, Background, Padding.

        /// <summary>
        ///     Identifies the Padding dependency property.
        /// </summary>
        public static readonly DependencyProperty PaddingProperty =
                SWC.Control.PaddingProperty.AddOwner(typeof(WindowsFormsHost));

        /// <summary>
        /// Specifies the size of the desired padding within the hosted Windows Forms control.
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        ///     Identifies the TabIndex dependency property.
        /// </summary>
        public static readonly DependencyProperty TabIndexProperty =
                SWC.Control.TabIndexProperty.AddOwner(typeof(WindowsFormsHost));

        /// <summary>
        ///     Gets or sets the hosted control's tab index.
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public int TabIndex
        {
            get { return (int)GetValue(TabIndexProperty); }
            set { SetValue(TabIndexProperty, value); }
        }

        /// <summary>
        ///     Identifies the FontFamily dependency property.
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty =
            SWC.Control.FontFamilyProperty.AddOwner(typeof(WindowsFormsHost));

        /// <summary>
        ///     Gets or sets the hosted control's font family as an ambient property.
        /// </summary>
        public SWM.FontFamily FontFamily
        {
            get { return (SWM.FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        ///     Identifies the FontSize dependency property.
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty =
            SWC.Control.FontSizeProperty.AddOwner(typeof(WindowsFormsHost));

        /// <summary>
        ///     Gets or sets the hosted control's font size as an ambient property.
        /// </summary>
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        ///     Identifies the FontStyle dependency property.
        /// </summary>
        public static readonly DependencyProperty FontStyleProperty =
            SWC.Control.FontStyleProperty.AddOwner(typeof(WindowsFormsHost));

        /// <summary>
        ///     Gets or sets the hosted control's font style as an ambient proeprty.
        /// </summary>
        public SW.FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        ///     Identifies the FontWeight dependency property.
        /// </summary>
        public static readonly DependencyProperty FontWeightProperty =
            SWC.Control.FontWeightProperty.AddOwner(typeof(WindowsFormsHost));

        /// <summary>
        ///     Gets or sets the hosted control's font weight as an ambient property.
        /// </summary>
        public SW.FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        ///     Identifies the Foreground dependency property.
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty =
            SWC.Control.ForegroundProperty.AddOwner(typeof(WindowsFormsHost));


        ///<summary>
        ///     Gets or sets the hosted control's foreground color as an ambient property.
        ///</summary>
        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        ///     Identifies the Background dependency property.
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty =
            SWC.Control.BackgroundProperty.AddOwner(typeof(WindowsFormsHost));

        /// <summary>
        ///     Gets or sets the hosted control's background as an ambient property.
        /// </summary>
        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <internalonly>
        ///     Forces the translation of a mapped property.
        /// </internalonly>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Invoke method currently set to handle this event
            if (_propertyMap != null)
            {
                _propertyMap.OnPropertyChanged(this, e.Property.Name, e.NewValue);
            }
        }

        /// <summary>
        ///     Gets the property map that determines how setting properties on the
        ///     WindowsFormsHost element affects the hosted Windows Forms control.
        /// </summary>
        public PropertyMap PropertyMap
        {
            get { return _propertyMap; }
        }
        private PropertyMap _propertyMap;

        #endregion Property Mapping

        #region DEBUG
#if DEBUG
        internal static readonly TraceSwitch _traceHandle = new TraceSwitch("WindowsFormsHostHandle", "Tracks WindowsFormsHost handle information.");
        internal static readonly TraceSwitch _traceLayout = new TraceSwitch("WindowsFormsHostLayout", "Tracks WindowsFormsHost layout information.");
#else
        internal static readonly TraceSwitch _traceHandle = null;
        internal static readonly TraceSwitch _traceLayout = null;
#endif
        #endregion DEBUG
    }



    #region WinFormsAdapter
    [System.ComponentModel.DesignerCategory("code")]
    internal class WinFormsAdapter : SWF.ContainerControl
    {
        private WindowsFormsHost _host;
        private FocusTargetControl _focusTarget;
        private IntPtr _prevFocusHwnd = IntPtr.Zero;

        private Control _child;
        public Control Child
        {
            get { return _child; }
            set
            {
                if (null != Child)
                { Controls.Remove(Child); }

                if (null != value)
                { Controls.Add(value); }

                _child = value;
            }
        }

        public void HandleChildActivate()
        {
            IntPtr focusHwnd = UnsafeNativeMethods.GetFocus();
            try
            {
                if (IntPtr.Zero == _prevFocusHwnd ) { return; }

                //If focus is changing from a child of the WinFormsAdapter to something outside
                //we will activate the a temporary control to cause leave and validation events
                if (
                    _focusTarget.Handle != _prevFocusHwnd &&
                    UnsafeNativeMethods.IsChild(Handle, _prevFocusHwnd) &&
                    !UnsafeNativeMethods.IsChild(Handle, focusHwnd))
                {
                    this.ActiveControl = _focusTarget;
                }
            }
            finally
            { _prevFocusHwnd = focusHwnd; }
        }

        //Note: there's no notification when the ambient cursor changes, so we
        //can't do a normal mapping for this and have it work.  We work around
        //this by overriding WinFormsAdapter.Cursor and walking the visual tree
        //to find the cursor IF the Cursor translator has not been changed.
        //host.PropertyMap["Cursor"] += delegate {MessageBox.Show("?");};
        //will cause the mapping to no longer happen.
        public override Cursor Cursor
        {
            get
            {
                if (_host == null) { return base.Cursor; }

                if (!_host.PropertyMap.PropertyMappedToEmptyTranslator("Cursor"))
                { return base.Cursor; }

                bool forceCursorMapped = _host.PropertyMap.PropertyMappedToEmptyTranslator("ForceCursor");

                FrameworkElement cursorSource = HostUtils.GetCursorSource(_host, forceCursorMapped);
                if (cursorSource == null) { return base.Cursor; }

                return Convert.ToSystemWindowsFormsCursor(cursorSource.Cursor);
            }
            set
            {
                base.Cursor = value;
            }
        }

        private class FocusTargetControl : Control
        {
            public FocusTargetControl()
            {
                this.Size = SD.Size.Empty;
                //Hide it as far away as possible
                this.Location = new SD.Point(short.MinValue, short.MinValue);
                this.TabStop = false;
#if DEBUG
                this.Name = "Focus target";
#endif //DEBUG
            }

        }

        public WinFormsAdapter(WindowsFormsHost host)
        {
            _host = host;
            _focusTarget = new FocusTargetControl();
            Controls.Add(_focusTarget);
            this.HandleCreated += WinFormsAdapter_HandleCreated;

            //Keyboard interop: We listen to this event to refresh the visual cues (workaround)
            SWI.InputManager.Current.PostProcessInput += InputManager_PostProcessInput;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    SWI.InputManager.Current.PostProcessInput -= InputManager_PostProcessInput;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
        #region Keyboard Interop
        /// <summary>
        ///     Forwards focus from Avalon and into the WinForms "sink".
        ///     The request is often First and Last, which isn't really mapped the
        ///     same way, but it seems to work, probably because we only host one control.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        internal bool FocusNext(SWI.TraversalRequest request)
        {
            UpdateUIState(NativeMethods.UIS_INITIALIZE);
            bool forward = true;
            bool tabStopOnly = true;
            switch (request.FocusNavigationDirection)
            {
                case System.Windows.Input.FocusNavigationDirection.Down:
                case System.Windows.Input.FocusNavigationDirection.Right:
                    forward = true;
                    tabStopOnly = false;
                    break;
                case System.Windows.Input.FocusNavigationDirection.Next:
                case System.Windows.Input.FocusNavigationDirection.First:
                    forward = true;
                    tabStopOnly = true;
                    break;
                case System.Windows.Input.FocusNavigationDirection.Up:
                case System.Windows.Input.FocusNavigationDirection.Left:
                    forward = false;
                    tabStopOnly = false;
                    break;
                case System.Windows.Input.FocusNavigationDirection.Previous:
                case System.Windows.Input.FocusNavigationDirection.Last:
                    forward = false;
                    tabStopOnly = true;
                    break;
                default:
                    Debug.Assert(false, "Unknown FocusNavigationDirection");
                    break;
            }
            _focusTarget.Enabled = false;
            try
            {
                if (this.SelectNextControl(null, forward, tabStopOnly, true, false))
                {
                    // find the inner most active control
                    ContainerControl ret = this;
                    while (ret.ActiveControl is ContainerControl)
                    {
                        ret = (ContainerControl)ret.ActiveControl;
                    }
                    if (!ret.ContainsFocus)
                    { ret.Focus(); }
                    return true;
                }
                return false;
            }
            finally
            { _focusTarget.Enabled = true; }
        }

        internal IKeyboardInputSite HostKeyboardInputSite
        {
            get
            {
                return (_host as IKeyboardInputSink)?.KeyboardInputSite;
            }
        }

        //  CSS added for keyboard interop
        protected override bool ProcessDialogKey(Keys keyData)
        {
            _focusTarget.Enabled = false;
            try
            {
                if ((keyData & (Keys.Alt | Keys.Control)) == Keys.None)
                {
                    Keys keyCode = (Keys)keyData & Keys.KeyCode;
                    if (keyCode == Keys.Tab || keyCode == Keys.Left ||
                        keyCode == Keys.Right || keyCode == Keys.Up ||
                        keyCode == Keys.Down)
                    {
                        if (this.Focused || this.ContainsFocus)
                        {
                            // CSS:  In WF apps, by default, arrow keys always wrap within a container
                            // However, we don't want them to wrap in the Adapter, which always has only
                            // one immediate child
                            if ((keyCode == Keys.Left || keyCode == Keys.Right || keyCode == Keys.Down || keyCode == Keys.Up)
                                && (this.ActiveControl != null && this.ActiveControl.Parent == this))
                            {
                                SWF.Control c = this.ActiveControl.Parent;
                                return c.SelectNextControl(this.ActiveControl, keyCode == Keys.Right || keyCode == Keys.Down, false, false, false);
                            }
                            else
                                return base.ProcessDialogKey(keyData);
                        }
                    }
                }
                return false;
            }
            finally { _focusTarget.Enabled = true; }
        }

        //  CSS added for keyboard interop
        internal bool PreProcessMessage(ref Message msg, bool hasFocus)
        {
            // Don't steal WM_CHAR messages from Avalon, Catch them later in InputManager_PostProcessInput
            if (!hasFocus && msg.Msg == NativeMethods.WM_CHAR)
            {
                return false;
            }
            return PreProcessMessage(ref msg);
        }

        /// <summary>
        ///     For keyboard interop, when this handle is recreated, the current state of the
        ///     visual shortcut key cues (AKA underlining the hotkey) is lost, so it needs
        ///     to be refreshed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WinFormsAdapter_HandleCreated(object sender, EventArgs e)
        {
            //CSS - Workaround OS bug.  The initial UI state was being set erratically
            // (sometimes cues were shown, sometimes not.  This forces it to a known state.
            UpdateUIState(NativeMethods.UIS_SET);
        }

        /// <summary>
        ///     For keyboard interop
        ///     Ensure the visual shortcut key cues on controls are in the same visual
        ///     state that we expect. This is a workaround for odd Windows API.
        /// </summary>
        internal void UpdateUIState(int uiAction)
        {
            Debug.Assert(uiAction == NativeMethods.UIS_INITIALIZE || uiAction == NativeMethods.UIS_SET, "Unexpected uiAction");
            int toSet = NativeMethods.UISF_HIDEACCEL | NativeMethods.UISF_HIDEFOCUS;
            if (UnsafeNativeMethods.SendMessage(new HandleRef(this, this.Handle),
                 NativeMethods.WM_UPDATEUISTATE,
                 (IntPtr)(uiAction | (toSet << 16)),
                 IntPtr.Zero) != IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        // CSS Added for keyboard interop
        // Catch WM_CHAR messages which weren't handled by Avalon
        //  (including mnemonics which were typed without the "Alt" key)
        private void InputManager_PostProcessInput(object sender, SWI.ProcessInputEventArgs e)
        {
            // Should return immediately if this WFH is not in the active Window
            PresentationSource presentationSource = PresentationSource.FromVisual(this._host);
            if (presentationSource == null)
            {
                return;
            }
            Window presentationSourceWindow = presentationSource.RootVisual as Window;

            //CSS This active window check may not work for multiple levels of nesting...
            // RootVisual isn't top level window.  Should we traverse upward through nested levels?
            if (presentationSourceWindow == null || !presentationSourceWindow.IsActive)
                return;

            // Now check for unhandled WM_CHAR messages and process them as mnemonics
            if (!e.StagingItem.Input.Handled && e.StagingItem.Input.RoutedEvent == SWI.TextCompositionManager.TextInputEvent)
            {
                SWI.TextCompositionEventArgs te = (SWI.TextCompositionEventArgs)e.StagingItem.Input;
                string text = te.Text;
                if (string.IsNullOrEmpty(text))
                {
                    text = te.SystemText;
                }
                if (!string.IsNullOrEmpty(text))
                {
                    e.StagingItem.Input.Handled = this.ProcessDialogChar(text[0]);
                }
            }
        }

        #endregion Keyboard Interop

        /// <internalonly>
        ///     Overridden to invalidate host layout when our layout changes.
        /// </internalonly>
        protected override void OnLayout(SWF.LayoutEventArgs e)
        {
            base.OnLayout(e);
            _host.InvalidateMeasure();

#if DEBUG
            string compName = "";
            if (e.AffectedControl != null)
            {
                compName = e.AffectedControl.Name;
            }
            else if (e.AffectedComponent != null)
            {
                compName = e.AffectedComponent.ToString();
            }
            Debug.WriteLineIf(WindowsFormsHost._traceLayout.TraceInfo, String.Format(CultureInfo.CurrentCulture, "WindowsFormsHost({0}): Layout invalidated (control='{1}',property='{2}')", _host.Name, compName, e.AffectedProperty));
#endif
        }

        /// <internalonly>
        ///     Forward the Host Containers paint events to the WFH, so that it can either
        ///     use a bitmap to do "fake" transparency or set the BackColor correctly.
        /// </internalonly>
        protected override void OnPaintBackground(SWF.PaintEventArgs e)
        {
            _host.PaintBackground();
            base.OnPaintBackground(e);
        }

        //Changing RightToLeft on the WinFormsAdapter recreates the handle. Since our
        //handle is being cached, this causes problems.  The handle gets recreated when
        //RightToLeft changes.  To avoid changing our handle, we can override the
        //RightToLeft property, and notify our child when the property changes.
        private RightToLeft _rightToLeft = RightToLeft.Inherit;
        /// <summary>
        /// Gets or sets a value indicating whether control's elements are aligned
        /// to support locales using right-to-left fonts.
        /// </summary>
        public override RightToLeft RightToLeft
        {
            get
            {
                //Return the same default value as Control does (No)
                return _rightToLeft == RightToLeft.Inherit ?
                    RightToLeft.No : _rightToLeft;
            }
            set
            {
                if (_rightToLeft != value)
                {
                    //If RightToLeft is Inherit and we're setting to the "default" value, don't call OnRTLChanged.
                    bool fireRightToLeftChanged = _rightToLeft != RightToLeft.Inherit || base.RightToLeft != value;
                    _rightToLeft = value;
                    if (fireRightToLeftChanged)
                    {
                        OnRightToLeftChanged(EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Override OnRightToLeftChanged as noted above
        /// </summary>
        /// <param name="e"></param>
        protected override void OnRightToLeftChanged(EventArgs e)
        {
            if (null != Child)
            {
                CallOnParentRightToLeftChanged(Child);
            }
        }

        //Use reflection to call protected OnParentRightToLeftChanged
        private void CallOnParentRightToLeftChanged(Control control)
        {
            MethodInfo methodInfo = typeof(SWF.Control).GetMethod("OnParentRightToLeftChanged", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(EventArgs) }, null);
            Debug.Assert(methodInfo != null, "Couldn't find OnParentRightToLeftChanged method!");
            if (methodInfo != null)
            {
                methodInfo.Invoke(control, new object[] { EventArgs.Empty });
            }
        }
    }
    #endregion WinFormsAdapter
}
