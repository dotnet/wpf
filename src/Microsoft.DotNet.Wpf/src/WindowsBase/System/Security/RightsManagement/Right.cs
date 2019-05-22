// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:  Enumeration of content right that represents the list of
//                  supported XrML rights
//
//
//
//

using System;

namespace System.Security.RightsManagement 
{
    /// <summary>
    /// Right enumeration represents the list of supported XrML rights. 
    /// </summary>
    public enum ContentRight : int
    {
    /// <summary>
    /// View right 
    /// </summary>
            View = 0,

    /// <summary>
    /// Edit right 
    /// </summary>
            Edit = 1,

    /// <summary>
    /// Print right 
    /// </summary>
            Print = 2,

    /// <summary>
    /// Extract right 
    /// </summary>
            Extract = 3,

    /// <summary>
    /// Object model right used to control programming access to the protected content
    /// </summary>
            ObjectModel = 4,

    /// <summary>
    /// Owner right 
    /// </summary>
            Owner = 5,

    /// <summary>
    /// ViewRightsData right 
    /// </summary>
            ViewRightsData = 6, 

    /// <summary>
    /// Forward right 
    /// </summary>
            Forward = 7,

    /// <summary>
    /// Reply right 
    /// </summary>
            Reply = 8,

    /// <summary>
    /// ReplyAll right 
    /// </summary>
            ReplyAll = 9,

    /// <summary>
    /// Sign right 
    /// </summary>
            Sign = 10,

    /// <summary>
    /// DocumentEdit right 
    /// </summary>
            DocumentEdit = 11,

    /// <summary>
    /// Export right 
    /// </summary>
            Export = 12,
    }
}

