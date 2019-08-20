// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTLIST_H
#define __FONTLIST_H

#include "Common.h"
#include "Font.h"
#include "LocalizedErrorMsgs.h"
#include "NativePointerWrapper.h"

using namespace MS::Internal::Text::TextInterface::Generics;

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /*******************************************************************************************************************************/
    //Forward declaration of FontCollection since there was a circular reference between "FontList", "FontCollection" & "FontFamily"
    ref class FontCollection;
    /*******************************************************************************************************************************/

    using namespace System;

    using namespace System::Collections::Generic;

    /// <summary>
    /// Represents a list of fonts.
    /// </summary>
    private ref class FontList : System::Collections::Generic::IEnumerable<Font^>
    {
        private:

            /// <summary>
            /// A pointer to the DWrite font list.
            /// </summary>
            NativeIUnknownWrapper<IDWriteFontList>^ _fontList;

        protected:

            /// <summary>
            /// Gets a pointer to the DWrite font list object.
            /// </summary>            
            property NativeIUnknownWrapper<IDWriteFontList>^ FontListObject
            {
                NativeIUnknownWrapper<IDWriteFontList>^ get();
            }

        internal:

            /// <summary>
            /// Constructs a Font List object.
            /// </summary>
            /// <param name="fontList">A pointer to the DWrite font list object.</param>
            FontList(IDWriteFontList* fontList);

            /// <summary>
            /// Gets a font given its zero-based index.
            /// </summary>
            property Font^ default[UINT32]
            {
                Font^ get(UINT32);
            }

            /// <summary>
            /// Gets the count of fonts in the list.
            /// </summary>
            property UINT32 Count
            {
                UINT32 get();
            }
            
            /// <summary>
            /// Gets the font collection that contains the fonts.
            /// </summary>
            property FontCollection^ FontsCollection
            {
                FontCollection^ get();
            }

        public:

            /*************************IEnumerable<KeyValuePair<ushort,double>> Members***********************************/           
            ref struct FontsEnumerator : public IEnumerator<Font^>
            {                
                FontList^ _fontList;
                INT64     _currentIndex;

                FontsEnumerator(FontList^ fontList)
                {
                    _fontList     = fontList;
                    _currentIndex = -1;
                }

                virtual bool MoveNext()
                {
                    if (_currentIndex >= _fontList->Count) //prevents _currentIndex from overflowing.
                    {
                        return false;
                    }
                    _currentIndex++;
                    return _currentIndex < _fontList->Count;
                }

                property Font^ Current
                {
                    virtual Font^ get()
                    {
                        if (_currentIndex >= _fontList->Count)
                        {
                            throw gcnew System::InvalidOperationException(LocalizedErrorMsgs::EnumeratorReachedEnd);
                        }
                        else if (_currentIndex == -1)
                        {
                            throw gcnew System::InvalidOperationException(LocalizedErrorMsgs::EnumeratorNotStarted);
                        }
                        return _fontList[(UINT32)_currentIndex];
                    }
                }

                property Object^ Current2
                {
                    virtual Object^ get() sealed =
                    System::Collections::IEnumerator::Current::get
                    {
                        return Current;
                    }
                }

                virtual void Reset()
                {
                    _currentIndex = -1;
                }

                ~FontsEnumerator(){}
            };

            virtual IEnumerator<Font^>^ GetEnumerator()
            {
                return gcnew FontsEnumerator(this);
            }           
            
            virtual System::Collections::IEnumerator^ GetEnumerator2() sealed = 
            System::Collections::IEnumerable::GetEnumerator
            {
                return gcnew FontsEnumerator(this);
            }

            /************************************************************************************************************/

    };

}}}}//MS::Internal::Text::TextInterface

#endif //__FONTLIST_H
