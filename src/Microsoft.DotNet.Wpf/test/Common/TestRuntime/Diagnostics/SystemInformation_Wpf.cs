// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Test.Win32;
using System.Globalization;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Data;

namespace Microsoft.Test.Diagnostics
{
    /// <summary>
    /// Provides operating system version information. Partial class to
    /// separate out code that has a link dependency upon WPF.
    /// </summary>
    public partial class SystemInformation
    {
        /// <summary>
        /// Gets the value indicating the current version of WPF.
        /// </summary>
        public static Version WpfVersion
        {
            get
            {
                if (Environment.Version.Major == 2)
                {
                    // As part of the 3.5 release, the property "IsDocumentEnabled"
                    // was added to RichTextBox. Therefore, if we can't find
                    // the property, we know the version is 3.0
                    if (typeof(RichTextBox).GetProperty("IsDocumentEnabled") == null)
                    {
                        return WpfVersions.Wpf30;
                    }
                    // If we aren't 3.0, then we're 3.5 at the moment. When we move
                    // past 3.5, then we can add a check for a property not in 3.5
                    // but that is added in that subsequent version.
                    else
                    {
                        return WpfVersions.Wpf35;
                    }
                }
                else if (Environment.Version.Major == 4)
                {
                    // As part of the 4.5 release, the property "Delay"
                    // is being added to BindingGroup in System.Windows.Data
                    // Therefore, if we can't find the property,
                    //  we know the version is 4.0
                    if(typeof(BindingBase).GetProperty("Delay") == null)
                    {
                        return WpfVersions.Wpf40;
                    }
                    else
                    {
                        return WpfVersions.Wpf45;
                    }
                }
                // What should we do if we run on CLR != 2 and != 4?  
                else
                {
                    return WpfVersions.Wpf45;
                }

            }
        }
    }

    /// <summary>
    /// Provides WPF Versions
    /// </summary>
    public static class WpfVersions
    {
        /// <summary>
        /// Initial release of WPF, shipped with Vista.
        /// </summary>
        public static Version Wpf30
        {
            get { return new Version(3, 0); }
        }

        /// <summary>
        /// Version of WPF as a part of the Orcas release.
        /// </summary>
        public static Version Wpf35
        {
            get { return new Version(3, 5); }
        }

        /// <summary>
        /// Version of WPF as a part of the .NET 4.0
        /// </summary>
        public static Version Wpf40
        {
            get { return new Version(4, 0); }
        }

        /// <summary>
        /// Version of WPF as a part of the .NET 4.5
        /// </summary>
        public static Version Wpf45
        {
            get { return new Version(4, 5); }
        }


    }
}
