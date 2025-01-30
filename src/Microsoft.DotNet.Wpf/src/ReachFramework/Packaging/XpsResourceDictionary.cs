// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++
                                                                              
                                                              
                                                                              
    Abstract:
        This file contains the definition  and implementation
        for the XpsResourceDictionary class.  This class inherits from
        XpsResource and controls ResourceDictionary specific aspects of
        a resource added to a fixed page.
                                                   
                                                                             
--*/
using System.IO.Packaging;

namespace System.Windows.Xps.Packaging
{
    /// <summary>
    ///
    /// </summary>
    public class XpsResourceDictionary : XpsResource
    {
        #region Constructors

        internal
        XpsResourceDictionary(
            XpsManager    xpsManager,
            INode           parent,
            PackagePart     part
            )
            : base(xpsManager, parent, part)
        {
        }

        #endregion Constructors
    }
}
