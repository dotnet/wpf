// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                              
                                                               
                                                                              
    Abstract:
        This is an decleration of the interfacess defining methods needed for Adding DocumentStructure and StoryFragments
                

--*/
using System;
using System.Windows.Documents;
using System.IO.Packaging;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

namespace System.Windows.Xps.Packaging
{
    /// <summary>
    /// This interface declares methods to add document structure.
    /// </summary>
    public interface IDocumentStructureProvider
    {
    #region Public methods
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        XpsStructure
        AddDocumentStructure(
            );

    #endregion Public methods
    }

     /// <summary>
    /// This interface declares methods to add story fragments structure
    /// </summary>
    public interface IStoryFragmentProvider
    {
    #region Public methods
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        XpsStructure
        AddStoryFragment(
            );

    #endregion Public methods
    }
}

