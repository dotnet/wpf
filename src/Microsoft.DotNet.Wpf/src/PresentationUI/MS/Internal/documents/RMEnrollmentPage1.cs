// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    RMEnrollmentPage1 is page 1 of the RM enrollment wizard.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.TrustUI;


namespace MS.Internal.Documents
{

    /// <summary>
    /// RMEnrollmentPage1 
    /// </summary>
    internal sealed partial class RMEnrollmentPage1 : DialogBaseForm
    {
        #region Constructors
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// The constructor
        /// </summary>
        internal RMEnrollmentPage1()
        {
            _pictureBox.Visible = false;
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

            _label1.Text = SR.Get(SRID.RMEnrollmentPage1a);
            _label2.Text = SR.Get(SRID.RMEnrollmentPage1b);
            _label3.Text = SR.Get(SRID.RMEnrollmentPage1c);
            _privacyLabel.Text = SR.Get(SRID.RMEnrollmentPage1d);
            _instructionlabel.Text = SR.Get(SRID.RMEnrollmentPage1e);
            _nextButton.Text = SR.Get(SRID.RMEnrollmentNext);
            _cancelButton.Text = SR.Get(SRID.RMEnrollmentCancel);
            Text = SR.Get(SRID.RMEnrollmentTitle);
        }

        #endregion Protected Methods

    }
}