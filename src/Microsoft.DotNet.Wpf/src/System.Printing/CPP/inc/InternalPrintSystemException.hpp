// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __INTERNALPRINTSYSTEMEXCEPTION_HPP__
#define __INTERNALPRINTSYSTEMEXCEPTION_HPP__

/*++                                                 
    Abstract:
        
        Print System exception objects declaration.
                                                         
--*/

#pragma once

namespace System
{
namespace Printing
{
    using namespace System::Security;
    
    private ref class InternalPrintSystemException
    {
        internal:

        InternalPrintSystemException(
            int   lastWin32Error
            );

        // FIX: remove pragma. done to fix compiler error which will be fixed later.
        #pragma warning ( disable:4376 )
        property
        int
        HResult
        {
            int get();
        }

        static
        void
        ThrowIfErrorIsNot(
            int     lastWin32Error,
            int     expectedLastWin32Error
            );

        static
        void
        ThrowIfLastErrorIsNot(
            int     expectedLastWin32Error
            );

        static
        void
        ThrowLastError(
            void
            );

        static
        void
        ThrowIfNotSuccess(
            int     lastWin32Error
            );

        static
		void
		ThrowIfNotCOMSuccess(
		    HRESULT  hresultCode
		    );
        private:
        
        int   hresult;

        static
        const 
        int  defaultWin32ErrorMessageLength = 256;
    };

    }
}
#endif

