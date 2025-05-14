// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// 
// Description: the TextComposition class
//

using System.Windows.Input;

namespace System.Windows.Documents
{
    /// <summary>
    ///     the Composition class provides input-text/composition event promotion
    /// </summary>
    public sealed class FrameworkRichTextComposition : FrameworkTextComposition
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal FrameworkRichTextComposition(InputManager inputManager, IInputElement source, object owner)  : base(inputManager, source, owner)
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        ///     The strat position of the result text of the text input.
        /// </summary>
        public TextPointer ResultStart
        {
            get
            {
                return _ResultStart == null ? null : (TextPointer)_ResultStart.GetFrozenPointer(LogicalDirection.Backward);
            }
        }

        /// <summary>
        ///     The end position of the result text of the text input.
        /// </summary>
        public TextPointer ResultEnd
        {
            get
            {
                return _ResultEnd == null ? null : (TextPointer)_ResultEnd.GetFrozenPointer(LogicalDirection.Forward);
            }
        }

        /// <summary>
        ///     The start position of the current composition text.
        /// </summary>
        public TextPointer CompositionStart
        {
            get
            {
                return _CompositionStart == null ? null : (TextPointer)_CompositionStart.GetFrozenPointer(LogicalDirection.Backward);
            }
        }

        /// <summary>
        ///     The start position of the current composition text.
        /// </summary>
        public TextPointer CompositionEnd
        {
            get
            {
                return _CompositionEnd == null ? null : (TextPointer)_CompositionEnd.GetFrozenPointer(LogicalDirection.Forward);
            }
        }
    }
}
