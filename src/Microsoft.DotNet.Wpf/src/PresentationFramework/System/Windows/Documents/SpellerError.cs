// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A misspelled word in a TextBox or RichTextBox.
//

namespace System.Windows.Controls
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows.Documents;
    using MS.Internal;

    /// <summary>
    /// A misspelled word in a TextBox or RichTextBox.
    /// </summary>
    public class SpellingError
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new instance.
        internal SpellingError(Speller speller, ITextPointer start, ITextPointer end)
        {
            Invariant.Assert(start.CompareTo(end) < 0);

            _speller = speller;
            _start = start.GetFrozenPointer(LogicalDirection.Forward);
            _end = end.GetFrozenPointer(LogicalDirection.Backward);
        }
 
        #endregion Constructors
 
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Replaces the spelling error text with a specificed correction.
        /// </summary>
        /// <param name="correctedText">
        /// Text to replace the error.
        /// </param>
        /// <remarks>
        /// This method repositions the caret to the position immediately
        /// following the corrected text.
        /// </remarks>
        public void Correct(string correctedText)
        {
            if (correctedText == null)
            {
                correctedText = String.Empty; // Parity with TextBox.Text.
            }

            ITextRange range = new TextRange(_start, _end);
            range.Text = correctedText;
        }

        /// <summary>
        /// Instructs the control to ignore this error and any duplicates for
        /// the remainder of its lifetime.
        /// </summary>
        public void IgnoreAll()
        {
            _speller.IgnoreAll(TextRangeBase.GetTextInternal(_start, _end));
        }

        #endregion Public methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// A list of suggested replaced for the misspelled text.
        /// </summary>
        /// <remarks>
        /// May be empty, meaning no suggestions are available.
        /// </remarks>
        public IEnumerable<string> Suggestions
        {
            get
            {
                IList suggestions = _speller.GetSuggestionsForError(this);

                for (int i=0; i<suggestions.Count; i++)
                {
                    yield return (string)suggestions[i];
                }
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Start position of the misspelled text.
        /// </summary>
        internal ITextPointer Start
        {
            get
            {
                return _start;
            }
        }

        /// <summary>
        /// End position of the misspelled text.
        /// </summary>
        internal ITextPointer End
        {
            get
            {
                return _end;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Speller associated with this error.
        private readonly Speller _speller;

        // Start position of the error text.
        private readonly ITextPointer _start;

        // End position of the error text.
        private readonly ITextPointer _end;

        #endregion Private Fields
    }
}

