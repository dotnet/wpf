// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: the TextComposition class
//

using MS.Internal;
using MS.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Security;

namespace System.Windows.Documents
{
    /// <summary>
    ///     the internal Composition class provides input-text/composition event promotion
    ///     This class is used for simple TextBox control that does not expose TextRange.
    /// </summary>
    public class FrameworkTextComposition: TextComposition
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        //
        // Constructor for TextStore's TextComposition.
        //
        internal FrameworkTextComposition(InputManager inputManager, IInputElement source, object owner)  : base(inputManager, source, String.Empty, TextCompositionAutoComplete.Off)
        {
            _owner = owner;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods 
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Interface Methods 
        //
        //------------------------------------------------------

        /// <summary>
        ///     Finalize the composition.
        ///     This does not call base.Complete() because TextComposition.Complete()
        ///     will call TextServicesManager.CompleteComposition() directly to generate TextCompositionEvent.
        ///     We finalize Cicero's composition and TextStore will automatically
        ///     generate the proper TextComposition events.
        /// </summary>
        public override void Complete()
        {
            _pendingComplete = true;
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Offset of the finalized composition text.
        /// </summary>
        /// <remarks>
        /// This property is only meaningful during the handling of a
        /// TextInput event.  During a TextInputStart or TextInputUpdate event,
        /// this property has a value of -1.
        /// 
        /// When read in the context of TextBox, the offset refers to unicode
        /// code points.  When read in the context of a RichTextBox, the offset
        /// refers to FlowDocument symbols (unicode code points, TextElement edges,
        /// or embedded UIElements).
        /// </remarks>
        public int ResultOffset
        {
            get
            {
                return (_ResultStart == null) ? -1 : _offset;
            }
        }

        /// <summary>
        /// Length of the current composition in unicode code points.
        /// </summary>
        /// <remarks>
        /// This property is only meaningful during the handling of a
        /// TextInput event.  During a TextInputStart or TextInputUpdate event,
        /// this property has a value of -1.
        /// </remarks>
        public int ResultLength
        {
            get
            {
                return (_ResultStart == null) ? -1 : _length;
            }
        }

        /// <summary>
        /// Offset of the composition text.
        /// </summary>
        /// <remarks>
        /// This property is only meaningful during the handling of a
        /// TextInputStart or TextInputUpdate event.  During a TextInput event,
        /// this property has a value of -1.
        /// 
        /// When read in the context of TextBox, the offset refers to unicode
        /// code points.  When read in the context of a RichTextBox, the offset
        /// refers to FlowDocument symbols (unicode code points, TextElement edges,
        /// or embedded UIElements).
        /// </remarks>
        public int CompositionOffset
        {
            get
            {
                return (_CompositionStart == null) ? -1 : _offset;
            }
        }

        /// <summary>
        /// Length of the current composition in unicode code points.
        /// </summary>
        /// <remarks>
        /// This property is only meaningful during the handling of a
        /// TextInputStart or TextInputUpdate event.  During a TextInput event,
        /// this property has a value of -1.
        /// </remarks>
        public int CompositionLength
        {
            get
            {
                return (_CompositionStart == null) ? -1 : _length;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        internal static void CompleteCurrentComposition(UnsafeNativeMethods.ITfDocumentMgr documentMgr)
        {
            UnsafeNativeMethods.ITfContext context;

            documentMgr.GetBase(out context);

            UnsafeNativeMethods.ITfCompositionView composition = GetComposition(context);

            if (composition != null)
            {
                UnsafeNativeMethods.ITfContextOwnerCompositionServices compositionService = context as UnsafeNativeMethods.ITfContextOwnerCompositionServices;

                // Terminate composition if there is a composition view.
                compositionService.TerminateComposition(composition);
                Marshal.ReleaseComObject(composition);
            }

            Marshal.ReleaseComObject(context);
        }

        internal static UnsafeNativeMethods.ITfCompositionView GetCurrentCompositionView(UnsafeNativeMethods.ITfDocumentMgr documentMgr)
        {
            UnsafeNativeMethods.ITfContext context;

            documentMgr.GetBase(out context);

            UnsafeNativeMethods.ITfCompositionView view = GetComposition(context);

            Marshal.ReleaseComObject(context);

            return view;
        }

        // Set result string to TextComposition.
        internal void SetResultPositions(ITextPointer start, ITextPointer end, string text)
        {
            Invariant.Assert(start != null);
            Invariant.Assert(end != null);
            Invariant.Assert(text != null);

            _compositionStart = null;
            _compositionEnd = null;

            // We need to have another instances of TextPointer since we don't want to
            // freeze original TextPointers.
            _resultStart = start.GetFrozenPointer(LogicalDirection.Backward);
            _resultEnd = end.GetFrozenPointer(LogicalDirection.Forward);

            this.Text = text;
            this.CompositionText = String.Empty;

            // We must cache integer offsets -- public listeners won't expect
            // them to float like TextPointers if the document changes.
            _offset = (_resultStart == null) ? -1 : _resultStart.Offset;
            _length = (_resultStart == null) ? -1 : _resultStart.GetOffsetToPosition(_resultEnd);
        }

        // Set composition string to TextComposition.
        internal void SetCompositionPositions(ITextPointer start, ITextPointer end, string text)
        {
            Invariant.Assert(start != null);
            Invariant.Assert(end != null);
            Invariant.Assert(text != null);

            // We need to have another instances of TextPointer since we don't want to
            // freeze original TextPointers.
            _compositionStart = start.GetFrozenPointer(LogicalDirection.Backward);
            _compositionEnd = end.GetFrozenPointer(LogicalDirection.Forward);

            _resultStart = null;
            _resultEnd = null;

            this.Text = String.Empty;
            this.CompositionText = text;

            // We must cache integer offsets -- public listeners won't expect
            // them to float like TextPointers if the document changes.
            _offset = (_compositionStart == null) ? -1 : _compositionStart.Offset;
            _length = (_compositionStart == null) ? -1 : _compositionStart.GetOffsetToPosition(_compositionEnd);
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        /// <summary>
        ///     The result text of the text input.
        /// </summary>
        internal ITextPointer _ResultStart
        {
            get
            {
                return _resultStart;
            }
        }

        /// <summary>
        ///     The result text of the text input.
        /// </summary>
        internal ITextPointer _ResultEnd
        {
            get
            {
                return _resultEnd;
            }
        }

        /// <summary>
        ///     The current composition text.
        /// </summary>
        internal ITextPointer _CompositionStart
        {
            get
            {
                return _compositionStart;
            }
        }

        /// <summary>
        ///     The current composition text.
        /// </summary>
        internal ITextPointer _CompositionEnd
        {
            get
            {
                return _compositionEnd;
            }
        }

        // True if Complete has been called.
        internal bool PendingComplete
        {
            get
            {
                return _pendingComplete;
            }
        }

        // TextStore or ImmComposition that created this object.
        internal object Owner
        {
            get
            {
                return _owner;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        /// <summary>
        ///     Get ITfContextView of the context.
        /// </summary>
        private static UnsafeNativeMethods.ITfCompositionView GetComposition(UnsafeNativeMethods.ITfContext context)
        {
            UnsafeNativeMethods.ITfContextComposition contextComposition;
            UnsafeNativeMethods.IEnumITfCompositionView enumCompositionView;
            UnsafeNativeMethods.ITfCompositionView[] compositionViews = new UnsafeNativeMethods.ITfCompositionView[1];
            int fetched;

            contextComposition = (UnsafeNativeMethods.ITfContextComposition)context;
            contextComposition.EnumCompositions(out enumCompositionView);

            enumCompositionView.Next(1, compositionViews, out fetched);

            Marshal.ReleaseComObject(enumCompositionView);
            return compositionViews[0];
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        // the start poisiotn of the result text
        private ITextPointer _resultStart;

        // the end poisiotn of the result text
        private ITextPointer _resultEnd;

        // the start position of the composition text
        private ITextPointer _compositionStart;

        // the end position of the composition text
        private ITextPointer _compositionEnd;

        // Composition or result offset.
        private int _offset;

        // Composition or result length.
        private int _length;

        // TextStore or ImmComposition that created this object.
        private readonly object _owner;

        private bool _pendingComplete;
    }
}
