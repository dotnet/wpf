// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Windows.Input;

namespace System.Windows.Interop
{
    /// <summary>
    ///     This interface is implementated by any chunk of UI (hereafter
    ///     "component") that wishes to participate in tabbing/accelerators/
    ///     mnemonics.  All methods must be implemented, there are no
    ///     optional methods.  For components that contain other components,
    ///     see also IKeyboardInputSite.  A component must track which of its
    ///     immediate children has focus.
    /// </summary>
    public interface IKeyboardInputSink
    {
        /// <summary>
        ///     Registers a child KeyboardInputSink with this sink.  A site
        ///     is returned.
        /// </summary>
        /// <remarks>
        ///     This API requires unrestricted UI Window permission.  There is a link demand here
        ///     so that we are protected via direct calls to this interface.  It can't be a full 
        ///     demand since those don't work declaratively on interface methods.  The implementors
        ///     of this interface method all do a full demand since that's really the protection we want.
        /// </remarks>
        IKeyboardInputSite RegisterKeyboardInputSink(IKeyboardInputSink sink);

        /// <summary>
        ///     Gives the component a chance to process keyboard input.
        ///     Return value is true if handled, false if not.  Components
        ///     will generally call a child component's TranslateAccelerator
        ///     if they can't handle the input themselves.  The message must
        ///     either be WM_KEYDOWN or WM_SYSKEYDOWN.  It is illegal to
        ///     modify the MSG structure, it's passed by reference only
        ///     as a performance optimization.
        /// </summary>
        /// <remarks>
        ///     This API requires unrestricted UI Window permission.
        /// </remarks>
        bool TranslateAccelerator(ref MSG msg, ModifierKeys modifiers);

        /// <summary>
        ///     Set focus to the first or last tab stop (according to the
        ///     TraversalRequest).  If it can't, because it has no tab stops,
        ///     the return value is false.
        /// </summary>
        bool TabInto(TraversalRequest request);

        /// <summary>
        ///     The property should start with a null value.  The component's
        ///     container will set this property to a non-null value before
        ///     any other methods are called.  It may be set multiple times,
        ///     and should be set to null before disposal.
        /// </summary>
        /// <remarks>
        ///     The setter for this property requires unrestricted UI Window permission.
        /// </remarks>
        IKeyboardInputSite KeyboardInputSite 
        {
            get;
            
            set;
        }

        /// <summary>
        ///     Gives the component a chance to process Mnemonics
        ///     The message must be WM_CHAR, WM_SYSCHAR, WM_DEADCHAR or WM_SYSDEADCHAR.
        ///     It is illegal to modify the MSG structure, it's passed by reference
        ///     only as a performance optimization.
        ///     If this component contains child components, the container must call
        ///     OnMnemonic on each of it's children.
        /// </summary>
        /// <remarks>
        ///     This API requires unrestricted UI Window permission.
        /// </remarks>
        bool OnMnemonic(ref MSG msg, ModifierKeys modifiers);

        /// <summary>
        ///     Gives the component a chance to process keyboard input messages
        ///     WM_CHAR, WM_SYSCHAR, WM_DEADCHAR or WM_SYSDEADCHAR before calling OnMnemonic.
        ///     Will return true if "handled" meaning don't pass it to OnMnemonic.
        ///     The message must be WM_CHAR, WM_SYSCHAR, WM_DEADCHAR or WM_SYSDEADCHAR.
        ///     It is illegal to modify the MSG structure, it's passed by reference
        ///     only as a performance optimization.
        /// </summary>
        /// <remarks>
        ///     This API requires unrestricted UI Window permission.
        /// </remarks>
        bool TranslateChar(ref MSG msg, ModifierKeys modifiers);

        /// <summary>
        ///     This returns true if the sink, or a child of it, has focus. And false otherwise.
        /// </summary>
        bool HasFocusWithin();
}
}

