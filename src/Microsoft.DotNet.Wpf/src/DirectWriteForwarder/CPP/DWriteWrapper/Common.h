// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __COMMON_H
#define __COMMON_H

#include "precomp.hxx"

using namespace System::Security;

namespace MS { namespace Internal { namespace Text { namespace TextInterface { namespace Native
{    
#include "DWrite.h"
#include "CorError.h"

private ref class Util sealed
{

public:

    __declspec(noinline) void static ConvertHresultToException(HRESULT hr)
    {
        
        if (FAILED(hr))
        {
            if (hr == DWRITE_E_FILENOTFOUND) 
            {
                throw gcnew System::IO::FileNotFoundException(); 
            }
            else if (hr == DWRITE_E_FILEACCESS) 
            {
                throw gcnew System::UnauthorizedAccessException(); 
            }
            else if (hr == DWRITE_E_FILEFORMAT)
            {
                throw gcnew System::IO::FileFormatException(); 
            }         
            else 
            {
                SanitizeAndThrowIfKnownException(hr);

                // ThrowExceptionForHR method returns an exception based on the IErrorInfo of 
                // the current thread if one is set. When this happens, the errorCode parameter 
                // is ignored.
                // We pass an IntPtr that has a value of -1 so that ThrowExceptionForHR ignores 
                // IErrorInfo of the current thread.
                System::Runtime::InteropServices::Marshal::ThrowExceptionForHR(hr, System::IntPtr(-1));
            }
        }
    }

    __declspec(noinline) const cli::interior_ptr<const System::Char> static GetPtrToStringChars(System::String^ s)
    {
        return CriticalPtrToStringChars(s);
    }


    /// <summary>
    /// The implementation of this method is taken from this msdn article:
    /// http://msdn.microsoft.com/en-us/library/wb8scw8f(VS.100).aspx
    /// </summary>
    __declspec(noinline) static _GUID ToGUID( System::Guid& guid ) 
    {
       array<System::Byte>^ guidData = guid.ToByteArray();
       pin_ptr<System::Byte> data = &(guidData[ 0 ]);

       return *(_GUID *)data;
    }

private:

    /// <summary>
    /// Exceptions known to have security sensitive data are sanitized in this method,
    /// by creating copy of original exception without sensitive data and re-thrown.
    /// Or, to put another way - this funciton acts only on a known whitelist of HRESULT/IErrorInfo combinations, throwing for matches.
    /// The IErrorInfo is taken into account in a call to GetExceptionForHR(HRESULT), see MSDN for more details.
    /// </summary>

    void static SanitizeAndThrowIfKnownException(HRESULT hr)
    {
        if (hr == COR_E_INVALIDOPERATION)
        {
            System::Exception^ e = System::Runtime::InteropServices::Marshal::GetExceptionForHR(hr);
            if (dynamic_cast<System::Net::WebException^>(e) != nullptr)                
            {
                throw e;
            }
        }
    }

    /// <summary>
    /// Checks if the caller is in full trust mode.
    /// </summary>
    static bool IsFullTrustCaller()
    {
        return true;
    }
};

#define ConvertHresultToException(hr, msg) Native::Util::ConvertHresultToException(hr)

}}}}}//MS::Internal::Text::TextInterface::Native

using namespace MS::Internal::Text::TextInterface::Native;

#endif //__COMMON_H
