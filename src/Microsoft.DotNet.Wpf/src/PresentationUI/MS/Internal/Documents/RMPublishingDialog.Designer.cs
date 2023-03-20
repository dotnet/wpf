// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Documents
{
    partial class RMPublishingDialog
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
            this.flowLayoutPanelMain = new System.Windows.Forms.FlowLayoutPanel();
            this.radioButtonUnrestricted = new System.Windows.Forms.RadioButton();
            this.radioButtonPermissions = new System.Windows.Forms.RadioButton();
            this.radioButtonTemplate = new System.Windows.Forms.RadioButton();
            this.groupBoxMainContent = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanelUnrestricted = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanelPermissions = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanelTemplate = new System.Windows.Forms.FlowLayoutPanel();
            this.textBoxUnrestrictedText = new System.Windows.Forms.TextBox();
            this.labelSelectTemplate = new System.Windows.Forms.Label();
            this.comboBoxTemplates = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanelPeople = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonPeoplePicker = new System.Windows.Forms.Button();
            this.textBoxUserName = new System.Windows.Forms.TextBox();
            this.buttonAddUser = new System.Windows.Forms.Button();
            this.buttonEveryone = new System.Windows.Forms.Button();
            this.buttonRemoveUser = new System.Windows.Forms.Button();
            this.rightsTable = new RightsTable();
            this.flowLayoutPanelExpires = new System.Windows.Forms.FlowLayoutPanel();
            this.checkBoxValidUntil = new System.Windows.Forms.CheckBox();
            this.datePickerValidUntil = new System.Windows.Forms.DateTimePicker();
            this.flowLayoutPanelContact = new System.Windows.Forms.FlowLayoutPanel();
            this.checkBoxPermissionsContact = new System.Windows.Forms.CheckBox();
            this.textBoxPermissionsContact = new System.Windows.Forms.TextBox();
            this.flowLayoutPanelActions = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonSaveAs = new System.Windows.Forms.Button();
            this.flowLayoutPanelMain.SuspendLayout();
            this.groupBoxMainContent.SuspendLayout();
            this.flowLayoutPanelUnrestricted.SuspendLayout();
            this.flowLayoutPanelPermissions.SuspendLayout();
            this.flowLayoutPanelTemplate.SuspendLayout();
            this.flowLayoutPanelPeople.SuspendLayout();
            this.flowLayoutPanelExpires.SuspendLayout();
            this.flowLayoutPanelActions.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanelMain
            // 
            this.flowLayoutPanelMain.AutoSize = true;
            this.flowLayoutPanelMain.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelMain.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.flowLayoutPanelMain.Controls.Add(this.radioButtonUnrestricted);
            this.flowLayoutPanelMain.Controls.Add(this.radioButtonPermissions);
            this.flowLayoutPanelMain.Controls.Add(this.radioButtonTemplate);
            this.flowLayoutPanelMain.Controls.Add(this.groupBoxMainContent);
            this.flowLayoutPanelMain.Controls.Add(this.flowLayoutPanelActions);
            this.flowLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelMain.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelMain.Name = "flowLayoutPanelMain";
            this.flowLayoutPanelMain.Padding = new System.Windows.Forms.Padding(8);
            this.flowLayoutPanelMain.WrapContents = false;
            // 
            // radioButtonUnrestricted
            // 
            this.radioButtonUnrestricted.AutoSize = true;
            this.radioButtonUnrestricted.Name = "radioButtonUnrestricted";
            this.radioButtonUnrestricted.Size = new System.Drawing.Size(85, 17);
            this.radioButtonUnrestricted.TabStop = true;
            this.radioButtonUnrestricted.UseVisualStyleBackColor = true;
            this.radioButtonUnrestricted.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // radioButtonPermissions
            // 
            this.radioButtonPermissions.AutoSize = true;
            this.radioButtonPermissions.Name = "radioButtonPermissions";
            this.radioButtonPermissions.Size = new System.Drawing.Size(85, 17);
            this.radioButtonPermissions.TabStop = true;
            this.radioButtonPermissions.UseVisualStyleBackColor = true;
            this.radioButtonPermissions.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // radioButtonTemplate
            // 
            this.radioButtonTemplate.AutoSize = true;
            this.radioButtonTemplate.Name = "radioButtonTemplate";
            this.radioButtonTemplate.Size = new System.Drawing.Size(85, 17);
            this.radioButtonTemplate.TabStop = true;
            this.radioButtonTemplate.UseVisualStyleBackColor = true;
            this.radioButtonTemplate.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // groupBoxMainContent
            // 
            this.groupBoxMainContent.BackColor = System.Drawing.Color.Transparent;
            this.groupBoxMainContent.Controls.Add(this.flowLayoutPanelUnrestricted);
            this.groupBoxMainContent.Controls.Add(this.flowLayoutPanelPermissions);
            this.groupBoxMainContent.Controls.Add(this.flowLayoutPanelTemplate);
            this.groupBoxMainContent.Margin = new System.Windows.Forms.Padding(5);
            this.groupBoxMainContent.Name = "groupBoxMainContent";
            this.groupBoxMainContent.Size = new System.Drawing.Size(650, 270);
            this.groupBoxMainContent.TabStop = false;
            // 
            // flowLayoutPanelUnrestricted
            // 
            this.flowLayoutPanelUnrestricted.AutoSize = true;
            this.flowLayoutPanelUnrestricted.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelUnrestricted.Controls.Add(this.textBoxUnrestrictedText);
            this.flowLayoutPanelUnrestricted.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelUnrestricted.Name = "flowLayoutPanelUnrestricted";
            this.flowLayoutPanelUnrestricted.Padding = new System.Windows.Forms.Padding(5, 18, 5, 5);
            // 
            // flowLayoutPanelPermissions
            // 
            this.flowLayoutPanelPermissions.AutoSize = true;
            this.flowLayoutPanelPermissions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelPermissions.Controls.Add(this.flowLayoutPanelPeople);
            this.flowLayoutPanelPermissions.Controls.Add(this.rightsTable);
            this.flowLayoutPanelPermissions.Controls.Add(this.flowLayoutPanelContact);
            this.flowLayoutPanelPermissions.Controls.Add(this.flowLayoutPanelExpires);
            this.flowLayoutPanelPermissions.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelPermissions.Name = "flowLayoutPanelPermissions";
            this.flowLayoutPanelPermissions.Padding = new System.Windows.Forms.Padding(5, 18, 5, 5);
            this.flowLayoutPanelPermissions.Visible = false;
            // 
            // flowLayoutPanelTemplate
            // 
            this.flowLayoutPanelTemplate.Controls.Add(this.labelSelectTemplate);
            this.flowLayoutPanelTemplate.Controls.Add(this.comboBoxTemplates);
            this.flowLayoutPanelTemplate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelTemplate.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelTemplate.Name = "flowLayoutPanelTemplate";
            this.flowLayoutPanelTemplate.Padding = new System.Windows.Forms.Padding(5, 18, 5, 5);
            this.flowLayoutPanelTemplate.Visible = false;
            // 
            // labelUnrestrictedText
            // 
            this.textBoxUnrestrictedText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBoxUnrestrictedText.Multiline = true;
            this.textBoxUnrestrictedText.Name = "textBoxUnrestrictedText";
            this.textBoxUnrestrictedText.Padding = new System.Windows.Forms.Padding(3);
            this.textBoxUnrestrictedText.Size = new System.Drawing.Size(625, 230);
            this.textBoxUnrestrictedText.ReadOnly = true;
            this.textBoxUnrestrictedText.WordWrap = true;
            // 
            // labelSelectTemplate
            // 
            this.labelSelectTemplate.AutoSize = true;
            this.labelSelectTemplate.Name = "labelSelectTemplate";
            this.labelSelectTemplate.Padding = new System.Windows.Forms.Padding(3);
            // 
            // comboBoxTemplates
            // 
            this.comboBoxTemplates.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxTemplates.FormattingEnabled = true;
            this.comboBoxTemplates.Margin = new System.Windows.Forms.Padding(13, 3, 3, 3);
            this.comboBoxTemplates.Name = "comboBox1";
            this.comboBoxTemplates.Size = new System.Drawing.Size(350, 21);
            // 
            // flowLayoutPanelPeople
            // 
            this.flowLayoutPanelPeople.AutoSize = true;
            this.flowLayoutPanelPeople.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelPeople.Controls.Add(this.buttonPeoplePicker);
            this.flowLayoutPanelPeople.Controls.Add(this.textBoxUserName);
            this.flowLayoutPanelPeople.Controls.Add(this.buttonAddUser);
            this.flowLayoutPanelPeople.Controls.Add(this.buttonEveryone);
            this.flowLayoutPanelPeople.Controls.Add(this.buttonRemoveUser);
            this.flowLayoutPanelPeople.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowLayoutPanelPeople.Name = "flowLayoutPanelPeople";
            this.flowLayoutPanelPeople.WrapContents = false;
            // 
            // buttonPeoplePicker
            // 
            this.buttonPeoplePicker.Name = "buttonPeoplePicker";
            this.buttonPeoplePicker.Click += new System.EventHandler(this.buttonPeoplePicker_Click);
            // 
            // textBoxUserName
            // 
            this.textBoxUserName.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.textBoxUserName.Name = "textBoxUserName";
            this.textBoxUserName.MinimumSize = new System.Drawing.Size(326, 20);
            this.textBoxUserName.GotFocus += new System.EventHandler(textBoxUserName_GotFocus);
            this.textBoxUserName.LostFocus += new System.EventHandler(textBoxUserName_LostFocus);
            // 
            // buttonAddUser
            // 
            this.buttonAddUser.AutoSize = true;
            this.buttonAddUser.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonAddUser.Name = "buttonAddUser";
            this.buttonAddUser.Click += new System.EventHandler(this.buttonAddUser_Click);
            // 
            // buttonAnyone
            // 
            this.buttonEveryone.Name = "buttonAnyone";
            this.buttonEveryone.Click += new System.EventHandler(this.buttonEveryone_Click);
            // 
            // buttonRemoveUser
            // 
            this.buttonRemoveUser.Name = "buttonRemoveUser";
            this.buttonRemoveUser.Click += new System.EventHandler(this.buttonRemoveUser_Click);
            // 
            // rightsTable
            // 
            this.rightsTable.AdvancedRowHeadersBorderStyle.Bottom = System.Windows.Forms.DataGridViewAdvancedCellBorderStyle.Single;
            this.rightsTable.AllowDrop = false;
            this.rightsTable.AllowUserToAddRows = false;
            this.rightsTable.AllowUserToDeleteRows = false;
            this.rightsTable.AllowUserToOrderColumns = false;
            this.rightsTable.AllowUserToResizeColumns = false;
            this.rightsTable.AllowUserToResizeRows = false;
            this.rightsTable.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rightsTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.rightsTable.MultiSelect = false;
            this.rightsTable.Name = "rightsTable";
            this.rightsTable.RowHeadersVisible = false;
            this.rightsTable.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.rightsTable.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.rightsTable.Size = new System.Drawing.Size(635, 150);
            this.rightsTable.SelectionChanged += new System.EventHandler(this.rightsTable_SelectionChanged);
            // 
            // flowLayoutPanelContact
            // 
            this.flowLayoutPanelContact.AutoSize = true;
            this.flowLayoutPanelContact.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelContact.Controls.Add(this.checkBoxPermissionsContact);
            this.flowLayoutPanelContact.Controls.Add(this.textBoxPermissionsContact);
            this.flowLayoutPanelContact.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelContact.Name = "flowLayoutPanelContact";
            this.flowLayoutPanelContact.WrapContents = true;
            // 
            // checkBoxPermissionsContact
            // 
            this.checkBoxPermissionsContact.AutoSize = true;
            this.checkBoxPermissionsContact.Name = "checkBoxPermissionsContact";
            this.checkBoxPermissionsContact.CheckedChanged += new System.EventHandler(this.checkBoxPermissionsContact_CheckedChanged);
            // 
            // textBoxPermissionsContact
            // 
            this.textBoxPermissionsContact.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBoxPermissionsContact.Enabled = false;
            this.textBoxPermissionsContact.Margin = new System.Windows.Forms.Padding(23, 3, 3, 3);
            this.textBoxPermissionsContact.Name = "textBoxPermissionsContact";
            this.textBoxPermissionsContact.ReadOnly = true;
            this.textBoxPermissionsContact.Size = new System.Drawing.Size(612, 20);
            // 
            // flowLayoutPanelExpires
            // 
            this.flowLayoutPanelExpires.AutoSize = true;
            this.flowLayoutPanelExpires.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelExpires.Controls.Add(this.checkBoxValidUntil);
            this.flowLayoutPanelExpires.Controls.Add(this.datePickerValidUntil);
            this.flowLayoutPanelExpires.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowLayoutPanelExpires.Name = "flowLayoutPanelExpires";
            this.flowLayoutPanelExpires.WrapContents = false;
            // 
            // checkBoxValidUntil
            // 
            this.checkBoxValidUntil.AutoSize = true;
            this.checkBoxValidUntil.Name = "checkBoxValidUntil";
            this.checkBoxValidUntil.CheckedChanged += new System.EventHandler(this.checkBoxValidUntil_CheckedChanged);
            // 
            // datePickerValidUntil
            // 
            this.datePickerValidUntil.Enabled = false;
            this.datePickerValidUntil.Format = System.Windows.Forms.DateTimePickerFormat.Long;
            this.datePickerValidUntil.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.datePickerValidUntil.Name = "datePickerValidUntil";
            // This mirrors the control if and only if the RightToLeft property (which is
            // retrieved from the parent control) is also set to Yes.
            this.datePickerValidUntil.RightToLeftLayout = true;
            // 
            // flowLayoutPanelActions
            // 
            this.flowLayoutPanelActions.AutoSize = true;
            this.flowLayoutPanelActions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelActions.Controls.Add(this.buttonCancel);
            this.flowLayoutPanelActions.Controls.Add(this.buttonSave);
            this.flowLayoutPanelActions.Controls.Add(this.buttonSaveAs);
            this.flowLayoutPanelActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelActions.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanelActions.Location = new System.Drawing.Point(6, 395);
            this.flowLayoutPanelActions.Name = "flowLayoutPanelActions";
            this.flowLayoutPanelActions.Size = new System.Drawing.Size(541, 29);
            this.flowLayoutPanelActions.WrapContents = false;
            // 
            // buttonCancel
            // 
            this.buttonCancel.AutoSize = true;
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(463, 3);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            // 
            // buttonSave
            //        
            this.buttonSave.AutoSize = true;
            this.buttonSave.Location = new System.Drawing.Point(382, 3);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(75, 23);
            // 
            // buttonSaveAs
            //             
            this.buttonSaveAs.AutoSize = true;
            this.buttonSaveAs.Location = new System.Drawing.Point(301, 3);
            this.buttonSaveAs.Name = "buttonSaveAs";
            this.buttonSaveAs.Size = new System.Drawing.Size(75, 23);

            // Tab Definitions
            // These are defined on all elements for consistency.  To enable an
            // actual tabstop ensure the control has TabStop=true.
            this.flowLayoutPanelMain.TabIndex           = 0;
            this.radioButtonUnrestricted.TabIndex       = 1;
            this.radioButtonPermissions.TabIndex        = 2;
            this.radioButtonTemplate.TabIndex           = 3;
            this.groupBoxMainContent.TabIndex           = 4;
            this.flowLayoutPanelUnrestricted.TabIndex   = 5;
            this.flowLayoutPanelPermissions.TabIndex    = 6;
            this.flowLayoutPanelTemplate.TabIndex       = 7;
            this.textBoxUnrestrictedText.TabIndex       = 8;
            this.labelSelectTemplate.TabIndex           = 9;
            this.comboBoxTemplates.TabIndex             = 10;
            this.flowLayoutPanelPeople.TabIndex         = 11;
            this.buttonPeoplePicker.TabIndex            = 12;
            this.textBoxUserName.TabIndex               = 13;
            this.buttonAddUser.TabIndex                 = 14;
            this.buttonEveryone.TabIndex                = 15;
            this.buttonRemoveUser.TabIndex              = 16;
            this.rightsTable.TabIndex                   = 17;
            this.flowLayoutPanelContact.TabIndex        = 18;
            this.checkBoxPermissionsContact.TabIndex    = 19;
            this.textBoxPermissionsContact.TabIndex     = 20;
            this.flowLayoutPanelExpires.TabIndex        = 21;
            this.checkBoxValidUntil.TabIndex            = 22;
            this.datePickerValidUntil.TabIndex          = 23;
            this.flowLayoutPanelActions.TabIndex        = 24;
            this.buttonSaveAs.TabIndex                  = 25;
            this.buttonSave.TabIndex                    = 26;
            this.buttonCancel.TabIndex                  = 27;


            // 
            // RMPublishing
            // 
            this.AcceptButton = buttonSaveAs;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = buttonCancel;
            this.ClientSize = new System.Drawing.Size(554, 431);
            this.Controls.Add(this.flowLayoutPanelMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RMPublishing";
            this.flowLayoutPanelMain.ResumeLayout(false);
            this.flowLayoutPanelMain.PerformLayout();
            this.groupBoxMainContent.ResumeLayout(false);
            this.groupBoxMainContent.PerformLayout();
            this.flowLayoutPanelUnrestricted.ResumeLayout(false);
            this.flowLayoutPanelUnrestricted.PerformLayout();
            this.flowLayoutPanelPermissions.ResumeLayout(false);
            this.flowLayoutPanelPermissions.PerformLayout();
            this.flowLayoutPanelTemplate.ResumeLayout(false);
            this.flowLayoutPanelTemplate.PerformLayout();
            this.flowLayoutPanelPeople.ResumeLayout(false);
            this.flowLayoutPanelPeople.PerformLayout();
            this.flowLayoutPanelExpires.ResumeLayout(false);
            this.flowLayoutPanelExpires.PerformLayout();
            this.flowLayoutPanelActions.ResumeLayout(false);
            this.flowLayoutPanelActions.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelMain;
        private System.Windows.Forms.RadioButton radioButtonUnrestricted;
        private System.Windows.Forms.RadioButton radioButtonPermissions;
        private System.Windows.Forms.RadioButton radioButtonTemplate;
        private System.Windows.Forms.GroupBox groupBoxMainContent;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelUnrestricted;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelPermissions;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelTemplate;
        private System.Windows.Forms.TextBox textBoxUnrestrictedText;
        private System.Windows.Forms.Label labelSelectTemplate;
        private System.Windows.Forms.ComboBox comboBoxTemplates;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelPeople;
        private System.Windows.Forms.Button buttonPeoplePicker;
        private System.Windows.Forms.TextBox textBoxUserName;
        private System.Windows.Forms.Button buttonEveryone;
        private System.Windows.Forms.Button buttonAddUser;
        private System.Windows.Forms.Button buttonRemoveUser;
        private RightsTable rightsTable;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelExpires;
        private System.Windows.Forms.CheckBox checkBoxValidUntil;
        private System.Windows.Forms.DateTimePicker datePickerValidUntil;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelContact;
        private System.Windows.Forms.CheckBox checkBoxPermissionsContact;
        private System.Windows.Forms.TextBox textBoxPermissionsContact;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelActions;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonSaveAs;
        private System.Windows.Forms.Button buttonCancel;
    }
} 