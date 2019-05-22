// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
//
// Description:
//  This is an internal class that enables interactions with Zip archives
//  for OPC scenarios 
//
//
//
//

using System;
using System.Collections.Generic;
using System.Collections;
    
namespace MS.Internal.IO.Zip
{
    /// <summary>
    /// The only reason for existence of this class is to restrict operations that caller of the 
    /// ZipArchive.GetFiles is allowed to perform. We want to prevent any modifications to the 
    /// actual collection of the FileItems as it is supposed to be a read-only data structure. 
    /// Although this is an internal API it seems that the safeguards are warranted.
    /// </summary>
    internal class ZipFileInfoCollection : IEnumerable
    {
        //------------------------------------------------------
        //
        // Internal NON API Constructor (this constructor is marked as internal 
        // and isNOT part of the ZIP IO API surface 
        //
        //------------------------------------------------------
        internal ZipFileInfoCollection(ICollection zipFileInfoCollection)
        {
            _zipFileInfoCollection = zipFileInfoCollection;
        }

        //------------------------------------------------------
        //
        // Internal API Methods (although these methods are marked as 
        // Internal they are part of the internal ZIP IO API surface 
        //
        //------------------------------------------------------
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _zipFileInfoCollection.GetEnumerator();
        }

        //------------------------------------------------------
        //
        //  Private Fields 
        //
        //------------------------------------------------------        
        private ICollection _zipFileInfoCollection;
    }
}
