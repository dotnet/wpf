// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using MS.Utility;
using MS.Internal;
using MS.Internal.Interop;
using MS.Win32;

namespace System.Windows.Interop
{
    internal sealed class HwndAppCommandInputProvider : DispatcherObject, IInputProvider, IDisposable
    {
        /// <SecurityNote>
        /// Accesses and store critical data. This class is also critical (_site and _source)
        /// </SecurityNote>
        internal HwndAppCommandInputProvider( HwndSource source )
        {
            (new UIPermission(PermissionState.Unrestricted)).Assert();
            try
            {
                 _site = new SecurityCriticalDataClass<InputProviderSite>(InputManager.Current.RegisterInputProvider(this));
            }
            finally
            {
                UIPermission.RevertAssert();
            }
            _source = new SecurityCriticalDataClass<HwndSource>(source);
        }

        /// <SecurityNote>
        /// Critical:This class accesses critical data, _site.
        /// TreatAsSafe: This class does not expose the critical data
        /// </SecurityNote>
        public void Dispose( )
        {
            if (_site != null)
            {
                _site.Value.Dispose();
                _site = null;
            }
            _source = null;
        }

        /// <SecurityNote>
        /// Critical: As this accesses critical data HwndSource
        /// TreatAsSafe:Information about whether a given input provider services
        /// a visual is safe to expose. This method does not expose the critical data either.
        /// </SecurityNote>
        bool IInputProvider.ProvidesInputForRootVisual( Visual v )
        {
            Debug.Assert(null != _source);
            return _source.Value.RootVisual == v;
        }

        void IInputProvider.NotifyDeactivate() {}

        /// <SecurityNote>
        /// Critical: As this accesses critical data HwndSource
        /// </SecurityNote>
        internal IntPtr FilterMessage( IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam, ref bool handled )
        {
            // It is possible to be re-entered during disposal.  Just return.
            if(null == _source || null == _source.Value)
            {
                return IntPtr.Zero;
            }
            
            if (msg == WindowMessage.WM_APPCOMMAND)
            {
                // WM_APPCOMMAND message notifies a window that the user generated an application command event,
                // for example, by clicking an application command button using the mouse or typing an application command
                // key on the keyboard.
                RawAppCommandInputReport report = new RawAppCommandInputReport(
                                                        _source.Value,
                                                        InputMode.Foreground,
                                                        SafeNativeMethods.GetMessageTime(),
                                                        GetAppCommand(lParam),
                                                        GetDevice(lParam),
                                                        InputType.Command);

                handled = _site.Value.ReportInput(report);
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

        /// <SecurityNote>
        /// This is got under an elevation and is hence critical. This data is not ok to expose.
        /// </SecurityNote>
        private SecurityCriticalDataClass<HwndSource> _source;

        /// <SecurityNote>
        /// This is got under an elevation and is hence critical.This data is not ok to expose.
        /// </SecurityNote>
        private SecurityCriticalDataClass<InputProviderSite> _site;
    }
}


