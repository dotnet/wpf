// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Input 
{
    /// <summary>
    ///     An enumeration of the supported cursor types.
    /// </summary>
    public enum CursorType : int
    {
        /// <summary>
        ///     a value indicating that no cursor should be displayed at all.
        /// </summary>
        None = 0,        

        /// <summary>
        ///     Standard No Cursor.
        /// </summary>
        No = 1,        

        /// <summary>
        ///     Standard arrow cursor.
        /// </summary>
        Arrow = 2,

        /// <summary>
        ///     Standard arrow with small hourglass cursor.
        /// </summary>
        AppStarting = 3,

        /// <summary>
        ///     Crosshair cursor.
        /// </summary>        
        Cross = 4,

        /// <summary>
        ///     Help cursor.
        /// </summary>        	
        Help = 5,

        /// <summary>
        ///     Text I-Beam cursor.
        /// </summary>
        IBeam = 6,        

        /// <summary>
        ///     Four-way pointing cursor.
        /// </summary>
        SizeAll = 7,

        /// <summary>
        ///     Double arrow pointing NE and SW.
        /// </summary>
        SizeNESW = 8,

        /// <summary>
        ///     Double arrow pointing N and S.
        /// </summary>
        SizeNS = 9,

        /// <summary>
        ///     Double arrow pointing NW and SE.
        /// </summary>
        SizeNWSE = 10,

        /// <summary>
        ///     Double arrow pointing W and E.
        /// </summary>
        SizeWE = 11,

        /// <summary>
        ///     Vertical arrow cursor.
        /// </summary>
        UpArrow = 12,        

        /// <summary>
        ///     Hourglass cursor.
        /// </summary>
        Wait = 13,

        /// <summary>
        ///     Hand cursor.
        /// </summary>
        Hand = 14,

        /// <summary>
        /// PenCursor
        /// </summary>
        Pen = 15,

        /// <summary>
        /// ScrollNSCursor
        /// </summary>
        ScrollNS = 16,

        /// <summary>
        /// ScrollWECursor
        /// </summary>
        ScrollWE = 17,

        /// <summary>
        /// ScrollAllCursor
        /// </summary>
        ScrollAll = 18,

        /// <summary>
        /// ScrollNCursor
        /// </summary>
        ScrollN = 19,

        /// <summary>
        /// ScrollSCursor
        /// </summary>
        ScrollS = 20,

        /// <summary>
        /// ScrollWCursor
        /// </summary>
        ScrollW = 21,

        /// <summary>
        /// ScrollECursor
        /// </summary>
        ScrollE = 22,

        /// <summary>
        /// ScrollNWCursor
        /// </summary>
        ScrollNW = 23,

        /// <summary>
        /// ScrollNECursor
        /// </summary>
        ScrollNE = 24,

        /// <summary>
        /// ScrollSWCursor
        /// </summary>
        ScrollSW = 25,

        /// <summary>
        /// ScrollSECursor
        /// </summary>
        ScrollSE = 26,

        /// <summary>
        /// ArrowCDCursor
        /// </summary>
        ArrowCD = 27

        // Update the count in Cursors.cs file if there is a new addition here.
    };
}

