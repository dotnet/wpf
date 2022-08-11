// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    DocumentPropertiesDialog - Dialog to view the current document properties.
using MS.Internal.Documents;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO.Packaging;                  // For Package
using System.Security;
using System.Text;
using System.Windows.Forms;
using System.Windows.TrustUI;               // For string resources

namespace MS.Internal.Documents.Application
{
    /// <summary>
    /// Dialog to view the current document properties.
    /// </summary>
    internal sealed partial class DocumentPropertiesDialog : DialogBaseForm
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region Constructors
        /// <summary>
        /// Construct a new dialog, and populate it with data.
        /// </summary>
        internal DocumentPropertiesDialog() : base()
        {
            if (DocumentProperties.Current == null)
            {
                throw new NotSupportedException(SR.Get(SRID.DocumentPropertiesDialogDocumentPropertiesMustExist));
            }

            PopulateDataFields();
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        #region Protected Methods
        /// <summary>
        /// Called from the base constructor, this will setup all of the required string
        /// resources for the dialog.
        /// </summary>
        protected override void ApplyResources()
        {
            base.ApplyResources();

            // Get strings from the string table.
            this.Text = SR.Get(SRID.DocumentPropertiesDialogTitle);
            SetTextProperty(_summaryTab, SR.Get(SRID.DocumentPropertiesDialogSummaryLabel));
            SetTextProperty(_titleLabel, SR.Get(SRID.DocumentPropertiesDialogTitleLabel));
            SetTextProperty(_authorLabel, SR.Get(SRID.DocumentPropertiesDialogAuthorLabel));
            SetTextProperty(_subjectLabel, SR.Get(SRID.DocumentPropertiesDialogSubjectLabel));
            SetTextProperty(_descriptionLabel, SR.Get(SRID.DocumentPropertiesDialogDescriptionLabel));
            SetTextProperty(_keywordsLabel, SR.Get(SRID.DocumentPropertiesDialogKeywordsLabel));
            SetTextProperty(_categoryLabel, SR.Get(SRID.DocumentPropertiesDialogCategoryLabel));
            SetTextProperty(_languageLabel, SR.Get(SRID.DocumentPropertiesDialogLanguageLabel));
            SetTextProperty(_contentLabel, SR.Get(SRID.DocumentPropertiesDialogContentLabel));
            SetTextProperty(_statusLabel, SR.Get(SRID.DocumentPropertiesDialogStatusLabel));
            SetTextProperty(_versionLabel, SR.Get(SRID.DocumentPropertiesDialogVersionLabel));
            SetTextProperty(_identifierLabel, SR.Get(SRID.DocumentPropertiesDialogIdentifierLabel));
            SetTextProperty(_okButton, SR.Get(SRID.DocumentPropertiesDialogOkButtonLabel));
            SetTextProperty(_infoTab, SR.Get(SRID.DocumentPropertiesDialogInfoLabel));
            SetTextProperty(_sizeLabel, SR.Get(SRID.DocumentPropertiesDialogSizeLabel));
            SetTextProperty(_documentDetailBox, SR.Get(SRID.DocumentPropertiesDialogDocumentDetailBoxLabel));
            SetTextProperty(_lastSavedLabel, SR.Get(SRID.DocumentPropertiesDialogLastSavedByLabel));
            SetTextProperty(_revisionLabel, SR.Get(SRID.DocumentPropertiesDialogRevisionLabel));
            SetTextProperty(_documentCreatedLabel, SR.Get(SRID.DocumentPropertiesDialogDocumentCreatedLabel));
            SetTextProperty(_documentModifiedLabel, SR.Get(SRID.DocumentPropertiesDialogDocumentModifiedLabel));
            SetTextProperty(_documentPrintedLabel, SR.Get(SRID.DocumentPropertiesDialogDocumentPrintedLabel));
            SetTextProperty(_fileSystemBox, SR.Get(SRID.DocumentPropertiesDialogFileSystemBoxLabel));
            SetTextProperty(_fileCreatedLabel, SR.Get(SRID.DocumentPropertiesDialogFileCreatedLabel));
            SetTextProperty(_fileModifiedLabel, SR.Get(SRID.DocumentPropertiesDialogFileModifiedLabel));
            SetTextProperty(_fileAccessedLabel, SR.Get(SRID.DocumentPropertiesDialogFileAccessedLabel));
        }
        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods
        /// <summary>
        /// PopulateDataFields:  Fill in all dialog data values.
        /// </summary>
        private void PopulateDataFields()
        {
            // Load CoreProperties values.
            SetTextProperty(_author, DocumentProperties.Current.CoreProperties.Creator);
            SetTextProperty(_category, DocumentProperties.Current.CoreProperties.Category);
            SetTextProperty(_content, DocumentProperties.Current.CoreProperties.ContentType);
            SetTextProperty(_description, DocumentProperties.Current.CoreProperties.Description);
            SetTextProperty(_documentCreatedDate, DocumentProperties.Current.CoreProperties.Created);
            SetTextProperty(_documentModifiedDate, DocumentProperties.Current.CoreProperties.Modified);
            SetTextProperty(_documentPrintedDate, DocumentProperties.Current.CoreProperties.LastPrinted);
            SetTextProperty(_documentType, DocumentProperties.Current.CoreProperties.ContentType);
            SetTextProperty(_identifier, DocumentProperties.Current.CoreProperties.Identifier);
            SetTextProperty(_keywords, DocumentProperties.Current.CoreProperties.Keywords);
            SetTextProperty(_lastSaved, DocumentProperties.Current.CoreProperties.LastModifiedBy);
            SetTextProperty(_revision, DocumentProperties.Current.CoreProperties.Revision);
            SetTextProperty(_status, DocumentProperties.Current.CoreProperties.ContentStatus);
            SetTextProperty(_subject, DocumentProperties.Current.CoreProperties.Subject);
            SetTextProperty(_title, DocumentProperties.Current.CoreProperties.Title);
            SetTextProperty(_version, DocumentProperties.Current.CoreProperties.Version);
            SetTextProperty(_language, DocumentProperties.Current.CoreProperties.Language);

            // Load additional properties (file or system related)
            SetTextProperty(_fileAccessedDate, DocumentProperties.Current.FileAccessed);  //SecurityCritical
            SetTextProperty(_fileCreatedDate, DocumentProperties.Current.FileCreated);    //SecurityCritical
            SetTextProperty(_fileModifiedDate, DocumentProperties.Current.FileModified);  //SecurityCritical
            SetTextProperty(_size, FormatFileSize(DocumentProperties.Current.FileSize));  //SecurityCritical

            SetTextProperty(_filename, DocumentProperties.Current.Filename);

            // If Image is not set, create it from the current form icon.
            if (DocumentProperties.Current.Image == null)
            {
                DocumentProperties.Current.Image = this.Icon.ToBitmap();
            }

            // Display the current Image if one is set.
            if (DocumentProperties.Current.Image != null)
            {
                _iconPictureBox.Image = DocumentProperties.Current.Image;
            }
        }

        /// <summary>
        /// Sets the Text property of a control to the selected value, or to String.Empty
        /// if the text parameter is null.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="text"></param>
        private void SetTextProperty(Control control, string text)
        {
            // Check if control exists
            if (control != null)
            {
                // Check if text is a valid string, otherwise use String.Empty
                control.Text = String.IsNullOrEmpty(text) ? 
                    String.Empty : text;
            }
        }

        /// <summary>
        /// Sets the Text property of a control to the string representation of the Date
        /// parameter, or String.Empty if the date is null.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="text"></param>
        private void SetTextProperty(Control control, DateTime? date)
        {
            // Check if control exists
            if (control != null)
            {
                // Check if date is valid, and format string.
                control.Text = (!date.HasValue) ? 
                    SR.Get(SRID.DocumentPropertiesDialogNotAvailable) : 
                    String.Format(
                        CultureInfo.CurrentCulture,
                        SR.Get(SRID.DocumentPropertiesDialogDateFormat),
                        date.Value);
            }
        }

        /// <summary>
        /// Formats the incoming filesize into a typical shell properties dialog
        /// format.
        /// 
        /// - If the filesize is less than 1024 bytes, then "filesize bytes" is returned;
        /// - If the filesize is less than 1 megabyte, then "filesize KB" is returned;
        /// - Otherwise "filesize MB" is returned.
        /// 
        /// Up to two decimal places of precision are returned, based on the number of digits
        /// in the result.
        /// </summary>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        private string FormatFileSize(long fileSize)
        {
            // Ensure that file size is valid.
            if (fileSize <= 0)
            {
                return String.Empty;
            }            

            // Determine the "short" representation for the file size.
            double size = fileSize;
            string unitString = String.Empty;
            
            // Calcuate the value to display, and determine the appropriate unit (bytes, KB, MB)
            if (size >= 1024)
            {
                size /= 1024;

                if (size >= 1024) 
                {
                    // Since the number is larger than 1 meg, select MB and compute the value
                    // relative to megabytes.
                    size /= 1024;
                    unitString = SR.Get(SRID.DocumentPropertiesDialogFileSizeMBUnit);
                }
                else
                {
                    // Select KB since the value is less than a meg, but greater than 1 KB.
                    unitString = SR.Get(SRID.DocumentPropertiesDialogFileSizeKBUnit);
                }
            }

            // Determine how many decimals are required
            int decimalPlaces = 0;
            if (size < 10)
            {
                decimalPlaces = 2;
            }
            else if (size < 100)
            {
                decimalPlaces = 1;
            }
            
            // Adjust the string format to represent the appropriate number of decimal places.
            string format = String.Format(
                CultureInfo.CurrentCulture,
                SR.Get(SRID.DocumentPropertiesDialogFileSizeFormat),
                new String('#', decimalPlaces));

            // Format the resulting string.
            return String.Format(
                CultureInfo.CurrentCulture,
                format,
                size,
                unitString,
                fileSize,
                SR.Get(SRID.DocumentPropertiesDialogFileSizeBytesUnit));
        }

        /// <summary>
        /// Handles the ok button click.
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">EventArgs</param>
        /// <returns></returns>
        private void _okButtonClick(object sender, EventArgs e)
        {
            Close();
        }
        #endregion Private Methods
    }
}
