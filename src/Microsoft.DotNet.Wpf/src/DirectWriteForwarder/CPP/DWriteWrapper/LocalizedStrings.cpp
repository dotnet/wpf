// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "LocalizedStrings.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// Constructs a LocalizedStrings object.
    /// </summary>
    /// <param name="localizedStrings">The DWrite localized Strings object that 
    /// this class wraps.</param>
    LocalizedStrings::LocalizedStrings(IDWriteLocalizedStrings* localizedStrings)
    {
        _localizedStrings = gcnew NativeIUnknownWrapper<IDWriteLocalizedStrings>(localizedStrings);
        _keys   = nullptr;
        _values = nullptr;
    }

    /// <summary>
    /// Constructs a LocalizedStrings object.
    /// </summary>
    /// <param name="localizedStrings">The DWrite localized Strings object that 
    /// this class wraps.</param>
    __declspec(noinline) LocalizedStrings::LocalizedStrings()
    {
        _localizedStrings = nullptr;
        _keys   = nullptr;
        _values = nullptr;
    }

    /// <summary>
    /// Gets the number of language/string pairs.
    /// </summary>
    __declspec(noinline) UINT32 LocalizedStrings::StringsCount::get()
    {
        UINT32 count = (_localizedStrings != nullptr)? _localizedStrings->Value->GetCount() : 0;
        System::GC::KeepAlive(_localizedStrings);
        return count;
    }

    int LocalizedStrings::Count::get()
    {
        unsigned int count = StringsCount;

        if (count > (unsigned int)Int32::MaxValue)
        {
            throw gcnew OverflowException("The number of elements is greater than System.Int32.MaxValue");
        }

        return (int)count;
    }

    /// <Summary>
    /// Lazily allocate the keys.
    /// </Summary>
    ICollection<CultureInfo^>^ LocalizedStrings::Keys::get()
    {
        return (ICollection<CultureInfo^>^)KeysArray;
    }

    /// <summary>
    /// Gets an array of the CultureInfos stored by the localizedStrings object.
    /// </summary>
    array<CultureInfo^>^ LocalizedStrings::KeysArray::get()
    {
        // Lazily allocate the keys.
        if (_keys == nullptr)
        {
            _keys = gcnew array<CultureInfo^>(StringsCount);
            for(unsigned int i = 0; i < StringsCount; i++)
            {
                _keys[i] = gcnew CultureInfo(GetLocaleName(i));
            }
        }
        return _keys;
    }
    
    /// <Summary>
    /// Lazily allocate the values.
    /// </Summary>
    ICollection<String^>^ LocalizedStrings::Values::get()
    {
        return (ICollection<String^>^)ValuesArray;
    }

    /// <summary>
    /// Gets an array of the string values stored by the localizedStrings object.
    /// </summary>
    array<String^>^ LocalizedStrings::ValuesArray::get()
    {
        if (_values == nullptr)
        {
            _values = gcnew array<String^>(StringsCount);
            
            for(unsigned int i = 0; i < StringsCount; i++)
            {
                _values[i] = GetString(i);
            }
        }

        return _values;
    }

    __declspec(noinline) KeyValuePair<CultureInfo^, String^> LocalizedStrings::LocalizedStringsEnumerator::Current::get()
    {
        if (_currentIndex >= _localizedStrings->StringsCount)
        {
            throw gcnew System::InvalidOperationException(LocalizedErrorMsgs::EnumeratorReachedEnd);
        }
        else if (_currentIndex == -1)
        {
            throw gcnew System::InvalidOperationException(LocalizedErrorMsgs::EnumeratorNotStarted);
        }

        array<CultureInfo^>^ keys = _localizedStrings->KeysArray;
        array<String^>^ values = _localizedStrings->ValuesArray;
        KeyValuePair<CultureInfo^, String^> current = KeyValuePair<CultureInfo^, String^>(keys[(UINT32)_currentIndex], values[(UINT32)_currentIndex]);
        return current;
    }

    Object^ LocalizedStrings::LocalizedStringsEnumerator::Current2::get()
    {
        return Current;
    }

    /// <summary>
    /// Gets the index of the item with the specified locale name.
    /// </summary>
    /// <param name="localeName">Locale name to look for.</param>
    /// <param name="index">Receives the zero-based index of the locale name/string pair.</param>
    /// <returns>TRUE if the locale name exists or FALSE if not.</returns>
    __declspec(noinline) bool LocalizedStrings::FindLocaleName(
                                                                                 System::String^ localeName,
                                         [System::Runtime::InteropServices::Out] UINT32%         index
                                         )
    {
        if (_localizedStrings == nullptr)
        {
            index = 0;
            return false;
        }
        else
        {
            pin_ptr<const wchar_t> localeNameWChar = PtrToStringChars(localeName);
            BOOL exists = FALSE;
            UINT32 localeNameIndex = 0;
            HRESULT hr = _localizedStrings->Value->FindLocaleName(
                                                          localeNameWChar,
                                                          &localeNameIndex,
                                                          &exists
                                                          );
            System::GC::KeepAlive(_localizedStrings);
            ConvertHresultToException(hr, "bool LocalizedStrings::FindLocaleName");
            index = localeNameIndex;
            return (!!exists);
        }
    }

    /// <summary>
    /// Gets the length in characters (not including the null terminator) of the locale name with the specified index.
    /// </summary>
    /// <param name="index">Zero-based index of the locale name.</param>
    /// <returns>The length in characters, not including the null terminator.</returns>
    __declspec(noinline) UINT32 LocalizedStrings::GetLocaleNameLength(
                                                UINT32 index
                                                )
    {
        if (_localizedStrings == nullptr)
        {
            return 0;
        }
        else
        {
            UINT32 length = 0;
            HRESULT hr = _localizedStrings->Value->GetLocaleNameLength(
                                                               index,
                                                               &length
                                                               );
            System::GC::KeepAlive(_localizedStrings);
            ConvertHresultToException(hr, "UINT32 LocalizedStrings::GetLocaleNameLength");
            return length;
        }
    }

    /// <summary>
    /// Gets the locale name with the specified index.
    /// </summary>
    /// <param name="index">Zero-based index of the locale name.</param>
    /// <returns>The locale name.</returns>
    __declspec(noinline) System::String^ LocalizedStrings::GetLocaleName(
                                                   UINT32 index
                                                   )
    {        
        if (_localizedStrings == nullptr)
        {
            return System::String::Empty;
        }
        else
        {
            UINT32 localeNameLength = this->GetLocaleNameLength(index);
            MS::Internal::Invariant::Assert(localeNameLength >= 0 && localeNameLength < UINT_MAX);
            WCHAR* localeNameWCHAR = NULL;
            try
            {
                localeNameWCHAR = new WCHAR[localeNameLength + 1];
                HRESULT hr = _localizedStrings->Value->GetLocaleName(
                                                             index,
                                                             localeNameWCHAR,
                                                             localeNameLength + 1
                                                             );
                System::GC::KeepAlive(_localizedStrings);
                ConvertHresultToException(hr, "System::String^ LocalizedStrings::GetLocaleName");
                System::String^ localeName = gcnew System::String(localeNameWCHAR);
                return localeName;
            }
            finally
            {
                if (localeNameWCHAR != NULL)
                {
                    delete[] localeNameWCHAR;
                }
            }
        }
    }

    /// <summary>
    /// Gets the length in characters (not including the null terminator) of the string with the specified index.
    /// </summary>
    /// <param name="index">Zero-based index of the string.</param>
    /// <returns>The length in characters, not including the null terminator.</returns>
    __declspec(noinline) UINT32 LocalizedStrings::GetStringLength(
                                            UINT32 index
                                            )
    {
        if (_localizedStrings == nullptr)
        {
            return 0;
        }
        else
        {
            UINT32 length = 0;
            HRESULT hr = _localizedStrings->Value->GetStringLength(
                                                           index,
                                                           &length
                                                           );
            System::GC::KeepAlive(_localizedStrings);
            ConvertHresultToException(hr, "UINT32 LocalizedStrings::GetStringLength");
            return length;
        }
    }

    /// <summary>
    /// Gets the string with the specified index.
    /// </summary>
    /// <param name="index">Zero-based index of the string.</param>
    /// <returns>The string.</returns>
    __declspec(noinline) System::String^ LocalizedStrings::GetString(
                                               UINT32 index
                                               )
    {        
        if (_localizedStrings == nullptr)
        {
            return System::String::Empty;
        }
        else
        {
            UINT32 stringLength = this->GetStringLength(index);
            MS::Internal::Invariant::Assert(stringLength >= 0 && stringLength < UINT_MAX);
            WCHAR* stringWCHAR = NULL;
            
            try
            {
                stringWCHAR = new WCHAR[stringLength + 1];
                HRESULT hr = _localizedStrings->Value->GetString(
                                                         index,
                                                         stringWCHAR,
                                                         stringLength + 1
                                                         );
                System::GC::KeepAlive(_localizedStrings);
                ConvertHresultToException(hr, "System::String^ LocalizedStrings::GetString");
                System::String^ string = gcnew System::String(stringWCHAR);
                return string;
            }
            finally
            {
                if (stringWCHAR != NULL)
                {
                    delete[] stringWCHAR;
                }
            }
        }
    }

}}}}//MS::Internal::Text::TextInterface
