// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: An implementation of ISpellCheckerChangedEventHandler
//              that is exposed by MsSpellCheckLib.RCW. 
//
//              An instance of SpellCheckerChangedEventHandler can be used 
//              to register with an ISpellChecker instance for receiving change 
//              callbacks and propagating it to managed listeners. 
//

namespace System.Windows.Documents
{
    namespace MsSpellCheckLib
    {
        using ISpellChecker = RCW.ISpellChecker;
        using ISpellCheckerChangedEventHandler = RCW.ISpellCheckerChangedEventHandler;

        internal partial class SpellChecker
        {
            /// <summary>
            /// Implements RCW.ISpellCheckerChangedEventHandler
            /// </summary>
            private class SpellCheckerChangedEventHandler : ISpellCheckerChangedEventHandler
            {
                #region Fields 

                private SpellCheckerChangedEventArgs _eventArgs;
                private SpellChecker _spellChecker;

                #endregion // Fields

                internal SpellCheckerChangedEventHandler(SpellChecker spellChecker)
                {
                    _spellChecker = spellChecker;
                    _eventArgs = new SpellCheckerChangedEventArgs(_spellChecker);
                }

                #region ISpellCheckerChangedEventHandler methods

                /// <summary>
                /// The spell-checker COM server will call back this method 
                /// when it detects any changes to the spell-checker. Typically, 
                /// this would indicated that the spell-checking needs to be 
                /// re-done (for e.g., a new dictionary is registered).
                /// </summary>
                /// <param name="sender"></param>
                public void Invoke(ISpellChecker sender)
                {
                    if (sender == _spellChecker?._speller?.Value)
                    {
                        _spellChecker?.OnChanged(_eventArgs);
                    }
                }

                #endregion // ISpellCheckerChangedEventHandler methods
            }
        }
    }
}