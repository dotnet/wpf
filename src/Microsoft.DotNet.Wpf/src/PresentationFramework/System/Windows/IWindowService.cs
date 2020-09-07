// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Defines the basic window interface for desktop and browser cases
//
//

using System;
using System.ComponentModel;

using System.Windows;
using System.Windows.Controls.Primitives;

using System.Windows.Navigation;
using System.Windows.Media;

namespace System.Windows 
{  
    /// <summary>
    /// 
    /// </summary>
    internal interface IWindowService 
    {
        /// <summary>
        /// The data that will be displayed as the title of the window.  Hosts 
        /// are free to display the title in any manner that they want.  For 
        /// example, the browser may display the title set via the Title 
        /// property somewhere besides the caption bar.
        /// </summary>
        string Title
        { 
            get; 
            set; 
        }
                       
        /// <summary>
        /// Height of the host window
        /// </summary>
        double Height
        {
            get;
            set;
        }

        /// <summary>
        /// Width of the host window
        /// </summary>
        double Width
        {
            get;
            set;
        }

        bool UserResized
        {
            get;
        }
    }
}

