// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Security;
using MS.Win32;
using MS.Internal;
using System.Runtime.Versioning;
using System.Windows.Input;

using SD = System.Drawing;
using SWF = System.Windows.Forms;

using SW = System.Windows;
using SWC = System.Windows.Controls;
using SWM = System.Windows.Media;
using SWMI = System.Windows.Media.Imaging;
using SWI = System.Windows.Input;
using SWT = System.Windows.Threading;

using NativeMethodsSetLastError = MS.Internal.WinFormsIntegration.NativeMethodsSetLastError;

namespace System.Windows.Forms.Integration
{
    /// <summary>
    ///     A Windows Forms control that can be used to host a Windows Presentation
    ///     Foundation element.
    /// </summary>
    [System.ComponentModel.DesignerCategory("code")]
    [ContentProperty("Child")]
    [DefaultEvent("ChildChanged")]
    [Designer("WindowsFormsIntegration.Design.ElementHostDesigner, WindowsFormsIntegration.Design, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [DesignerSerializer("WindowsFormsIntegration.Design.ElementHostCodeDomSerializer, WindowsFormsIntegration.Design, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                    "System.ComponentModel.Design.Serialization.CodeDomSerializer, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ElementHost : Control
    {
        private HwndSource _hwndSource;
        /// <summary>
        /// The decorator is used to provide an Adorner layer which is needed to show UI highlights
        /// for focus etc.  In the pure-Avalon case, this would be handled by Window
        /// </summary>
        private AdornerDecorator _decorator;
        private AvalonAdapter _hostContainerInternal;
        private bool _backColorTransparent;
        private SW.UIElement _child;

        private bool _processingWmInputLangChanged;  // Flag to prevent re-entrancy when processing WM_INPUTLANGCHANGED

        #region Constructors
        /// <summary>
        ///     Initializes a new instance of the ElementHost class.
        /// </summary>
        /// <internalonly>
        ///     Overridden to plug the Avalon control into WinForm's layout engines.
        /// </internalonly>
        public ElementHost()
            : base()
        {
            this._hostContainerInternal = new AvalonAdapter(this);
            this._decorator = new AdornerDecorator();
            _decorator.Child = this._hostContainerInternal;

            SetStyle(SWF.ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(SWF.ControlStyles.UserPaint, true);
            SetStyle(SWF.ControlStyles.AllPaintingInWmPaint, true);

            System.Windows.Input.FocusManager.SetIsFocusScope(this._decorator, true);
            //For keyboarding, this listens for WM_CHAR events not handled, so that mnemonics
            //that are pressed without the ALT key can be handled.
            SWI.InputManager.Current.PostProcessInput += InputManager_PostProcessInput;

            this.SetAutoSizeMode(AutoSizeMode.GrowAndShrink);
            StartPropertyMapping();
            SizeChanged += new EventHandler(CallUpdateBackground);
            LocationChanged += new EventHandler(CallUpdateBackground);
        }
        #endregion

        #region Layout

        /// <internalonly>
        ///     Overrides the base class implementation of GetPreferredSize to provide
        ///     correct layout behavior for the hosted Windows Presentation Foundation elements.
        /// </internalonly>
        public override SD.Size GetPreferredSize(SD.Size proposedSize)
        {
            if (Disposing)
            {
                return base.GetPreferredSize(proposedSize);
            }

            proposedSize = HostUtils.IntersectSizes(HostUtils.ConvertZeroOrOneToUnbounded(proposedSize), HostUtils.ConvertZeroToUnbounded(MaximumSize));
            proposedSize = HostUtils.UnionSizes(proposedSize, MinimumSize);

            // Apply the child's scaling, if any
            Vector scale = (Child == null ? new Vector(1d, 1d) : HostUtils.GetScale(Child));

            Size constraints = Convert.ToSystemWindowsSize(proposedSize, scale);

            // At this point, an unbounded value is represented by Int32.MaxValue: WPF wants double.PositiveInfinity.
            if (constraints.Width == Int32.MaxValue) { constraints.Width = Double.PositiveInfinity; }
            if (constraints.Height == Int32.MaxValue) { constraints.Height = Double.PositiveInfinity; }

            // Request that control recompute desired size with new constraints.
            _decorator.Measure(constraints);
            SD.Size prefSize = Convert.ToSystemDrawingSize(_decorator.DesiredSize, scale);

            // WindowsForms guarantees results will be bounded by control Min/MaxSize
            prefSize = HostUtils.IntersectSizes(prefSize, HostUtils.ConvertZeroToUnbounded(MaximumSize));
            prefSize = HostUtils.UnionSizes(prefSize, MinimumSize);

            Debug.WriteLineIf(_traceLayout.TraceInfo, String.Format(CultureInfo.CurrentCulture, "AvalonAdapter({0}): MeasureOverride (constraint={1},result={2})", this.Name, proposedSize, prefSize));
            return prefSize;
        }

        /// <summary>
        ///     Gets the default size of the control.
        /// </summary>
        protected override System.Drawing.Size DefaultSize
        {
            get
            {
                return new SD.Size(200, 100);
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the control is
        ///     automatically resized to display its entire contents.
        /// </summary>
        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override bool AutoSize
        {
            get
            {
                return base.AutoSize;
            }
            set
            {
                base.AutoSize = value;
            }
        }
        #endregion Layout

        #region Containership
        /// <summary>
        ///     Gets the parent container of the hosted Windows Presentation Foundation element.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SWC.Panel HostContainer
        {
            get { return HostContainerInternal; }
        }

        internal HwndSource HwndSource
        {
            get { return _hwndSource; }
        }

        /// <summary>
        /// Indicates that the Child property has been changed.
        /// </summary>
        public event EventHandler<ChildChangedEventArgs> ChildChanged;

        /// <summary>
        ///     Gets or sets the UIElement hosted by the ElementHost control.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SW.UIElement Child
        {
            get
            {
                return _child;
            }
            set
            {
                UIElement oldValue = Child;
#pragma warning disable 1634, 1691
#pragma warning disable 56526
                _child = value;
#pragma warning restore 1634, 1691, 56526
                HostContainerInternal.Children.Clear();
                if (_child != null)
                {
                    HostContainerInternal.Children.Add(_child);
                    _propertyMap.ApplyAll();
                    InitializeChildProperties();
                }
                OnChildChanged(oldValue);
            }
        }

        protected override bool CanEnableIme
        {
            get
            {
                if (this.Child == null)
                {
                    return false;
                }
                return base.CanEnableIme;
            }
        }

        /// <summary>
        ///     If focus on HwndSource, we need to return true here.
        /// </summary>
        public override bool Focused
        {
            get
            {
                if( IsHandleCreated )
                {
                    // observe that the EH.Child may be null and still it has an HwndSource.
                    IntPtr focusHandle = UnsafeNativeMethods.GetFocus();
                    return (focusHandle == this.Handle || (this.HwndSource != null && focusHandle == this.HwndSource.Handle));
                }

                return false;
            }
        }

        protected override SWF.ImeMode ImeModeBase
        {
            get
            {
                return this.CanEnableIme ? base.ImeModeBase : ImeMode.Disable;
            }
            set
            {
                base.ImeModeBase = value;

                // When the ImeMode is set to ImeMode.NoControl, Winforms stops raising the ImeModeChanged event
                // on the control, we need to notify the property mapper explicitly.
                if (value == SWF.ImeMode.NoControl)
                {
                    this.OnPropertyChangedImeMode(this, EventArgs.Empty);
                }

            }
        }

        /// <summary>
        ///    Raises the EnabledChanged event
        /// </summary>
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            if (this.HwndSource != null && this.HwndSource.Handle != IntPtr.Zero)
            {
                NativeMethodsSetLastError.EnableWindow(this.HwndSource.Handle, this.Enabled);
            }
        }

        private void OnChildChanged(UIElement oldChild)
        {
            if (this.Child != null)
            {
                SyncHwndSrcImeStatus();
            }

            if (ChildChanged != null)
            {
                ChildChanged(this, new ChildChangedEventArgs(oldChild));
            }
        }

        /// <summary>
        /// Raises the Leave event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            System.Windows.Input.FocusManager.SetFocusedElement(_decorator, null);
            base.OnLeave(e);
        }

        /// <summary>
        /// Raises the GotFocus event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            SyncHwndSrcImeStatus();
            _decorator.Focus();
        }

        private void InitializeChildProperties()
        {
            FrameworkElement childFrameworkElement = Child as FrameworkElement;
            if (childFrameworkElement != null)
            {
                childFrameworkElement.SizeChanged += new SizeChangedEventHandler(childFrameworkElement_SizeChanged);
                childFrameworkElement.Height = double.NaN;
                childFrameworkElement.Width = double.NaN;
                childFrameworkElement.Margin = new Thickness(0d);
                childFrameworkElement.VerticalAlignment = SW.VerticalAlignment.Stretch;
                childFrameworkElement.HorizontalAlignment = SW.HorizontalAlignment.Stretch;

                DesignerProperties.SetIsInDesignMode(childFrameworkElement, this.DesignMode);

                Vector scale = HostUtils.GetScale(childFrameworkElement);
                SD.Size maxElementSize = Convert.ToSystemDrawingSize(new Size(childFrameworkElement.MaxWidth, childFrameworkElement.MaxHeight), scale);
                SD.Size priorMaxSize = HostUtils.ConvertZeroToUnbounded(MaximumSize);
                int maxWidth = Math.Min(priorMaxSize.Width, maxElementSize.Width);
                int maxHeight = Math.Min(priorMaxSize.Height, maxElementSize.Height);
                MaximumSize = HostUtils.ConvertUnboundedToZero(new SD.Size(maxWidth, maxHeight));
            }
        }

        /// <summary>
        /// Raises the VisibleChanged event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateBackground();
        }

        void CallUpdateBackground(object sender, EventArgs e)
        {
            UpdateBackground();
        }

        void UpdateBackground()
        {
            OnPropertyChanged("BackgroundImage", BackgroundImage); //Update the background
        }

        void childFrameworkElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AutoSize)
            {
                PerformLayout();
            }
        }

        internal AvalonAdapter HostContainerInternal
        {
            get
            {
                return _hostContainerInternal;
            }
        }
        #endregion Containership

        #region Rendering

        /// <summary>
        ///     Gets or sets a value indicating whether the hosted element has a transparent background.
        /// </summary>
        [DefaultValue(false)]
        public bool BackColorTransparent
        {
            get { return _backColorTransparent; }
            set
            {
                _backColorTransparent = value;
                UpdateBackground();
            }
        }

        /// <internalonly>
        ///     Hide GDI painting because the HwndTarget is going to just bitblt the root
        ///     visual on top of everything.
        /// </internalonly>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        protected override void OnPaint(SWF.PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        /// <internalonly>
        ///     Paint our parent's background into an offscreen HBITMAP.
        ///     We then draw this as our background in the hosted Avalon
        ///     control's Render() method to support WinForms->Avalon transparency.
        /// </internalonly>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        protected override void OnPaintBackground(SWF.PaintEventArgs pevent)
        {
            base.OnPaintBackground(pevent);
            _decorator.InvalidateVisual();
        }

        internal void InvokePaintBackgroundAndPaint(SWF.Control control, SWF.PaintEventArgs args)
        {
            this.InvokePaintBackground(control, args);
            this.InvokePaint(control, args);
        }

        /// <summary>
        /// Renders the control using the provided Graphics object.
        /// </summary>
        /// <param name="e"></param>
        [ResourceExposure(ResourceScope.None)]
        // Resource consumption: HostUtils.GetBitmapFromRenderTargetBitmap.
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        protected override void OnPrint(PaintEventArgs e)
        {
            SWMI.RenderTargetBitmap renderBitmap = HostUtils.GetBitmapForFrameworkElement(_decorator);
            if (renderBitmap != null)
            {
                using (SD.Bitmap bitmap = HostUtils.GetBitmapFromRenderTargetBitmap(this, renderBitmap, new Point(0, 0)))
                {
                    e.Graphics.DrawImage(bitmap, SD.Point.Empty);
                }
            }
        }

        #endregion rendering

        #region Keyboarding
        /// <summary>
        ///     Activates the hosted element.
        /// </summary>
        protected override void Select(bool directed, bool forward)
        {
            if (directed == true)
            {
                SWI.TraversalRequest request = new SWI.TraversalRequest(forward
                                                        ? SWI.FocusNavigationDirection.First
                                                        : SWI.FocusNavigationDirection.Last);

                //Currently ignore TabInto's return value
                (_hwndSource as IKeyboardInputSink).TabInto(request);
            }
            else
            {
                if (Child != null)
                {
                    Child.Focus();
                }
            }

            base.Select(directed, forward);
        }

        /// <summary>
        ///     Processes a command key, ensuring that the hosted element has an
        ///     opportunity to handle the command before normal Windows Forms processing.
        /// </summary>
        /// <internal>
        ///     This will try see if the pressed key is an Avalon accelerator, if so it returns TRUE,
        ///     which tells WinForms to stop trying to process this key.
        /// </internal>
        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, System.Windows.Forms.Keys keyData)
        {
            MSG msg2 = Convert.ToSystemWindowsInteropMSG(msg);
            SWI.ModifierKeys modifiers = Convert.ToSystemWindowsInputModifierKeys(keyData);

            // Let the AvalonAdapter know that ElementHost is currently processing a TabKey
            if (_hostContainerInternal != null) {
                _hostContainerInternal.ProcessingTabKeyFromElementHost = (keyData & SWF.Keys.Tab) == SWF.Keys.Tab;
            }

            bool result = (_hwndSource as IKeyboardInputSink).TranslateAccelerator(ref msg2, modifiers);

            // _hostContainerInternal can be disposed if the control is closed using keyboard.
            if (_hostContainerInternal != null && _hostContainerInternal.ProcessingTabKeyFromElementHost) {
                _hostContainerInternal.ProcessingTabKeyFromElementHost = false;
            }

            return result || base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        ///     Processes a mnemonic character, ensuring that the hosted element has an opportunity to handle the mnemonic before
        ///     normal Windows Forms processing.
        /// </summary>
        /// <internal>
        ///     This will try see if the pressed key is an Avalon accelerator, if so it tries
        ///     to process the key, which may move focus to the Avalon control.
        /// </internal>
        protected override bool ProcessMnemonic(char charCode)
        {
            string upperKey = Char.ToUpper(charCode, CultureInfo.CurrentCulture).ToString();
            if (SWI.AccessKeyManager.IsKeyRegistered(_hwndSource, upperKey))
            {
                // ProcessKey doesn't return enough information for us to handle
                // mnemonic cycling reliably.  I.e. we can't tell the difference
                // between a key which wasn't processed (returns false) from the
                // case where there is just one element with this key registered
                // (also returns false).
                SWI.AccessKeyManager.ProcessKey(_hwndSource, upperKey, false);
                return true;
            }
            else
            {
                return base.ProcessMnemonic(charCode);
            }
        }

        /// <summary>
        ///     Ensures that all WM_CHAR key messages are forwarded to the hosted element.
        /// </summary>
        /// <internal>
        ///     Grab all WM_CHAR messages as text input to ensure they're sent to
        ///     Avalon.  If Avalon doesn't handle the message, we will call
        ///     ProcessDialogChar later on.
        /// </internal>
        protected override bool IsInputChar(char charCode)
        {
            return true;
        }

        /// <summary>
        ///     Catch WM_CHAR messages which weren't handled by Avalon
        ///     (including mnemonics which were typed without the "Alt" key)
        /// </summary>
        private void InputManager_PostProcessInput(object sender, SWI.ProcessInputEventArgs e)
        {
            IKeyboardInputSink ikis = _hwndSource as IKeyboardInputSink;
            if (ikis == null || !ikis.HasFocusWithin())
            {
                return;
            }
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

        /// <summary>
        ///     Enables a window to receive keyboard messages correctly when it is opened modelessly from Windows Forms.
        /// </summary>
        /// <param name="window">The System.Windows.Window which will be opened modelessly.</param>
        public static void EnableModelessKeyboardInterop(SW.Window window)
        {
            ApplicationInterop.EnableModelessKeyboardInterop(window);
        }

        #endregion Keyboarding

        #region Window Handling & Misc
        /// <internalonly>
        ///     Raises the HandleCreated event.
        /// </internalonly>
        protected override void OnHandleCreated(EventArgs e)
        {
            // We are about to call into WPF, and possibly from a callstack unrelated to WPF at all.
            // WPF needs to install its synchronization context, exception handlers, etc. So just
            // do it the normal way through Invoke.
            SWT.Dispatcher.CurrentDispatcher.Invoke(()=>
            {
                if (_hwndSource != null)
                {
                    DisposeHWndSource();
                }
                SWF.CreateParams cp = this.CreateParams;

                HwndSourceParameters HWSParam = new HwndSourceParameters(this.Text, cp.Width, cp.Height);
                HWSParam.WindowClassStyle = cp.ClassStyle;
                HWSParam.WindowStyle = cp.Style;
                HWSParam.ExtendedWindowStyle = cp.ExStyle;
                HWSParam.ParentWindow = Handle;
                HWSParam.HwndSourceHook = HwndSourceHook;

                _hwndSource = new HwndSource(HWSParam);
                _hwndSource.RootVisual = _decorator;
                //For keyboarding: Set the IKeyboardInputSite so that keyboard interop works...
                (_hwndSource as IKeyboardInputSink).KeyboardInputSite = (HostContainerInternal as IKeyboardInputSite);
            });

            base.OnHandleCreated(e);
        }

        /// <summary>
        ///     Hook for the HwndSource.WndProc.
        /// </summary>
        private IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case NativeMethods.WM_SETFOCUS:
                    if (wParam != Handle)
                    {
                        // wParam != Handle means focus was lost by a window different from this (EH).
                        // The message was sent directly to the HwndSource (by direct mouse input).
                        // We need to notify Winforms so it can set the active control and in turn
                        // notify Control.ActiveXImpl in case we are hosted in an unmanaged app.
                        // EH will set focus back to the WPF control from its OnGotFocus method.
                        this.Focus();
                    }
                    break;

                case NativeMethods.WM_KILLFOCUS:
                    if (!this.Focused)
                    {
                        UnsafeNativeMethods.SendMessage(new HandleRef(this, this.Handle), msg, wParam, lParam);
                    }
                    break;

                case NativeMethods.WM_INPUTLANGCHANGE:
                    OnHwndSrcWmInputLangChange(msg, wParam, lParam, ref handled);
                    break;

                case NativeMethods.WM_IME_NOTIFY:
                    OnHwndSrcWmImeNotify(msg, wParam, lParam, ref handled);

                    break;
            }

            return IntPtr.Zero; // This value depends on the message being processed, see MSDN for the particular message.
        }

        private void SetHWndSourceWindowPos()
        {
            if (_hwndSource != null)
            {
                SafeNativeMethods.SetWindowPos(_hwndSource.Handle, NativeMethods.HWND_TOP, 0, 0, this.Width, this.Height, 0);
            }
        }

        /// <summary>
        ///     Synchronizes the HwndSource context status to the ElementHost's
        /// </summary>
        internal bool SyncHwndSrcImeStatus()
        {
            Debug.WriteLineIf(HostUtils.ImeMode.Level >= TraceLevel.Info, "Inside SyncHwndSrcImeStatus(), this = " + this);
            Debug.Indent();

            bool handled = false;

            if (this.HwndSource != null)
            {
                ImeMode ehImeMode = this.ImeMode != ImeMode.NoControl ? this.ImeMode : SWF.Control.PropagatingImeMode;
                ImeMode hsImeMode = SWF.ImeContext.GetImeMode(this.HwndSource.Handle);

                SetChildElementsIsImeEnabled(this.Child, ehImeMode != ImeMode.Disable);

                if (ehImeMode != hsImeMode)
                {
                    SWF.ImeContext.SetImeStatus(ehImeMode, this.HwndSource.Handle);
                }

                handled = true;
            }

            Debug.Unindent();

            return handled;
        }

        /// <summary>
        ///     Maps the ImeMode property.  This is needed to synchronize the EH IME context with TFS used in WPF.
        /// </summary>
        private static void SetChildElementsIsImeEnabled(SW.DependencyObject element, bool isEnabled)
        {
            if (element == null)
            {
                return;
            }

            if (element is SW.IInputElement)
            {
                Debug.WriteLineIf(HostUtils.ImeMode.Level >= TraceLevel.Verbose, "SetChildElementsIsImeEnabled, element = " + element);
                SWI.InputMethod.SetIsInputMethodEnabled(element, isEnabled);
            }

            // Ideally, setting the top control context mode should suffice as with any other mapped property on EH
            // but there is no InputMethodEnabledChanged event for clients to listen to to update child elements.
            // we need to traverse the visual tree and do it ourselves.

            int childCount = VisualTreeHelper.GetChildrenCount(element);

            for (int childIndex = 0; childIndex < childCount; childIndex++)
            {
                SetChildElementsIsImeEnabled(VisualTreeHelper.GetChild(element, childIndex), isEnabled);
            }
        }

        /// <summary>
        ///     Handles HwndSrc WM_INPUTLANGCHANGED.
        /// </summary>
        private void OnHwndSrcWmInputLangChange(int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            Debug.WriteLineIf(HostUtils.ImeMode.Level >= TraceLevel.Info, "Inside OnHwndSrcWmInputLangChange(), this = " + this);
            Debug.Indent();

            if (!_processingWmInputLangChanged)
            {
                _processingWmInputLangChanged = true;

                try
                {
                    // IME is being attached, notify the ElementHost to update its handle context, it in turn will pass
                    // the message to the HwndSource to update its context too.
                    OnHwndSourceMsgNotifyElementHost(msg, wParam, lParam);
                    handled = true;
                }
                finally
                {
                    _processingWmInputLangChanged = false;
                }
            }

            Debug.Unindent();
        }


        /// <summary>
        ///     Handles HwndSrc WM_IME_NOTIFY.
        /// </summary>
        private void OnHwndSrcWmImeNotify(int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            Debug.WriteLineIf(HostUtils.ImeMode.Level >= TraceLevel.Info, "Inside OnHwndSrcWmImeNotify(), this = " + this);
            Debug.Indent();

            handled = false;

            if (SWF.ImeModeConversion.IsCurrentConversionTableSupported) // TODO: remove this condition?
            {
                if (wParam == (IntPtr)NativeMethods.IMN_SETCONVERSIONMODE || wParam == (IntPtr)NativeMethods.IMN_SETOPENSTATUS)
                {
                    // IME context mode has been changed by interaction or a different IME has been attached,
                    // notify EH to update its ImeMode property if needed.
                    ImeMode hsImeMode = SWF.ImeContext.GetImeMode(this.HwndSource.Handle);
                    ImeMode ehImeMode = this.ImeMode;

                    if (hsImeMode != ehImeMode)
                    {
                        OnHwndSourceMsgNotifyElementHost(msg, wParam, lParam);
                    }

                    handled = true;
                }
            }

            Debug.Unindent();
        }

        /// <summary>
        ///     Passes a msg sent to the HwndSource into the ElementHost for preprocessing.
        /// </summary>
        private void OnHwndSourceMsgNotifyElementHost(int msg, IntPtr wParam, IntPtr lParam)
        {
            Debug.WriteLineIf(HostUtils.ImeMode.Level >= TraceLevel.Verbose, "Inside OnHwndSourceMsgNotifyElementHost()");
            Debug.Indent();

            UnsafeNativeMethods.SendMessage(new HandleRef(this, this.Handle), msg, wParam, lParam);

            Debug.Unindent();
        }

        /// <summary>
        /// Processes Windows messages.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeMethods.WM_MOVE:
                case NativeMethods.WM_SIZE:
                case NativeMethods.WM_WINDOWPOSCHANGED:
                case NativeMethods.WM_WINDOWPOSCHANGING:
                    base.WndProc(ref m);
                    SetHWndSourceWindowPos();
                    break;
                case NativeMethods.WM_PARENTNOTIFY:
                case NativeMethods.WM_REFLECT + NativeMethods.WM_PARENTNOTIFY:
                    base.WndProc(ref m);
                    if (HostUtils.LOWORD(m.WParam) == NativeMethods.WM_CREATE)
                    {
                        this.BeginInvoke(new MethodInvoker(SetHWndSourceWindowPos));
                    }
                    break;
                case NativeMethods.WM_SETREDRAW:
                    if (!DesignMode && m.WParam == IntPtr.Zero)
                    {
                        base.WndProc(ref m);
                    }
                    break;
                case NativeMethods.WM_KILLFOCUS:
                    // if focus is being set on the HwndSource then we should prevent LostFocus by
                    // handling this message.
                    if (this.HwndSource == null || this.HwndSource.Handle != m.WParam)
                    {
                        base.WndProc(ref m);

                    }
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        /// <summary>
        ///     Scales the parent container and the Windows Forms control.
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        protected override void ScaleCore(float dx, float dy)
        {
            TransformGroup layoutTransforms = new TransformGroup();
            layoutTransforms.Children.Add(_decorator.LayoutTransform);
            layoutTransforms.Children.Add(new ScaleTransform(dx, dy));
            _decorator.LayoutTransform = layoutTransforms;
            base.ScaleCore(dx, dy);
        }

        /// <internalonly/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (_hostContainerInternal != null)
                    {
                        _hostContainerInternal.Dispose();
                        _hostContainerInternal = null;
                    }
                }
                finally
                {
                    try
                    {
                        if (_hwndSource != null)
                        {
                            DisposeHWndSource();
                        }
                    }
                    finally
                    {
                        SWI.InputManager.Current.PostProcessInput -= InputManager_PostProcessInput;

                        IDisposable disposableChild = Child as IDisposable;
                        if (disposableChild != null)
                        {
                            disposableChild.Dispose();
                        }
                    }
                }
            }
            base.Dispose(disposing);
        }

        private void DisposeHWndSource()
        {
            //For keyboarding: As per comment in the Avalon code, this is set to null before disposal
            (_hwndSource as IKeyboardInputSink).KeyboardInputSite = null;

            _hwndSource.Dispose();
            _hwndSource = null;
        }
        #endregion Window Handling & Misc

        #region Property Mapping

        /// <summary>
        ///     This initializes the default property mapping for the element host.
        ///     First: it creates a new PropertyMap to store all the mappings, this
        ///         is exposed to the user as the PropertyMap property.
        ///     Second: it forwards all property Changed events that we want to listen to
        ///         towards the OnPropertyChanged method.
        ///     Third: it registers several default translators, all of which are handled by
        ///         the ElementHostPropertyTranslator deligate.
        /// </summary>
        private void StartPropertyMapping()
        {
            _propertyMap = new ElementHostPropertyMap(this);

            //This forwards property change notification to OnPropertyChanged, since
            //there is no generic way to handle listening to property value changes.
            this.BackColorChanged += this.OnPropertyChangedBackColor;
            this.BackgroundImageChanged += this.OnPropertyChangedBackgroundImage;
            this.BackgroundImageLayoutChanged += this.OnPropertyChangedBackgroundImageLayout;
            this.CursorChanged += this.OnPropertyChangedCursor;
            this.EnabledChanged += this.OnPropertyChangedEnabled;
            this.FontChanged += this.OnPropertyChangedFont;
            this.ForeColorChanged += this.OnPropertyChangedForeColor;
            this.RightToLeftChanged += this.OnPropertyChangedRightToLeft;
            this.VisibleChanged += this.OnPropertyChangedVisible;

            //We forward these property changes, but don't do anything with them.
            this.AutoSizeChanged += this.OnPropertyChangedAutoSize;
            this.BindingContextChanged += this.OnPropertyChangedBindingContext;
            this.CausesValidationChanged += this.OnPropertyChangedCausesValidation;
            this.ContextMenuStripChanged += this.OnPropertyChangedContextMenuStrip;
            this.DockChanged += this.OnPropertyChangedDock;
            this.LocationChanged += this.OnPropertyChangedLocation;
            this.MarginChanged += this.OnPropertyChangedMargin;
            this.PaddingChanged += this.OnPropertyChangedPadding;
            this.ParentChanged += this.OnPropertyChangedParent;
            this.RegionChanged += this.OnPropertyChangedRegion;
            this.SizeChanged += this.OnPropertyChangedSize;
            this.TabIndexChanged += this.OnPropertyChangedTabIndex;
            this.TabStopChanged += this.OnPropertyChangedTabStop;
            this.TextChanged += this.OnPropertyChangedText;
            this.ImeModeChanged += this.OnPropertyChangedImeMode;
        }

        /// <summary>
        ///     These delegates just map property changed events to OnPropertyChanged. These are
        ///     necessary in the WinForms model since there is no common path for property change
        ///     notification. (Avalon has an OnPropertyChanged method.)
        /// </summary>
        private void OnPropertyChangedBackColor(object sender, System.EventArgs e)
        {
            OnPropertyChanged("BackColor", this.BackColor);
        }
        private void OnPropertyChangedBackgroundImage(object sender, System.EventArgs e)
        {
            OnPropertyChanged("BackgroundImage", this.BackgroundImage);
        }
        private void OnPropertyChangedBackgroundImageLayout(object sender, System.EventArgs e)
        {
            OnPropertyChanged("BackgroundImageLayout", this.BackgroundImageLayout);
        }
        private void OnPropertyChangedCursor(object sender, System.EventArgs e)
        {
            OnPropertyChanged("Cursor", this.Cursor);
        }
        private void OnPropertyChangedEnabled(object sender, System.EventArgs e)
        {
            OnPropertyChanged("Enabled", this.Enabled);
        }
        private void OnPropertyChangedFont(object sender, System.EventArgs e)
        {
            OnPropertyChanged("Font", this.Font);
        }
        private void OnPropertyChangedForeColor(object sender, System.EventArgs e)
        {
            OnPropertyChanged("ForeColor", this.ForeColor);
        }
        private void OnPropertyChangedRightToLeft(object sender, System.EventArgs e)
        {
            OnPropertyChanged("RightToLeft", this.RightToLeft);
        }
        private void OnPropertyChangedTabStop(object sender, System.EventArgs e)
        {
            OnPropertyChanged("TabStop", this.TabStop);
        }
        private void OnPropertyChangedVisible(object sender, System.EventArgs e)
        {
            OnPropertyChanged("Visible", this.Visible);
        }

        // These properties don't have default mappings, but are added in case anyone wants to add them

        private void OnPropertyChangedAutoSize(object sender, System.EventArgs e)
        {
            OnPropertyChanged("AutoSize", this.AutoSize);
        }
        private void OnPropertyChangedPadding(object sender, System.EventArgs e)
        {
            OnPropertyChanged("Padding", this.Padding);
        }
        private void OnPropertyChangedBindingContext(object sender, System.EventArgs e)
        {
            OnPropertyChanged("BindingContext", this.BindingContext);
        }
        private void OnPropertyChangedCausesValidation(object sender, System.EventArgs e)
        {
            OnPropertyChanged("CausesValidation", this.CausesValidation);
        }
        private void OnPropertyChangedContextMenuStrip(object sender, System.EventArgs e)
        {
            OnPropertyChanged("ContextMenuStrip", this.ContextMenuStrip);
        }
        private void OnPropertyChangedDock(object sender, System.EventArgs e)
        {
            OnPropertyChanged("Dock", this.Dock);
        }
        private void OnPropertyChangedLocation(object sender, System.EventArgs e)
        {
            OnPropertyChanged("Location", this.Location);
        }
        private void OnPropertyChangedMargin(object sender, System.EventArgs e)
        {
            OnPropertyChanged("Margin", this.Margin);
        }
        private void OnPropertyChangedParent(object sender, System.EventArgs e)
        {
            OnPropertyChanged("Parent", this.Parent);
        }
        private void OnPropertyChangedRegion(object sender, System.EventArgs e)
        {
            OnPropertyChanged("Region", this.Region);
        }
        private void OnPropertyChangedSize(object sender, System.EventArgs e)
        {
            OnPropertyChanged("Size", this.Size);
        }
        private void OnPropertyChangedTabIndex(object sender, System.EventArgs e)
        {
            OnPropertyChanged("TabIndex", this.TabIndex);
        }
        private void OnPropertyChangedText(object sender, System.EventArgs e)
        {
            OnPropertyChanged("Text", this.Text);
        }
        private void OnPropertyChangedImeMode(object sender, System.EventArgs e)
        {
            OnPropertyChanged("ImeMode", this.ImeMode);
        }


        /// <summary>
        ///     Notifies the property map that a property has changed.
        /// </summary>
        /// <param name="propertyName">the name of the property which has changed and requires translation</param>
        /// <param name="value">the new value of the property</param>
        public virtual void OnPropertyChanged(string propertyName, object value)
        {
            if (PropertyMap != null)
            {
                PropertyMap.OnPropertyChanged(this, propertyName, value);
            }
        }

        /// <summary>
        ///     Gets the property map, which determines how setting properties on the
        ///     ElementHost control affects the hosted Windows Presentation Foundation element.
        /// </summary>
        [Browsable(false)]
        public PropertyMap PropertyMap
        {
            get { return _propertyMap; }
        }
        private ElementHostPropertyMap _propertyMap;

        #endregion Property Mapping

        #region Hidden Events
        /// <summary>
        /// Occurs when the value of the BindingContext property changes.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler BindingContextChanged
        {
            add { base.BindingContextChanged += value; }
            remove { base.BindingContextChanged -= value; }
        }

        /// <summary>
        /// Occurs when the control is clicked.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler Click
        {
            add { base.Click += value; }
            remove { base.Click -= value; }
        }

        /// <summary>
        /// Occurs when the value of the ClientSize property changes.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler ClientSizeChanged
        {
            add { base.ClientSizeChanged += value; }
            remove { base.ClientSizeChanged -= value; }
        }

        /// <summary>
        /// Occurs when a new control is added to the Control.ControlCollection.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event ControlEventHandler ControlAdded
        {
            add { base.ControlAdded += value; }
            remove { base.ControlAdded -= value; }
        }

        /// <summary>
        /// Occurs when a control is removed from the Control.ControlCollection.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event ControlEventHandler ControlRemoved
        {
            add { base.ControlRemoved += value; }
            remove { base.ControlRemoved -= value; }
        }

        /// <summary>
        /// Occurs when the value of the Cursor property changes.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler CursorChanged
        {
            add { base.CursorChanged += value; }
            remove { base.CursorChanged -= value; }
        }

        /// <summary>
        /// Occurs when the control is double-clicked.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler DoubleClick
        {
            add { base.DoubleClick += value; }
            remove { base.DoubleClick -= value; }
        }

        /// <summary>
        /// Occurs when a drag-and-drop operation is completed.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event DragEventHandler DragDrop
        {
            add { base.DragDrop += value; }
            remove { base.DragDrop -= value; }
        }

        /// <summary>
        /// Occurs when an object is dragged into the control's bounds.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event DragEventHandler DragEnter
        {
            add { base.DragEnter += value; }
            remove { base.DragEnter -= value; }
        }

        /// <summary>
        /// Occurs when an object is dragged out of the control's bounds.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler DragLeave
        {
            add { base.DragLeave += value; }
            remove { base.DragLeave -= value; }
        }

        /// <summary>
        /// Occurs when an object is dragged over the control's bounds.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event DragEventHandler DragOver
        {
            add { base.DragOver += value; }
            remove { base.DragOver -= value; }
        }

        /// <summary>
        /// Occurs when the control is entered.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler Enter
        {
            add { base.Enter += value; }
            remove { base.Enter -= value; }
        }

        /// <summary>
        /// Occurs when the Font property value changes.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler FontChanged
        {
            add { base.FontChanged += value; }
            remove { base.FontChanged -= value; }
        }

        /// <summary>
        /// Occurs when the ForeColor property value changes.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler ForeColorChanged
        {
            add { base.ForeColorChanged += value; }
            remove { base.ForeColorChanged -= value; }
        }

        /// <summary>
        /// Occurs during a drag operation.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event GiveFeedbackEventHandler GiveFeedback
        {
            add { base.GiveFeedback += value; }
            remove { base.GiveFeedback -= value; }
        }

        /// <summary>
        /// Occurs when the control receives focus.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler GotFocus
        {
            add { base.GotFocus += value; }
            remove { base.GotFocus -= value; }
        }

        /// <summary>
        /// Occurs when a control's display requires redrawing.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event InvalidateEventHandler Invalidated
        {
            add { base.Invalidated += value; }
            remove { base.Invalidated -= value; }
        }

        /// <summary>
        /// Occurs when a key is pressed while the control has focus.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event KeyEventHandler KeyDown
        {
            add { base.KeyDown += value; }
            remove { base.KeyDown -= value; }
        }

        /// <summary>
        /// Occurs when a key is pressed while the control has focus.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event KeyPressEventHandler KeyPress
        {
            add { base.KeyPress += value; }
            remove { base.KeyPress -= value; }
        }

        /// <summary>
        /// Occurs when a key is released while the control has focus.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event KeyEventHandler KeyUp
        {
            add { base.KeyUp += value; }
            remove { base.KeyUp -= value; }
        }

        /// <summary>
        /// Occurs when a control should reposition its child controls.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event LayoutEventHandler Layout
        {
            add { base.Layout += value; }
            remove { base.Layout -= value; }
        }

        /// <summary>
        /// Occurs when the input focus leaves the control.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler Leave
        {
            add { base.Leave += value; }
            remove { base.Leave -= value; }
        }

        /// <summary>
        /// Occurs when the control loses focus.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler LostFocus
        {
            add { base.LostFocus += value; }
            remove { base.LostFocus -= value; }
        }

        /// <summary>
        /// Occurs when the control loses mouse capture.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler MouseCaptureChanged
        {
            add { base.MouseCaptureChanged += value; }
            remove { base.MouseCaptureChanged -= value; }
        }

        /// <summary>
        /// Occurs when the control is clicked by the mouse.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event MouseEventHandler MouseClick
        {
            add { base.MouseClick += value; }
            remove { base.MouseClick -= value; }
        }

        /// <summary>
        /// Occurs when the control is double clicked by the mouse.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event MouseEventHandler MouseDoubleClick
        {
            add { base.MouseDoubleClick += value; }
            remove { base.MouseDoubleClick -= value; }
        }

        /// <summary>
        /// Occurs when the mouse pointer is over the control and a mouse button is pressed.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event MouseEventHandler MouseDown
        {
            add { base.MouseDown += value; }
            remove { base.MouseDown -= value; }
        }

        /// <summary>
        /// Occurs when the mouse pointer enters the control.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler MouseEnter
        {
            add { base.MouseEnter += value; }
            remove { base.MouseEnter -= value; }
        }

        /// <summary>
        /// Occurs when the mouse pointer rests on the control.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler MouseHover
        {
            add { base.MouseHover += value; }
            remove { base.MouseHover -= value; }
        }

        /// <summary>
        /// Occurs when the mouse pointer leaves the control. (
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler MouseLeave
        {
            add { base.MouseLeave += value; }
            remove { base.MouseLeave -= value; }
        }

        /// <summary>
        /// Occurs when the mouse pointer is moved over the control.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event MouseEventHandler MouseMove
        {
            add { base.MouseMove += value; }
            remove { base.MouseMove -= value; }
        }

        /// <summary>
        /// Occurs when the mouse pointer is over the control and a mouse button is released.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event MouseEventHandler MouseUp
        {
            add { base.MouseUp += value; }
            remove { base.MouseUp -= value; }
        }

        /// <summary>
        /// Occurs when the mouse wheel moves while the control has focus.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event MouseEventHandler MouseWheel
        {
            add { base.MouseWheel += value; }
            remove { base.MouseWheel -= value; }
        }

        /// <summary>
        /// Occurs when the control's padding changes.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler PaddingChanged
        {
            add { base.PaddingChanged += value; }
            remove { base.PaddingChanged -= value; }
        }

        /// <summary>
        /// Occurs when the control is redrawn.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event PaintEventHandler Paint
        {
            add { base.Paint += value; }
            remove { base.Paint -= value; }
        }

        /// <summary>
        /// Occurs before the KeyDown event when a key is pressed while focus is on this control.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event PreviewKeyDownEventHandler PreviewKeyDown
        {
            add { base.PreviewKeyDown += value; }
            remove { base.PreviewKeyDown -= value; }
        }

        /// <summary>
        /// Occurs during a drag-and-drop operation and enables the drag source to determine
        /// whether the drag-and-drop operation should be canceled.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event QueryContinueDragEventHandler QueryContinueDrag
        {
            add { base.QueryContinueDrag += value; }
            remove { base.QueryContinueDrag -= value; }
        }

        /// <summary>
        /// Occurs when the control is resized.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler Resize
        {
            add { base.Resize += value; }
            remove { base.Resize -= value; }
        }

        /// <summary>
        /// Occurs when the RightToLeft property value changes.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler RightToLeftChanged
        {
            add { base.RightToLeftChanged += value; }
            remove { base.RightToLeftChanged -= value; }
        }

        /// <summary>
        /// Occurs when the Size property value changes.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler SizeChanged
        {
            add { base.SizeChanged += value; }
            remove { base.SizeChanged -= value; }
        }

        /// <summary>
        /// Occurs when the Text property value changes.
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler TextChanged
        {
            add { base.TextChanged += value; }
            remove { base.TextChanged -= value; }
        }
        #endregion

        #region DEBUG
#if DEBUG
        private static readonly TraceSwitch _traceLayout = new TraceSwitch("ElementHostLayout", "Tracks ElementHost layout information.");
#else
        private static readonly TraceSwitch _traceLayout = null;
#endif

        #endregion DEBUG
    }


    #region AvalonAdapter
    internal class AvalonAdapter : SWC.DockPanel, IDisposable, IKeyboardInputSite, IAvalonAdapter
    {
        private ElementHost _hostControl;

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static AvalonAdapter()
        {
            //These properties define how tabbing occurs in the DockPanel. To be most like
            //WinForms these are set to use Continue mode as the default.
            //TODO: Do we need to have ControlTabNavigationProperty to for Ctrl-Tab???
            SWI.KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(
                    typeof(AvalonAdapter),
                    new FrameworkPropertyMetadata(SWI.KeyboardNavigationMode.Continue));
            SWI.KeyboardNavigation.TabNavigationProperty.OverrideMetadata(
                    typeof(AvalonAdapter),
                    new FrameworkPropertyMetadata(SWI.KeyboardNavigationMode.Continue));
        }

        /// <internalOnly>
        ///     Creates the Avalon control we use to host
        ///     other WinForms control in Avalon.
        ///</internalOnly>
        public AvalonAdapter(ElementHost hostControl)
        {
            _hostControl = hostControl;
        }

        ///<internalOnly>
        ///   unsink our events
        ///</internalOnly>
        public void Dispose()
        {
            _hostControl = null;
        }

        // used to force invalidation of the avalon control's client area.
        public static readonly DependencyProperty ForceInvalidateProperty =
                DependencyProperty.Register(
                        "ForceInvalidate",
                        typeof(bool),
                        typeof(AvalonAdapter),
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        private static object OnGetForceInvalidate(DependencyObject d)
        {
            return ((AvalonAdapter)d).ForceInvalidate;
        }

        /// <summary>
        ///     This property is toggled by ElementHost to force the hosted
        ///     Avalon control to redraw itself
        /// </summary>
        public bool ForceInvalidate
        {
            get { return (bool)GetValue(ForceInvalidateProperty); }
            set { SetValue(ForceInvalidateProperty, value); }
        }

        /// <summary>
        ///     This is a transparency optimization. It looks up the SWF visual heirarchy to
        ///     find the first opaque control. It then uses this color as the background for
        ///     OnRender. This doesn't work all the time, but is much faster than the bitmap
        ///     conversion.
        /// </summary>
        /// <param name="whichControl">The current control</param>
        /// <returns>The first opaque color found, or Color.Empty if none is found</returns>
        private static SD.Color FindSolidColorParent(Control whichControl)
        {
            Control control = whichControl;
            while (control.Parent != null)
            {
                control = control.Parent;
                if (control.BackColor != SD.Color.Empty && control.BackColor.A == 255)
                {
                    return control.BackColor;
                }
            }
            return SD.Color.Empty;
        }

        #region IAvalonAdapter

        /// <summary>
        ///     Implements IAvalonAdapter.OnNoMoreTabStops an extended version of IKeyboardInputSite.OnNoMoreTabStops.
        ///     Components call this when they want to move focus ("tab") but have nowhere further to tab within their
        ///     own component.
        /// </summary>
        /// <param name="shouldCycle">
        ///     Whether focus should wrap around from the caller if we couldn't move focus.
        /// </param>
        /// <returns>
        ///     True if the site moved focus, false if the calling component still has focus.
        /// </returns>
        public bool OnNoMoreTabStops(SWI.TraversalRequest request, ref bool shouldCycle)
        {
            if(ProcessingTabKeyFromElementHost)
            {
                shouldCycle = false;
                return false;
            }

            return OnNoMoreTabStops(request);
        }

        #endregion

        #region IKeyboardInputSite
        public void Unregister()
        {
        }

        public bool OnNoMoreTabStops(SWI.TraversalRequest request)
        {
            //Tabbing out of hosted elements

            //Select the next control
            bool forward = true;
            Debug.Assert(request != null, "request was null!");
            if (request != null)
            {
                switch (request.FocusNavigationDirection)
                {
                    case System.Windows.Input.FocusNavigationDirection.Down:
                    case System.Windows.Input.FocusNavigationDirection.Right:
                    case System.Windows.Input.FocusNavigationDirection.Next:
                        forward = true;
                        break;
                    case System.Windows.Input.FocusNavigationDirection.Up:
                    case System.Windows.Input.FocusNavigationDirection.Left:
                    case System.Windows.Input.FocusNavigationDirection.Previous:
                        forward = false;
                        break;
                    case System.Windows.Input.FocusNavigationDirection.First:
                    case System.Windows.Input.FocusNavigationDirection.Last:
                        break;
                    default:
                        Debug.Assert(false, "Unknown FocusNavigationDirection");
                        break;
                }
            }

            if (_hostControl != null)
            {
                Control topMostParent = null;

                if (forward)
                {
                    // Get _hostControl's top-most parent.
                    topMostParent = _hostControl;
                    while (topMostParent.Parent != null)
                    {
                        topMostParent = topMostParent.Parent;
                    }

                    bool shouldWrap = ShouldSearchWrapForParentControl(topMostParent);

                    if (topMostParent.SelectNextControl(_hostControl, true, true, true, shouldWrap))
                    {
                        return true;
                    }

                }
                else
                {
                    // DDVSO: 458074. The above logic may not work in some cases going backwards,
                    // namely when a UserControl is involved, since the previous selectable item
                    // starting from the ElementHost would be its own UserControl, causing it to
                    // select the ElementHost again. Instead try to select the next control using
                    // the direct parent and starting from the current control, if that fails we
                    // go one level up.
                    Control currentControl = _hostControl;
                    Control parentControl = _hostControl.Parent;

                    while (parentControl != null)
                    {
                        bool shouldWrap = ShouldSearchWrapForParentControl(parentControl);

                        if (parentControl.SelectNextControl(currentControl, false, true, true, shouldWrap))
                        {
                            return true;
                        }

                        currentControl = parentControl;
                        parentControl = parentControl.Parent;
                    }

                    topMostParent = currentControl;
                }

                if (!CoreAppContextSwitches.UseNetFx471CompatibleAccessibilityFeatures)
                {
                    // DDVSO: 445603. If the top-most parent in this WinForms hierarchy is a WinFormsAdapter,
                    // that means there may be more WPF controls that can receive focus outside, forward the
                    // OnNoMoreTabStops call to the adapter's host.
                    WinFormsAdapter adapter = topMostParent as WinFormsAdapter;

                    if (adapter != null)
                    {
                        return adapter.HostKeyboardInputSite?.OnNoMoreTabStops(request) == true;
                    }
                }
            }

            return false;
        }

        private bool ShouldSearchWrapForParentControl(Control control)
        {
            // Only wrap for topmost controls
            // DDVSO 445603. If the topmost parent is a WinFormsAdapter we
            // shouldn't wrap, since there may be more WPF controls outside of
            // this WinForms hierarchy, only apply this logic if the accessibility
            // flag is set.
            return (control.Parent == null) &&
                    (CoreAppContextSwitches.UseNetFx471CompatibleAccessibilityFeatures
                    || !(control is WinFormsAdapter));
        }

        public IKeyboardInputSink Sink
        {
            get
            {
                return (_hostControl.HwndSource as IKeyboardInputSink);
            }
        }
        #endregion IKeyboardInputSite

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new ElementHostAutomationPeer(this);
        }

        // Set by ElementHost to let this AvalonAdapter it is currently processing a Tab Key.
        internal bool ProcessingTabKeyFromElementHost{ get; set; }
    }
    #endregion AvalonAdapter
}
