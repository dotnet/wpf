// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Readability enum used by LocalizabilityAttribute 
//

#if PBTCOMPILER
namespace MS.Internal.Globalization
#else
namespace System.Windows
#endif
{
    /// <summary>
    /// Readability of the attribute's targeted value in Baml
    /// </summary>
    // Less restrictive value has a higher numeric value
    // NOTE: Enum values must be made in sync with the enum parsing logic in 
    // Framework/MS/Internal/Globalization/LocalizationComments.cs    
#if PBTCOMPILER
    internal enum Readability 
#else
    public enum Readability 
#endif    
    {
        /// <summary>
        /// Targeted value is not readable.
        /// </summary>
        Unreadable = 0,

        /// <summary>
        /// Targeted value is readable text.
        /// </summary>
        Readable   = 1,

        /// <summary>
        /// Targeted value's readability inherites from parent nodes.
        /// </summary>
        Inherit    = 2,            
    }
}

