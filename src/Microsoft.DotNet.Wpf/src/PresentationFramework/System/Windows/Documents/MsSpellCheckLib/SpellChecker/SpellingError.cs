// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A class encapsulating the functionality of ISpellingError
//              exposed by MsSpellCheckLib.RCW. 
//              
//              In addition to ISpellingError fields, this class also automatically
//              generates a list of suggestions when the corrective action suggested 
//              is CorrectiveAction.GetSuggestions. 
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Windows.Documents.MsSpellCheckLib
{
    using ISpellingError = RCW.ISpellingError;

    internal partial class SpellChecker
    {
        /// <summary>
        /// This Enum corresponds to <see cref="RCW.CORRECTIVE_ACTION"/>, and 
        /// supports the SpellingError type that follows.
        /// </summary>
        internal enum CorrectiveAction
        {
            None = 0,
            GetSuggestions = 1,
            Replace = 2,
            Delete = 3
        }

        /// <summary>
        /// This type encapsulates information from an instnace 
        /// of <see cref="RCW.ISpellingError"/>
        /// </summary>
        internal class SpellingError
        {
            #region Fields 

            #region ISpellingError Fields

            internal uint StartIndex { get; }
            internal uint Length { get; }
            internal CorrectiveAction CorrectiveAction { get; }
            internal string Replacement { get; }

            #endregion // ISpellingError Fields

            private List<string> _suggestions;

            /// <summary>
            /// When <see cref="CorrectiveAction"/> is <see cref="CorrectiveAction.GetSuggestions"/>, 
            /// this field will be populated with those suggestions. Otherwise, this will return an 
            /// empty list.
            /// </summary>
            internal IReadOnlyCollection<string> Suggestions
            {
                get
                {
                    return _suggestions.AsReadOnly();
                }
            }

            #endregion // Fields

            /// <summary>
            /// Copies simple fields from ISpellingError, and populates suggestions
            /// when CorrectiveAction == GetSuggestions.
            /// </summary>
            internal SpellingError(ISpellingError error, SpellChecker spellChecker, string text, bool shouldSuppressCOMExceptions = true, bool shouldReleaseCOMObject = true)
            {
                if (error == null)
                {
                    throw new ArgumentNullException(nameof(error));
                }

                StartIndex = error.StartIndex;
                Length = error.Length;
                CorrectiveAction = (CorrectiveAction)error.CorrectiveAction;
                Replacement = error.Replacement;

                PopulateSuggestions(error, spellChecker, text, shouldSuppressCOMExceptions, shouldReleaseCOMObject);
            }

            /// <summary>
            /// Populates suggestions when CorrectiveAction == GetSuggestions
            /// </summary>
            private void PopulateSuggestions(ISpellingError error, SpellChecker spellChecker, string text, bool shouldSuppressCOMExceptions, bool shouldReleaseCOMObject)
            {
                try
                {
                    _suggestions = new List<string>();
                    if (CorrectiveAction == CorrectiveAction.GetSuggestions)
                    {
                        var suggestions = spellChecker.Suggest(text, shouldSuppressCOMExceptions);
                        _suggestions.AddRange(suggestions);
                    }
                    else if (CorrectiveAction == CorrectiveAction.Replace)
                    {
                        _suggestions.Add(Replacement);
                    }
                }
                finally
                {
                    if (shouldReleaseCOMObject)
                    {
                        Marshal.ReleaseComObject(error);
                    }
                }
            }           
        }
    }
}
