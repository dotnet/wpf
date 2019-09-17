// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: FloaterBaseParaClient class: Base para client class
//              for floaters and UIElements 
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;
using MS.Internal.Documents;
using MS.Internal.Text;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // FloaterBaseParaClient class: base class for floater and UIElement
    // para clients
    // ----------------------------------------------------------------------
    internal abstract class FloaterBaseParaClient : BaseParaClient
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        // ------------------------------------------------------------------
        // Constructor.
        //
        //      paragraph - Paragraph associated with this object.
        // ------------------------------------------------------------------
        protected FloaterBaseParaClient(FloaterBaseParagraph paragraph)
            : base(paragraph)
        {
        }

        #endregion Constructors
        
        // ------------------------------------------------------------------
        // Arrange floater
        //
        //      rcFloater - rectangle of the floater
        //      rcHostPara - rectangle of the host text paragraph.
        //      fswdirParent- flow direction of parent
        //      pageContext - page context
        // ------------------------------------------------------------------
        internal virtual void ArrangeFloater(PTS.FSRECT rcFloater, PTS.FSRECT rcHostPara, uint fswdirParent, PageContext pageContext)
        {
        }
                 
        // ------------------------------------------------------------------
        // Return TextContentRange for the content of the paragraph.
        // ------------------------------------------------------------------
        internal override abstract TextContentRange GetTextContentRange();
    }
}
