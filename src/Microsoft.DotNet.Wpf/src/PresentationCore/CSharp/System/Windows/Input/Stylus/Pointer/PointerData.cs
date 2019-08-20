// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using System.Security;
using System.Windows.Input;
using System.Windows.Interop;
using static MS.Win32.Pointer.UnsafeNativeMethods;

namespace System.Windows.Input.StylusPointer
{
    /// <summary>
    /// Provides a wrapping class that aggregates Pointer data from a pointer event/message
    /// </summary>
    internal class PointerData
    {
        #region Member Variables

        /// <summary>
        /// Standard pointer information
        /// </summary>
        private POINTER_INFO _info;

        /// <summary>
        /// Pointer information specific to a touch device
        /// </summary>
        private POINTER_TOUCH_INFO _touchInfo;

        /// <summary>
        /// Pointer information specific to a pen device
        /// </summary>
        private POINTER_PEN_INFO _penInfo;

        /// <summary>
        /// The full history available for the current pointer (used for coalesced input)
        /// </summary>
        private POINTER_INFO[] _history;

        #endregion

        #region Properties

        /// <summary>
        /// If true, we have correctly queried pointer data, false otherwise.
        /// </summary>
        internal bool IsValid { get; private set; } = false;

        /// <summary>
        /// Standard pointer information
        /// </summary>
        internal POINTER_INFO Info
        {
            get
            {
                return _info;
            }
        }

        /// <summary>
        /// Pointer information specific to a touch device
        /// </summary>
        internal POINTER_TOUCH_INFO TouchInfo
        {
            get
            {
                return _touchInfo;
            }
        }

        /// <summary>
        /// Pointer information specific to a pen device
        /// </summary>
        internal POINTER_PEN_INFO PenInfo
        {
            get
            {
                return _penInfo;
            }
        }

        /// <summary>
        /// The full history available for the current pointer (used for coalesced input)
        /// </summary>
        internal POINTER_INFO[] History
        {
            get
            {
                return _history;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Queries all needed data from a particular pointer message and stores
        /// it locally.
        /// </summary>
        /// <param name="pointerId">The id of the pointer message</param>
        internal PointerData(uint pointerId)
        {
            if (IsValid = GetPointerInfo(pointerId, ref _info))
            {
                _history = new POINTER_INFO[_info.historyCount];

                // Fill the pointer history
                // If we fail just return a blank history
                if (!GetPointerInfoHistory(pointerId, ref _info.historyCount, _history))
                {
                    _history = new POINTER_INFO[0];
                }

                switch (_info.pointerType)
                {
                    case POINTER_INPUT_TYPE.PT_TOUCH:
                        {
                            // If we have a touch device, pull the touch specific information down
                            IsValid &= GetPointerTouchInfo(pointerId, ref _touchInfo);
                        }
                        break;
                    case POINTER_INPUT_TYPE.PT_PEN:
                        {
                            // Otherwise we have a pen device, so pull down pen specific information
                            IsValid &= GetPointerPenInfo(pointerId, ref _penInfo);
                        }
                        break;
                    default:
                        {
                            // Only process touch or pen messages, do not process mouse or touchpad
                            IsValid = false;
                        }
                        break;
                }
            }
        }

        #endregion
    }
}
