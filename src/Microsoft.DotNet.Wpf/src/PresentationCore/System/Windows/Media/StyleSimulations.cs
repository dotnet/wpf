// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//
//  Contents:  StyleSimulations declaration
//
//

using System;

namespace System.Windows.Media
{
    // Compatibility warning:
    /************************/
    // This enum has a mirror enum Cpp\DWriteWrapper\FontSimulation.h
    // If any changes happens to StyleSimulations they should be reflected
    // in FontSimulation.h

    /// <summary>
    /// Font style simulation
    /// </summary>
    [Flags]
    public enum StyleSimulations
    {
        /// <summary>
        /// No font style simulation
        /// </summary>
        None                 = 0,
        /// <summary>
        /// Bold style simulation
        /// </summary>
        BoldSimulation       = 1,
        /// <summary>
        /// Italic style simulation
        /// </summary>
        ItalicSimulation     = 2,
        /// <summary>
        /// Bold and Italic style simulation.
        /// </summary>
        BoldItalicSimulation = BoldSimulation | ItalicSimulation
    }
}

