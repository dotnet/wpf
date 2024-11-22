// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Forms;

namespace MS.Internal.Documents.Application
{
    partial class DocumentPropertiesDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        protected override void InitializeComponent()
        {
            this._okButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this._tabControl = new System.Windows.Forms.TabControl();
            this._summaryTab = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this._language = new System.Windows.Forms.TextBox();
            this._title = new System.Windows.Forms.TextBox();
            this._titleLabel = new System.Windows.Forms.Label();
            this._identifier = new System.Windows.Forms.TextBox();
            this._versionLabel = new System.Windows.Forms.Label();
            this._authorLabel = new System.Windows.Forms.Label();
            this._statusLabel = new System.Windows.Forms.Label();
            this._contentLabel = new System.Windows.Forms.Label();
            this._languageLabel = new System.Windows.Forms.Label();
            this._subjectLabel = new System.Windows.Forms.Label();
            this._version = new System.Windows.Forms.TextBox();
            this._categoryLabel = new System.Windows.Forms.Label();
            this._category = new System.Windows.Forms.TextBox();
            this._status = new System.Windows.Forms.TextBox();
            this._content = new System.Windows.Forms.TextBox();
            this._keywordsLabel = new System.Windows.Forms.Label();
            this._author = new System.Windows.Forms.TextBox();
            this._subject = new System.Windows.Forms.TextBox();
            this._description = new System.Windows.Forms.TextBox();
            this._keywords = new System.Windows.Forms.TextBox();
            this._identifierLabel = new System.Windows.Forms.Label();
            this._descriptionLabel = new System.Windows.Forms.Label();
            this._infoTab = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this._size = new System.Windows.Forms.TextBox();
            this._iconPictureBox = new System.Windows.Forms.PictureBox();
            this._filename = new System.Windows.Forms.TextBox();
            this._documentType = new System.Windows.Forms.TextBox();
            this._sizeLabel = new System.Windows.Forms.Label();
            this._documentDetailBox = new System.Windows.Forms.GroupBox();
            this._documentTable = new System.Windows.Forms.TableLayoutPanel();
            this._documentPrintedDate = new System.Windows.Forms.TextBox();
            this._documentModifiedDate = new System.Windows.Forms.TextBox();
            this._documentCreatedDate = new System.Windows.Forms.TextBox();
            this._revision = new System.Windows.Forms.TextBox();
            this._documentPrintedLabel = new System.Windows.Forms.Label();
            this._documentModifiedLabel = new System.Windows.Forms.Label();
            this._documentCreatedLabel = new System.Windows.Forms.Label();
            this._revisionLabel = new System.Windows.Forms.Label();
            this._lastSavedLabel = new System.Windows.Forms.Label();
            this._lastSaved = new System.Windows.Forms.TextBox();
            this._fileSystemBox = new System.Windows.Forms.GroupBox();
            this._fileTable = new System.Windows.Forms.TableLayoutPanel();
            this._fileModifiedDate = new System.Windows.Forms.TextBox();
            this._fileAccessedDate = new System.Windows.Forms.TextBox();
            this._fileCreatedLabel = new System.Windows.Forms.Label();
            this._fileModifiedLabel = new System.Windows.Forms.Label();
            this._fileCreatedDate = new System.Windows.Forms.TextBox();
            this._fileAccessedLabel = new System.Windows.Forms.Label();
            this._subjectLine = new DialogDivider();
            this._categoryLine = new DialogDivider();
            this._contentLine = new DialogDivider();
            this.tableLayoutPanel3.SuspendLayout();
            this._tabControl.SuspendLayout();
            this._summaryTab.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this._infoTab.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._iconPictureBox)).BeginInit();
            this._documentDetailBox.SuspendLayout();
            this._documentTable.SuspendLayout();
            this._fileSystemBox.SuspendLayout();
            this._fileTable.SuspendLayout();
            this.SuspendLayout();
            // 
            // _okButton
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.AutoSize = true;
            this._okButton.Margin = new System.Windows.Forms.Padding(0,0,2,0);
            this._okButton.Name = "_okButton";
            this._okButton.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this._okButton.TabIndex = 0;
            this._okButton.Click += new System.EventHandler(_okButtonClick);
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.AutoSize = true;
            this.tableLayoutPanel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Controls.Add(this._okButton, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this._tabControl, 0, 0);
            this.tableLayoutPanel3.Location = new System.Drawing.Point(6, 6);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.Size = new System.Drawing.Size(362, 437);
            // 
            // _tabControl
            // 
            this._tabControl.Controls.Add(this._summaryTab);
            this._tabControl.Controls.Add(this._infoTab);
            this._tabControl.Location = new System.Drawing.Point(3, 3);
            this._tabControl.Name = "_tabControl";
            this._tabControl.Padding = new System.Drawing.Point(7, 3);
            this._tabControl.SelectedIndex = 0;
            this._tabControl.Size = new System.Drawing.Size(436, 535); 
            this._tabControl.TabIndex = 3;
            this._tabControl.BackColor = System.Drawing.SystemColors.Control;

            // 
            // _summaryTab
            // 
            this._summaryTab.BackColor = System.Drawing.SystemColors.Control;
            this._summaryTab.Controls.Add(this.tableLayoutPanel1);
            this._summaryTab.Location = new System.Drawing.Point(4, 22);
            this._summaryTab.Name = "_summaryTab";
            this._summaryTab.Padding = new System.Windows.Forms.Padding(12);
            this._summaryTab.Size = new System.Drawing.Size(324, 355);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 94));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 335));
            this.tableLayoutPanel1.Controls.Add(this._titleLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this._title, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this._authorLabel, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this._author, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this._subjectLabel, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this._subject, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this._subjectLine, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this._descriptionLabel, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this._description, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this._keywordsLabel, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this._keywords, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this._categoryLabel, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this._category, 1, 6);
            this.tableLayoutPanel1.Controls.Add(this._categoryLine, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this._languageLabel, 0, 8);
            this.tableLayoutPanel1.Controls.Add(this._language, 1, 8);
            this.tableLayoutPanel1.Controls.Add(this._contentLabel, 0, 9);
            this.tableLayoutPanel1.Controls.Add(this._content, 1, 9);
            this.tableLayoutPanel1.Controls.Add(this._contentLine, 0, 10);
            this.tableLayoutPanel1.Controls.Add(this._statusLabel, 0, 11);
            this.tableLayoutPanel1.Controls.Add(this._status, 1, 11);
            this.tableLayoutPanel1.Controls.Add(this._versionLabel, 0, 12);
            this.tableLayoutPanel1.Controls.Add(this._version, 1, 12);
            this.tableLayoutPanel1.Controls.Add(this._identifierLabel, 0, 13);
            this.tableLayoutPanel1.Controls.Add(this._identifier, 1, 13);
            this.tableLayoutPanel1.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 14;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(324, 297);
            // 
            // _language
            // 
            this._language.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._language.Location = new System.Drawing.Point(69, 205);
            this._language.Name = "_language";
            this._language.ReadOnly = true;
            this._language.Size = new System.Drawing.Size(335, 13);
            this._language.TabStop = false;
            // 
            // _title
            // 
            this._title.BackColor = System.Drawing.SystemColors.Control;
            this._title.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._title.Location = new System.Drawing.Point(69, 3);
            this._title.Name = "_title";
            this._title.ReadOnly = true;
            this._title.Size = new System.Drawing.Size(335, 13);
            this._title.TabStop = false;
            // 
            // _titleLabel
            // 
            this._titleLabel.AutoSize = true;
            this._titleLabel.Location = new System.Drawing.Point(3, 3);
            this._titleLabel.Margin = new System.Windows.Forms.Padding(3);
            this._titleLabel.Name = "_titleLabel";
            this._titleLabel.Size = new System.Drawing.Size(27, 13);
            // 
            // _identifier
            // 
            this._identifier.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._identifier.Location = new System.Drawing.Point(69, 281);
            this._identifier.Name = "_identifier";
            this._identifier.ReadOnly = true;
            this._identifier.Size = new System.Drawing.Size(335, 13);
            this._identifier.TabStop = false;
            // 
            // _versionLabel
            // 
            this._versionLabel.AutoSize = true;
            this._versionLabel.Location = new System.Drawing.Point(3, 262);
            this._versionLabel.Margin = new System.Windows.Forms.Padding(3);
            this._versionLabel.Name = "_versionLabel";
            this._versionLabel.Size = new System.Drawing.Size(42, 13);
            // 
            // _authorLabel
            // 
            this._authorLabel.AutoSize = true;
            this._authorLabel.Location = new System.Drawing.Point(3, 22);
            this._authorLabel.Margin = new System.Windows.Forms.Padding(3);
            this._authorLabel.Name = "_authorLabel";
            this._authorLabel.Size = new System.Drawing.Size(38, 13);
            // 
            // _statusLabel
            // 
            this._statusLabel.AutoSize = true;
            this._statusLabel.Location = new System.Drawing.Point(3, 243);
            this._statusLabel.Margin = new System.Windows.Forms.Padding(3);
            this._statusLabel.Name = "_statusLabel";
            this._statusLabel.Size = new System.Drawing.Size(37, 13);
            // 
            // _contentLabel
            // 
            this._contentLabel.AutoSize = true;
            this._contentLabel.Location = new System.Drawing.Point(3, 224);
            this._contentLabel.Margin = new System.Windows.Forms.Padding(3);
            this._contentLabel.Name = "_contentLabel";
            this._contentLabel.Size = new System.Drawing.Size(44, 13);
            // 
            // _languageLabel
            // 
            this._languageLabel.AutoSize = true;
            this._languageLabel.Location = new System.Drawing.Point(3, 205);
            this._languageLabel.Margin = new System.Windows.Forms.Padding(3);
            this._languageLabel.Name = "_languageLabel";
            this._languageLabel.Size = new System.Drawing.Size(55, 13);
            // 
            // _subjectLabel
            // 
            this._subjectLabel.AutoSize = true;
            this._subjectLabel.Location = new System.Drawing.Point(3, 41);
            this._subjectLabel.Margin = new System.Windows.Forms.Padding(3);
            this._subjectLabel.Name = "_subjectLabel";
            this._subjectLabel.Size = new System.Drawing.Size(43, 13);
            // 
            // _version
            // 
            this._version.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._version.Location = new System.Drawing.Point(69, 262);
            this._version.Name = "_version";
            this._version.ReadOnly = true;
            this._version.Size = new System.Drawing.Size(335, 13);
            this._version.TabStop = false;
            // 
            // _categoryLabel
            // 
            this._categoryLabel.AutoSize = true;
            this._categoryLabel.Location = new System.Drawing.Point(3, 186);
            this._categoryLabel.Margin = new System.Windows.Forms.Padding(3);
            this._categoryLabel.Name = "_categoryLabel";
            this._categoryLabel.Size = new System.Drawing.Size(49, 13);
            // 
            // _category
            // 
            this._category.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._category.Location = new System.Drawing.Point(69, 186);
            this._category.Name = "_category";
            this._category.ReadOnly = true;
            this._category.Size = new System.Drawing.Size(335, 13);
            this._category.TabStop = false;
            // 
            // _status
            // 
            this._status.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._status.Location = new System.Drawing.Point(69, 243);
            this._status.Name = "_status";
            this._status.ReadOnly = true;
            this._status.Size = new System.Drawing.Size(335, 13);
            this._status.TabStop = false;
            // 
            // _content
            // 
            this._content.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._content.Location = new System.Drawing.Point(69, 224);
            this._content.Name = "_content";
            this._content.ReadOnly = true;
            this._content.Size = new System.Drawing.Size(335, 13);
            this._content.TabStop = false;
            // 
            // _keywordsLabel
            // 
            this._keywordsLabel.AutoSize = true;
            this._keywordsLabel.Location = new System.Drawing.Point(3, 167);
            this._keywordsLabel.Margin = new System.Windows.Forms.Padding(3);
            this._keywordsLabel.Name = "_keywordsLabel";
            this._keywordsLabel.Size = new System.Drawing.Size(53, 13);
            // 
            // _author
            // 
            this._author.BackColor = System.Drawing.SystemColors.Control;
            this._author.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._author.Location = new System.Drawing.Point(69, 22);
            this._author.Name = "_author";
            this._author.ReadOnly = true;
            this._author.Size = new System.Drawing.Size(335, 13);
            this._author.TabStop = false;
            // 
            // _subject
            // 
            this._subject.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._subject.Location = new System.Drawing.Point(69, 41);
            this._subject.Name = "_subject";
            this._subject.ReadOnly = true;
            this._subject.Size = new System.Drawing.Size(335, 13);
            this._subject.TabStop = false;
            // 
            // _description
            // 
            this._description.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._description.Location = new System.Drawing.Point(69, 60);
            this._description.Multiline = true;
            this._description.Name = "_description";
            this._description.ReadOnly = true;
            this._description.Size = new System.Drawing.Size(335, 101);
            this._description.TabStop = false;
            this._description.Margin = new System.Windows.Forms.Padding(0,3,3,0);
            // 
            // _keywords
            // 
            this._keywords.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._keywords.Location = new System.Drawing.Point(69, 167);
            this._keywords.Multiline = true;
            this._keywords.Name = "_keywords";
            this._keywords.ReadOnly = true;
            this._keywords.Size = new System.Drawing.Size(335, 28);
            this._keywords.TabStop = false;
            this._keywords.Margin = new System.Windows.Forms.Padding(0,3,3,0);
            // 
            // _identifierLabel
            // 
            this._identifierLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._identifierLabel.AutoSize = true;
            this._identifierLabel.Location = new System.Drawing.Point(3, 281);
            this._identifierLabel.Margin = new System.Windows.Forms.Padding(3);
            this._identifierLabel.Name = "_identifierLabel";
            this._identifierLabel.Size = new System.Drawing.Size(47, 13);
            // 
            // _descriptionLabel
            // 
            this._descriptionLabel.AutoSize = true;
            this._descriptionLabel.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this._descriptionLabel.Location = new System.Drawing.Point(3, 60);
            this._descriptionLabel.Margin = new System.Windows.Forms.Padding(3);
            this._descriptionLabel.Name = "_descriptionLabel";
            this._descriptionLabel.Size = new System.Drawing.Size(60, 13);
            // 
            // _subjectLine
            // 
            this._subjectLine.Margin = new System.Windows.Forms.Padding(3);
            this._subjectLine.Name = "_subjectLine";
            this.tableLayoutPanel1.SetColumnSpan(this._subjectLine, 2);
            // 
            // _categoryLine
            // 
            this._categoryLine.Margin = new System.Windows.Forms.Padding(3);
            this._categoryLine.Name = "_categoryLine";
            this.tableLayoutPanel1.SetColumnSpan(this._categoryLine, 2);
            // 
            // _contentLine
            // 
            this._contentLine.Margin = new System.Windows.Forms.Padding(3);
            this._contentLine.Name = "_contentLine";
            this.tableLayoutPanel1.SetColumnSpan(this._contentLine, 2);
            // 
            // _infoTab
            // 
            this._infoTab.BackColor = System.Drawing.SystemColors.Control;
            this._infoTab.Controls.Add(this.tableLayoutPanel2);
            this._infoTab.Location = new System.Drawing.Point(4, 22);
            this._infoTab.Name = "_infoTab";
            this._infoTab.Size = new System.Drawing.Size(348, 379);
            this._infoTab.TabIndex = 2;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.AutoSize = true;
            this.tableLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel4, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this._documentDetailBox, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this._fileSystemBox, 0, 1);
            this.tableLayoutPanel2.Dock = DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(6, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 3;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(314, 355);
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.AutoSize = true;
            this.tableLayoutPanel4.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel4.ColumnCount = 3;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel4.Controls.Add(this._iconPictureBox, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this._filename, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this._documentType, 1, 1);
            this.tableLayoutPanel4.Controls.Add(this._sizeLabel, 1, 2);
            this.tableLayoutPanel4.Controls.Add(this._size, 2, 2);
            this.tableLayoutPanel4.Dock = DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 3;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            // 
            // _size
            // 
            this._size.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._size.Location = new System.Drawing.Point(90, 41);
            this._size.Multiline = true;
            this._size.Name = "_size";
            this._size.ReadOnly = true;
            this._size.Size = new System.Drawing.Size(270, 28);
            this._size.TabStop = false;
            // 
            // _iconPictureBox
            // 
            this._iconPictureBox.BackColor = System.Drawing.Color.Transparent;
            this._iconPictureBox.Name = "_iconPictureBox";
            this.tableLayoutPanel4.SetRowSpan(this._iconPictureBox, 3);
            this._iconPictureBox.Size = new System.Drawing.Size(40, 40);
            this._iconPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this._iconPictureBox.TabStop = false;
            // 
            // _filename
            // 
            this._filename.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._filename.Location = new System.Drawing.Point(75, 3);
            this._filename.Name = "_filename";
            this._filename.ReadOnly = true;
            this._filename.Size = new System.Drawing.Size(340, 13);
            this._filename.TabStop = false;
            this.tableLayoutPanel4.SetColumnSpan(this._filename, 2);
            // 
            // _documentType
            // 
            this._documentType.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._documentType.Location = new System.Drawing.Point(75, 22);
            this._documentType.Name = "_documentType";
            this._documentType.ReadOnly = true;
            this._documentType.Size = new System.Drawing.Size(340, 13);
            this._documentType.TabStop = false;
            this.tableLayoutPanel4.SetColumnSpan(this._documentType, 2);
            // 
            // _sizeLabel
            // 
            this._sizeLabel.AutoSize = true;
            this._sizeLabel.Location = new System.Drawing.Point(75, 41);
            this._sizeLabel.Margin = new System.Windows.Forms.Padding(3);
            this._sizeLabel.Name = "_sizeLabel";
            this._sizeLabel.Size = new System.Drawing.Size(40, 13);
            this._sizeLabel.Margin = new System.Windows.Forms.Padding(0,3,3,3);
            // 
            // _documentDetailBox
            // 
            this._documentDetailBox.AutoSize = true;
            this._documentDetailBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._documentDetailBox.BackColor = System.Drawing.Color.Transparent;
            this._documentDetailBox.Controls.Add(this._documentTable);
            this._documentDetailBox.Location = new System.Drawing.Point(3, 225);
            this._documentDetailBox.Name = "_documentDetailBox";
            this._documentDetailBox.Size = new System.Drawing.Size(305, 127);
            this._documentDetailBox.TabStop = false;
            this._documentDetailBox.Margin = new System.Windows.Forms.Padding(3, 3, 10, 3);
            // 
            // _documentTable
            // 
            this._documentTable.AutoSize = true;
            this._documentTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._documentTable.ColumnCount = 2;
            this._documentTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 103));
            this._documentTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 283));
            this._documentTable.Controls.Add(this._documentPrintedDate, 1, 4);
            this._documentTable.Controls.Add(this._documentModifiedDate, 1, 3);
            this._documentTable.Controls.Add(this._documentCreatedDate, 1, 2);
            this._documentTable.Controls.Add(this._revision, 1, 1);
            this._documentTable.Controls.Add(this._documentPrintedLabel, 0, 4);
            this._documentTable.Controls.Add(this._documentModifiedLabel, 0, 3);
            this._documentTable.Controls.Add(this._documentCreatedLabel, 0, 2);
            this._documentTable.Controls.Add(this._revisionLabel, 0, 1);
            this._documentTable.Controls.Add(this._lastSavedLabel, 0, 0);
            this._documentTable.Controls.Add(this._lastSaved, 1, 0);
            this._documentTable.Location = new System.Drawing.Point(16, 16);
            this._documentTable.Name = "_documentTable";
            this._documentTable.RowCount = 5;
            this._documentTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._documentTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._documentTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._documentTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._documentTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._documentTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            // 
            // _documentPrintedDate
            // 
            this._documentPrintedDate.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._documentPrintedDate.Location = new System.Drawing.Point(88, 79);
            this._documentPrintedDate.Name = "_documentPrintedDate";
            this._documentPrintedDate.ReadOnly = true;
            this._documentPrintedDate.Size = new System.Drawing.Size(282, 13);
            this._documentPrintedDate.TabStop = false;
            // 
            // _documentModifiedDate
            // 
            this._documentModifiedDate.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._documentModifiedDate.Location = new System.Drawing.Point(88, 60);
            this._documentModifiedDate.Name = "_documentModifiedDate";
            this._documentModifiedDate.ReadOnly = true;
            this._documentModifiedDate.Size = new System.Drawing.Size(282, 13);
            this._documentModifiedDate.TabStop = false;
            // 
            // _documentCreatedDate
            // 
            this._documentCreatedDate.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._documentCreatedDate.Location = new System.Drawing.Point(88, 41);
            this._documentCreatedDate.Name = "_documentCreatedDate";
            this._documentCreatedDate.ReadOnly = true;
            this._documentCreatedDate.Size = new System.Drawing.Size(282, 13);
            this._documentCreatedDate.TabStop = false;
            // 
            // _revision
            // 
            this._revision.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._revision.Location = new System.Drawing.Point(88, 22);
            this._revision.Name = "_revision";
            this._revision.ReadOnly = true;
            this._revision.Size = new System.Drawing.Size(282, 13);
            this._revision.TabStop = false;
            // 
            // _documentPrintedLabel
            // 
            this._documentPrintedLabel.AutoSize = true;
            this._documentPrintedLabel.Location = new System.Drawing.Point(3, 79);
            this._documentPrintedLabel.Margin = new System.Windows.Forms.Padding(3);
            this._documentPrintedLabel.Name = "_documentPrintedLabel";
            this._documentPrintedLabel.Size = new System.Drawing.Size(43, 13);
            // 
            // _documentModifiedLabel
            // 
            this._documentModifiedLabel.AutoSize = true;
            this._documentModifiedLabel.Location = new System.Drawing.Point(3, 60);
            this._documentModifiedLabel.Margin = new System.Windows.Forms.Padding(3);
            this._documentModifiedLabel.Name = "_documentModifiedLabel";
            this._documentModifiedLabel.Size = new System.Drawing.Size(50, 13);
            // 
            // _documentCreatedLabel
            // 
            this._documentCreatedLabel.AutoSize = true;
            this._documentCreatedLabel.Location = new System.Drawing.Point(3, 41);
            this._documentCreatedLabel.Margin = new System.Windows.Forms.Padding(3);
            this._documentCreatedLabel.Name = "_documentCreatedLabel";
            this._documentCreatedLabel.Size = new System.Drawing.Size(47, 13);
            // 
            // _revisionLabel
            // 
            this._revisionLabel.AutoSize = true;
            this._revisionLabel.Location = new System.Drawing.Point(3, 22);
            this._revisionLabel.Margin = new System.Windows.Forms.Padding(3);
            this._revisionLabel.Name = "_revisionLabel";
            this._revisionLabel.Size = new System.Drawing.Size(51, 13);
            // 
            // _lastSavedLabel
            // 
            this._lastSavedLabel.AutoSize = true;
            this._lastSavedLabel.Location = new System.Drawing.Point(3, 3);
            this._lastSavedLabel.Margin = new System.Windows.Forms.Padding(3);
            this._lastSavedLabel.Name = "_lastSavedLabel";
            this._lastSavedLabel.Size = new System.Drawing.Size(79, 13);
            // 
            // _lastSaved
            // 
            this._lastSaved.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._lastSaved.Location = new System.Drawing.Point(88, 3);
            this._lastSaved.Name = "_lastSaved";
            this._lastSaved.ReadOnly = true;
            this._lastSaved.Size = new System.Drawing.Size(282, 13);
            this._lastSaved.TabStop = false;
            // 
            // _fileSystemBox
            // 
            this._fileSystemBox.AutoSize = true;
            this._fileSystemBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._fileSystemBox.BackColor = System.Drawing.Color.Transparent;
            this._fileSystemBox.Controls.Add(this._fileTable);
            this._fileSystemBox.Location = new System.Drawing.Point(3, 124);
            this._fileSystemBox.Name = "_fileSystemBox";
            this._fileSystemBox.TabStop = false;
            this._fileSystemBox.Margin = new System.Windows.Forms.Padding(3, 3, 10, 3);
            // 
            // _fileTable
            // 
            this._fileTable.AutoSize = true;
            this._fileTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._fileTable.ColumnCount = 2;
            this._fileTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 103));
            this._fileTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 283));
            this._fileTable.Controls.Add(this._fileModifiedDate, 1, 1);
            this._fileTable.Controls.Add(this._fileAccessedDate, 1, 2);
            this._fileTable.Controls.Add(this._fileCreatedLabel, 0, 0);
            this._fileTable.Controls.Add(this._fileModifiedLabel, 0, 1);
            this._fileTable.Controls.Add(this._fileCreatedDate, 1, 0);
            this._fileTable.Controls.Add(this._fileAccessedLabel, 0, 2);
            this._fileTable.Location = new System.Drawing.Point(16, 19);
            this._fileTable.Name = "_fileTable";
            this._fileTable.RowCount = 3;
            this._fileTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._fileTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._fileTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            // 
            // _fileModifiedDate
            // 
            this._fileModifiedDate.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._fileModifiedDate.Location = new System.Drawing.Point(66, 22);
            this._fileModifiedDate.Name = "_fileModifiedDate";
            this._fileModifiedDate.ReadOnly = true;
            this._fileModifiedDate.Size = new System.Drawing.Size(282, 13);
            this._fileModifiedDate.TabStop = false;
            // 
            // _fileAccessedDate
            // 
            this._fileAccessedDate.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._fileAccessedDate.Location = new System.Drawing.Point(66, 41);
            this._fileAccessedDate.Name = "_fileAccessedDate";
            this._fileAccessedDate.ReadOnly = true;
            this._fileAccessedDate.Size = new System.Drawing.Size(282, 13);
            this._fileAccessedDate.TabStop = false;
            // 
            // _fileCreatedLabel
            // 
            this._fileCreatedLabel.AutoSize = true;
            this._fileCreatedLabel.Location = new System.Drawing.Point(3, 3);
            this._fileCreatedLabel.Margin = new System.Windows.Forms.Padding(3);
            this._fileCreatedLabel.Name = "_fileCreatedLabel";
            this._fileCreatedLabel.Size = new System.Drawing.Size(47, 13);
            // 
            // _fileModifiedLabel
            // 
            this._fileModifiedLabel.AutoSize = true;
            this._fileModifiedLabel.Location = new System.Drawing.Point(3, 22);
            this._fileModifiedLabel.Margin = new System.Windows.Forms.Padding(3);
            this._fileModifiedLabel.Name = "_fileModifiedLabel";
            this._fileModifiedLabel.Size = new System.Drawing.Size(50, 13);
            // 
            // _fileCreatedDate
            // 
            this._fileCreatedDate.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._fileCreatedDate.Location = new System.Drawing.Point(66, 3);
            this._fileCreatedDate.Name = "_fileCreatedDate";
            this._fileCreatedDate.ReadOnly = true;
            this._fileCreatedDate.Size = new System.Drawing.Size(282, 13);
            this._fileCreatedDate.TabStop = false;
            // 
            // _fileAccessedLabel
            // 
            this._fileAccessedLabel.AutoSize = true;
            this._fileAccessedLabel.Location = new System.Drawing.Point(3, 41);
            this._fileAccessedLabel.Margin = new System.Windows.Forms.Padding(3);
            this._fileAccessedLabel.Name = "_fileAccessedLabel";
            this._fileAccessedLabel.Size = new System.Drawing.Size(57, 13);
            // 
            // DocPropForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(466, 574);
            this.Controls.Add(this.tableLayoutPanel3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "DocumentPropertiesDialog";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Padding = new System.Windows.Forms.Padding(3);
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();

            this.AcceptButton = this._okButton;
            this.CancelButton = this._okButton;

            this._tabControl.ResumeLayout(false);
            this._tabControl.PerformLayout();

            this._summaryTab.ResumeLayout(false);
            this._summaryTab.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this._infoTab.ResumeLayout(false);
            this._infoTab.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._iconPictureBox)).EndInit();
            this._documentDetailBox.ResumeLayout(false);
            this._documentDetailBox.PerformLayout();
            this._documentTable.ResumeLayout(false);
            this._documentTable.PerformLayout();
            this._fileSystemBox.ResumeLayout(false);
            this._fileSystemBox.PerformLayout();
            this._fileTable.ResumeLayout(false);
            this._fileTable.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        #region Private Fields
        private System.Windows.Forms.TextBox _size;
        private System.Windows.Forms.TextBox _fileAccessedDate;
        private System.Windows.Forms.TextBox _fileCreatedDate;
        private System.Windows.Forms.Label _contentLabel;
        private System.Windows.Forms.Label _fileCreatedLabel;
        private System.Windows.Forms.TabPage _infoTab;
        private System.Windows.Forms.Label _sizeLabel;
        private System.Windows.Forms.PictureBox _iconPictureBox;
        private System.Windows.Forms.TextBox _documentType;
        private System.Windows.Forms.GroupBox _fileSystemBox;
        private System.Windows.Forms.Label _fileModifiedLabel;
        private System.Windows.Forms.Label _fileAccessedLabel;
        private System.Windows.Forms.GroupBox _documentDetailBox;
        private System.Windows.Forms.TextBox _lastSaved;
        private System.Windows.Forms.Label _revisionLabel;
        private System.Windows.Forms.Label _documentCreatedLabel;
        private System.Windows.Forms.Label _documentModifiedLabel;
        private System.Windows.Forms.Label _lastSavedLabel;
        private System.Windows.Forms.Label _documentPrintedLabel;
        private System.Windows.Forms.TextBox _filename;
        private System.Windows.Forms.Label _statusLabel;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.TextBox _identifier;
        private System.Windows.Forms.Label _identifierLabel;
        private System.Windows.Forms.TextBox _category;
        private System.Windows.Forms.Label _categoryLabel;
        private System.Windows.Forms.TextBox _version;
        private System.Windows.Forms.Label _versionLabel;
        private System.Windows.Forms.TabControl _tabControl;
        private System.Windows.Forms.TabPage _summaryTab;
        private System.Windows.Forms.TextBox _subject;
        private System.Windows.Forms.TextBox _description;
        private System.Windows.Forms.Label _titleLabel;
        private System.Windows.Forms.TextBox _status;
        private System.Windows.Forms.Label _authorLabel;
        private System.Windows.Forms.TextBox _content;
        private System.Windows.Forms.Label _subjectLabel;
        private System.Windows.Forms.TextBox _keywords;
        private System.Windows.Forms.Label _descriptionLabel;
        private System.Windows.Forms.TextBox _author;
        private System.Windows.Forms.Label _keywordsLabel;
        private System.Windows.Forms.Label _languageLabel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox _title;
        private System.Windows.Forms.TextBox _language;
        private System.Windows.Forms.TableLayoutPanel _documentTable;
        private System.Windows.Forms.TextBox _documentPrintedDate;
        private System.Windows.Forms.TextBox _documentModifiedDate;
        private System.Windows.Forms.TextBox _documentCreatedDate;
        private System.Windows.Forms.TextBox _revision;
        private System.Windows.Forms.TableLayoutPanel _fileTable;
        private System.Windows.Forms.TextBox _fileModifiedDate;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private DialogDivider _subjectLine;
        private DialogDivider _categoryLine;
        private DialogDivider _contentLine;
        #endregion Private Fields
    }
}