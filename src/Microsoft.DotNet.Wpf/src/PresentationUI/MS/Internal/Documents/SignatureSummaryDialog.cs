// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    SignatureSummaryDialog.  This dialog class handles both the Signature Summary
//    dialog and the Request Signature Dialog.  The toggle for changing the dialog is
//    in the constructor (bool showRequestDialog).  Some fields are used for both
//    dialog types and others are only used in one or the other.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Security;
using System.Windows.Forms;
using System.IO.Packaging;
using System.Windows.TrustUI;
using MS.Internal.PresentationUI;

namespace MS.Internal.Documents
{
    internal sealed partial class SignatureSummaryDialog : DialogBaseForm
    {
        #region Constructors
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="IList<SignatureResources>">Ad</param>
        /// <param name="docSigManager">The DocSigManager ref so dialog can call sign method.</param>
        /// <returns></returns>
        internal SignatureSummaryDialog(IList<SignatureResources> signatureResourcesList, 
                                        DocumentSignatureManager docSigManager,
                                        bool showRequestDialog 
                                     )
        {
            //Init private fields
            _documentSignatureManager = docSigManager;

            //Set Dialog Type (Summary or Request)
            _showRequestDialog = showRequestDialog;

            //  Initialize all the UI components
            InitializeDialogType();

            //Show only the buttons appropriate for the current mode.
            if (_showRequestDialog == true)
            {
                Text = SR.Get(SRID.DigSigRequestTitle);

                _buttonSign.Visible = false;
                _buttonViewCert.Visible = false;
                _buttonRequestAdd.Visible = true;
                _buttonRequestDelete.Visible = true;
            }
            else
            {
                Text = SR.Get(SRID.DigSigSumTitle);

                _buttonRequestAdd.Visible = false;
                _buttonRequestDelete.Visible = false;
                _buttonSign.Visible = true;
                _buttonViewCert.Visible = true;
            }

            //Find the signatures applied to this package and add them to the the ListBox
            foreach (SignatureResources signatureResources in signatureResourcesList)
            {
                AddDigSig(signatureResources);
            }

            // Add a handler to update the dialog when signature status changes
            DocumentSignatureManager.Current.SignatureStatusChange +=
                new DocumentSignatureManager.SignatureStatusChangeHandler(OnSignatureStatusChange);
        }

        #endregion Constructors

        #region Private Properties
        //------------------------------------------------------    
        //    
        //  Private Properties
        //    
        //------------------------------------------------------

        /// <summary>
        /// Indicates what width the Listbox has reserved for the Icon.
        /// </summary>
        /// <value></value>
        private int IconWidth
        {
            get
            {
                return _showRequestDialog ? 0 : 35;
            }
        }

        /// <summary>
        /// Indicates what height the Listbox has reserved for the Icon.
        /// </summary>
        /// <value></value>
        private int IconHeight
        {
            get
            {
                return _showRequestDialog ? 0 : 35;
            }
        }

        /// <summary>
        /// Indicates what width the Listbox has reserved for all the 
        /// text (minus icon).
        /// </summary>
        /// <value></value>
        private int RemainingTextWidth
        {
            get { return _listBoxSummary.Width - IconWidth; }
        }

        /// <summary>
        /// Indicates the width reserved for the Subject text.
        /// </summary>
        /// <value></value>
        private int SummaryNameTextWidth
        {
            get { return _showRequestDialog ? 
                         (int)(RemainingTextWidth * 0.20) : 
                         (int)(RemainingTextWidth * 0.60); 
                }
        }

        /// <summary>
        /// Indicates the width reserved for the message text.
        /// </summary>
        /// <value></value>
        private int IntentTextWidth
        {
            get { return    _showRequestDialog ? 
                            (int)((RemainingTextWidth - SummaryNameTextWidth) * 0.55) :
                            (int)(RemainingTextWidth * 0.40);
                }
        }
        

        /// <summary>
        /// Indicates the width reserved for the signed content text.
        /// </summary>
        /// <value></value>
        private int LocaleTextWidth
        {
            get
            {
                return (int)((RemainingTextWidth - SummaryNameTextWidth - IntentTextWidth) * 0.5);
            }
        }

        /// <summary>
        /// Indicates the width reserved for the signed content text.
        /// </summary>
        /// <value></value>
        private int SignByTextWidth
        {
            get { return RemainingTextWidth - SummaryNameTextWidth - IntentTextWidth - LocaleTextWidth; }
        }

        /// <summary>
        /// Indicates the padding within the reserved width for each text section.
        /// </summary>
        private Padding CellPadding
        {
            get
            {
                return _cellPadding;
            }
        }

        #endregion Private Properties

        #region Private Methods
        //------------------------------------------------------    
        //    
        //  Private Methods
        //    
        //------------------------------------------------------

        /// <summary>
        /// ApplySignatureSpecificResources - sets the text based on Cert and Signature.
        /// </summary>
        private void InitializeDialogType()
        {
            InitializeColumnHeaders();

            //Show only the buttons appropriate for the current mode.
            if (_showRequestDialog == true)
            {
                Text = SR.Get(SRID.DigSigRequestTitle);

                _buttonSign.Visible = false;
                _buttonViewCert.Visible = false;
                _buttonRequestAdd.Visible = true;
                _buttonRequestDelete.Visible = true;
                AcceptButton = _buttonRequestAdd;
            }
            else
            {
                Text = SR.Get(SRID.DigSigSumTitle);

                _buttonRequestAdd.Visible = false;
                _buttonRequestDelete.Visible = false;
                _buttonSign.Visible = true;
                _buttonViewCert.Visible = true;
                AcceptButton = _buttonSign;
            }

            _buttonRequestDelete.Click += new System.EventHandler(_buttonRequestDelete_Click);
        }

        /// <summary>
        /// InitializeColumnHeaders.
        /// </summary>
        /// <returns></returns>
        private void InitializeColumnHeaders()
        {
            Padding columnMargin = new Padding(0);
            Padding columnPadding = new Padding(CellPadding.Left, 3, 0, 3);

            Font columnHeaderFont = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold);

            if (_showRequestDialog)
            {
                Label nameHeader = new Label();
                nameHeader.AutoSize = true;
                nameHeader.Text = SR.Get(SRID.DigSigSumColumnHeaderName);
                nameHeader.Font = columnHeaderFont;
                nameHeader.MinimumSize = new Size(IconWidth + SummaryNameTextWidth, 0);
                nameHeader.MaximumSize = new Size(IconWidth + SummaryNameTextWidth, 0);
                nameHeader.Margin = columnMargin;
                nameHeader.Padding = columnPadding;

                Label intentHeader = new Label();
                intentHeader.AutoSize = true;
                intentHeader.Text = SR.Get(SRID.DigSigSumColumnHeaderIntent);
                intentHeader.Font = columnHeaderFont;
                intentHeader.MinimumSize = new Size(IntentTextWidth, 0);
                intentHeader.MaximumSize = new Size(IntentTextWidth, 0);
                intentHeader.Margin = columnMargin;
                intentHeader.Padding = columnPadding;

                Label locationHeader = new Label();
                locationHeader.AutoSize = true;
                locationHeader.Text = SR.Get(SRID.DigSigSumColumnHeaderLocale);
                locationHeader.Font = columnHeaderFont;
                locationHeader.MinimumSize = new Size(LocaleTextWidth, 0);
                locationHeader.MaximumSize = new Size(LocaleTextWidth, 0);
                locationHeader.Margin = columnMargin;
                locationHeader.Padding = columnPadding;

                Label signByHeader = new Label();
                signByHeader.AutoSize = true;
                signByHeader.Text = SR.Get(SRID.DigSigSumColumnHeaderSignBy);
                signByHeader.Font = columnHeaderFont;
                signByHeader.MinimumSize = new Size(SignByTextWidth, 0);
                signByHeader.MaximumSize = new Size(SignByTextWidth, 0);
                signByHeader.Margin = columnMargin;
                signByHeader.Padding = columnPadding;

                _columnHeaderPanel.Controls.Add(nameHeader);
                _columnHeaderPanel.Controls.Add(intentHeader);
                _columnHeaderPanel.Controls.Add(locationHeader);
                _columnHeaderPanel.Controls.Add(signByHeader);

            }
            else
            {
                Label statusHeader = new Label();
                statusHeader.AutoSize = true;
                statusHeader.Text = SR.Get(SRID.DigSigSumColumnHeaderSignatureStatus);
                statusHeader.Font = columnHeaderFont;
                statusHeader.MaximumSize = new Size(IconWidth + SummaryNameTextWidth,0);
                statusHeader.MinimumSize = new Size(IconWidth + SummaryNameTextWidth, 0);
                statusHeader.Margin = columnMargin;
                statusHeader.Padding = columnPadding;

                Label intentHeader = new Label();
                intentHeader.AutoSize = true;
                intentHeader.Text = SR.Get(SRID.DigSigSumColumnHeaderIntent);
                intentHeader.Font = columnHeaderFont;
                intentHeader.MinimumSize = new Size(IntentTextWidth, 0);
                intentHeader.MaximumSize = new Size(IntentTextWidth, 0);
                intentHeader.Margin = columnMargin;
                intentHeader.Padding = columnPadding;
                
                _columnHeaderPanel.Controls.Add(statusHeader);
                _columnHeaderPanel.Controls.Add(intentHeader);
            }
        }

        /// <summary>
        /// AddDigSig add the Dig Sig to the ListBox.
        /// </summary>
        /// <param name="rds">The IDigitalSignature to add to the ListBox.</param>
        /// <returns></returns>
        private void AddDigSig(SignatureResources signatureResources)
        {
            //Add the item to the ListBox.
            _listBoxSummary.Items.Add(signatureResources);
        }

        /// <summary>
        /// _buttonDone_Click Handles the "Done" button click event.
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">EventArgs</param>
        /// <returns></returns>
        private void _buttonDone_Click(object sender, EventArgs e)
        {
            if (_showRequestDialog && _listBoxSummary.Items.Count > 0)
            {
                if (System.Windows.MessageBox.Show(
                                SR.Get(SRID.DigitalSignatureMessageSignNow),
                                SR.Get(SRID.DigitalSignatureMessageSignNowTitle),
                                System.Windows.MessageBoxButton.YesNo,
                                System.Windows.MessageBoxImage.Exclamation
                    ) == System.Windows.MessageBoxResult.Yes)
                {
                    //Call the DocumentSignatureManager OnSign method.  
                    _documentSignatureManager.OnSign(null, this.Handle);
                }
            }

            Close();
        }

        /// <summary>
        /// _buttonSign_Click Handles the "Sign" button click event.
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">EventArgs</param>
        /// <returns></returns>
        private void _buttonSign_Click(object sender, EventArgs e)
        {
            //verify that something is selected.
            if (_listBoxSummary.SelectedIndex >= 0)
            {
                //Call the DocumentSignatureManager OnSign method.
                _documentSignatureManager.OnSign(
                    (SignatureResources)_listBoxSummary.Items[_listBoxSummary.SelectedIndex],
                    this.Handle);

            }
            else
            {
                //Nothing was selected so this is a regular sign.
                //Call the DocumentSignatureManager OnSign method.  
                _documentSignatureManager.OnSign(null, this.Handle);

            }

            RefreshSignatureList(false);
        }

        /// <summary>
        /// _buttonViewCert_Click Handles the "View Certificate" button click event.
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">EventArgs</param>
        private void _buttonViewCert_Click(object sender, EventArgs e)
        {
            //verify that something is selected.
            if (_listBoxSummary.SelectedIndex >= 0)
            {
                _documentSignatureManager.OnCertificateView(
                    (SignatureResources)_listBoxSummary.Items[_listBoxSummary.SelectedIndex],
                    this.Handle);
            }
        }

        /// <summary>
        /// _buttonRequestAdd_Click Handles the Request "Add" button click event.
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">EventArgs</param>
        /// <returns></returns>
        private void _buttonRequestAdd_Click(object sender, EventArgs e)
        {
            //Create and show the Request Signature Dialog (modal)
            _documentSignatureManager.OnSummaryAdd();

            RefreshSignatureList(true);
        }

        /// <summary>
        /// _buttonRequestDelete_Click Handles the Request "Delete" button click event.
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">EventArgs</param>
        /// <returns></returns>
        private void _buttonRequestDelete_Click(object sender, EventArgs e)
        {
            //verify that something is selected.
            if (_listBoxSummary.SelectedIndex >= 0)
            {
                //Create and show the Signature Details Dialog (modal)
                _documentSignatureManager.OnSummaryDelete(
                                                            (SignatureResources)
                                                            _listBoxSummary.Items[_listBoxSummary.SelectedIndex]
                                                            );

                RefreshSignatureList(true);
            }
        }

        /// <summary>
        /// Event handler for when the document's signature status changes. This forces the dialog
        /// to refresh the list of signatures and status.
        /// </summary>
        /// <param name="sender">Sender (not used)</param>
        /// <param name="args">Event arguments (not used)</param>
        private void OnSignatureStatusChange(object sender, DocumentSignatureManager.SignatureStatusEventArgs args)
        {
            // Save the selected index of the list box so it can be restored
            int selectedIndex = _listBoxSummary.SelectedIndex;

            RefreshSignatureList(_showRequestDialog);

            _listBoxSummary.SelectedIndex = selectedIndex;
        }

        /// <summary>
        /// RefreshSignatureList.
        /// </summary>
        private void RefreshSignatureList(bool requestOnly)
        {
            //
            //now we need to refresh everything.
            //Start by removing all items in the list.
            _buttonRequestDelete.Enabled = false;
            _buttonViewCert.Enabled = false;
            _listBoxSummary.Items.Clear();

            //Get a new collection of signatureResources
            IList<SignatureResources> signatureResourcesList = _documentSignatureManager.GetSignatureResourceList(requestOnly /*requestsOnly*/);

            //Find the signatures applied to this package and add them to the the ListBox
            foreach (SignatureResources signatureResources in signatureResourcesList)
            {
                AddDigSig(signatureResources);
            }

        }

        /// <summary>
        /// _listBoxSummary_SelectedIndexChanged Handles the Index Change
        /// event for the ListBox.  Using to redraw all items so that the font
        /// color is corect.
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">EventArgs</param>
        /// <returns></returns>
        private void _listBoxSummary_SelectedIndexChanged(object sender, 
                                                            EventArgs e)
        {
            //We should think about optimizing this.  We shouldn't Invalidate
            //entire control when there is a selection change. 
            _listBoxSummary.Invalidate();

            if (_listBoxSummary.SelectedIndex >= 0)
            {
                _listBoxSummary.TabStop = true;
                _buttonRequestDelete.Enabled = true;

                if (_documentSignatureManager.HasCertificate((SignatureResources)_listBoxSummary.Items[_listBoxSummary.SelectedIndex]))
                {
                    _buttonViewCert.Enabled = true;
                }
                else
                {
                    _buttonViewCert.Enabled = false;
                }

                //We have selected an item, the AcceptButton is now the
                //"View Certificates" button if we're showing the
                //Summary dialog.
                if (!_showRequestDialog)
                {
                    AcceptButton = _buttonViewCert;
                }
            }
            else
            {
                _listBoxSummary.TabStop = false;
                _buttonViewCert.Enabled = false;
                //Nothing selected, put AcceptButton back to defaults
                AcceptButton = _showRequestDialog ? _buttonRequestAdd : _buttonSign;
            }
        }

        /// <summary>
        /// _listBoxSummary_Resize Handles the ListBox Resize
        /// event so all items in the listbox can get redrawn.
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">EventArgs</param>
        /// <returns></returns>
        private void _listBoxSummary_Resize(object sender, EventArgs e)
        {
            _listBoxSummary.Invalidate();
        }

        /// <summary>
        /// _listBoxSummary_MeasureItem Handles MeasureItem event.
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">MeasureItemEventArgs</param>
        /// <returns></returns>
        private void _listBoxSummary_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = CalculateItemHeight(e.Graphics,
                                               (SignatureResources)_listBoxSummary.Items[e.Index]
                                              );
        }

        /// <summary>
        /// CalculateItemHeight calculates the height for a ListBox item.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="item">ListBoxSummaryItem</param>
        /// <returns></returns>
        private int CalculateItemHeight(Graphics graphics, SignatureResources item)
        {
            //
            //Determine the height of all the text fields taking into account
            //the reserved widths and padding.  All height/padding and width are
            //non-negative
            //

            int heightIntent = (int)graphics.MeasureString(item._reason,
                                                _listBoxSummary.Font,
                                                IntentTextWidth - CellPadding.Left - CellPadding.Right).Height;

            int heightSummary = 0;
            int heightSignBy = 0;
            int heightLocale = 0;
            if (_showRequestDialog)
            {
                // For the request dialog, we use the subject name instead of the summary message
                heightSummary = (int)graphics.MeasureString(item._subjectName,
                                        _listBoxSummary.Font,
                                        SummaryNameTextWidth - CellPadding.Left - CellPadding.Right).Height;

                heightSignBy = (int)graphics.MeasureString(item._signBy,
                                        _listBoxSummary.Font,
                                        SignByTextWidth - CellPadding.Left - CellPadding.Right).Height;

                heightLocale = (int)graphics.MeasureString(item._location,
                                        _listBoxSummary.Font,
                                        LocaleTextWidth - CellPadding.Left - CellPadding.Right).Height;

            }
            else
            {
                heightSummary = (int)graphics.MeasureString(item._summaryMessage,
                                       _listBoxSummary.Font,
                                       SummaryNameTextWidth - CellPadding.Left - CellPadding.Right).Height;
            }


            //Want to return the biggest height.
            int h = Math.Max(Math.Max(Math.Max(Math.Max(IconHeight,heightSummary), heightIntent), heightLocale), heightSignBy);
            
            //Will return max text height plus the cellpadding.
            return h + CellPadding.Top + CellPadding.Bottom;
        }


        /// <summary>
        /// _listBoxSummary_DrawItem handles the DrawItem event.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">DrawItemEventArgs</param>
        /// <returns></returns>
        private void _listBoxSummary_DrawItem(object sender, DrawItemEventArgs e)
        {
            // draws it if this is a valid item
            if (e.Index > -1 && e.Index < _listBoxSummary.Items.Count)
            {
                // if we can draw the item we do
                SignatureResources item = (SignatureResources)_listBoxSummary.Items[e.Index];
                e.DrawBackground();
                DrawListBoxSummaryItem(e.Graphics, 
                                            e.Bounds, 
                                            item, 
                                            _listBoxSummary.SelectedIndex == e.Index
                                            );
                e.DrawFocusRectangle();
            }
        }

        /// <summary>
        /// DrawListBoxSummaryItem draws the item in the given bounds and 
        /// taking the reserved widths and padding.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">DrawItemEventArgs</param>
        /// <returns></returns>
        private void DrawListBoxSummaryItem(Graphics graphics, 
                                            Rectangle bounds,
                                            SignatureResources item, 
                                            bool isSelected)
        {
            
            
            StringFormat stringFormat = new StringFormat();

            // Determine the StringFormat we use to render our text -- for
            // RTL we need to set the DirectionRightToLeft Flag, for LTR
            // we use the default.
            // Additionally, in RTL we right-align rendered text to its bounding
            // rect (StringAlignment.Near).
            if (RightToLeft == RightToLeft.Yes)
            {
                stringFormat.LineAlignment = StringAlignment.Near;
                stringFormat.FormatFlags = StringFormatFlags.DirectionRightToLeft;
            }
           
            // Calculate the X offsets for the bounding rects for the items we're 
            // going to display (for RTL the X coordinates are effectively mirrored)
            int iconXOffset;
            int statusXOffset;
            int intentXOffset;

            // Flip for RTL Layout
            if (RightToLeftLayout)
            {       
                iconXOffset = bounds.Right - (CellPadding.Right + IconWidth);

                // We offset the status and intent text by the padding on both sides so that the
                // right edge of the text is properly aligned with the column header.
                statusXOffset = bounds.Right + CellPadding.Right - (IconWidth + SummaryNameTextWidth);
                intentXOffset = bounds.Right + CellPadding.Right + CellPadding.Left - 
                    (IconWidth + SummaryNameTextWidth + IntentTextWidth);                               
            }
            else
            {
                iconXOffset = bounds.Left + CellPadding.Left;
                statusXOffset = bounds.Left + IconWidth + CellPadding.Left;
                intentXOffset = bounds.Left + IconWidth + SummaryNameTextWidth + CellPadding.Left;
            }

            // The text color for this item 
            Brush brush = isSelected ? SystemBrushes.HighlightText : SystemBrushes.ControlText;
            
            //Create bounds to draw Icon
            Rectangle iconRect = new Rectangle(
                                        iconXOffset,
                                        bounds.Y + CellPadding.Top,
                                        IconWidth - CellPadding.Left - CellPadding.Right,
                                        bounds.Height - CellPadding.Top - CellPadding.Bottom
                                                );

            //All padding and width are non-negative.

            //Create bounds to draw Name / Status text
            Rectangle statusRect = new Rectangle(
                                        statusXOffset,
                                        bounds.Y + CellPadding.Top,
                                        SummaryNameTextWidth - CellPadding.Left - CellPadding.Right,
                                        bounds.Height - CellPadding.Top - CellPadding.Bottom
                                                );

            //Create bounds to draw Intent text
            Rectangle intentRect = new Rectangle(
                                        intentXOffset,
                                        bounds.Y + CellPadding.Top,
                                        IntentTextWidth - CellPadding.Left - CellPadding.Right,
                                        bounds.Height - CellPadding.Top - CellPadding.Bottom
                                                  );

            if (_showRequestDialog)
            {
                int signByXOffset;
                int localeXOffset;

                //Flip for RTL Layout
                if (RightToLeftLayout)
                {
                    // We offset the sign by and locale text by the padding on both sides so that the
                    // right edge of the text is properly aligned with the column header.
                    signByXOffset = bounds.Left + CellPadding.Left + CellPadding.Right;
                    localeXOffset = bounds.Left + CellPadding.Left + CellPadding.Right + SignByTextWidth;
                }
                else
                {
                    signByXOffset = bounds.Left + IconWidth + SummaryNameTextWidth + IntentTextWidth + SignByTextWidth + CellPadding.Left;
                    localeXOffset = bounds.Left + IconWidth + SummaryNameTextWidth + IntentTextWidth + CellPadding.Left;
                }

                //Create bounds to draw SignedBy text
                Rectangle signByRect = new Rectangle(
                                            signByXOffset,
                                            bounds.Y + CellPadding.Top,
                                            SignByTextWidth - CellPadding.Left - CellPadding.Right,
                                            bounds.Height - CellPadding.Top - CellPadding.Bottom
                                                        );

                //Create bounds to draw Locale text
                Rectangle localeRect = new Rectangle(
                                            localeXOffset,
                                            bounds.Y + CellPadding.Top,
                                            LocaleTextWidth - CellPadding.Left - CellPadding.Right,
                                            bounds.Height - CellPadding.Top - CellPadding.Bottom
                                                           );

                //Draw the Name Text
                graphics.DrawString(item._subjectName,
                                    _listBoxSummary.Font,
                                    brush,
                                    statusRect,
                                    stringFormat);

                //Draw the SignedBy Text
                graphics.DrawString(item._signBy,
                                    _listBoxSummary.Font,
                                    brush,
                                    signByRect,
                                    stringFormat);

                //Draw the locale Text
                graphics.DrawString(item._location,
                                    _listBoxSummary.Font,
                                    brush,
                                    localeRect,
                                    stringFormat);
            }
            else
            {
                //Draw the summary text
                graphics.DrawString(item._summaryMessage,
                                    _listBoxSummary.Font,
                                    brush,
                                    statusRect,
                                    stringFormat);

                //Draw the icon
                Debug.Assert(item._displayImage != null, "Signature icon is null");
                if (item._displayImage != null)
                {
                    graphics.DrawImage(item._displayImage, iconRect.Location);
                }
            }


            //Draw the Intent Text
            graphics.DrawString(item._reason,
                                _listBoxSummary.Font,
                                brush,
                                intentRect,
                                stringFormat);

        }

        #endregion Private Methods

        #region Protected Methods
        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        protected override void ApplyResources()
        {
            base.ApplyResources();

            //Get string from stringtable.
            _buttonDone.Text = SR.Get(SRID.DigSigSumDone);
            _buttonSign.Text = SR.Get(SRID.DigSigSumSign);
            _buttonViewCert.Text = SR.Get(SRID.DigSigSumViewCert);
            _buttonRequestAdd.Text = SR.Get(SRID.DigSigSumRequestAdd);
            _buttonRequestDelete.Text = SR.Get(SRID.DigSigSumRequestDelete);
        }

        #endregion Protected Methods


        #region Private Fields
        //------------------------------------------------------    
        //    
        //  Private Fields
        //    
        //------------------------------------------------------

        private DocumentSignatureManager _documentSignatureManager;
        private bool _showRequestDialog;

        /// <summary>
        /// The padding around each block of text
        /// </summary>
        private Padding _cellPadding = new Padding(10);

        #endregion Private Fields

    }
}
