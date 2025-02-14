// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: 
//    RMEnrollmentPage1 is page 1 of the RM enrollment wizard.
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

            _label1.Text = SR.RMEnrollmentPage1a;
            _label2.Text = SR.RMEnrollmentPage1b;
            _label3.Text = SR.RMEnrollmentPage1c;
            _privacyLabel.Text = SR.RMEnrollmentPage1d;
            _instructionlabel.Text = SR.RMEnrollmentPage1e;
            _nextButton.Text = SR.RMEnrollmentNext;
            _cancelButton.Text = SR.RMEnrollmentCancel;
            Text = SR.RMEnrollmentTitle;
        }

        #endregion Protected Methods

    }
}