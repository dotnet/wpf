// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Thread local state for the TextEditor.
//

namespace System.Windows.Documents
{
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics;

    // Thread local state for the TextEditor.
    internal class TextEditorThreadLocalStore
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal TextEditorThreadLocalStore()
        {
        }
 
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // Ref count for TextEditorTyping's InputLanguageChangeEventHandler.
        internal int InputLanguageChangeEventHandlerCount
        {
            get
            {
                return _inputLanguageChangeEventHandlerCount;
            }

            set
            {
                _inputLanguageChangeEventHandlerCount = value;
            }
        }

        // Queue of pending KeyDownEvent/TextInputEvent items.
        // We store events here, and handle them at Background priority.
        // This has the effect of batching multiple events when layout
        // cannot keep up with the input stream.
        // A non-null value means a background queue item is pending.
        internal ArrayList PendingInputItems
        {
            get
            {
                return _pendingInputItems;
            }

            set
            {
                _pendingInputItems = value;
            }
        }

        // Flag indicating that Shift key up happened immediately after Shift Down
        // without any intermediate key presses. This flag is used in
        // FlowDirection commands - Control+RightShift and Control+LeftShift (on KeyUp).
        internal bool PureControlShift
        {
            get
            {
                return _pureControlShift;
            }

            set
            {
                _pureControlShift = value;
            }
        }

        // Bidirectional input
        internal bool Bidi
        {
            get
            {
                return _bidi;
            }

            set
            {
                _bidi = value;
            }
        }


        // Currently active text selection - the one that owns a caret.
        internal TextSelection FocusedTextSelection
        {
            get
            {
                return _focusedTextSelection;
            }

            set
            {
                _focusedTextSelection = value;
            }
        }

        // Manages registration of all TextStores in a thread.
        internal TextServicesHost TextServicesHost
        {
            get
            {
                return _textServicesHost;
            }

            set
            {
                _textServicesHost = value;
            }
        }

        // Set true while hiding the mouse cursor after typing.
        internal bool HideCursor
        {
            get
            {
                return _hideCursor;
            }

            set
            {
                _hideCursor = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Ref count for TextEditorTyping's InputLanguageChangeEventHandler.
        private int _inputLanguageChangeEventHandlerCount;

        // Queue of pending KeyDownEvent/TextInputEvent items.
        // We store events here, and handle them at Background priority.
        // This has the effect of batching multiple events when layout
        // cannot keep up with the input stream.
        // A non-null value means a background queue item is pending.
        private ArrayList _pendingInputItems;

        // Flag indicating that Shift key up happened immediately after Shift Down
        // without any intermediate key presses. This flag is used in
        // FlowDirection commands - Control+RightShift and Control+LeftShift (on KeyUp).
        private bool _pureControlShift;

        // bidi caret for middle east(Hebrew, Arablic)
        private bool _bidi;

        // Currently active text selection - the one that owns a caret.
        private TextSelection _focusedTextSelection;

        // Manages registration of all TextStores in a thread.
        private TextServicesHost _textServicesHost;

        // Set true while hiding the mouse cursor after typing.
        private bool _hideCursor;

        #endregion Private Fields
    }
}

