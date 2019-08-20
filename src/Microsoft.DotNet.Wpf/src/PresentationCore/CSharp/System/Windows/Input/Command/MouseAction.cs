// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: The MouseAction class is used to create Gestures for Mouse Device 
//
//              See spec at : http://avalon/coreUI/Specs/Commanding%20--%20design.htm 
// 
//
//

using System;
using System.ComponentModel;
using System.Windows.Markup;

namespace System.Windows.Input
{
    /// <summary>
    /// Mouse Action Enumeration
    /// </summary>
    [TypeConverter(typeof(MouseActionConverter))]
    [ValueSerializer(typeof(MouseActionValueSerializer))]
    public enum MouseAction : byte
    {
        /// <summary>
        /// None
        /// </summary>
        None, 
        /// <summary>
        /// LeftClick
        /// </summary>
        LeftClick, 
        /// <summary>
        /// RightClick
        /// </summary>
        RightClick, 
        /// <summary>
        /// MiddleClick
        /// </summary>
        MiddleClick, 
        /// <summary>
        /// WheelClick
        /// </summary>
        WheelClick,
        /// <summary>
        /// LeftDoubleClick
        /// </summary>
        LeftDoubleClick, 
        /// <summary>
        /// RightDoubleClick
        /// </summary>
        RightDoubleClick, 
        /// <summary>
        /// MiddleDoubleClick
        /// </summary>
        MiddleDoubleClick
    }
}
