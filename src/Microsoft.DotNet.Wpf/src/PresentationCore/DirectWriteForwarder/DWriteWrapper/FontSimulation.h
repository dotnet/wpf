// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTSIMULATIONS_H
#define __FONTSIMULATIONS_H
  
namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    // Compatibility warning:
    /************************/
    // This enum has a mirror enum CSharp\System\Windows\Media\StyleSimulations.cs
    // If any changes happens to FontSimulation they should be reflected
    // in StyleSimulations.cs

    /// <summary>
    /// Specifies algorithmic style simulations to be applied to the font face.
    /// Bold and oblique simulations can be combined via bitwise OR operation.
    /// </summary>
    [System::FlagsAttribute]
    private enum class FontSimulations
    {
        /// <summary>
        /// No simulations are performed.
        /// </summary>
        None    = 0x0000,

        /// <summary>
        /// Algorithmic emboldening is performed.
        /// </summary>
        Bold    = 0x0001,

        /// <summary>
        /// Algorithmic italicization is performed.
        /// </summary>
        Oblique = 0x0002
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__FONTSIMULATIONS_H