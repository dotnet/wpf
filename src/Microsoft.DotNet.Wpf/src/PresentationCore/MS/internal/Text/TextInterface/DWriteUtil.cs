// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface
{
    internal static class DWriteUtil
    {
        private const int COR_E_INVALIDOPERATION = 0x1509;
        private const int DWRITE_E_FILENOTFOUND = unchecked((int)0x88985003L);
        private const int DWRITE_E_FILEACCESS = unchecked((int)0x88985004L);
        private const int DWRITE_E_FILEFORMAT = unchecked((int)0x88985000L);

        internal static void ConvertHresultToException(int hr)
        {

            if (hr != 0)
            {
                if (hr == DWRITE_E_FILENOTFOUND)
                {
                    throw new System.IO.FileNotFoundException();
                }
                else if (hr == DWRITE_E_FILEACCESS)
                {
                    throw new System.UnauthorizedAccessException();
                }
                else if (hr == DWRITE_E_FILEFORMAT)
                {
                    throw new System.IO.FileFormatException();
                }
                else
                {
                    SanitizeAndThrowIfKnownException(hr);

                    // ThrowExceptionForHR method returns an exception based on the IErrorInfo of 
                    // the current thread if one is set. When this happens, the errorCode parameter 
                    // is ignored.
                    // We pass an IntPtr that has a value of -1 so that ThrowExceptionForHR ignores 
                    // IErrorInfo of the current thread.
                    System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hr, new System.IntPtr(-1));
                }
            }
        }

        /// <summary>
        /// Exceptions known to have security sensitive data are sanitized in this method,
        /// by throwing a copy of the original exception without security sensitive data. 
        /// Or, to put another way - this function acts only on a list of security sensitive HRESULT/IErrorInfo combinations, throwing for matches.
        /// The IErrorInfo is taken into account in a call to GetExceptionForHR(HRESULT), see MSDN for more details.
        /// </summary>

        private static void SanitizeAndThrowIfKnownException(int hr)
        {
            if (hr == COR_E_INVALIDOPERATION)
            {
                System.Exception e = System.Runtime.InteropServices.Marshal.GetExceptionForHR(hr);
                if (e is System.Net.WebException)
                {
                    throw e;
                }
            }
        }
    }
}
