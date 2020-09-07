// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: EventArgs class that supports SpellCheckerChangedEventHandler.
//

using System;

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