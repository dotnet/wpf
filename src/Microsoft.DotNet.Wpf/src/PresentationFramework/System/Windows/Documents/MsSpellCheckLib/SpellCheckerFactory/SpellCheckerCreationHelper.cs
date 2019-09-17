// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Helper type use to supply SecurityCritical delegates 
//              for creation of spell-checkers. This type exists primarily 
//              to work-around the fact that anon. lambdas can not be 
//              marked as SecurityCritical. 
//

using MS.Internal;
using System.Collections.Generic;
using System.Security;

namespace System.Windows.Documents
{
    namespace MsSpellCheckLib
    {
        internal partial class SpellCheckerFactory
        {
            /// <summary>
            /// Helper type use to supply SecurityCritical delegates for creation of spell-checkers. 
            /// This type exists primarily to work-around the fact that anon. lambdas can not be 
            /// marked as SecurityCritical. 
            /// 
            /// The key SecurityCritical methods that are exposed from this type, and consumed further down by 
            /// CreateSpellCheckerImplWithRetries are: 
            /// 
            ///     public ISpellChecker CreateSpellChecker()
            ///     public bool CreateSpellCheckerRetryPreamble(out Func<ISpellChecker> func)
            /// </summary>
            private class SpellCheckerCreationHelper
            {
                #region Fields

                /// <summary>
                /// Cached of instances corresponding to different languageTags
                /// </summary>
                private static Dictionary<string, SpellCheckerCreationHelper> _instances =
                    new Dictionary<string, SpellCheckerCreationHelper>();

                private static ReaderWriterLockSlimWrapper _lock
                    = new ReaderWriterLockSlimWrapper(System.Threading.LockRecursionPolicy.NoRecursion);

                /// <summary>
                /// languageTag
                /// </summary>
                private string _language;

                #endregion // Fields

                /// <summary>
                /// Accesses the ISpellCheckerFactory handle from the singleton 
                /// SpellCheckerFactory instance
                /// </summary>
                public static RCW.ISpellCheckerFactory ComFactory
                {
                    get
                    {
                        return SpellCheckerFactory.Singleton.ComFactory;
                    }
                }

                #region Constructor and Factory

                private SpellCheckerCreationHelper(string language)
                {
                    _language = language;
                }

                private static void CreateForLanguage(string language)
                {
                    _instances[language] = new SpellCheckerCreationHelper(language);
                }

                public static SpellCheckerCreationHelper Helper(string language)
                {
                    if (!_instances.ContainsKey(language))
                    {
                        _lock.WithWriteLock(CreateForLanguage, language);
                    }

                    return _instances[language];
                }

                #endregion // Constructor and Factory

                #region SecurityCritical methods used by CreateSpellCheckerImplWithRetries

                /// <summary>
                /// Creates an ISpellChecker instance for a given language
                /// </summary>
                /// <returns></returns>
                public RCW.ISpellChecker CreateSpellChecker()
                {
                    return ComFactory?.CreateSpellChecker(_language);
                }

                /// <summary>
                /// Tries to reinitialize the SpellCheckerFactory singleton, and upon
                /// success, updates the out parameter <paramref name="func"/> with 
                /// a delegate for ISpellChecker creation.
                /// </summary>
                /// <param name="func"></param>
                /// <returns></returns>
                public bool CreateSpellCheckerRetryPreamble(out Func<RCW.ISpellChecker> func)
                {
                    bool success = false;
                    func = null;

                    if (success = SpellCheckerFactory.Reinitalize())
                    {
                        func = SpellCheckerCreationHelper.Helper(_language).CreateSpellChecker;
                    }

                    return success;
                }

                #endregion // SecurityCritical methods used by CreateSpellCheckerImplWithRetries
            }
        }
    }
}
