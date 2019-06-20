// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __LOCALIZEDSTRINGS_H
#define __LOCALIZEDSTRINGS_H

#include "Common.h"
#include "LocalizedErrorMsgs.h"

using namespace System::Collections::Generic;
using namespace System::Globalization;
using namespace System;

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    
    /// <summary>
    /// Represents a collection of strings indexed by locale name.
    /// </summary>
    private ref class LocalizedStrings sealed: IDictionary<CultureInfo^, String^>
    {
        private:

            /// <summary>
            /// A pointer to the wrapped DWrite Localized Strings object.
            /// </summary>
            NativeIUnknownWrapper<IDWriteLocalizedStrings>^ _localizedStrings;

            /// <summary>
            /// Gets the length in characters (not including the null terminator) of the locale name with the specified index.
            /// </summary>
            /// <param name="index">Zero-based index of the locale name.</param>
            /// <returns>The length in characters, not including the null terminator.</returns>
            UINT32 GetLocaleNameLength(
                                      UINT32 index
                                      );

            /// <summary>
            /// Gets the length in characters (not including the null terminator) of the string with the specified index.
            /// </summary>
            /// <param name="index">Zero-based index of the string.</param>
            /// <returns>The length in characters, not including the null terminator.</returns>
            UINT32 GetStringLength(
                                  UINT32 index
                                  );
           
            // This is a lazily initialized array of CultureInfo's used when this
            // object is used through the ICollection interface.
            array<CultureInfo^>^ _keys;

            // This is a lazily initialized array of string's used when this
            // object is used through the ICollection interface.
            array<String^>^ _values;

            /// <summary>
            /// Gets an array of the CultureInfos stored by the localizedStrings object.
            /// </summary>
            property array<CultureInfo^>^ KeysArray
            {
                array<CultureInfo^>^ get();
            }

            /// <summary>
            /// Gets an array of the string values stored by the localizedStrings object.
            /// </summary>
            property array<String^>^ ValuesArray
            {
                array<String^>^ get();
            }

        internal:

            /// <summary>
            /// Constructs a LocalizedStrings object.
            /// </summary>
            /// <param name="localizedStrings">The DWrite localized Strings object that 
            /// this class wraps.</param>
            LocalizedStrings(
                            IDWriteLocalizedStrings* localizedStrings
                            );

            /// <summary>
            /// Constructs an empty LocalizedStrings object.
            /// </summary>
            LocalizedStrings(
                            );

            /// <summary>
            /// Gets the number of language/string pairs.
            /// </summary>
            property UINT32 StringsCount
            {
                UINT32 get();
            }

            /// <summary>
            /// Gets the index of the item with the specified locale name.
            /// </summary>
            /// <param name="localeName">Locale name to look for.</param>
            /// <param name="index">Receives the zero-based index of the locale name/string pair.</param>
            /// <returns>TRUE if the locale name exists or FALSE if not.</returns>
            bool FindLocaleName(
                                                               String^ localeName,
                               [Runtime::InteropServices::Out] UINT32% index
                               );

            /// <summary>
            /// Gets the locale name with the specified index.
            /// </summary>
            /// <param name="index">Zero-based index of the locale name.</param>
            /// <returns>The locale name.</returns>
            String^ GetLocaleName(
                                 UINT32 index
                                 );

            /// <summary>
            /// Gets the string with the specified index.
            /// </summary>
            /// <param name="index">Zero-based index of the string.</param>
            /// <returns>The string.</returns>
            String^ GetString(
                             UINT32 index
                             );

        public:

            /// <summary>
            /// Add is not supported.
            /// </summary>
            virtual void Add(
                            CultureInfo^ key,
                            String^ value
                            )
            {
                throw gcnew NotSupportedException();
            }

            /// <summary>
            /// Checks whether the localizedString contains a certain CultureInfo.
            /// </summary>
            /// <param name="key">the key to look for.</param>
            virtual bool ContainsKey(
                                    CultureInfo^ key
                                    )
            {
                unsigned int index = 0;
                return FindLocaleName(key->Name, index);
            }

            /// <summary>
            /// Gets a collection of the CultureInfos stored by the localizedStrings object.
            /// </summary>
            property ICollection<CultureInfo^>^ Keys
            {
                virtual ICollection<CultureInfo^>^ get();
            }

            /// <summary>
            /// Remove not supported.
            /// </summary>
            virtual bool Remove(
                               CultureInfo^ key
                               )
            {
                throw gcnew NotSupportedException();
            }

            __declspec(noinline) virtual bool TryGetValue(
                                                                    CultureInfo^  key,
                                    [Runtime::InteropServices::Out] String^%      value
                                    )
            {
                Invariant::Assert(key != nullptr);

                unsigned int index = 0;
                if (FindLocaleName(key->Name, index))
                {
                    value = GetString(index);
                    return true;
                }
                return false;                
            }

            property ICollection<String^>^ Values
            {
                virtual ICollection<String^>^ get();
            }

            property String^ default[CultureInfo^]
            {
                virtual String^ get(CultureInfo^ key)
                {
                    String^ value;
                    if (TryGetValue(key, value))
                    {
                        return value;
                    }
                    else
                    {
                        return nullptr;
                    }
                }
                virtual void set(CultureInfo^ key, String^ value)
                {
                    throw gcnew NotSupportedException();
                }
            }

            //#region ICollection<KeyValuePair<CultureInfo,String^>> Members
            virtual void Add(
                    KeyValuePair<CultureInfo^, String^> item
                    )
            {
                throw gcnew NotSupportedException();
            }

            virtual void Clear()
            {
                throw gcnew NotSupportedException();
            }

            virtual bool Contains(KeyValuePair<CultureInfo^, String^> item)
            {
                throw gcnew NotImplementedException();
            }

            virtual void CopyTo(
                               array<KeyValuePair<CultureInfo^, String^>>^ arrayObj,
                               int arrayIndex
                               )
            {
                int index = arrayIndex;
                for each (KeyValuePair<CultureInfo^, String^> pair in this)
                {
                    arrayObj[index] = pair;
                    ++index;
                }
            }

            property int Count
            {
                virtual int get();
            }

            property bool IsReadOnly
            {
                virtual bool get()
                { 
                    return true;
                }
            }

            virtual bool Remove(KeyValuePair<CultureInfo^, String^> item)
            {
                throw gcnew NotSupportedException();
            }

            //#region IEnumerable<KeyValuePair<CultureInfo,String^>> Members            
            ref struct LocalizedStringsEnumerator : public IEnumerator<KeyValuePair<CultureInfo^, String^>>
            {                
                LocalizedStrings^ _localizedStrings;
                INT64             _currentIndex;
                LocalizedStringsEnumerator(LocalizedStrings^ localizedStrings)
                {
                    _localizedStrings = localizedStrings;
                    _currentIndex     = -1;
                }

                virtual bool MoveNext()
                {
                    if (_currentIndex >= _localizedStrings->StringsCount) //prevents _currentIndex from overflowing.
                    {
                        return false;
                    }
                    _currentIndex++;
                    return _currentIndex < _localizedStrings->StringsCount;
                }

                property KeyValuePair<CultureInfo^, String^> Current
                {
                    virtual KeyValuePair<CultureInfo^, String^> get();
                }

                property Object^ Current2
                {
                    virtual Object^ get() sealed =
                    System::Collections::IEnumerator::Current::get;
                }

                virtual void Reset()
                {
                    _currentIndex = -1;
                }

                ~LocalizedStringsEnumerator(){}
            };

            virtual IEnumerator<KeyValuePair<CultureInfo^, String^>>^ GetEnumerator()
            {
                return gcnew LocalizedStringsEnumerator(this);
            }

            //#region IEnumerable Members
            virtual System::Collections::IEnumerator^ GetEnumerator2() sealed = System::Collections::IEnumerable::GetEnumerator
            {
                return GetEnumerator();
            }
    };    

}}}}//MS::Internal::Text::TextInterface

#endif
