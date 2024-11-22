// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

#if PBTCOMPILER
namespace MS.Internal.Markup
#elif SYSTEM_XAML
namespace System.Xaml
#else

namespace MS.Internal
#endif
{
    internal static class CriticalExceptions
    {
        // these are all the exceptions considered critical by PreSharp
        internal static bool IsCriticalException(Exception ex)
        {
            ex = Unwrap(ex);

            return ex is NullReferenceException ||
                   ex is StackOverflowException ||
                   ex is OutOfMemoryException   ||
                   ex is System.Threading.ThreadAbortException ||
                   ex is System.Runtime.InteropServices.SEHException ||
                   ex is System.Security.SecurityException;
        }

        // these are exceptions that we should treat as critical when they
        // arise during callbacks into application code
        #if !PBTCOMPILER && !SYSTEM_XAML
        internal static bool IsCriticalApplicationException(Exception ex)
        {
            ex = Unwrap(ex);

            return ex is StackOverflowException ||
                   ex is OutOfMemoryException   ||
                   ex is System.Threading.ThreadAbortException ||
                   ex is System.Security.SecurityException;
        }
        #endif

        internal static Exception Unwrap(Exception ex)
        {
            // for certain types of exceptions, we care more about the inner
            // exception
            while (ex.InnerException != null &&
                    (   ex is System.Reflection.TargetInvocationException
                    ))
            {
                ex = ex.InnerException;
            }

            return ex;
        }
    }
}
