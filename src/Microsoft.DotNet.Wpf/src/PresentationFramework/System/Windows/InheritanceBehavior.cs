// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
*  Modes used for inheritance property lookup as well as resource lookup.
*
*
\***************************************************************************/
namespace System.Windows
{
    /// <summary>
    /// Modes used for inheritance property lookup as well as resource lookup
    /// </summary>
    public enum InheritanceBehavior
    {
        /// <summary>
        /// 1. Inheritable property lookup will query the current element and further.
        /// 2. Resource lookup will query through the current element and further.
        /// </summary>
        Default = 0,

        /// <summary>
        /// 1. Inheritable property lookup will not query the current element or any further.
        /// 2. Resource lookup will not query the current element but will skip over to the app and theme dictionaries.
        /// </summary>
        SkipToAppNow = 1,
        
        /// <summary>
        /// 1. Inheritable property lookup will query the current element but not any further.
        /// 2. Resource lookup will query the current element and will then skip over to the app and theme dictionaries.
        /// </summary>
        SkipToAppNext = 2,

        /// <summary>
        /// 1. Inheritable property lookup will not query the current element or any further.
        /// 2. Resource lookup will not query the current element but will skip over to the theme dictionaries.
        /// </summary>
        SkipToThemeNow = 3,

        /// <summary>
        /// 1. Inheritable property lookup will query the current element but not any further.
        /// 2. Resource lookup will query the current element and will then skip over to the theme dictionaries.
        /// </summary>
        SkipToThemeNext = 4,
        
        /// <summary>
        /// 1. Inheritable property lookup will not query the current element or any further.
        /// 2. Resource lookup will not query the current element or any further.
        /// </summary>
        SkipAllNow = 5,
        
        /// <summary>
        /// 1. Inheritable property lookup will query the current element but not any further.
        /// 2. Resource lookup will query the current element but not any further.
        /// </summary>
        SkipAllNext = 6,
    }
}

