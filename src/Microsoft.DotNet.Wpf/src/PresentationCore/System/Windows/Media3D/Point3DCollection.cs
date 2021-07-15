// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: 3D point collection partial class. 
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht 
//

using System.Windows;
using System.Windows.Media.Media3D;
using MS.Internal.PresentationCore; 
using System;
using System.IO; 
using MS.Internal.Media; 

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// Point3D - 3D point representation. 
    /// Defaults to (0,0,0).
    /// </summary>
    public partial class Point3DCollection
    {
        ///<summary>
        ///  Deserialize this object from  BAML binary format.
        ///</summary>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal static object DeserializeFrom(BinaryReader reader)
        {
            // Get the size.
            uint count = reader.ReadUInt32() ; 
            
            Point3DCollection collection = new Point3DCollection( (int) count) ; 
            
            for ( uint i = 0; i < count ; i ++ ) 
            {
                Point3D point = new Point3D(
                                             XamlSerializationHelper.ReadDouble( reader ), 
                                             XamlSerializationHelper.ReadDouble( reader ) , 
                                             XamlSerializationHelper.ReadDouble( reader ) ) ; 

                collection.Add( point );                 
            }

            return collection ; 
        }
}
}
