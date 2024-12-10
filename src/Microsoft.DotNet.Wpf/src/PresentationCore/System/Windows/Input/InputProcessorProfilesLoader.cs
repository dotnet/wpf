// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Threading;
using MS.Win32;
using Windows.Win32.Foundation;

namespace System.Windows.Input
{
    /// <summary>
    ///  Loads an instance of the Text Services Input Processor Profiles.
    /// </summary>
    internal static class InputProcessorProfilesLoader
    {
        /// <summary>
        ///  Loads an instance of the Text Services Framework.
        /// </summary>
        /// <returns>
        ///  May return <see langword="null"/> if no text services are available.
        /// </returns>
        internal static UnsafeNativeMethods.ITfInputProcessorProfiles? Load()
        {
            Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "Load called on MTA thread!");

            return UnsafeNativeMethods.TF_CreateInputProcessorProfiles(out UnsafeNativeMethods.ITfInputProcessorProfiles obj) == HRESULT.S_OK
                ? obj
                : null;
        }
    }
}
