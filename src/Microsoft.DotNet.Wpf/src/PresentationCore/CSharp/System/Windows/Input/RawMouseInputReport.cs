// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Media; 
using System.Security;
using MS.Internal;
using MS.Internal.PresentationCore;
using MS.Win32;
using System;

namespace System.Windows.Input 
{
    /// <summary>
    ///     The RawMouseInputReport class encapsulates the raw input provided
    ///     from a mouse.
    /// </summary>
    /// <remarks>
    ///     It is important to note that the InputReport class only contains
    ///     blittable types.  This is required so that the report can be
    ///     marshalled across application domains.
    /// </remarks>
    [FriendAccessAllowed]
    internal class RawMouseInputReport : InputReport
    {
        /// <summary>
        ///     Constructs ad instance of the RawMouseInputReport class.
        /// </summary>
        /// <param name="mode">
        ///     The mode in which the input is being provided.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="inputSource">
        ///     The PresentationSource over which the mouse is moved.
        /// </param>
        /// <param name="actions">
        ///     The set of actions being reported.
        /// </param>
        /// <param name="x">
        ///     If horizontal position being reported.
        /// </param>
        /// <param name="y">
        ///     If vertical position being reported.
        /// </param>
        /// <param name="wheel">
        ///     If wheel delta being reported.
        /// </param>
        /// <param name="extraInformation">
        ///     Any extra information being provided along with the input.
        /// </param>
        public RawMouseInputReport(
            InputMode mode,
            int timestamp, 
            PresentationSource inputSource,
            RawMouseActions actions, 
            int x, 
            int y, 
            int wheel, 
            IntPtr extraInformation) : base(inputSource, InputType.Mouse, mode, timestamp)
        {
            if (!IsValidRawMouseActions(actions))
                throw new System.ComponentModel.InvalidEnumArgumentException("actions", (int)actions, typeof(RawMouseActions));

            /* we pass a null state from MouseDevice.PreProcessorInput, so null is valid value for state */
            _actions = actions;
            _x = x;
            _y = y;
            _wheel = wheel;
            _extraInformation = new SecurityCriticalData<IntPtr>(extraInformation);
        }

        /// <summary>
        ///     Read-only access to the set of actions that were reported.
        /// </summary>
        public RawMouseActions Actions {get {return _actions;}}

        /// <summary>
        ///     Read-only access to the horizontal position that was reported.
        /// </summary>
        public int X {get {return _x;}}

        /// <summary>
        ///     Read-only access to the vertical position that was reported.
        /// </summary>
        public int Y {get {return _y;}}

        /// <summary>
        ///     Read-only access to the wheel delta that was reported.
        /// </summary>
        public int Wheel {get {return _wheel;}}

        /// <summary>
        ///     Read-only access to the extra information was provided along
        ///     with the input.
        /// </summary>
        public IntPtr ExtraInformation 
        {
            get 
            {
                return _extraInformation.Value;
            }
        }

        // IsValid Method for RawMouseActions. Relies on the enum being flags.
        internal static bool IsValidRawMouseActions(RawMouseActions actions)
        {
            if (actions == RawMouseActions.None)
                return true;

            if ((( RawMouseActions.AttributesChanged | RawMouseActions.Activate | RawMouseActions.Deactivate |
                  RawMouseActions.RelativeMove | RawMouseActions.AbsoluteMove | RawMouseActions.VirtualDesktopMove |
                  RawMouseActions.Button1Press | RawMouseActions.Button1Release |
                  RawMouseActions.Button2Press | RawMouseActions.Button2Release |
                  RawMouseActions.Button3Press | RawMouseActions.Button3Release |
                  RawMouseActions.Button4Press | RawMouseActions.Button4Release |
                  RawMouseActions.Button5Press | RawMouseActions.Button5Release |
                  RawMouseActions.VerticalWheelRotate | RawMouseActions.HorizontalWheelRotate |
                  RawMouseActions.CancelCapture |
                  RawMouseActions.QueryCursor) & actions) == actions)
            {
                if (!(((RawMouseActions.Deactivate & actions) == actions && RawMouseActions.Deactivate != actions ) ||
                      (((RawMouseActions.Button1Press | RawMouseActions.Button1Release) & actions) == (RawMouseActions.Button1Press | RawMouseActions.Button1Release)) ||
                      (((RawMouseActions.Button2Press | RawMouseActions.Button2Release) & actions) == (RawMouseActions.Button2Press | RawMouseActions.Button2Release)) ||
                      (((RawMouseActions.Button3Press | RawMouseActions.Button3Release) & actions) == (RawMouseActions.Button3Press | RawMouseActions.Button3Release)) ||
                      (((RawMouseActions.Button4Press | RawMouseActions.Button4Release) & actions) == (RawMouseActions.Button4Press | RawMouseActions.Button4Release)) ||
                      (((RawMouseActions.Button5Press | RawMouseActions.Button5Release) & actions) == (RawMouseActions.Button5Press | RawMouseActions.Button5Release))))
                {
                    return true;
                }
            }
            return false;
        }

        private RawMouseActions _actions;
        private int _x;
        private int _y;
        private int _wheel;
        
        internal bool _isSynchronize; // Set from MouseDevice.Synchronize.
        
        private SecurityCriticalData<IntPtr> _extraInformation;
    }    
}
