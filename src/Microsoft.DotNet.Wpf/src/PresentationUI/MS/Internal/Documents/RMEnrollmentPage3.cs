// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.TrustUI;

namespace MS.Internal.Documents
{
    /// <summary>
    /// RMEnrollmentPage3
    /// </summary>
    internal sealed partial class RMEnrollmentPage3 : DialogBaseForm
    {
        #region Constructors

        /// <summary>
        /// The constructor
        /// </summary>
        internal RMEnrollmentPage3()
        {
        }

        #endregion Constructors

        #region Protected Methods
        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// ApplyResources override.  Called to apply dialog resources.
        /// </summary>
        protected override void ApplyResources()
        {
            base.ApplyResources();
            
            this.label2.Text = SR.Get(SRID.RMEnrollmentPage3b);
            this.label3.Text = SR.Get(SRID.RMEnrollmentPage3c);
            this.Text = SR.Get(SRID.RMEnrollmentTitle);
        }

        #endregion Protected Methods
    }
}