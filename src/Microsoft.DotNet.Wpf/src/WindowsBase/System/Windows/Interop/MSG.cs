// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using MS.Internal.WindowsBase;
    
namespace System.Windows.Interop
{
    /// <summary>
    /// This class is the managed version of the Win32 MSG datatype.
    /// </summary>
    /// <remarks>
    /// For Avalon/WinForms interop to work, WinForms needs to be able to modify MSG structs as they are
    /// processed, so they are passed by ref (it's also a perf gain)
    ///
    /// - but in the Partial Trust scenario, this would be a security vulnerability; allowing partially trusted code
    ///    to intercept and arbitrarily change MSG contents could potentially create a spoofing opportunity.
    ///
    /// - so rather than try to secure all posible current and future extensibility points against untrusted code
    ///    getting write access to a MSG struct during message processing, we decided the simpler,  more performant, and 
    ///    more secure, both now and going forward, solution was to secure write access to the MSG struct directly
    ///    at the source.
    ///
    /// - get access is unrestricted and should in-line nicely for zero perf cost
    ///
    /// - set access is restricted via a call to SecurityHelper.DemandUnrestrictedUIPermission, which is optimized
    ///    to a no-op in the Full Trust scenario, and will throw a security exception in the Partial Trust scenario
    ///
    /// - NOTE: This breaks Avalon/WinForms interop in the Partial Trust scenario, but that's not a supported
    ///              scenario anyway.
    /// </remarks>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    [SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased")]
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct MSG
    {
        /// <SecurityNote>
        ///     Critical: Setting critical data
        /// </SecurityNote>
        [SecurityCritical]
        [FriendAccessAllowed] // Built into Base, used by Core or Framework.
        internal MSG(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, int time, int pt_x, int pt_y)
        {
            _hwnd = hwnd;
            _message = message;
            _wParam = wParam;
            _lParam = lParam;
            _time = time;
            _pt_x = pt_x;
            _pt_y = pt_y;
        }
        
        //
        // Public Properties:
        //
        
        /// <summary> 
        ///     The handle of the window to which the message was sent. 
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code as that may be exploited for spoofing purposes
        ///     PublicOK: This data is safe for Partial Trust code to read (getter), There is a demand on the setter to block Partial Trust code
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        [SuppressMessage("Microsoft.Security", "CA2111:PointersShouldNotBeVisible")]
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        public IntPtr hwnd
        {
            [SecurityCritical]
            get
            {
                return _hwnd;
            }
            [SecurityCritical]
            set
            {
                SecurityHelper.DemandUnrestrictedUIPermission();
                _hwnd = value;   
            }
}
        
         /// <summary> 
         ///    The Value of the window message. 
         /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code as that may be exploited for spoofing purposes
        ///     PublicOK: This data is safe for Partial Trust code to read (getter), There is a demand on the setter to block Partial Trust code
        /// </SecurityNote>
        public int message
        {
            [SecurityCritical]
            get
            {
                return _message;
            }
            [SecurityCritical]
            set
            {
                SecurityHelper.DemandUnrestrictedUIPermission();
                _message = value;   
            }
}

        /// <summary> 
        ///     The wParam of the window message. 
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code as that may be exploited for spoofing purposes
        ///     PublicOK: This data is safe for Partial Trust code to read (getter), There is a demand on the setter to block Partial Trust code
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [SuppressMessage("Microsoft.Security", "CA2111:PointersShouldNotBeVisible")]
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        public IntPtr wParam
        {
            [SecurityCritical]
            get
            {
                return _wParam;
            }
            [SecurityCritical]
            set
            {
                SecurityHelper.DemandUnrestrictedUIPermission();
                _wParam = value;   
            }
}

        /// <summary> 
        ///     The lParam of the window message. 
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code as that may be exploited for spoofing purposes
        ///     PublicOK: This data is safe for Partial Trust code to read (getter), There is a demand on the setter to block Partial Trust code
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [SuppressMessage("Microsoft.Security", "CA2111:PointersShouldNotBeVisible")]
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        public IntPtr lParam
        {
            [SecurityCritical]
            get
            {
                return _lParam;
            }
            [SecurityCritical]
            set
            {
                SecurityHelper.DemandUnrestrictedUIPermission();
                _lParam = value;   
            }
}

        /// <summary>
        ///     The time the window message was sent.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code as that may be exploited for spoofing purposes
        ///     PublicOK: This data is safe for Partial Trust code to read (getter), There is a demand on the setter to block Partial Trust code
        /// </SecurityNote>
        public int time
        {
            [SecurityCritical]
            get
            {
                return _time;
            }
            [SecurityCritical]
            set
            {
                SecurityHelper.DemandUnrestrictedUIPermission();
                _time = value;   
            }
}

        // In the original Win32, pt was a by-Value POINT structure
        /// <summary> 
        ///     The X coordinate of the message POINT struct. 
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code as that may be exploited for spoofing purposes
        ///     PublicOK: This data is safe for Partial Trust code to read (getter), There is a demand on the setter to block Partial Trust code
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUpperCased")]
        [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        public int pt_x
        {
            [SecurityCritical]
            get
            {
                return _pt_x;
            }
            [SecurityCritical]
            set
            {
                SecurityHelper.DemandUnrestrictedUIPermission();
                _pt_x = value;   
            }
}

        /// <summary> 
        ///     The Y coordinate of the message POINT struct. 
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code as that may be exploited for spoofing purposes
        ///     PublicOK: This data is safe for Partial Trust code to read (getter), There is a demand on the setter to block Partial Trust code
        /// </SecurityNote>
        [SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUpperCased")]
        [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        public int pt_y
        {
            [SecurityCritical]
            get
            {
                return _pt_y;
            }
            [SecurityCritical]
            set
            {
                SecurityHelper.DemandUnrestrictedUIPermission();
                _pt_y = value;   
            }
}

        //
        // Internal data:
        // - do not alter the number, order or size of ANY of this members!
        // - they must agree EXACTLY with the native Win32 MSG structure

        /// <summary>
        ///     The handle of the window to which the message was sent. 
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code for spoofing purposes
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _hwnd;

        /// <summary>
        ///     The Value of the window message. 
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code for spoofing purposes
        /// </SecurityNote>
        [SecurityCritical]
        private int _message;

        /// <summary>
        ///     The wParam of the window message. 
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code for spoofing purposes
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _wParam;

        /// <summary>
        ///     The lParam of the window message. 
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code for spoofing purposes
        /// </SecurityNote>
        [SecurityCritical]
        private IntPtr _lParam;

        /// <summary>
        ///     The time the window message was sent.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code for spoofing purposes
        /// </SecurityNote>
        [SecurityCritical]
        private int _time;

        /// <summary>
        ///     The X coordinate of the message POINT struct. 
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code for spoofing purposes
        /// </SecurityNote>
        [SecurityCritical]
        private int _pt_x;

        /// <summary>
        ///     The Y coordinate of the message POINT struct. 
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This data can not be modified by Partial Trust code for spoofing purposes
        /// </SecurityNote>
        [SecurityCritical]
        private int _pt_y;
}
}
