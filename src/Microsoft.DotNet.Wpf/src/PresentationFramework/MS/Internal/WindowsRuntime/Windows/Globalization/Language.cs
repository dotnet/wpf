// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Provides an implementation of the WinRT class 
//                  Windows.Data.Globalization.Language, which provides information related to BCP-47
//                  language tags such as the language name and the script.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MS.Internal.WindowsRuntime
{
    namespace Windows.Globalization
    {
        internal class Language
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            static Language()
            {
                ConstructorInfo constructor = null; 

                // We don't want to throw here - so wrap in a try..catch
                try
                {
                    s_WinRTType = Type.GetType(s_TypeName);
                    if (s_WinRTType != null)
                    {
                        constructor = s_WinRTType.GetConstructor(new Type[] { typeof(string) });
                    }
                }
                catch
                {
                    s_WinRTType = null; 
                }

                s_Supported = (s_WinRTType != null) && (constructor != null);
            }

            #region Windows.Globalization.Language constructor, methods and properties

            /// <summary>
            /// Creates a <see cref="Language"/> object.
            /// </summary>
            /// <param name="languageTag">A BCP-47 language tag. See Remarks</param>
            /// <remarks>
            /// If your app passes language tags in this class to any Win32 NLS functions, it must first convert the tags by calling ResolveLocaleName Win32 function.
            /// <b>Starting in Windows 8.1:</b> Language tags support the Unicode extensions "ca-" and "nu-". 
            /// See &lt;a href="http://go.microsoft.com/fwlink/p/?LinkId=308919"&gt; Unicode Key/Type Definitions &lt;/a&gt;
            /// Note that these extensions can affect the numeral system or calendar used by globalization objects.
            /// </remarks>
            public Language(string languageTag)
            {
                if (!s_Supported)
                {
                    throw new PlatformNotSupportedException();
                }

                try
                {
                    _language = s_WinRTType.ReflectionNew<string>(languageTag);
                }
                catch (TargetInvocationException tiex) when (tiex.InnerException is ArgumentException)
                {
                    throw new ArgumentException(string.Empty, nameof(languageTag), tiex);
                }

                if (_language == null)
                {
                    throw new NotSupportedException();
                }
            }

            /// <summary>
            /// Retrieves a vector of extension subtags in the current language for the given extension identified by singleton.
            /// </summary>
            /// <param name="singleton">A single-character subtag for the LanguageTag of the current language.</param>
            /// <returns>The list of extension subtags identified by <i>singleton</i>.</returns>
            public IReadOnlyList<string> GetExtensionSubtags(string singleton)
            {
                object extensionSubtags = _language.ReflectionCall<string>("GetExtensionSubtags", singleton);

                List<string> result = new List<string>();
                foreach(var item in (IEnumerable)extensionSubtags)
                {
                    if (item != null)
                    {
                        result.Add((string)item);
                    }
                }

                return result.AsReadOnly();
            }

            /// <summary>
            /// Determines whether a BCP-47 language tag is well-formed
            /// </summary>
            /// <param name="languageTag"></param>
            /// <returns></returns>
            /// <exception cref="PlatformNotSupportedException"/>
            public static bool IsWellFormed(string languageTag)
            {
                if (!s_Supported)
                {
                    throw new PlatformNotSupportedException();
                }

                return s_WinRTType.ReflectionStaticCall<bool, string>("IsWellFormed", languageTag);
            }

            /// <summary>
            /// Tries to set the normalized BCP-47 language tag of this language.
            /// </summary>
            /// <param name="languageTag"></param>
            /// <returns></returns>
            /// <exception cref="PlatformNotSupportedException"/>
            public static bool TrySetInputMethodLanguageTag(string languageTag)
            {
                if (!s_Supported)
                {
                    throw new PlatformNotSupportedException();
                }

                return s_WinRTType.ReflectionStaticCall<bool, string>("TrySetInputMethodLanguageTag", languageTag);
            }

            /// <summary>
            /// Gets the BCP-47 language tag for the currently enabled keyboard layout or Input Method Editor (IME).
            /// </summary>
            /// <exception cref="PlatformNotSupportedException"/>
            public static string CurrentInputMethodLanguageTag
            {
                get
                {
                    if (!s_Supported)
                    {
                        throw new PlatformNotSupportedException();
                    }

                    return s_WinRTType.ReflectionStaticGetProperty<string>("CurrentInputMethodLanguageTag");
                }
            }

            /// <summary>
            /// Gets a localized string that is suitable for display to the user for identifying the language.
            /// </summary>
            public string DisplayName
            {
                get
                {
                    return _language.ReflectionGetProperty<string>("DisplayName");
                }
            }

            /// <summary>
            /// Gets the normalized BCP-47 language tag for this language.
            /// </summary>
            public string LanguageTag
            {
                get
                {
                    return _language.ReflectionGetProperty<string>("LanguageTag");
                }
            }

            /// <summary>
            /// Gets the name of the language in the language itself.
            /// </summary>
            public string NativeName
            {
                get
                {
                    return _language.ReflectionGetProperty<string>("NativeName");
                }
            }

            /// <summary>
            /// Gets the four-letter ISO 15924 script code of the language.
            /// </summary>
            public string Script
            {
                get
                {
                    return _language.ReflectionGetProperty<string>("Script");
                }
            }

            #endregion // Windows.Globalization.Language constructor, methods and properties


            private static Type s_WinRTType;
            private static readonly bool s_Supported;

            private static readonly string s_TypeName = "Windows.Globalization.Language, Windows, ContentType=WindowsRuntime";

            private object _language;
}
    }
}