// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                           
    Description:
       Abstract class that provides a common base class for all containers directly accessible from the page level          
--*/

namespace System.Windows.Documents
{
    using System.Collections.Generic;
    using System.Diagnostics;
    
    internal abstract class FixedSOMPageElement :FixedSOMContainer
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------
        
        #region Constructors
        public FixedSOMPageElement(FixedSOMPage page)
        {
            _page = page;
        }
        #endregion Constructors        

        //--------------------------------------------------------------------
        //
        // Public properties
        //
        //---------------------------------------------------------------------
        
        #region Public properties
        public FixedSOMPage FixedSOMPage
        {
            get
            {
                return _page;
            }
        }

        public abstract bool IsRTL
        {
            get;
        }
        #endregion Constructors        
        


        //--------------------------------------------------------------------
        //
        // Protected Fields
        //
        //---------------------------------------------------------------------

        #region Protected Fields
        protected FixedSOMPage _page;
        #endregion Protected Fields
        
    }
}

