// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: EventArgs class that supports SpellCheckerChangedEventHandler.
//

namespace System.Windows.Documents
{
    namespace MsSpellCheckLib
    {
        internal partial class SpellChecker
        {
            /// <summary>
            /// This EventArgs type supports SpellCheckerChangedEventHandler. 
            /// </summary>
            internal class SpellCheckerChangedEventArgs : EventArgs
            {
                internal SpellCheckerChangedEventArgs(SpellChecker spellChecker)
                {
                    SpellChecker = spellChecker;
                }

                internal SpellChecker SpellChecker { get; private set; }
            }
        }
    }
}