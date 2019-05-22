// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Modifiability enum used by LocalizabilityAttribute 
//

#if PBTCOMPILER
namespace MS.Internal.Globalization 
#else
namespace System.Windows
#endif
{
    /// <summary>
    /// Modifiability of the attribute's targeted value in baml
    /// </summary>
    // Less restrictive value has a higher numeric value
    // NOTE: Enum values must be made in sync with the enum parsing logic in 
    // Framework/MS/Internal/Globalization/LocalizationComments.cs
#if PBTCOMPILER
    internal enum Modifiability 
#else
    public enum Modifiability
#endif    
    {
        /// <summary>
        /// Targeted value is not modifiable by localizers.
        /// </summary>
        Unmodifiable = 0,

        /// <summary>
        /// Targeted value is modifiable by localizers.
        /// </summary>
        Modifiable   = 1,

        /// <summary>
        /// Targeted value's modifiability inherits from the the parent nodes.
        /// </summary>
        Inherit      = 2, 
    }
}    

