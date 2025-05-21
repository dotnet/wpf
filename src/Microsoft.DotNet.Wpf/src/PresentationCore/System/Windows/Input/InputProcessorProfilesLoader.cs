// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Creates ITfThreadMgr instances, the root object of the Text
//              Services Framework.
//
//

using System.Threading;
using MS.Win32;

namespace System.Windows.Input
{
    //------------------------------------------------------
    //
    //  InputProcessorProfilesLoader class
    //
    //------------------------------------------------------

    /// <summary>
    /// Loads an instance of the Text Services Framework.
    /// </summary>
    internal static class InputProcessorProfilesLoader
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties
        
        /// <summary>
        /// Loads an instance of the Text Services Framework.
        /// </summary>
        /// <returns>
        /// May return null if no text services are available.
        /// </returns>
        internal static UnsafeNativeMethods.ITfInputProcessorProfiles Load()
        {
            UnsafeNativeMethods.ITfInputProcessorProfiles obj;

            Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "Load called on MTA thread!");

            if (UnsafeNativeMethods.TF_CreateInputProcessorProfiles(out obj) == NativeMethods.S_OK)
            {
                return obj;
            }
            return null;
        }

        #endregion Internal Properties
    }
}
