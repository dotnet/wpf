// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Runtime.InteropServices;
using System.Security;
using System.ComponentModel;


using System.Windows;
using System.Windows.Interop;
using MS.Utility;
using MS.Win32;
using MS.Internal.PresentationFramework;

namespace System.Windows
{
    /// <devdoc>
    ///    <para>
    ///       Displays a
    ///       message box that can contain text, button, and symbols that
    ///       inform and instruct the
    ///       user.
    ///    </para>
    /// </devdoc>
    public sealed class MessageBox 
    {
#if NEVER
        class WindowWin32Window : IWin32Window
        {
            Window window;
            internal WindowWin32Window(Window window)
            {
                this.window = window;
            }
            IntPtr IWin32Window.Handle { get { return window.SourceWindow.Handle; } }
            IntPtr IWin32Window.Parent
            {   
                get
                {
                    return ((IWin32Window)window).Parent;
                }
                set
                {
                    ((IWin32Window)window).Parent = value;
                }
            }
        }
#endif
        private const int IDOK             = 1;
        private const int IDCANCEL         = 2;
        private const int IDABORT          = 3;
        private const int IDRETRY          = 4;
        private const int IDIGNORE         = 5;
        private const int IDYES            = 6;
        private const int IDNO             = 7;
        private const int DEFAULT_BUTTON1  = 0x00000000;
        private const int DEFAULT_BUTTON2  = 0x00000100;
        private const int DEFAULT_BUTTON3  = 0x00000200;

        /// <devdoc>
        ///     This constructor is private so people aren't tempted to try and create
        ///     instances of these -- they should just use the static show
        ///     methods.
        /// </devdoc>
        private MessageBox() 
        {
        }

        private static MessageBoxResult Win32ToMessageBoxResult(int value) 
        {
            switch (value) 
            {
                case IDOK:
                    return MessageBoxResult.OK;
                case IDCANCEL:
                    return MessageBoxResult.Cancel;
                case IDYES:
                    return MessageBoxResult.Yes;
                case IDNO:
                    return MessageBoxResult.No;
                default:
                    return MessageBoxResult.No;
            }
        }

        #region No Owner Methods
        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text, caption, and style.
        ///    </para>
        /// </devdoc>
        public static MessageBoxResult Show(
            string messageBoxText, 
            string caption, 
            MessageBoxButton button, 
            MessageBoxImage icon, 
            MessageBoxResult defaultResult, 
            MessageBoxOptions options) 
        {
            return ShowCore(IntPtr.Zero, messageBoxText, caption, button, icon, defaultResult, options);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text, caption, and style.
        ///    </para>
        /// </devdoc>
        public static MessageBoxResult Show(
            string messageBoxText, 
            string caption, 
            MessageBoxButton button, 
            MessageBoxImage icon, 
            MessageBoxResult defaultResult) 
        {
            return ShowCore(IntPtr.Zero, messageBoxText, caption, button, icon, defaultResult, 0);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text, caption, and style.
        ///    </para>
        /// </devdoc>
        public static MessageBoxResult Show(
            string messageBoxText, 
            string caption, 
            MessageBoxButton button, 
            MessageBoxImage icon) 
        {
            return ShowCore(IntPtr.Zero, messageBoxText, caption, button, icon, 0, 0);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text, caption, and style.
        ///    </para>
        /// </devdoc>
        public static MessageBoxResult Show(
            string messageBoxText, 
            string caption, 
            MessageBoxButton button) 
        {
            return ShowCore(IntPtr.Zero, messageBoxText, caption, button, MessageBoxImage.None, 0, 0);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text and caption.
        ///    </para>
        /// </devdoc>
        public static MessageBoxResult Show(string messageBoxText, string caption) 
        {
            return ShowCore(IntPtr.Zero, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, 0, 0);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text.
        ///    </para>
        /// </devdoc>
        public static MessageBoxResult Show(string messageBoxText) 
        {
            return ShowCore(IntPtr.Zero, messageBoxText, String.Empty, MessageBoxButton.OK, MessageBoxImage.None, 0, 0);
        }
        #endregion

#if WIN32_OWNER_WINDOW
        #region IWin32Window Methods
        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text, caption, and style.
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        public static MessageBoxResult Show(IWin32Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, 
            MessageBoxResult defaultResult, MessageBoxOptions options) 
        {
            return ShowCore(owner, messageBoxText, caption, button, icon, defaultResult, options);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text, caption, and style.
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        public static MessageBoxResult Show(IWin32Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, 
            MessageBoxResult defaultResult) 
        {
            return ShowCore(owner, messageBoxText, caption, button, icon, defaultResult, 0);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text, caption, and style.
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        public static MessageBoxResult Show(IWin32Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon) 
        {
            return ShowCore(owner, messageBoxText, caption, button, icon, 0, 0);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text, caption, and style.
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        public static MessageBoxResult Show(IWin32Window owner, string messageBoxText, string caption, MessageBoxButton button) 
        {
            return ShowCore(owner, messageBoxText, caption, button, MessageBoxImage.None, 0, 0);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text and caption.
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        public static MessageBoxResult Show(IWin32Window owner, string messageBoxText, string caption) 
        {
            return ShowCore(owner, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, 0, 0);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text.
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        public static MessageBoxResult Show(IWin32Window owner, string messageBoxText) 
        {
            return ShowCore(owner, messageBoxText, String.Empty, MessageBoxButton.OK, MessageBoxImage.None, 0, 0);
        }
        #endregion
#endif
        #region Window Methods
        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text, caption, and style.
        ///    </para>
        /// </devdoc>
        public static MessageBoxResult Show(
            Window owner, 
            string messageBoxText, 
            string caption, 
            MessageBoxButton button, 
            MessageBoxImage icon, MessageBoxResult defaultResult, 
            MessageBoxOptions options) 
        {
            return ShowCore((new WindowInteropHelper(owner)).CriticalHandle, messageBoxText, caption, button, icon, defaultResult, options);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text, caption, and style.
        ///    </para>
        /// </devdoc>
        public static MessageBoxResult Show(
            Window owner, 
            string messageBoxText, 
            string caption, 
            MessageBoxButton button, 
            MessageBoxImage icon, 
            MessageBoxResult defaultResult) 
        {
            return ShowCore((new WindowInteropHelper (owner)).CriticalHandle, messageBoxText, caption, button, icon, defaultResult, 0);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text, caption, and style.
        ///    </para>
        /// </devdoc>
        public static MessageBoxResult Show(
            Window owner, 
            string messageBoxText, 
            string caption, 
            MessageBoxButton button, 
            MessageBoxImage icon) 
        {
            return ShowCore((new WindowInteropHelper (owner)).CriticalHandle, messageBoxText, caption, button, icon, 0, 0);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text, caption, and style.
        ///    </para>
        /// </devdoc>
        public static MessageBoxResult Show(
            Window owner, 
            string messageBoxText, 
            string caption, 
            MessageBoxButton button) 
        {
            return ShowCore((new WindowInteropHelper (owner)).CriticalHandle, messageBoxText, caption, button, MessageBoxImage.None, 0, 0);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text and caption.
        ///    </para>
        /// </devdoc>
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption) 
        {
            return ShowCore((new WindowInteropHelper (owner)).CriticalHandle, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, 0, 0);
        }

        /// <devdoc>
        ///    <para>
        ///       Displays a message box with specified text.
        ///    </para>
        /// </devdoc>
        public static MessageBoxResult Show(Window owner, string messageBoxText) 
        {
            return ShowCore((new WindowInteropHelper (owner)).CriticalHandle, messageBoxText, String.Empty, MessageBoxButton.OK, MessageBoxImage.None, 0, 0);
        }
        #endregion

        private static int DefaultResultToButtonNumber(MessageBoxResult result, MessageBoxButton button)
        {
            if (result == 0) return DEFAULT_BUTTON1;

            switch (button)
            {
                case MessageBoxButton.OK:
                    return DEFAULT_BUTTON1;
                case MessageBoxButton.OKCancel:
                    if (result == MessageBoxResult.Cancel) return DEFAULT_BUTTON2;
                    return DEFAULT_BUTTON1;
                case MessageBoxButton.YesNo:
                    if (result == MessageBoxResult.No) return DEFAULT_BUTTON2;
                    return DEFAULT_BUTTON1;
                case MessageBoxButton.YesNoCancel:
                    if (result == MessageBoxResult.No) return DEFAULT_BUTTON2;
                    if (result == MessageBoxResult.Cancel) return DEFAULT_BUTTON3;
                    return DEFAULT_BUTTON1;
                default:
                    return DEFAULT_BUTTON1;
            }
        }

        internal static MessageBoxResult ShowCore(
            IntPtr owner, 
            string messageBoxText, 
            string caption,
            MessageBoxButton button, 
            MessageBoxImage icon, 
            MessageBoxResult defaultResult,
            MessageBoxOptions options) 
        {
            if (!IsValidMessageBoxButton(button))
            {
                throw new InvalidEnumArgumentException ("button", (int)button, typeof(MessageBoxButton));
            }
            if (!IsValidMessageBoxImage(icon))
            {
                throw new InvalidEnumArgumentException ("icon", (int)icon, typeof(MessageBoxImage));
            }
            if (!IsValidMessageBoxResult(defaultResult))
            {
                throw new InvalidEnumArgumentException ("defaultResult", (int)defaultResult, typeof(MessageBoxResult));
            }
            if (!IsValidMessageBoxOptions(options))
            {
                throw new InvalidEnumArgumentException("options", (int)options, typeof(MessageBoxOptions));
            }            
            
            // UserInteractive??
            //
            /*if (!SystemInformation.UserInteractive && (options & (MessageBoxOptions.ServiceNotification | MessageBoxOptions.DefaultDesktopOnly)) == 0) {
                throw new InvalidOperationException("UNDONE: SR.GetString(SR.CantShowModalOnNonInteractive)");
            }*/

            if ( (options & (MessageBoxOptions.ServiceNotification | MessageBoxOptions.DefaultDesktopOnly)) != 0) 
            {
                if (owner != IntPtr.Zero)
                {
                    throw new ArgumentException(SR.Get(SRID.CantShowMBServiceWithOwner));
                }                
            }
            else
            {                                    
                if (owner == IntPtr.Zero)
                {
                    owner = UnsafeNativeMethods.GetActiveWindow();
                }                
            }
            
            int style = (int) button | (int) icon | (int) DefaultResultToButtonNumber(defaultResult, button) | (int) options;

            // modal dialog notification?
            //
            //Application.BeginModalMessageLoop();
            //MessageBoxResult result = Win32ToMessageBoxResult(SafeNativeMethods.MessageBox(new HandleRef(owner, handle), messageBoxText, caption, style));
            MessageBoxResult result = Win32ToMessageBoxResult (UnsafeNativeMethods.MessageBox (new HandleRef (null, owner), messageBoxText, caption, style));
            // modal dialog notification?
            //
            //Application.EndModalMessageLoop();

            return result;
        }

        private static bool IsValidMessageBoxButton(MessageBoxButton value)
        {
            return value == MessageBoxButton.OK
                || value == MessageBoxButton.OKCancel
                || value == MessageBoxButton.YesNo
                || value == MessageBoxButton.YesNoCancel;
        }

        private static bool IsValidMessageBoxImage(MessageBoxImage value)
        {
            return value == MessageBoxImage.Asterisk
                || value == MessageBoxImage.Error
                || value == MessageBoxImage.Exclamation
                || value == MessageBoxImage.Hand
                || value == MessageBoxImage.Information
                || value == MessageBoxImage.None
                || value == MessageBoxImage.Question
                || value == MessageBoxImage.Stop
                || value == MessageBoxImage.Warning;
        }

        private static bool IsValidMessageBoxResult(MessageBoxResult value)
        {
            return value == MessageBoxResult.Cancel
                || value == MessageBoxResult.No
                || value == MessageBoxResult.None
                || value == MessageBoxResult.OK
                || value == MessageBoxResult.Yes;
        }

        private static bool IsValidMessageBoxOptions(MessageBoxOptions value)
        {
            int  mask = ~((int)MessageBoxOptions.ServiceNotification |
                         (int)MessageBoxOptions.DefaultDesktopOnly |
                         (int)MessageBoxOptions.RightAlign |
                         (int)MessageBoxOptions.RtlReading);
            
            if (((int)value & mask) == 0)
                return true;
            return false;
        }


    }
    /// <devdoc>
    ///    <para>
    ///       Specifies identifiers to
    ///       indicate the return value of a dialog box.
    ///    </para>
    /// </devdoc>
    /// <ExternalAPI/> 
    public enum MessageBoxResult 
    {

        /// <devdoc>
        ///    <para>
        ///       
        ///       Nothing is returned from the dialog box. This
        ///       means that the modal dialog continues running.
        ///       
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        None = 0,

        /// <devdoc>
        ///    <para>
        ///       The
        ///       dialog box return value is
        ///       OK (usually sent from a button labeled OK).
        ///       
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        OK = 1,

        /// <devdoc>
        ///    <para>
        ///       The
        ///       dialog box return value is Cancel (usually sent
        ///       from a button labeled Cancel).
        ///       
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        Cancel = 2,

        /// <devdoc>
        ///    <para>
        ///       The dialog box return value is
        ///       Yes (usually sent from a button labeled Yes).
        ///       
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        Yes = 6,

        /// <devdoc>
        ///    <para>
        ///       The dialog box return value is
        ///       No (usually sent from a button labeled No).
        ///       
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        No = 7,

        // NOTE: if you add or remove any values in this enum, be sure to update MessageBox.IsValidMessageBoxResult()
    }
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [Flags]
    public enum MessageBoxOptions 
    {
        /// <devdoc>
        ///     <para>
        ///         Specifies that all default options should be used.
        ///     </para>
        /// </devdoc>
        /// <ExternalApi />
        None = 0x00000000,
        
        /// <devdoc>
        ///    <para>
        ///       Specifies that the message box is displayed on the active desktop. 
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        ServiceNotification = 0x00200000,

        /// <devdoc>
        ///    <para>
        ///       Specifies that the message box is displayed on the active desktop. 
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        DefaultDesktopOnly = 0x00020000,

        /// <devdoc>
        ///    <para>
        ///       Specifies that the message box text is right-aligned.
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        RightAlign         = 0x00080000,

        /// <devdoc>
        ///    <para>
        ///       Specifies that the message box text is displayed with Rtl reading order.
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        RtlReading         = 0x00100000,
    }
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public enum MessageBoxImage 
    {
        /// <devdoc>
        ///    <para>
        ///       Specifies that the
        ///       message box contain no symbols. 
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        None         = 0,

        /// <devdoc>
        ///    <para>
        ///       Specifies that the
        ///       message box contains a
        ///       hand symbol. 
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        Hand         = 0x00000010,

        /// <devdoc>
        ///    <para>
        ///       Specifies
        ///       that the message
        ///       box contains a question
        ///       mark symbol. 
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        Question     = 0x00000020,

        /// <devdoc>
        ///    <para>
        ///       Specifies that the
        ///       message box contains an
        ///       exclamation symbol. 
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        Exclamation  = 0x00000030,

        /// <devdoc>
        ///    <para>
        ///       Specifies that the
        ///       message box contains an
        ///       asterisk symbol. 
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        Asterisk     = 0x00000040,

        /// <devdoc>
        ///    <para>
        ///       Specifies that the message box contains a hand icon. This field is
        ///       constant.
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        Stop         = Hand,

        /// <devdoc>
        ///    <para>
        ///       Specifies that the
        ///       message box contains a
        ///       hand icon. 
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        Error        = Hand,

        /// <devdoc>
        ///    <para>
        ///       Specifies that the message box contains an exclamation icon. 
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        Warning      = Exclamation,

        /// <devdoc>
        ///    <para>
        ///       Specifies that the
        ///       message box contains an
        ///       asterisk icon. 
        ///    </para>
        /// </devdoc>
        /// <ExternalAPI/> 
        Information  = Asterisk,

        // NOTE: if you add or remove any values in this enum, be sure to update MessageBox.IsValidMessageBoxIcon()    
    }
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public enum MessageBoxButton 
    {
        /// <devdoc>
        ///    <para>
        ///       Specifies that the
        ///       message box contains an OK button. This field is
        ///       constant.
        ///    </para>
        /// </devdoc>
        OK               = 0x00000000,

        /// <devdoc>
        ///    <para>
        ///       Specifies that the
        ///       message box contains OK and Cancel button. This field
        ///       is
        ///       constant.
        ///    </para>
        /// </devdoc>
        OKCancel         = 0x00000001,

        /// <devdoc>
        ///    <para>
        ///       Specifies that the
        ///       message box contains Yes, No, and Cancel button. This
        ///       field is
        ///       constant.
        ///    </para>
        /// </devdoc>
        YesNoCancel      = 0x00000003,

        /// <devdoc>
        ///    <para>
        ///       Specifies that the
        ///       message box contains Yes and No button. This field is
        ///       constant.
        ///    </para>
        /// </devdoc>
        YesNo            = 0x00000004,

        // NOTE: if you add or remove any values in this enum, be sure to update MessageBox.IsValidMessageBoxButton()
    }
}

