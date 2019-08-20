// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;

namespace MS.Internal.FontCache
{
    ///<summary>
    /// This class defines font-cache related constants used by the font-cache code.
    ///
    /// Since the compiler optimizes these constants away from asmmeta file in the release
    /// build, we put them in a separate file.  FontCacheConstants is compiled in core
    /// and again in FontCache to ensure that both the client code and the server code
    /// can access them.
    /// 
    /// This class will not be duplicated in the debug build because we explicitly exclude it
    /// from the asmmeta file.
    ///</summary>
    internal static class FontCacheConstants
    {
        // Cache sizes for shared and local cache:
        internal const int InitialSharedCacheSize = 1024 * 1024 * 4;
        internal const int MaximumSharedCacheSize = 1024 * 1024 * 64;

        internal const int InitialLocalCacheSize = 1024 * 1024 * 4;
        internal const int MaximumLocalCacheSize = 1024 * 1024 * 64;

        internal const int CacheGrowthFactor = 2;

        //Messages to send to the font cache server
        internal const int GetCacheNameMessage = 0;    //Indicates a request to get the server cache name
        internal const int SendMissReportMessage = 1;  //Indicates that a miss report is being sent
        internal const int ServerShutdownMessage = 2;  //Indicates a request for the server to shutdown.
}
}
