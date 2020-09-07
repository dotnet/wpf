// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



namespace MS.Internal.Printing.Configuration
{
    /// <remarks>
    /// From http://msdn.microsoft.com/en-us/library/cc244659(PROT.13).aspx
    /// </remarks>
    internal enum DevModeTrueTypeOption : short
    {
        /// <summary>
        /// Prints TrueType fonts as graphics.
        /// </summary>
        DMTT_BITMAP = 1,

        /// <summary>
        /// Downloads TrueType fonts as soft fonts. 
        /// </summary>
        DMTT_DOWNLOAD = 2,

        /// <summary>
        /// Substitutes device fonts for TrueType fonts.
        /// </summary>
        DMTT_SUBDEV = 3,

        /// <summary>
        /// Window 95/98/Me, Windows NT 4.0 and later: Downloads TrueType fonts as outline soft fonts.
        /// </summary>
        DMTT_DOWNLOAD_OUTLINE = 4
}
}