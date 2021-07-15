// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: RCW for ISpellChecker and related COM types.
//

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security;

/// <summary>
/// RCW for spellcheck.idl found in Windows SDK
/// This is generated code with minor manual edits. 
/// i.  Generate TLB
///      MIDL /TLB MsSpellCheckLib.tlb SpellCheck.IDL //SpellCheck.IDL found in Windows SDK
/// ii. Generate RCW in a DLL
///      TLBIMP MsSpellCheckLib.tlb // Generates MsSpellCheckLib.dll
/// iii.Decompile the DLL and copy out the RCW by hand.
///      ILDASM MsSpellCheckLib.dll
/// </summary>


namespace System.Windows.Documents
{
    namespace MsSpellCheckLib
    {
        internal class RCW
        {
            #region WORDLIST_TYPE

            // Types of user custom wordlists
            // Custom wordlists are language-specific
            internal enum WORDLIST_TYPE : int
            {
                WORDLIST_TYPE_IGNORE = 0, // Ignore wordlist - words that should be considered correctly spelled in a single spell checking session
                WORDLIST_TYPE_ADD = 1, // Added words wordlist - words that should be considered correctly spelled - permanent and applies to all clients
                WORDLIST_TYPE_EXCLUDE = 2, // Excluded words wordlist - words that should be considered misspelled - permanent and applies to all clients
                WORDLIST_TYPE_AUTOCORRECT = 3, // Autocorrect wordlit - pairs of words with a word that should be automatically substituted by the other word in the pair - permanent and applies to all clients
            }

            #endregion // WORDLIST_TYPE

            #region CORRECTIVE_ACTION

            // Action that a client should take on a specific spelling error(obtained from ISpellingError::get_CorrectiveAction)
            internal enum CORRECTIVE_ACTION : int
            {
                CORRECTIVE_ACTION_NONE = 0, // None - there's no error
                CORRECTIVE_ACTION_GET_SUGGESTIONS = 1, // GetSuggestions - the client should show a list of suggestions (obtained through ISpellChecker::Suggest) to the user
                CORRECTIVE_ACTION_REPLACE = 2, // Replace - the client should autocorrect the word to the word obtained from ISpellingError::get_Replacement
                CORRECTIVE_ACTION_DELETE = 3, // Delete - the client should delete this word
            }

            #endregion // CORRECTIVE_ACTION

            #region ISpellingError

            // This interface represents a spelling error - you can get information like the range that comprises the error, or the suggestions for that misspelled word
            // Should be implemented by any spell check provider (someone who provides a spell checking engine), and used by clients of spell checking
            // It is obtained through IEnumSpellingError::Next
            [ComImport]
            [Guid("B7C82D61-FBE8-4B47-9B27-6C0D2E0DE0A3")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface ISpellingError
            {
                uint StartIndex
                {
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }

                uint Length
                {
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }

                CORRECTIVE_ACTION CorrectiveAction
                {
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }

                string Replacement
                {
                    [return: MarshalAs(UnmanagedType.LPWStr)]
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }
            }

            #endregion //ISpellingError

            #region IEnumSpellingError

            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("803E3BD4-2828-4410-8290-418D1D73C762")]
            internal interface IEnumSpellingError
            {
                [return: MarshalAs(UnmanagedType.Interface)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                ISpellingError Next();
            }

            #endregion // IEnumSpellingError

            #region IEnumString

            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("00000101-0000-0000-C000-000000000046")]
            internal interface IEnumString
            {
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void RemoteNext([In] uint celt, [MarshalAs(UnmanagedType.LPWStr)] out string rgelt, out uint pceltFetched);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void Skip([In] uint celt);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void Reset();

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumString ppenum);
            }

            #endregion // IEnumString

            #region IOptionDescription

            [ComImport]
            [Guid("432E5F85-35CF-4606-A801-6F70277E1D7A")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface IOptionDescription
            {
                string Id
                {
                    [return: MarshalAs(UnmanagedType.LPWStr)]
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }

                string Heading
                {
                    [return: MarshalAs(UnmanagedType.LPWStr)]
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }

                string Description
                {
                    [return: MarshalAs(UnmanagedType.LPWStr)]
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }

                IEnumString Labels
                {
                    [return: MarshalAs(UnmanagedType.Interface)]
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }
            }

            #endregion // IOptionDescription

            #region ISpellCheckerChangedEventHandler

            [ComImport]
            [Guid("0B83A5B0-792F-4EAB-9799-ACF52C5ED08A")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface ISpellCheckerChangedEventHandler
            {
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void Invoke([In, MarshalAs(UnmanagedType.Interface)] ISpellChecker sender);
            }

            #endregion // #region ISpellCheckerChangedEventHandler

            #region ISpellChecker

            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("B6FD0B71-E2BC-4653-8D05-F197E412770B")]
            internal interface ISpellChecker
            {
                string languageTag
                {
                    [return: MarshalAs(UnmanagedType.LPWStr)]
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }

                [return: MarshalAs(UnmanagedType.Interface)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                IEnumSpellingError Check([In, MarshalAs(UnmanagedType.LPWStr)] string text);

                [return: MarshalAs(UnmanagedType.Interface)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                IEnumString Suggest([In, MarshalAs(UnmanagedType.LPWStr)] string word);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void Add([In, MarshalAs(UnmanagedType.LPWStr)] string word);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void Ignore([In, MarshalAs(UnmanagedType.LPWStr)] string word);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void AutoCorrect([In, MarshalAs(UnmanagedType.LPWStr)] string from, [In, MarshalAs(UnmanagedType.LPWStr)] string to);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                byte GetOptionValue([In, MarshalAs(UnmanagedType.LPWStr)] string optionId);

                IEnumString OptionIds
                {
                    [return: MarshalAs(UnmanagedType.Interface)]
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }

                string Id
                {
                    [return: MarshalAs(UnmanagedType.LPWStr)]
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }

                string LocalizedName
                {
                    [return: MarshalAs(UnmanagedType.LPWStr)]
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                uint add_SpellCheckerChanged([In, MarshalAs(UnmanagedType.Interface)] ISpellCheckerChangedEventHandler handler);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void remove_SpellCheckerChanged([In] uint eventCookie);

                [return: MarshalAs(UnmanagedType.Interface)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                IOptionDescription GetOptionDescription([In, MarshalAs(UnmanagedType.LPWStr)] string optionId);

                [return: MarshalAs(UnmanagedType.Interface)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                IEnumSpellingError ComprehensiveCheck([In, MarshalAs(UnmanagedType.LPWStr)] string text);
            }

            #endregion // ISpellChecker

            #region ISpellCheckerFactory

            [ComImport]
            [Guid("8E018A9D-2415-4677-BF08-794EA61F94BB")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface ISpellCheckerFactory
            {
                IEnumString SupportedLanguages
                {
                    [return: MarshalAs(UnmanagedType.Interface)]
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                int IsSupported([In, MarshalAs(UnmanagedType.LPWStr)] string languageTag);

                [return: MarshalAs(UnmanagedType.Interface)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                ISpellChecker CreateSpellChecker([In, MarshalAs(UnmanagedType.LPWStr)] string languageTag);
            }

            #endregion // ISpellCheckerFactory

            #region IUserDictionariesRegistrar

            [ComImport]
            [Guid("AA176B85-0E12-4844-8E1A-EEF1DA77F586")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface IUserDictionariesRegistrar
            {
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void RegisterUserDictionary([In, MarshalAs(UnmanagedType.LPWStr)] string dictionaryPath, [In, MarshalAs(UnmanagedType.LPWStr)] string languageTag);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void UnregisterUserDictionary([In, MarshalAs(UnmanagedType.LPWStr)] string dictionaryPath, [In, MarshalAs(UnmanagedType.LPWStr)] string languageTag);
            }

            #endregion // IUserDictionariesRegistrar

            #region SpellCheckerFactoryCoClass

            [ComImport]
            [Guid("7AB36653-1796-484B-BDFA-E74F1DB7C1DC")]
            [TypeLibType(TypeLibTypeFlags.FCanCreate)]
            [ClassInterface(ClassInterfaceType.None)]
            internal class SpellCheckerFactoryCoClass : ISpellCheckerFactory, SpellCheckerFactoryClass, IUserDictionariesRegistrar
            {
                [return: MarshalAs(UnmanagedType.Interface)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                public virtual extern ISpellChecker CreateSpellChecker([In, MarshalAs(UnmanagedType.LPWStr)] string languageTag);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                public virtual extern int IsSupported([In, MarshalAs(UnmanagedType.LPWStr)] string languageTag);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                public virtual extern void RegisterUserDictionary([In, MarshalAs(UnmanagedType.LPWStr)] string dictionaryPath, [In, MarshalAs(UnmanagedType.LPWStr)] string languageTag);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                public virtual extern void UnregisterUserDictionary([In, MarshalAs(UnmanagedType.LPWStr)] string dictionaryPath, [In, MarshalAs(UnmanagedType.LPWStr)] string languageTag);

                public virtual extern IEnumString SupportedLanguages
                {
                    [return: MarshalAs(UnmanagedType.Interface)]
                    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                    get;
                }
            }

            #endregion // SpellCheckerFactoryCoClass

            #region SpellCheckerFactoryClass

            [ComImport]
            [CoClass(typeof(SpellCheckerFactoryCoClass))]
            [Guid("8E018A9D-2415-4677-BF08-794EA61F94BB")]
            internal interface SpellCheckerFactoryClass : ISpellCheckerFactory
            {
            }

            #endregion // SpellCheckerFactory
        }
    }
}
