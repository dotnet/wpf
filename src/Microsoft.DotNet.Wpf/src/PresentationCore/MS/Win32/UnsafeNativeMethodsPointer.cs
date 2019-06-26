// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal.PresentationCore;
using MS.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace MS.Win32.Pointer
{
    /// <summary>
    /// Contains data structures and functions related to the WM_POINTER stack native interface.
    /// 
    /// This includes WM_POINTER specific functions and InteractionContext functions.
    /// 
    /// For WM_POINTER <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/hh454916(v=vs.85).aspx"/>
    /// For InteractionContext <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/hh448840(v=vs.85).aspx"/>
    /// </summary>
    internal class UnsafeNativeMethods
    {
        #region Enumerations

        #region WM_POINTER

        /// <summary>
        /// Not used right now in the WM_POINTER API, here for completeness.
        /// </summary>
        [Flags]
        internal enum TOUCH_FLAGS : UInt32
        {
            TOUCH_FLAG_NONE = 0x00000000,
        }

        /// <summary>
        /// Determines if the various fields of the touch info are valid
        /// </summary>
        [Flags]
        internal enum TOUCH_MASK : UInt32
        {
            TOUCH_MASK_NONE = 0x00000000,
            TOUCH_MASK_CONTACTAREA = 0x00000001,
            TOUCH_MASK_ORIENTATION = 0x00000002,
            TOUCH_MASK_PRESSURE = 0x00000004,
        }

        /// <summary>
        /// Determines if the various fields of the pen info are valid
        /// </summary>
        [Flags]
        internal enum PEN_MASK : UInt32
        {
            PEN_MASK_NONE = 0x00000000,
            PEN_MASK_PRESSURE = 0x00000001,
            PEN_MASK_ROTATION = 0x00000002,
            PEN_MASK_TILT_X = 0x00000004,
            PEN_MASK_TILT_Y = 0x00000008,
        }

        /// <summary>
        /// Describes various button states and pen properties
        /// </summary>
        [Flags]
        internal enum PEN_FLAGS : UInt32
        {
            PEN_FLAG_NONE = 0x00000000,
            PEN_FLAG_BARREL = 0x00000001,
            PEN_FLAG_INVERTED = 0x00000002,
            PEN_FLAG_ERASER = 0x00000004,
        }

        /// <summary>
        /// Dictates the type of cursor being used
        /// </summary>
        internal enum POINTER_DEVICE_CURSOR_TYPE : UInt32
        {
            POINTER_DEVICE_CURSOR_TYPE_UNKNOWN = 0x00000000,
            POINTER_DEVICE_CURSOR_TYPE_TIP = 0x00000001,
            POINTER_DEVICE_CURSOR_TYPE_ERASER = 0x00000002,
            POINTER_DEVICE_CURSOR_TYPE_MAX = 0xFFFFFFFF
        }

        /// <summary>
        /// The type of the touch device
        /// </summary>
        internal enum POINTER_DEVICE_TYPE : UInt32
        {
            POINTER_DEVICE_TYPE_INTEGRATED_PEN = 0x00000001,
            POINTER_DEVICE_TYPE_EXTERNAL_PEN = 0x00000002,
            POINTER_DEVICE_TYPE_TOUCH = 0x00000003,
            POINTER_DEVICE_TYPE_TOUCH_PAD = 0x00000004,
            POINTER_DEVICE_TYPE_MAX = 0xFFFFFFFF
        }

        /// <summary>
        /// The type of input device used (WPF only supports touch and pen)
        /// </summary>
        internal enum POINTER_INPUT_TYPE : UInt32
        {
            PT_POINTER = 0x00000001,
            PT_TOUCH = 0x00000002,
            PT_PEN = 0x00000003,
            PT_MOUSE = 0x00000004,
            PT_TOUCHPAD = 0x00000005
        }

        /// <summary>
        /// Flag field for conveying various pointer info
        /// </summary>
        [Flags]
        internal enum POINTER_FLAGS : UInt32
        {
            POINTER_FLAG_NONE = 0x00000000,
            POINTER_FLAG_NEW = 0x00000001,
            POINTER_FLAG_INRANGE = 0x00000002,
            POINTER_FLAG_INCONTACT = 0x00000004,
            POINTER_FLAG_FIRSTBUTTON = 0x00000010,
            POINTER_FLAG_SECONDBUTTON = 0x00000020,
            POINTER_FLAG_THIRDBUTTON = 0x00000040,
            POINTER_FLAG_FOURTHBUTTON = 0x00000080,
            POINTER_FLAG_FIFTHBUTTON = 0x00000100,
            POINTER_FLAG_PRIMARY = 0x00002000,
            POINTER_FLAG_CONFIDENCE = 0x000004000,
            POINTER_FLAG_CANCELED = 0x000008000,
            POINTER_FLAG_DOWN = 0x00010000,
            POINTER_FLAG_UPDATE = 0x00020000,
            POINTER_FLAG_UP = 0x00040000,
            POINTER_FLAG_WHEEL = 0x00080000,
            POINTER_FLAG_HWHEEL = 0x00100000,
            POINTER_FLAG_CAPTURECHANGED = 0x00200000,
            POINTER_FLAG_HASTRANSFORM = 0x00400000,
        }

        /// <summary>
        /// State of stylus buttons
        /// </summary>
        internal enum POINTER_BUTTON_CHANGE_TYPE : UInt32
        {
            POINTER_CHANGE_NONE,
            POINTER_CHANGE_FIRSTBUTTON_DOWN,
            POINTER_CHANGE_FIRSTBUTTON_UP,
            POINTER_CHANGE_SECONDBUTTON_DOWN,
            POINTER_CHANGE_SECONDBUTTON_UP,
            POINTER_CHANGE_THIRDBUTTON_DOWN,
            POINTER_CHANGE_THIRDBUTTON_UP,
            POINTER_CHANGE_FOURTHBUTTON_DOWN,
            POINTER_CHANGE_FOURTHBUTTON_UP,
            POINTER_CHANGE_FIFTHBUTTON_DOWN,
            POINTER_CHANGE_FIFTHBUTTON_UP
        }

        #endregion

        #region Interaction

        /// <summary>
        /// Used to get/set the units that the interaction engine outputs
        /// </summary>
        internal enum InteractionMeasurementUnits : UInt32
        {
            HiMetric = 0,
            Screen = 1
        }

        /// <summary>
        /// Used to select a particular property of the interaction engine to get/set
        /// </summary>
        internal enum INTERACTION_CONTEXT_PROPERTY : UInt32
        {
            INTERACTION_CONTEXT_PROPERTY_MEASUREMENT_UNITS = 0x00000001,
            INTERACTION_CONTEXT_PROPERTY_INTERACTION_UI_FEEDBACK = 0x00000002,
            INTERACTION_CONTEXT_PROPERTY_FILTER_POINTERS = 0x00000003,
            INTERACTION_CONTEXT_PROPERTY_MAX = 0xffffffff
        }

        /// <summary>
        /// Flags used in the cross slide interaction
        /// </summary>
        [Flags]
        internal enum CROSS_SLIDE_FLAGS : UInt32
        {
            CROSS_SLIDE_FLAGS_NONE = 0x00000000,
            CROSS_SLIDE_FLAGS_SELECT = 0x00000001,
            CROSS_SLIDE_FLAGS_SPEED_BUMP = 0x00000002,
            CROSS_SLIDE_FLAGS_REARRANGE = 0x00000004,
            CROSS_SLIDE_FLAGS_MAX = 0xffffffff
        }

        /// <summary>
        /// Used in the rail manipulation interaction
        /// </summary>
        internal enum MANIPULATION_RAILS_STATE : UInt32
        {
            MANIPULATION_RAILS_STATE_UNDECIDED = 0x00000000,
            MANIPULATION_RAILS_STATE_FREE = 0x00000001,
            MANIPULATION_RAILS_STATE_RAILED = 0x00000002,
            MANIPULATION_RAILS_STATE_MAX = 0xffffffff
        }

        /// <summary>
        /// Used to show what an interaction output signifies
        /// </summary>
        [Flags]
        internal enum INTERACTION_FLAGS : UInt32
        {
            INTERACTION_FLAG_NONE = 0x00000000,
            INTERACTION_FLAG_BEGIN = 0x00000001,
            INTERACTION_FLAG_END = 0x00000002,
            INTERACTION_FLAG_CANCEL = 0x00000004,
            INTERACTION_FLAG_INERTIA = 0x00000008,
            INTERACTION_FLAG_MAX = 0xffffffff
        }

        /// <summary>
        /// Determines the general interaction being described
        /// </summary>
        internal enum INTERACTION_ID : UInt32
        {
            INTERACTION_ID_NONE = 0x00000000,
            INTERACTION_ID_MANIPULATION = 0x00000001,
            INTERACTION_ID_TAP = 0x00000002,
            INTERACTION_ID_SECONDARY_TAP = 0x00000003,
            INTERACTION_ID_HOLD = 0x00000004,
            INTERACTION_ID_DRAG = 0x00000005,
            INTERACTION_ID_CROSS_SLIDE = 0x00000006,
            INTERACTION_ID_MAX = 0xffffffff
        }

        /// <summary>
        /// Flags to specify details about a given interaction.  Used to configure
        /// what interactions are to be supported.
        /// </summary>
        [Flags]
        internal enum INTERACTION_CONFIGURATION_FLAGS : UInt32
        {
            INTERACTION_CONFIGURATION_FLAG_NONE = 0x00000000,
            INTERACTION_CONFIGURATION_FLAG_MANIPULATION = 0x00000001,
            INTERACTION_CONFIGURATION_FLAG_MANIPULATION_TRANSLATION_X = 0x00000002,
            INTERACTION_CONFIGURATION_FLAG_MANIPULATION_TRANSLATION_Y = 0x00000004,
            INTERACTION_CONFIGURATION_FLAG_MANIPULATION_ROTATION = 0x00000008,
            INTERACTION_CONFIGURATION_FLAG_MANIPULATION_SCALING = 0x00000010,
            INTERACTION_CONFIGURATION_FLAG_MANIPULATION_TRANSLATION_INERTIA = 0x00000020,
            INTERACTION_CONFIGURATION_FLAG_MANIPULATION_ROTATION_INERTIA = 0x00000040,
            INTERACTION_CONFIGURATION_FLAG_MANIPULATION_SCALING_INERTIA = 0x00000080,
            INTERACTION_CONFIGURATION_FLAG_MANIPULATION_RAILS_X = 0x00000100,
            INTERACTION_CONFIGURATION_FLAG_MANIPULATION_RAILS_Y = 0x00000200,
            INTERACTION_CONFIGURATION_FLAG_MANIPULATION_EXACT = 0x00000400,
            INTERACTION_CONFIGURATION_FLAG_CROSS_SLIDE = 0x00000001,
            INTERACTION_CONFIGURATION_FLAG_CROSS_SLIDE_HORIZONTAL = 0x00000002,
            INTERACTION_CONFIGURATION_FLAG_CROSS_SLIDE_SELECT = 0x00000004,
            INTERACTION_CONFIGURATION_FLAG_CROSS_SLIDE_SPEED_BUMP = 0x00000008,
            INTERACTION_CONFIGURATION_FLAG_CROSS_SLIDE_REARRANGE = 0x00000010,
            INTERACTION_CONFIGURATION_FLAG_CROSS_SLIDE_EXACT = 0x00000020,
            INTERACTION_CONFIGURATION_FLAG_TAP = 0x00000001,
            INTERACTION_CONFIGURATION_FLAG_TAP_DOUBLE = 0x00000002,
            INTERACTION_CONFIGURATION_FLAG_SECONDARY_TAP = 0x00000001,
            INTERACTION_CONFIGURATION_FLAG_HOLD = 0x00000001,
            INTERACTION_CONFIGURATION_FLAG_HOLD_MOUSE = 0x00000002,
            INTERACTION_CONFIGURATION_FLAG_DRAG = 0x00000001,
            INTERACTION_CONFIGURATION_FLAG_MAX = 0xffffffff
        }

        #endregion

        #endregion

        #region Constants

        #region WM_POINTER

        /// <summary>
        /// The maximum length of the product string for a touch device
        /// </summary>
        internal const int POINTER_DEVICE_PRODUCT_STRING_MAX = 520;

        #endregion

        #endregion

        #region Structures

        #region WM_POINTER

        /// <summary>
        /// A struct representing the information for a pointer touch event.
        /// This corresponds to a TouchDevice in WPF.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct POINTER_TOUCH_INFO
        {
            internal POINTER_INFO pointerInfo;
            internal TOUCH_FLAGS touchFlags;
            internal TOUCH_MASK touchMask;
            internal RECT rcContact;
            internal RECT rcContactRaw;
            internal UInt32 orientation;
            internal UInt32 pressure;
        }

        /// <summary>
        /// A struct representing the information for a particular pointer property.
        /// These correspond to the raw data from WM_POINTER.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct POINTER_DEVICE_PROPERTY
        {
            internal Int32 logicalMin;
            internal Int32 logicalMax;
            internal Int32 physicalMin;
            internal Int32 physicalMax;
            internal UInt32 unit;
            internal UInt32 unitExponent;
            internal UInt16 usagePageId;
            internal UInt16 usageId;
        }

        /// <summary>
        /// A struct representing the information for a pointer cursor.
        /// This corresponds to a StylusDevice in WPF.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct POINTER_DEVICE_CURSOR_INFO
        {
            internal UInt32 cursorId;
            internal POINTER_DEVICE_CURSOR_TYPE cursor;
        }

        /// <summary>
        /// A structure for holding information related to a pointer device.
        /// This corresponds to a TabletDevice in WPF.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct POINTER_DEVICE_INFO
        {
            internal UInt32 displayOrientation;
            internal IntPtr device;
            internal POINTER_DEVICE_TYPE pointerDeviceType;
            internal IntPtr monitor;
            internal UInt32 startingCursorId;
            internal UInt16 maxActiveContacts;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = POINTER_DEVICE_PRODUCT_STRING_MAX)]
            internal string productString;
        }

        /// <summary>
        /// Point structure for use with WM_POINTER calls/structs
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct POINT
        {
            internal Int32 X;
            internal Int32 Y;

            public override string ToString()
            {
                return $"X: {X}, Y: {Y}";
            }
        }

        /// <summary>
        /// A structure for holding information related to a pointer pen.
        /// This corresponds to a pure Stylus device in WPF.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct POINTER_PEN_INFO
        {
            internal POINTER_INFO pointerInfo;
            internal PEN_FLAGS penFlags;
            internal PEN_MASK penMask;
            internal UInt32 pressure;
            internal UInt32 rotation;
            internal Int32 tiltX;
            internal Int32 tiltY;
        }

        /// <summary>
        /// A structure for holding information related to a pointer.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct POINTER_INFO
        {
            internal POINTER_INPUT_TYPE pointerType;
            internal UInt32 pointerId;
            internal UInt32 frameId;
            internal POINTER_FLAGS pointerFlags;
            internal IntPtr sourceDevice;
            internal IntPtr hwndTarget;
            internal POINT ptPixelLocation;
            internal POINT ptHimetricLocation;
            internal POINT ptPixelLocationRaw;
            internal POINT ptHimetricLocationRaw;
            internal UInt32 dwTime;
            internal UInt32 historyCount;
            internal Int32 inputData;
            internal UInt32 dwKeyStates;
            internal UInt64 PerformanceCount;
            internal POINTER_BUTTON_CHANGE_TYPE ButtonChangeType;
        }

        /// <summary>
        /// Rectangle structure for use with WM_POINTER calls
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct RECT
        {
            internal int left;
            internal int top;
            internal int right;
            internal int bottom;
        }

        #endregion

        #region Interaction

        /// <summary>
        /// Data about the velocity of a manipulation result
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct MANIPULATION_VELOCITY
        {
            internal float velocityX;
            internal float velocityY;
            internal float velocityExpansion;
            internal float velocityAngular;
        }

        /// <summary>
        /// Data about the manipulation that has occured.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct MANIPULATION_TRANSFORM
        {
            internal float translationX;
            internal float translationY;
            internal float scale;
            internal float expansion;
            internal float rotation;
        }

        /// <summary>
        /// Provides all state and data about a manipulation interaction
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct INTERACTION_ARGUMENTS_MANIPULATION
        {
            internal MANIPULATION_TRANSFORM delta;
            internal MANIPULATION_TRANSFORM cumulative;
            internal MANIPULATION_VELOCITY velocity;
            internal MANIPULATION_RAILS_STATE railsState;
        }

        /// <summary>
        /// The configuration for the interaction context.  This selects
        /// what gestures and interactions are processed.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct INTERACTION_CONTEXT_CONFIGURATION
        {
            internal INTERACTION_ID interactionId;
            internal INTERACTION_CONFIGURATION_FLAGS enable;
        }

        /// <summary>
        /// Determines tap vs double tap.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct INTERACTION_ARGUMENTS_TAP
        {
            internal UInt32 count;
        }

        /// <summary>
        /// Information about the cross slide interaction
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct INTERACTION_ARGUMENTS_CROSS_SLIDE
        {
            internal CROSS_SLIDE_FLAGS flags;
        }

        /// <summary>
        /// A structure representing a unioned output from the interaction engine.
        /// Needed to satisfy marshalling.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        internal struct INTERACTION_CONTEXT_OUTPUT_UNION
        {
            [FieldOffset(0)]
            internal INTERACTION_ARGUMENTS_MANIPULATION manipulation;

            [FieldOffset(0)]
            internal INTERACTION_ARGUMENTS_TAP tap;

            [FieldOffset(0)]
            internal INTERACTION_ARGUMENTS_CROSS_SLIDE crossSlide;
        }

        /// <summary>
        /// The overall output of the interaction engine in response to a specific WM_POINTER input.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct INTERACTION_CONTEXT_OUTPUT
        {
            internal INTERACTION_ID interactionId;
            internal INTERACTION_FLAGS interactionFlags;
            internal POINTER_INPUT_TYPE inputType;
            internal float x;
            internal float y;
            internal INTERACTION_CONTEXT_OUTPUT_UNION arguments;
        }

        #endregion

        #endregion

        #region Imports

        #region WM_POINTER

        /// <summary>
        /// Gets the list of pointer devices currently installed on the system.  Analagous to TabletDevice for WPF.
        /// </summary>
        [DllImport(DllImport.User32, EntryPoint = "GetPointerDevices", SetLastError = true)]
        internal static extern bool GetPointerDevices([In, Out] ref UInt32 deviceCount, [In, Out] POINTER_DEVICE_INFO[] devices);

        /// <summary>
        /// Gets the set of cursors (analagous to a StylusDevice for WPF) for a pointer device (TabletDevice).
        /// </summary>
        [DllImport(DllImport.User32, EntryPoint = "GetPointerDeviceCursors", SetLastError = true)]
        internal static extern bool GetPointerDeviceCursors([In] IntPtr device, [In, Out] ref UInt32 cursorCount, [In, Out] POINTER_DEVICE_CURSOR_INFO[] cursors);

        /// <summary>
        /// Gets the data for the current pointer event for the passed pointer id
        /// </summary>
        [DllImport(DllImport.User32, EntryPoint = "GetPointerInfo", SetLastError = true)]
        internal static extern bool GetPointerInfo([In] UInt32 pointerId, [In, Out] ref POINTER_INFO pointerInfo);

        /// <summary>
        /// Gets the history data for the current pointer event for the passed pointer id
        /// </summary>
        [DllImport(DllImport.User32, EntryPoint = "GetPointerInfoHistory", SetLastError = true)]
        internal static extern bool GetPointerInfoHistory([In] UInt32 pointerId, [In, Out] ref UInt32 entriesCount, [In, Out] POINTER_INFO[] pointerInfo);

        /// <summary>
        /// Gets the pointer device properties for the passed device
        /// </summary>
        [DllImport(DllImport.User32, EntryPoint = "GetPointerDeviceProperties", SetLastError = true)]
        internal static extern bool GetPointerDeviceProperties([In] IntPtr device, [In, Out] ref UInt32 propertyCount, [In, Out] POINTER_DEVICE_PROPERTY[] pointerProperties);

        /// <summary>
        /// Gets the pointer device rectangle and associated display rectangle for the particular pointer device
        /// </summary>
        [DllImport(DllImport.User32, EntryPoint = "GetPointerDeviceRects", SetLastError = true)]
        internal static extern bool GetPointerDeviceRects([In] IntPtr device, [In, Out] ref RECT pointerDeviceRect, [In, Out] ref RECT displayRect);

        /// <summary>
        /// Gets the cursor id for the particular pointer input (by pointer id)
        /// </summary>
        [DllImport(DllImport.User32, EntryPoint = "GetPointerCursorId", SetLastError = true)]
        internal static extern bool GetPointerCursorId([In] UInt32 pointerId, [In, Out] ref UInt32 cursorId);

        /// <summary>
        /// Gets the pen information for the given pen pointer
        /// </summary>
        [DllImport(DllImport.User32, EntryPoint = "GetPointerPenInfo", SetLastError = true)]
        internal static extern bool GetPointerPenInfo([In] UInt32 pointerId, [In, Out] ref POINTER_PEN_INFO penInfo);

        /// <summary>
        /// Gets the touch information for the given touch pointer
        /// </summary>
        [DllImport(DllImport.User32, EntryPoint = "GetPointerTouchInfo", SetLastError = true)]
        internal static extern bool GetPointerTouchInfo([In] UInt32 pointerId, [In, Out] ref POINTER_TOUCH_INFO touchInfo);

        /// <summary>
        /// Gets the raw data corresponding to the various properties.
        /// </summary>
        [DllImport(DllImport.User32, EntryPoint = "GetRawPointerDeviceData", SetLastError = true)]
        internal static extern bool GetRawPointerDeviceData([In] UInt32 pointerId, [In] UInt32 historyCount, [In] UInt32 propertiesCount, [In] POINTER_DEVICE_PROPERTY[] pProperties, [In, Out] int[] pValues);

        #endregion

        #region Interaction

        /// <summary>
        /// A delegate used as a callback parameter for the interaction engine
        /// </summary>
        /// <param name="clientData"></param>
        /// <param name="output"></param>
        internal delegate void INTERACTION_CONTEXT_OUTPUT_CALLBACK(IntPtr clientData, ref INTERACTION_CONTEXT_OUTPUT output);

        /// <summary>
        /// Creates the interaction context
        /// </summary>
        /// <param name="interactionContext"></param>
        [DllImport(DllImport.NInput, EntryPoint = "CreateInteractionContext", SetLastError = true)]
        internal static extern void CreateInteractionContext([Out] out IntPtr interactionContext);

        /// <summary>
        /// Destroys the interaction context
        /// </summary>
        /// <param name="interactionContext"></param>
        [DllImport(DllImport.NInput, EntryPoint = "DestroyInteractionContext", SetLastError = true)]
        internal static extern void DestroyInteractionContext([In] IntPtr interactionContext);

        /// <summary>
        /// Configures the interaction context to support a certain set of interactions
        /// </summary>
        /// <param name="interactionContext"></param>
        /// <param name="configurationCount"></param>
        /// <param name="configuration"></param>
        [DllImport(DllImport.NInput, EntryPoint = "SetInteractionConfigurationInteractionContext", SetLastError = true)]
        internal static extern void SetInteractionConfigurationInteractionContext([In] IntPtr interactionContext, [In] UInt32 configurationCount, [In] INTERACTION_CONTEXT_CONFIGURATION[] configuration);

        /// <summary>
        /// Registers a callback for interaction results
        /// </summary>
        /// <param name="interactionContext"></param>
        /// <param name="outputCallback"></param>
        /// <param name="clientData"></param>
        [DllImport(DllImport.NInput, EntryPoint = "RegisterOutputCallbackInteractionContext", SetLastError = true, PreserveSig = false)]
        internal static extern void RegisterOutputCallbackInteractionContext([In] IntPtr interactionContext, [In] INTERACTION_CONTEXT_OUTPUT_CALLBACK outputCallback, [In, Optional] IntPtr clientData);

        /// <summary>
        /// Sets a particular property of the interaction engine
        /// </summary>
        /// <param name="interactionContext"></param>
        /// <param name="contextProperty"></param>
        /// <param name="value"></param>
        [DllImport(DllImport.NInput, EntryPoint = "SetPropertyInteractionContext", SetLastError = true)]
        internal static extern void SetPropertyInteractionContext([In] IntPtr interactionContext, [In] INTERACTION_CONTEXT_PROPERTY contextProperty, [In] UInt32 value);

        /// <summary>
        /// Adds a WM_POINTER POINTER_INFO structure to the buffer of unprocessed WM_POINTER messages waiting
        /// </summary>
        /// <param name="interactionContext"></param>
        /// <param name="entriesCount"></param>
        /// <param name="pointerInfo"></param>
        [DllImport(DllImport.NInput, EntryPoint = "BufferPointerPacketsInteractionContext", SetLastError = true, PreserveSig = false)]
        internal static extern void BufferPointerPacketsInteractionContext([In] IntPtr interactionContext, [In] UInt32 entriesCount, [In] POINTER_INFO[] pointerInfo);

        /// <summary>
        /// Forces processing of the buffered WM_POINTER messages.
        /// </summary>
        /// <param name="interactionContext"></param>
        [DllImport(DllImport.NInput, EntryPoint = "ProcessBufferedPacketsInteractionContext", SetLastError = true, PreserveSig = false)]
        internal static extern void ProcessBufferedPacketsInteractionContext([In] IntPtr interactionContext);

        #endregion

        #endregion
    }
}
