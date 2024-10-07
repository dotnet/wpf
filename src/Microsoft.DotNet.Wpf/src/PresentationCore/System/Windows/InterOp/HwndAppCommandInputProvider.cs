// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using MS.Utility;
using MS.Internal;
using MS.Internal.Interop;
using MS.Win32;

namespace System.Windows.Interop
{
    internal sealed class HwndAppCommandInputProvider : DispatcherObject, IInputProvider, IDisposable
    {
        internal HwndAppCommandInputProvider( HwndSource source )
        {
            _site = InputManager.Current.RegisterInputProvider(this);
            _source = source;
        }

        public void Dispose( )
        {
            _site?.Dispose();
            _site = null;
            _source = null;
        }

        bool IInputProvider.ProvidesInputForRootVisual( Visual v )
        {
            Debug.Assert(null != _source);
            return _source.RootVisual == v;
        }

        void IInputProvider.NotifyDeactivate() {}

        internal IntPtr FilterMessage( IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam, ref bool handled )
        {
            // It is possible to be re-entered during disposal.  Just return.
            if(_source is null)
            {
                return IntPtr.Zero;
            }
            
            if (msg == WindowMessage.WM_APPCOMMAND)
            {
                // WM_APPCOMMAND message notifies a window that the user generated an application command event,
                // for example, by clicking an application command button using the mouse or typing an application command
                // key on the keyboard.
                RawAppCommandInputReport report = new RawAppCommandInputReport(
                                                        _source,
                                                        InputMode.Foreground,
                                                        SafeNativeMethods.GetMessageTime(),
                                                        GetAppCommand(lParam),
                                                        GetDevice(lParam),
                                                        InputType.Command);

                handled = _site.ReportInput(report);
            }
            
            return handled ? new IntPtr(1) : IntPtr.Zero ;
        }

        /// <summary>
        /// Implementation of the GET_APPCOMMAND_LPARAM macro defined in Winuser.h
        /// </summary>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static int GetAppCommand( IntPtr lParam )
        {
            return ((short)(NativeMethods.SignedHIWORD(NativeMethods.IntPtrToInt32(lParam)) & ~NativeMethods.FAPPCOMMAND_MASK));
        }

        /// <summary>
        /// Returns the input device that originated this app command.
        /// InputType.Hid represents an unspecified device that is neither the
        /// keyboard nor the mouse.
        /// </summary>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static InputType GetDevice(IntPtr lParam)
        {
            InputType inputType = InputType.Hid;

            // Implementation of the GET_DEVICE_LPARAM macro defined in Winuser.h
            // Returns either FAPPCOMMAND_KEY (the user pressed a key), FAPPCOMMAND_MOUSE
            // (the user clicked a mouse button) or FAPPCOMMAND_OEM (unknown device)
            ushort  deviceId = (ushort)(NativeMethods.SignedHIWORD(NativeMethods.IntPtrToInt32(lParam)) & NativeMethods.FAPPCOMMAND_MASK);

            switch (deviceId)
            {
                case NativeMethods.FAPPCOMMAND_MOUSE:
                    inputType = InputType.Mouse;
                    break;

                case NativeMethods.FAPPCOMMAND_KEY:
                    inputType =  InputType.Keyboard;
                    break;

                case NativeMethods.FAPPCOMMAND_OEM:
                default:
                    // Unknown device id or FAPPCOMMAND_OEM.
                    // In either cases we set it to the generic human interface device.
                    inputType=InputType.Hid;
                    break;
            }

            return inputType;
        }

        private HwndSource _source;

        private InputProviderSite _site;
    }
}


