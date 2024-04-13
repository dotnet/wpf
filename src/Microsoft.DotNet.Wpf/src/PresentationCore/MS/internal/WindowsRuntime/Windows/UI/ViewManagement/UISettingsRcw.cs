using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Media;

namespace MS.Internal.WindowsRuntime
{
    namespace Windows.UI.ViewManagement
    {
        /// <summary>
        /// Contains internal RCWs for invoking the UISettings
        /// </summary>
        internal static class UISettingsRCW
        {

            public static object GetUISettingsInstance()
            {
                const string typeName = "Windows.UI.ViewManagement.UISettings";
                IntPtr hstring = IntPtr.Zero;
                int hr = NativeMethods.WindowsCreateString(typeName, typeName.Length, out hstring);
                Marshal.ThrowExceptionForHR(hr);
                try
                {
                    hr = NativeMethods.RoActivateInstance(hstring, out object instance);
                    Marshal.ThrowExceptionForHR(hr);
                    return instance;
                }
                finally
                {
                    hr = NativeMethods.WindowsDeleteString(hstring);
                    Marshal.ThrowExceptionForHR(hr);
                }
            }

            [Guid("03021BE4-5254-4781-8194-5168F7D06D7B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [ComImport]
            internal interface IUISettings3
            {
                void GetIids(out uint iidCount, out IntPtr iids);

                void GetRuntimeClassName(out string className);

                void GetTrustLevel(out TrustLevel TrustLevel);

                UIColor GetColorValue(UIColorType desiredColor);
            }

            internal enum TrustLevel
            {
                BaseTrust,
                PartialTrust,
                FullTrust
            }

            public enum UIColorType
            {
                Background = 0,
                Foreground = 1,
                AccentDark3 = 2,
                AccentDark2 = 3,
                AccentDark1 = 4,
                Accent = 5,
                AccentLight1 = 6,
                AccentLight2 = 7,
                AccentLight3 = 8,
                Complement = 9
            }

            internal readonly record struct UIColor(byte A, byte R, byte G, byte B);
        }
    }
}