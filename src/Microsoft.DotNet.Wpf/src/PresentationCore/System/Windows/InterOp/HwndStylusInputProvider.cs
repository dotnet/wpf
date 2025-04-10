// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Input.StylusWisp;
using System.Windows.Media;
using System.Windows.Threading;
using MS.Win32;
using MS.Internal.Interop;
using MS.Utility;

namespace System.Windows.Interop
{
    /////////////////////////////////////////////////////////////////////////

    internal sealed class HwndStylusInputProvider : DispatcherObject, IStylusInputProvider
    {
        private const uint TABLET_PRESSANDHOLD_DISABLED = 0x00000001;
        private const uint TABLET_TAPFEEDBACK_DISABLED  = 0x00000008;
        private const uint TABLET_TOUCHUI_FORCEON       = 0x00000100;
        private const uint TABLET_TOUCHUI_FORCEOFF      = 0x00000200;
        private const uint TABLET_FLICKS_DISABLED       = 0x00010000;

        private const int MultiTouchEnabledFlag         = 0x01000000;

        /////////////////////////////////////////////////////////////////////
        internal HwndStylusInputProvider(HwndSource source)
        {
            InputManager inputManager = InputManager.Current;
            _stylusLogic = StylusLogic.GetCurrentStylusLogicAs<WispLogic>();

            IntPtr sourceHandle;

            // Register ourselves as an input provider with the input manager.
            _site = inputManager.RegisterInputProvider(this);

            sourceHandle = source.Handle;

            _stylusLogic.RegisterHwndForInput(inputManager, source);
            _source = source;

            // Enables multi-touch input
            UnsafeNativeMethods.SetProp(new HandleRef(this, sourceHandle), "MicrosoftTabletPenServiceProperty", new HandleRef(null, new IntPtr(MultiTouchEnabledFlag)));
        }

        /////////////////////////////////////////////////////////////////////


        public void Dispose()
        {
            if(_site is not null)
            {
                _site.Dispose();
                _site = null;

                _stylusLogic.UnRegisterHwndForInput(_source);
                _stylusLogic = null;
                _source = null;
            }
        }

        /////////////////////////////////////////////////////////////////////
        bool IInputProvider.ProvidesInputForRootVisual(Visual v) => _source.RootVisual == v;

        void IInputProvider.NotifyDeactivate() {}

        IntPtr IStylusInputProvider.FilterMessage(IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr result = IntPtr.Zero ;

            // It is possible to be re-entered during disposal.  Just return.
            if (_source is null)
            {
                return result;
            }

            switch(msg)
            {
                case WindowMessage.WM_ENABLE:
                    _stylusLogic.OnWindowEnableChanged(hwnd, (int)NativeMethods.IntPtrToInt32(wParam) == 0);
                    break;

                case WindowMessage.WM_TABLET_QUERYSYSTEMGESTURESTATUS:
                    handled = true;

                    NativeMethods.POINT pt1 = new NativeMethods.POINT(
                                            NativeMethods.SignedLOWORD(lParam),
                                            NativeMethods.SignedHIWORD(lParam));
                    SafeNativeMethods.ScreenToClient(new HandleRef(this, hwnd), ref pt1);
                    Point ptClient1 = new Point(pt1.x, pt1.y);

                    IInputElement inputElement = StylusDevice.LocalHitTest(_source, ptClient1);
                    if (inputElement != null)
                    {
                        // walk up the parent chain
                        DependencyObject elementCur = (DependencyObject)inputElement;
                        bool isPressAndHoldEnabled = Stylus.GetIsPressAndHoldEnabled(elementCur);
                        bool isFlicksEnabled = Stylus.GetIsFlicksEnabled(elementCur);
                        bool isTapFeedbackEnabled = Stylus.GetIsTapFeedbackEnabled(elementCur);
                        bool isTouchFeedbackEnabled = Stylus.GetIsTouchFeedbackEnabled(elementCur);

                        uint flags = 0;

                        if (!isPressAndHoldEnabled)
                        {
                            flags |= TABLET_PRESSANDHOLD_DISABLED;
                        }

                        if (!isTapFeedbackEnabled)
                        {
                            flags |= TABLET_TAPFEEDBACK_DISABLED;
                        }

                        if (isTouchFeedbackEnabled)
                        {
                            flags |= TABLET_TOUCHUI_FORCEON;
                        }
                        else
                        {
                            flags |= TABLET_TOUCHUI_FORCEOFF;
                        }

                        if (!isFlicksEnabled)
                        {
                            flags |= TABLET_FLICKS_DISABLED;
                        }

                        result = new IntPtr(flags);
                    }
                    break;

                case WindowMessage.WM_TABLET_FLICK:
                    handled = true;

                    int flickData = NativeMethods.IntPtrToInt32(wParam);

                    // We always handle any scroll actions if we are enabled.  We do this when we see the SystemGesture Flick come through.
                    // Note: Scrolling happens on window flicked on even if it is not the active window.
                    if(_stylusLogic != null && _stylusLogic.Enabled && (WispLogic.GetFlickAction(flickData) == StylusLogic.FlickAction.Scroll))
                    {
                        result = new IntPtr(0x0001); // tell UIHub the flick has already been handled.
                    }
                    break;
            }

            if (handled && EventTrace.IsEnabled(EventTrace.Keyword.KeywordInput | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientInputMessage, EventTrace.Keyword.KeywordInput | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info,
                                                    (_source.CompositionTarget != null ? _source.CompositionTarget.Dispatcher.GetHashCode() : 0),
                                                     hwnd.ToInt64(),
                                                     msg,
                                                     (int)wParam,
                                                     (int)lParam);
            }

            return result;
        }

        /////////////////////////////////////////////////////////////////////

        private WispLogic         _stylusLogic;
        private HwndSource        _source;
        private InputProviderSite _site;
    }
}
