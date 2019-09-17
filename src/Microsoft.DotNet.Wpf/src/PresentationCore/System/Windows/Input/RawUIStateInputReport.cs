// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using MS.Internal;
using MS.Win32;
using System.Windows;

namespace System.Windows.Input
{
    /// <summary>
    ///     The RawUIStateInputReport class encapsulates the raw input
    ///     provided from WM_*UISTATE* messages.
    /// </summary>
    internal class RawUIStateInputReport : InputReport
    {
         /// <summary>
        ///     Constructs an instance of the RawUIStateInputReport class.
        /// </summary>
        /// <param name="inputSource">
        ///     The input source that provided this input.
        /// </param>
        /// <param name="mode">
        ///     The mode in which the input is being provided.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="action">
        ///     The action being reported.
        /// </param>
        /// <param name="targets">
        ///     The targets being reported.
        /// </param>
        public RawUIStateInputReport(
            PresentationSource inputSource,
            InputMode mode,
            int timestamp,
            RawUIStateActions action,
            RawUIStateTargets targets) : base(inputSource, InputType.Keyboard, mode, timestamp)
        {
            if (!IsValidRawUIStateAction(action))
                throw new System.ComponentModel.InvalidEnumArgumentException("action", (int)action, typeof(RawUIStateActions));
            if (!IsValidRawUIStateTargets(targets))
                throw new System.ComponentModel.InvalidEnumArgumentException("targets", (int)targets, typeof(RawUIStateTargets));

            _action = action;
            _targets = targets;
        }

        /// <summary>
        ///     Read-only access to the action that was reported.
        /// </summary>
        public RawUIStateActions Action {get {return _action;}}

        /// <summary>
        ///     Read-only access to the targets that were reported.
        /// </summary>
        public RawUIStateTargets Targets {get {return _targets;}}

        // IsValid Method for RawUIStateActions.
        internal static bool IsValidRawUIStateAction(RawUIStateActions action)
        {
            return (action == RawUIStateActions.Set ||
                    action == RawUIStateActions.Clear ||
                    action == RawUIStateActions.Initialize);
        }

        // IsValid Method for RawUIStateTargets. Relies on the enum being [Flags].
        internal static bool IsValidRawUIStateTargets(RawUIStateTargets targets)
        {
            return ((targets & (RawUIStateTargets.HideFocus |
                                RawUIStateTargets.HideAccelerators |
                                RawUIStateTargets.Active))
                    == targets);
        }

        private RawUIStateActions _action;
        private RawUIStateTargets _targets;
    }
}
