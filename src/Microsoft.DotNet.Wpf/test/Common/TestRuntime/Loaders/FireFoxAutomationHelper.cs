// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Input;
using MTI = Microsoft.Test.Input;
using Accessibility;
using System.Runtime.InteropServices;

namespace Microsoft.Test.Loaders
{
    /// <summary>
    /// Contains helper APIs for manipulating an instance of the FireFox browser, given the main hWnd of the window.
    /// </summary>
    public static class FireFoxAutomationHelper
    {
        #region Public Members

        /// <summary>
        /// Navigates instance of firefox to specified URL
        /// </summary>
        /// <param name="mainWindowHwnd">hWnd of FireFox Window</param>
        /// <param name="urlToNavigate">Destination URL</param>
        public static void NavigateFireFox(IntPtr mainWindowHwnd, string urlToNavigate)
        {
        }

        /// <summary>
        /// Clicks FireFox's "Back" button
        /// </summary>
        /// <param name="mainWindowHwnd">hWnd of FireFox instance</param>
        public static void ClickFireFoxBackButton(IntPtr mainWindowHwnd)
        {
            DoDefaultActionByNameAndHwnd(mainWindowHwnd, "Back", 0);
        }
        /// <summary>
        /// Clicks FireFox's "Forward" button
        /// </summary>
        /// <param name="mainWindowHwnd">hWnd of FireFox instance</param>
        public static void ClickFireFoxForwardButton(IntPtr mainWindowHwnd)
        {
            DoDefaultActionByNameAndHwnd(mainWindowHwnd, "Forward", 0);
        }
        /// <summary>
        /// Clicks FireFox's "Reload" button
        /// </summary>
        /// <param name="mainWindowHwnd">hWnd of FireFox instance</param>
        public static void ClickFireFoxRefreshButton(IntPtr mainWindowHwnd)
        {
            DoDefaultActionByNameAndHwnd(mainWindowHwnd, "Reload", 0);
        }
        /// <summary>
        /// Clicks FireFox's "Stop" button
        /// </summary>
        /// <param name="mainWindowHwnd">hWnd of FireFox instance</param>
        public static void ClickFireFoxStopButton(IntPtr mainWindowHwnd)
        {
            DoDefaultActionByNameAndHwnd(mainWindowHwnd, "Stop", 0);
        }
        /// <summary>
        /// Clicks FireFox's "Go" button
        /// </summary>
        /// <param name="mainWindowHwnd">hWnd of FireFox instance</param>
        public static void ClickFireFoxGoButton(IntPtr mainWindowHwnd)
        {
            DoDefaultActionByNameAndHwnd(mainWindowHwnd, "Go", 0);
        }

        #endregion

        #region Private Members

        // Import method to figure out current keyboard layout... need to hit enter twice for IME languages.
        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(int idThread);

        private static void DoDefaultActionByNameAndHwnd(IntPtr mainWindowHwnd, string name, int childIndex)
        {
            IAccessible ffIAcc = MsaaUIHelper.IAccessibleFromHWnd(mainWindowHwnd);
            MsaaUIHelper.AccessibleChild ffWindowAccessibleChild = new MsaaUIHelper.AccessibleChild(ffIAcc, MsaaUIHelper.CHILDID_SELF);

            (FindAccessibleChildrenByName(ffWindowAccessibleChild, name))[childIndex].IAccessible.accDoDefaultAction(((object)MsaaUIHelper.CHILDID_SELF));
        }

        private static MsaaUIHelper.AccessibleChild[] FindAccessibleChildrenByName(MsaaUIHelper.AccessibleChild node, string name)
        {
            List<MsaaUIHelper.AccessibleChild> foundStuff = new List<MsaaUIHelper.AccessibleChild>();

            Queue<MsaaUIHelper.AccessibleChild> searchQueue = new Queue<MsaaUIHelper.AccessibleChild>();

            foreach (MsaaUIHelper.AccessibleChild child in MsaaUIHelper.GetAccessibleChildren(node))
            {
                searchQueue.Enqueue(child);
            }

            while (!(searchQueue.Count == 0))
            {
                string currentNodeAccName = "";
                MsaaUIHelper.AccessibleChild currentNode = searchQueue.Dequeue();
                try
                {
                    currentNodeAccName = currentNode.IAccessible.get_accName(((object)MsaaUIHelper.CHILDID_SELF));
                    // Can return null OR "" for nameless nodes.  Set null ones to "" for uniformity.
                    if (currentNodeAccName == null)
                    {
                        currentNodeAccName = "";
                    }
                }
                catch
                {
                    // Do nothing. MSAA can throw several weird exceptions here 
                }

                if (currentNodeAccName.ToLowerInvariant() == name.ToLowerInvariant())
                {
                    foundStuff.Add(currentNode);
                }
                else
                {
                    foreach (MsaaUIHelper.AccessibleChild child in MsaaUIHelper.GetAccessibleChildren(currentNode))
                    {
                        searchQueue.Enqueue(child);
                    }
                }
            }
            return foundStuff.ToArray();
        }

        #endregion

        #region Internal Classes

        internal sealed class MsaaUIHelper
        {
            /// <summary>
            /// GUID used internally that must be passed by reference.  We must use this
            /// instance of the GUID since the IID.* GUID (defined further below) is
            /// readonly.  The purpose of storing this as a static variable is as an
            /// optimization, so we don't need to copy the IID.* GUID everytime a 
            /// reference is needed.  Used in the IAccessibleFromHWnd wrapper method
            /// </summary>
            private static Guid IID_IAccessible_Ref = new Guid("{618736E0-3C3D-11CF-810C-00AA00389B71}");

            [StructLayout(LayoutKind.Sequential)]
            internal struct POINT
            {
                public Int32 x;
                public Int32 y;
            } 

            internal sealed class IID
            {
                public static readonly Guid IID_IUnknown = new Guid("{00000000-0000-0000-0000-000000000046}");
                public static readonly Guid IID_IAccessible = new Guid("{618736E0-3C3D-11CF-810C-00AA00389B71}");
            } 

            public const int CHILDID_SELF = 0;

            [Flags]
            internal enum STATE : int
            {
                STATE_SYSTEM_NORMAL = 0x00000000,
                STATE_SYSTEM_UNAVAILABLE = 0x00000001,  // Disabled
                STATE_SYSTEM_SELECTED = 0x00000002,
                STATE_SYSTEM_FOCUSED = 0x00000004,
                STATE_SYSTEM_PRESSED = 0x00000008,
                STATE_SYSTEM_CHECKED = 0x00000010,
                STATE_SYSTEM_MIXED = 0x00000020,  // 3-state checkbox or toolbar button
                STATE_SYSTEM_READONLY = 0x00000040,
                STATE_SYSTEM_HOTTRACKED = 0x00000080,
                STATE_SYSTEM_DEFAULT = 0x00000100,
                STATE_SYSTEM_EXPANDED = 0x00000200,
                STATE_SYSTEM_COLLAPSED = 0x00000400,
                STATE_SYSTEM_BUSY = 0x00000800,
                STATE_SYSTEM_FLOATING = 0x00001000,  // Children "owned" not "contained" by parent
                STATE_SYSTEM_MARQUEED = 0x00002000,
                STATE_SYSTEM_ANIMATED = 0x00004000,
                STATE_SYSTEM_INVISIBLE = 0x00008000,
                STATE_SYSTEM_OFFSCREEN = 0x00010000,
                STATE_SYSTEM_SIZEABLE = 0x00020000,
                STATE_SYSTEM_MOVEABLE = 0x00040000,
                STATE_SYSTEM_SELFVOICING = 0x00080000,
                STATE_SYSTEM_FOCUSABLE = 0x00100000,
                STATE_SYSTEM_SELECTABLE = 0x00200000,
                STATE_SYSTEM_LINKED = 0x00400000,
                STATE_SYSTEM_TRAVERSED = 0x00800000,
                STATE_SYSTEM_MULTISELECTABLE = 0x01000000,  // Supports multiple selection
                STATE_SYSTEM_EXTSELECTABLE = 0x02000000,  // Supports extended selection
                STATE_SYSTEM_ALERT_LOW = 0x04000000,  // This information is of low priority
                STATE_SYSTEM_ALERT_MEDIUM = 0x08000000,  // This information is of medium priority
                STATE_SYSTEM_ALERT_HIGH = 0x10000000,  // This information is of high priority
                STATE_SYSTEM_VALID = 0x1FFFFFFF,
                STATE_SYSTEM_PROTECTED = 0x20000000,
                STATE_SYSTEM_HASPOPUP = 0x40000000
            } 

            [Flags]
            internal enum ROLE
            {
                ROLE_SYSTEM_TITLEBAR = 0x00000001,
                ROLE_SYSTEM_MENUBAR = 0x00000002,
                ROLE_SYSTEM_SCROLLBAR = 0x00000003,
                ROLE_SYSTEM_GRIP = 0x00000004,
                ROLE_SYSTEM_SOUND = 0x00000005,
                ROLE_SYSTEM_CURSOR = 0x00000006,
                ROLE_SYSTEM_CARET = 0x00000007,
                ROLE_SYSTEM_ALERT = 0x00000008,
                ROLE_SYSTEM_WINDOW = 0x00000009,
                ROLE_SYSTEM_CLIENT = 0x0000000A,
                ROLE_SYSTEM_MENUPOPUP = 0x0000000B,
                ROLE_SYSTEM_MENUITEM = 0x0000000C,
                ROLE_SYSTEM_TOOLTIP = 0x0000000D,
                ROLE_SYSTEM_APPLICATION = 0x0000000E,
                ROLE_SYSTEM_DOCUMENT = 0x0000000F,
                ROLE_SYSTEM_PANE = 0x00000010,
                ROLE_SYSTEM_CHART = 0x00000011,
                ROLE_SYSTEM_DIALOG = 0x00000012,
                ROLE_SYSTEM_BORDER = 0x00000013,
                ROLE_SYSTEM_GROUPING = 0x00000014,
                ROLE_SYSTEM_SEPARATOR = 0x00000015,
                ROLE_SYSTEM_TOOLBAR = 0x00000016,
                ROLE_SYSTEM_STATUSBAR = 0x00000017,
                ROLE_SYSTEM_TABLE = 0x00000018,
                ROLE_SYSTEM_COLUMNHEADER = 0x00000019,
                ROLE_SYSTEM_ROWHEADER = 0x0000001A,
                ROLE_SYSTEM_COLUMN = 0x0000001B,
                ROLE_SYSTEM_ROW = 0x0000001C,
                ROLE_SYSTEM_CELL = 0x0000001D,
                ROLE_SYSTEM_LINK = 0x0000001E,
                ROLE_SYSTEM_HELPBALLOON = 0x0000001F,
                ROLE_SYSTEM_CHARACTER = 0x00000020,
                ROLE_SYSTEM_LIST = 0x00000021,
                ROLE_SYSTEM_LISTITEM = 0x00000022,
                ROLE_SYSTEM_OUTLINE = 0x00000023,
                ROLE_SYSTEM_OUTLINEITEM = 0x00000024,
                ROLE_SYSTEM_PAGETAB = 0x00000025,
                ROLE_SYSTEM_PROPERTYPAGE = 0x00000026,
                ROLE_SYSTEM_INDICATOR = 0x00000027,
                ROLE_SYSTEM_GRAPHIC = 0x00000028,
                ROLE_SYSTEM_STATICTEXT = 0x00000029,
                ROLE_SYSTEM_TEXT = 0x0000002A,  // Editable, selectable, etc.
                ROLE_SYSTEM_PUSHBUTTON = 0x0000002B,
                ROLE_SYSTEM_CHECKBUTTON = 0x0000002C,
                ROLE_SYSTEM_RADIOBUTTON = 0x0000002D,
                ROLE_SYSTEM_COMBOBOX = 0x0000002E,
                ROLE_SYSTEM_DROPLIST = 0x0000002F,
                ROLE_SYSTEM_PROGRESSBAR = 0x00000030,
                ROLE_SYSTEM_DIAL = 0x00000031,
                ROLE_SYSTEM_HOTKEYFIELD = 0x00000032,
                ROLE_SYSTEM_SLIDER = 0x00000033,
                ROLE_SYSTEM_SPINBUTTON = 0x00000034,
                ROLE_SYSTEM_DIAGRAM = 0x00000035,
                ROLE_SYSTEM_ANIMATION = 0x00000036,
                ROLE_SYSTEM_EQUATION = 0x00000037,
                ROLE_SYSTEM_BUTTONDROPDOWN = 0x00000038,
                ROLE_SYSTEM_BUTTONMENU = 0x00000039,
                ROLE_SYSTEM_BUTTONDROPDOWNGRID = 0x0000003A,
                ROLE_SYSTEM_WHITESPACE = 0x0000003B,
                ROLE_SYSTEM_PAGETABLIST = 0x0000003C,
                ROLE_SYSTEM_CLOCK = 0x0000003D,
                ROLE_SYSTEM_SPLITBUTTON = 0x0000003E,
                ROLE_SYSTEM_IPADDRESS = 0x0000003F,
                ROLE_SYSTEM_OUTLINEBUTTON = 0x00000040
            } 

            [Flags]
            internal enum SELFLAG : uint
            {
                SELFLAG_NONE = 0x00000000,
                SELFLAG_TAKEFOCUS = 0x00000001,
                SELFLAG_TAKESELECTION = 0x00000002,
                SELFLAG_EXTENDSELECTION = 0x00000004,
                SELFLAG_ADDSELECTION = 0x00000008,
                SELFLAG_REMOVESELECTION = 0x00000010,
                SELFLAG_VALID = 0x0000001F
            } 

            internal enum OBJID : uint
            {
                OBJID_WINDOW = 0x00000000,
                OBJID_SYSMENU = 0xFFFFFFFF,
                OBJID_TITLEBAR = 0xFFFFFFFE,
                OBJID_MENU = 0xFFFFFFFD,
                OBJID_CLIENT = 0xFFFFFFFC,
                OBJID_VSCROLL = 0xFFFFFFFB,
                OBJID_HSCROLL = 0xFFFFFFFA,
                OBJID_SIZEGRIP = 0xFFFFFFF9,
                OBJID_CARET = 0xFFFFFFF8,
                OBJID_CURSOR = 0xFFFFFFF7,
                OBJID_ALERT = 0xFFFFFFF6,
                OBJID_SOUND = 0xFFFFFFF5
            } 

            internal enum NAVDIR
            {
                NAVDIR_MIN = 0x00000000,
                NAVDIR_UP = 0x00000001,
                NAVDIR_DOWN = 0x00000002,
                NAVDIR_LEFT = 0x00000003,
                NAVDIR_RIGHT = 0x00000004,
                NAVDIR_NEXT = 0x00000005,
                NAVDIR_PREVIOUS = 0x00000006,
                NAVDIR_FIRSTCHILD = 0x00000007,
                NAVDIR_LASTCHILD = 0x00000008,
                NAVDIR_MAX = 0x00000009
            } 

            internal enum HRESULT
            {
                S_OK = 0,
                S_FALSE = 1
            } 

            /// <summary>
            /// Record that contains both an IAccessible reference and a child ID
            /// </summary>
            internal struct AccessibleChild
            {
                public AccessibleChild(IAccessible iacc, int childId)
                {
                    this.IAccessible = iacc;
                    ChildId = childId;
                }

                public IAccessible IAccessible;

                public int ChildId;
            }

            /// <summary>
            /// Helper method to convert a childObject returned from an IAccessible
            /// method into an IAccessible object and a child ID
            /// </summary>
            /// <param name="parentAcc">IAccessible interface for the parent of the 
            /// child specified by childObject</param>
            /// <param name="childObject">object that was returned by a call to an
            /// IAccessible method such as accNavigate</param>
            /// <param name="childAcc">IAccessible interface for the child's associated
            /// IAccessible interface</param>
            /// <param name="childID">ID of the child relative to the IAccessible interface</param>
            /// <remarks>20030718 [dbecht] not sure I like this function, maybe it should 
            /// simply be a wrapper for accNavigate</remarks>
            private static void GetAccessibleChild(IAccessible parentAcc, object childObject, out IAccessible childAcc, out int childID)
            {
                if (childObject is int)
                {
                    // this is a child that shares the parent accessibility interface
                    childAcc = parentAcc;
                    childID = (int)childObject;
                }
                else
                {
                    // this should be an IAccessible interface, if it is not, we let an exception
                    // get thrown
                    childAcc = (IAccessible)childObject;
                    childID = CHILDID_SELF;
                }
            }

            /// <summary>
            /// Wrapper method that gets an IAccessible interface for a given
            /// window with Role=ROLE_SYSTEM_WINDOW
            /// </summary>
            /// <param name="hWnd"></param>
            /// <returns></returns>
            public static IAccessible IAccessibleFromHWnd(IntPtr hWnd)
            {
                IAccessible hWndIAcc = null;

                if (!hWnd.Equals(IntPtr.Zero))
                {
                    NativeMethods.AccessibleObjectFromWindow(hWnd, OBJID.OBJID_CLIENT, ref IID_IAccessible_Ref, ref hWndIAcc);
                }

                return hWndIAcc;
            }

            /// <summary>
            /// Get an array of accessible child records, one for each child
            /// of the given parent IAccessible interface.
            /// </summary>
            /// <param name="parent"></param>
            /// <returns></returns>
            public static AccessibleChild[] GetAccessibleChildren(AccessibleChild parent)
            {
                int childCount;
                AccessibleChild[] childArray;
                object[] childObjectArray;
                int i;
                IAccessible iaccChild;
                int childId;

                if (parent.ChildId != CHILDID_SELF)
                {
                    // children with valid IDs cannot have their own children
                    childCount = 0;
                }
                else if (parent.IAccessible != null)
                {
                    childCount = parent.IAccessible.accChildCount;
                }
                else
                {
                    childCount = 0;
                }
                if (childCount > 0)
                {
                    childArray = new AccessibleChild[childCount];
                    childObjectArray = new object[childCount];
                    NativeMethods.AccessibleChildren(parent.IAccessible,
                                                      0,
                                                      childCount,
                                                      childObjectArray,
                                                      ref childCount);
                    for (i = 0; i < childCount; i++)
                    {
                        childId = CHILDID_SELF;
                        iaccChild = childObjectArray[i] as IAccessible;
                        if (iaccChild == null)
                        {
                            // this child is identified by an ID and it shares its
                            // IAccessible interface with its parent
                            try
                            {
                                childId = (int)childObjectArray[i];
                                iaccChild = parent.IAccessible;
                            }
                            catch (InvalidCastException)
                            {
                                childId = CHILDID_SELF;
                                // we can't tell what the object is
                            }
                        }
                        childArray[i] = new AccessibleChild(iaccChild, childId);
                    }
                }
                else
                {
                    childArray = new AccessibleChild[0];
                }
                return childArray;
            }

            internal sealed class NativeMethods
            {
                // non-creatable
                private NativeMethods() { }

                [DllImport("oleacc.dll")]
                public static extern
                int AccessibleChildren(IAccessible paccContainer,
                                       int iChildStart,
                                       int cChildren,
                                       [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), In, Out] object[] rgvarChildren,
                                       ref int pcObtained);

                [DllImport("oleacc.dll")]
                public static extern
                int AccessibleObjectFromPoint(POINT ptScreen,
                                              ref IAccessible ppoleAcc,
                                              ref object pvarChild);

                [DllImport("oleacc.dll")]
                public static extern
                int AccessibleObjectFromWindow(IntPtr hWnd, OBJID dwObjectID, 
                                               [In] ref Guid riid,
                                               ref IAccessible ppvObject);

                [DllImport("oleacc.dll")]
                public static extern
                int GetRoleText(int dwRole, StringBuilder lpszRole, int cchRoleMax);

                [DllImport("oleacc.dll")]
                public static extern
                int GetStateText(int dwStateBit, StringBuilder lpszStateBit, int cchStateBitMax);

                [DllImport("oleacc.dll")]
                public static extern
                int WindowFromAccessibleObject(IAccessible pacc, ref IntPtr phWnd);
            }
        }

        #endregion
    }
}
