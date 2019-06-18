// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A class encapsulating the functionality of ISpellCheckerFactory
//              and IUserDictionariesRegistrar types exposed by MsSpellCheckLib.RCW. 
//              
//              It also provides resilience against out-of-proc COM server failures 
//              when calling into methods from the above interfaces, and translates from 
//              COM types to .NET types. 
//

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Security;

using MS.Internal;

namespace System.Windows.Documents
{
    namespace MsSpellCheckLib
    {
        using ISpellChecker = RCW.ISpellChecker;
        using ISpellCheckerFactory = RCW.ISpellCheckerFactory;
        using IUserDictionariesRegistrar = RCW.IUserDictionariesRegistrar;
        using SpellCheckerFactoryClass = RCW.SpellCheckerFactoryClass;

        /// <summary>
        /// Encapsulation of RCW.ISpellCheckerFactory and RCW.IUserDictionariesRegistrar funcionalities
        /// and provides a resilient (to out-of-proc COM server failures) interface to callers.
        /// </summary>
        /// <remarks>
        /// The ISpellCheckerFactory and IUserDictionareisRegistrar methods are implemented using the following pattern. 
        /// For a method Foo(), we see the following entries: 
        /// 
        ///     1. The most basic implementation of the method.
        ///         private FooImpl();
        ///         
        ///     2. Some resilience added to the basic implementation. This calls into FooImpl repeatedly.
        ///         private FooImplWithRetries(bool shouldSuppressCOMExceptions);
        ///         
        ///     3. Finally, the version that is exposed to callers. 
        ///         public Foo(bool shouldSuppressCOMExceptions = true);
        /// </remarks>
        internal partial class SpellCheckerFactory
        {
            #region Fields

            internal ISpellCheckerFactory ComFactory { get; private set; }

            private static ReaderWriterLockSlimWrapper _factoryLock 
                = new ReaderWriterLockSlimWrapper(System.Threading.LockRecursionPolicy.SupportsRecursion);
            internal static SpellCheckerFactory Singleton { get; private set; }

            private static Dictionary<bool, List<Type>> SuppressedExceptions = new Dictionary<bool, List<Type>>
            {
                {false, new List<Type> { /* empty */ } },
                {true, new List<Type> { typeof(COMException), typeof(UnauthorizedAccessException)} }
            };

            #endregion

            #region Factory and Constructors

            public static SpellCheckerFactory Create(bool shouldSuppressCOMExceptions = false)
            {
                SpellCheckerFactory result = null;

                bool creationSucceeded = false;
                if (
                    _factoryLock.WithWriteLock(CreateLockFree, shouldSuppressCOMExceptions, false, out creationSucceeded) &&
                    creationSucceeded)
                {
                    result = Singleton;
                }

                return result; 
            }

            /// <summary>
            /// Private constructor prevents explicit instantiation from outside. Callers shuould use Create instead.
            /// </summary>
            private SpellCheckerFactory()
            {
            }

            static SpellCheckerFactory()
            {
                Singleton = new SpellCheckerFactory();

                bool creationResult = false;
                _factoryLock.WithWriteLock(CreateLockFree, true, true, out creationResult);
            }

            #endregion // Factory and Constructors

            #region Creation and Reinitialization Methods

            /// <summary>
            /// Reinitializes the ISpellCheckerFactory handle
            /// </summary>
            /// <returns>True if successful, False otherwise</returns>
            private static bool Reinitalize()
            {
                bool creationResult = false;
                return
                    _factoryLock.WithWriteLock(CreateLockFree, false, false, out creationResult)
                    && creationResult;
            }

            /// <summary>
            /// Creates a new instance of ISpellCheckerFactory type and updates 
            /// the singleton instance of SpellCheckerFactory. 
            /// </summary>
            /// <param name="suppressCOMExceptions"></param>
            /// <param name="suppressOtherExceptions"></param>
            /// <returns>True if successful, False otherwise</returns>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            private static bool CreateLockFree(bool suppressCOMExceptions = true, bool suppressOtherExceptions = true)
            {
                if (Singleton.ComFactory != null)
                {
                    try
                    {
                        Marshal.ReleaseComObject(Singleton.ComFactory);
                    }
                    catch
                    {
                        // do nothing
                    }

                    Singleton.ComFactory = null; 
                }

                bool success = false;
                ISpellCheckerFactory result = null; 
                try
                {
                    result = new SpellCheckerFactoryClass();
                    success = true; 
                }
                catch (COMException) when (suppressCOMExceptions)
                {
                    // do nothing
                }
                catch (UnauthorizedAccessException uae)
                {
                    // Sometimes, COM initialization can throw UnauthorizedAccessException
                    // We will treat it the same as COMException
                    if (!suppressCOMExceptions)
                    {
                        throw new COMException(string.Empty, uae);
                    }
                }
                catch (InvalidCastException icex)
                {
                    // Sometimes, InvalidCastException is thrown when SpellCheckerFactory fails to instantiate correctly
                    // We will treat it as if it were a COM Exception and translate it to one.
                    if (!suppressCOMExceptions)
                    {
                        throw new COMException(string.Empty, icex);
                    }
                }
                catch (Exception e) when (suppressOtherExceptions && !(e is COMException) && !(e is UnauthorizedAccessException)) 
                {
                    // do nothing
                }

                if (success)
                {
                    Singleton.ComFactory = result;
                }

                return success;
            }

            #endregion // Creation and Reinitialization 

            #region ISpellCheckerFactory services 

            #region SupportedLanguages
            
            private List<string> SupportedLanguagesImpl()
            {
                var languages = ComFactory?.SupportedLanguages;

                List<string> result = null; 
                if (languages != null)
                {
                    result = languages.ToList();
                }

                return result;
            }

            private List<string> SupportedLanguagesImplWithRetries(bool shouldSuppressCOMExceptions)
            {
                List<string> languages = null;

                // Note:
                //  SupportedLanguagesImpl is Safe 
                //  preamble delegate only calls Safe or Transparent methods.
                bool callSucceeded  =
                    RetryHelper.TryExecuteFunction(
                        func: SupportedLanguagesImpl,
                        result: out languages,
                        preamble: () => Reinitalize(),
                        ignoredExceptions: SuppressedExceptions[shouldSuppressCOMExceptions]);

                return callSucceeded ? languages : null; 
            }         

            private List<string> GetSupportedLanguagesPrivate(bool shouldSuppressCOMExceptions = true)
            {
                List<string> result = null;
                bool lockedExecutionSucceeded = 
                    _factoryLock.WithWriteLock(SupportedLanguagesImplWithRetries, shouldSuppressCOMExceptions, out result);

                return lockedExecutionSucceeded ? result : null; 
            }

            internal static List<string> GetSupportedLanguages(bool shouldSuppressCOMExceptions = true)
            {
                return Singleton?.GetSupportedLanguagesPrivate(shouldSuppressCOMExceptions);
            }

            #endregion // SupportedLanguages

            #region IsSupported
            
            /// <summary>
            /// 
            /// </summary>
            /// <param name="languageTag"></param>
            /// <returns></returns>
            private bool IsSupportedImpl(string languageTag)
            {
                return ((ComFactory != null) && (ComFactory.IsSupported(languageTag) != 0));
            }

            private bool IsSupportedImplWithRetries(string languageTag, bool suppressCOMExceptions = true)
            {
                bool isSupported = false; 

                // Note: 
                //  The delegate passed as [func] is Transparent, and calls only Safe methods in turn.
                //  The delegate passed as [preamble] is Transparent, and calls only Safe or Transparent methods in turn.
                bool callSucceeded = 
                   RetryHelper.TryExecuteFunction<bool>(
                       func: () => IsSupportedImpl(languageTag),
                       result: out isSupported,
                       preamble: () => Reinitalize(), 
                       ignoredExceptions: SuppressedExceptions[suppressCOMExceptions]);

                return callSucceeded ? isSupported : false;
            }

            private bool IsSupportedPrivate(string languageTag, bool suppressCOMExceptons = true)
            {
                bool isSupported = false;
                bool lockedExecutionSucceeded = 
                    _factoryLock.WithWriteLock(IsSupportedImplWithRetries, languageTag, suppressCOMExceptons, out isSupported);

                return lockedExecutionSucceeded ? isSupported : false;
            }

            internal static bool IsSupported(string languageTag, bool suppressCOMExceptons = true)
            {
                return ((Singleton != null) && Singleton.IsSupportedPrivate(languageTag, suppressCOMExceptons));
            }

            #endregion // IsSupported

            #region CreateSpellChecker 

            private ISpellChecker CreateSpellCheckerImpl(string languageTag)
            {
                return SpellCheckerCreationHelper.Helper(languageTag).CreateSpellChecker();
            }

            private ISpellChecker CreateSpellCheckerImplWithRetries(string languageTag, bool suppressCOMExceptions = true)
            {
                ISpellChecker spellChecker = null;

                bool callSucceeded =
                    RetryHelper.TryExecuteFunction<ISpellChecker>(
                        func: SpellCheckerCreationHelper.Helper(languageTag).CreateSpellChecker,
                        result: out spellChecker,
                        preamble:  SpellCheckerCreationHelper.Helper(languageTag).CreateSpellCheckerRetryPreamble,
                        ignoredExceptions: SuppressedExceptions[suppressCOMExceptions]);


                return callSucceeded ? spellChecker : null;
            }

            private ISpellChecker CreateSpellCheckerPrivate(string languageTag, bool suppressCOMExceptions = true)
            {
                ISpellChecker spellChecker = null;
                bool lockedExecutionSucceeded = 
                    _factoryLock.WithWriteLock(CreateSpellCheckerImplWithRetries, languageTag, suppressCOMExceptions, out spellChecker);

                return lockedExecutionSucceeded ? spellChecker : null; 
            }

            internal static ISpellChecker CreateSpellChecker(string languageTag, bool suppressCOMExceptions = true)
            {
                return Singleton?.CreateSpellCheckerPrivate(languageTag, suppressCOMExceptions);
            }

            #endregion // CreateSpellChecker 

            #endregion // ISpellCheckerFactory methods

            #region IUserDictionaryRegistrar services

            private void RegisterUserDicionaryImpl(string dictionaryPath, string languageTag)
            {
                var registrar = (IUserDictionariesRegistrar)ComFactory;

                registrar?.RegisterUserDictionary(dictionaryPath, languageTag);
            }

            private void RegisterUserDictionaryImplWithRetries(string dictionaryPath, string languageTag, bool suppressCOMExceptions = true)
            {
                if (dictionaryPath == null) throw new ArgumentNullException(nameof(dictionaryPath));
                if (languageTag == null) throw new ArgumentNullException(nameof(languageTag));

                // RegisterUserDicionaryImpl is SecuritySafeCritical, so it is okay to 
                // create an anon. lambda that calls into it, and pass 
                // that lambda below.
                RetryHelper.TryCallAction(
                    action: () => RegisterUserDicionaryImpl(dictionaryPath, languageTag),
                    preamble: () => Reinitalize(), 
                    ignoredExceptions: SuppressedExceptions[suppressCOMExceptions]);
            }

            private void RegisterUserDictionaryPrivate(string dictionaryPath, string languageTag, bool suppressCOMExceptions = true)
            {
                _factoryLock.WithWriteLock(() => { RegisterUserDictionaryImplWithRetries(dictionaryPath, languageTag, suppressCOMExceptions); });
            }

            internal static void RegisterUserDictionary(string dictionaryPath, string languageTag, bool suppressCOMExceptions = true)
            {
                Singleton?.RegisterUserDictionaryPrivate(dictionaryPath, languageTag, suppressCOMExceptions);
            }

            private void UnregisterUserDictionaryImpl(string dictionaryPath, string languageTag)
            {
                var registrar = (IUserDictionariesRegistrar)ComFactory;

                registrar?.UnregisterUserDictionary(dictionaryPath, languageTag);
            }

            private void UnregisterUserDictionaryImplWithRetries(string dictionaryPath, string languageTag, bool suppressCOMExceptions = true)
            {
                if (dictionaryPath == null) throw new ArgumentNullException(nameof(dictionaryPath));
                if (languageTag == null) throw new ArgumentNullException(nameof(languageTag));

                // UnregisterUserDictionaryImpl is SecuritySafeCritical, so it is okay to 
                // create an anon. lambda that calls into it, and pass 
                // that lambda below.
                RetryHelper.TryCallAction(
                    action: () => UnregisterUserDictionaryImpl(dictionaryPath, languageTag),
                    preamble: () => Reinitalize(),
                    ignoredExceptions: SuppressedExceptions[suppressCOMExceptions]);
            }
            
            private void UnregisterUserDictionaryPrivate(string dictionaryPath, string languageTag, bool suppressCOMExceptions = true)
            {
                _factoryLock.WithWriteLock(() => { UnregisterUserDictionaryImplWithRetries(dictionaryPath, languageTag, suppressCOMExceptions); });
            }

            internal static void UnregisterUserDictionary(string dictionaryPath, string languageTag, bool suppressCOMExceptions = true)
            {
                Singleton?.UnregisterUserDictionaryPrivate(dictionaryPath, languageTag, suppressCOMExceptions);
            }

            #endregion // IUserDictionaryRegistrar services
        }
    }
}
