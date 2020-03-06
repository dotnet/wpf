// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Documents
{
    partial class RequestedSignatureDialog
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
            _buttomControlLayoutpanel = new System.Windows.Forms.FlowLayoutPanel();
            _addButton = new System.Windows.Forms.Button();
            _cancelButton = new System.Windows.Forms.Button();
            _divider = new DialogDivider();
            _userInputTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            _dateTimePicker = new System.Windows.Forms.DateTimePicker();
            _requestedLocationTextBox = new System.Windows.Forms.TextBox();
            _signatureAppliedByDateLabel = new System.Windows.Forms.Label();
            _requestSignerNameLabel = new System.Windows.Forms.Label();
            _requestLocationLabel = new System.Windows.Forms.Label();
            _requestedSignerNameTextBox = new System.Windows.Forms.TextBox();
            _intentLabel = new System.Windows.Forms.Label();
            _intentComboBox = new System.Windows.Forms.ComboBox();
            _mainDialogTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            _buttomControlLayoutpanel.SuspendLayout();            
            _userInputTableLayoutPanel.SuspendLayout();
            _mainDialogTableLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _buttomControlLayoutpanel
            // 
            _buttomControlLayoutpanel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            _buttomControlLayoutpanel.AutoSize = true;
            _buttomControlLayoutpanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;            
            _buttomControlLayoutpanel.Controls.Add(_cancelButton);
            _buttomControlLayoutpanel.Controls.Add(_addButton);
            _buttomControlLayoutpanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _buttomControlLayoutpanel.Location = new System.Drawing.Point(374, 246);
            _buttomControlLayoutpanel.Margin = new System.Windows.Forms.Padding(10, 2, 0, 0);
            _buttomControlLayoutpanel.Name = "_buttomControlLayoutpanel";
            _buttomControlLayoutpanel.Size = new System.Drawing.Size(56, 24);            
            // 
            // _addButton
            // 
            _addButton.AutoSize = true;
            _addButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;                    
            _addButton.Name = "_addButton";
            _addButton.Padding = new System.Windows.Forms.Padding(20, 0, 20, 0);
            _addButton.Size = new System.Drawing.Size(65, 6);
            _addButton.TabIndex = 4;
            _addButton.Click += new System.EventHandler(_addButton_Click);
            // 
            // _cancelButton
            // 
            _cancelButton.AutoSize = true;
            _cancelButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;                      
            _cancelButton.Name = "_cancelButton";            
            _cancelButton.Padding = new System.Windows.Forms.Padding(20, 0, 20, 0);
            _cancelButton.Size = new System.Drawing.Size(65, 6);
            _cancelButton.TabIndex = 5;            
            // 
            // _divider
            //
            _divider.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _divider.Margin = new System.Windows.Forms.Padding(0,20,0,5);
            _divider.Name = "_divider";                        
            // 
            // _userInputTableLayoutPanel
            // 
            _userInputTableLayoutPanel.AutoSize = true;
            _userInputTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _userInputTableLayoutPanel.ColumnCount = 1;
            _userInputTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            _userInputTableLayoutPanel.Controls.Add(_dateTimePicker, 0, 7);
            _userInputTableLayoutPanel.Controls.Add(_requestedLocationTextBox, 0, 5);
            _userInputTableLayoutPanel.Controls.Add(_signatureAppliedByDateLabel, 0, 6);
            _userInputTableLayoutPanel.Controls.Add(_requestSignerNameLabel, 0, 0);
            _userInputTableLayoutPanel.Controls.Add(_requestLocationLabel, 0, 4);
            _userInputTableLayoutPanel.Controls.Add(_requestedSignerNameTextBox, 0, 1);
            _userInputTableLayoutPanel.Controls.Add(_intentLabel, 0, 2);
            _userInputTableLayoutPanel.Controls.Add(_intentComboBox, 0, 3);
            _userInputTableLayoutPanel.Location = new System.Drawing.Point(10, 23);
            _userInputTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            _userInputTableLayoutPanel.Name = "_userInputTableLayoutPanel";
            _userInputTableLayoutPanel.RowCount = 8;
            _userInputTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _userInputTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _userInputTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _userInputTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _userInputTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _userInputTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _userInputTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _userInputTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _userInputTableLayoutPanel.Size = new System.Drawing.Size(407, 197);            
            // 
            // _dateTimePicker
            //
            _dateTimePicker.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _dateTimePicker.Location = new System.Drawing.Point(3, 174);
            _dateTimePicker.Name = "_dateTimePicker";
            _dateTimePicker.Size = new System.Drawing.Size(245, 20);
            _dateTimePicker.TabIndex = 3;
            // This mirrors the control if and only if the RightToLeft property (which is
            // retrieved from the parent control) is also set to Yes.
            _dateTimePicker.RightToLeftLayout = true;
            // 
            // _requestedLocationTextBox
            // 
            _requestedLocationTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _requestedLocationTextBox.Location = new System.Drawing.Point(3, 125);
            _requestedLocationTextBox.MaxLength = _maxLocationLength;
            _requestedLocationTextBox.Name = "_requestedLocationTextBox";
            _requestedLocationTextBox.Size = new System.Drawing.Size(401, 20);
            _requestedLocationTextBox.TabIndex = 2;
            // 
            // _signatureAppliedByDateLabel
            // 
            _signatureAppliedByDateLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _signatureAppliedByDateLabel.AutoSize = true;
            _signatureAppliedByDateLabel.Location = new System.Drawing.Point(3, 158);
            _signatureAppliedByDateLabel.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
            _signatureAppliedByDateLabel.Name = "_signatureAppliedByDateLabel";
            _signatureAppliedByDateLabel.Size = new System.Drawing.Size(17, 13);            
            // 
            // _requestSignerNameLabel
            // 
            _requestSignerNameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _requestSignerNameLabel.AutoSize = true;
            _requestSignerNameLabel.Location = new System.Drawing.Point(3, 10);
            _requestSignerNameLabel.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);            
            _requestSignerNameLabel.Name = "_requestSignerNameLabel";
            _requestSignerNameLabel.Size = new System.Drawing.Size(17, 13);            
            // 
            // _requestLocationLabel
            // 
            _requestLocationLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _requestLocationLabel.AutoSize = true;
            _requestLocationLabel.Location = new System.Drawing.Point(3, 109);
            _requestLocationLabel.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
            _requestLocationLabel.Name = "_requestLocationLabel";
            _requestLocationLabel.Size = new System.Drawing.Size(17, 13);            
            // 
            // _requestedSignerNameTextBox
            // 
            _requestedSignerNameTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _requestedSignerNameTextBox.Location = new System.Drawing.Point(3, 26);
            _requestedSignerNameTextBox.MaxLength = _maxNameLength;
            _requestedSignerNameTextBox.Name = "_requestedSignerNameTextBox";
            _requestedSignerNameTextBox.Size = new System.Drawing.Size(401, 20);
            _requestedSignerNameTextBox.TabIndex = 0;
            // 
            // _intentLabel
            // 
            _intentLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _intentLabel.AutoSize = true;
            _intentLabel.Location = new System.Drawing.Point(3, 59);
            _intentLabel.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
            _intentLabel.Name = "_intentLabel";
            _intentLabel.Size = new System.Drawing.Size(17, 13);            
            // 
            // _intentComboBox
            // 
            _intentComboBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _intentComboBox.FormattingEnabled = true;
            _intentComboBox.Location = new System.Drawing.Point(3, 75);
            _intentComboBox.MaxLength = _maxIntentLength;
            _intentComboBox.Name = "_intentComboBox";
            _intentComboBox.Size = new System.Drawing.Size(401, 21);
            _intentComboBox.TabIndex = 1;
            // 
            // _mainDialogTableLayoutPanel
            // 
            _mainDialogTableLayoutPanel.AutoSize = true;
            _mainDialogTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _mainDialogTableLayoutPanel.ColumnCount = 1;
            _mainDialogTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());            
            _mainDialogTableLayoutPanel.Controls.Add(_userInputTableLayoutPanel, 0, 0);
            _mainDialogTableLayoutPanel.Controls.Add(_divider, 0, 1);
            _mainDialogTableLayoutPanel.Controls.Add(_buttomControlLayoutpanel, 0, 2);
            _mainDialogTableLayoutPanel.Location = new System.Drawing.Point(9, 9);
            _mainDialogTableLayoutPanel.Margin = new System.Windows.Forms.Padding(10);
            _mainDialogTableLayoutPanel.Name = "_mainDialogTableLayoutPanel";
            _mainDialogTableLayoutPanel.RowCount = 2;
            _mainDialogTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _mainDialogTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _mainDialogTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _mainDialogTableLayoutPanel.Size = new System.Drawing.Size(433, 273);            
            // 
            // RequestedSignatureDialog
            // 
            AcceptButton = _addButton;
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            ClientSize = new System.Drawing.Size(451, 282);
            Controls.Add(_mainDialogTableLayoutPanel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            CancelButton = _cancelButton;
            Name = "RequestedSignatureDialog";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            _buttomControlLayoutpanel.ResumeLayout(false);
            _buttomControlLayoutpanel.PerformLayout();            
            _userInputTableLayoutPanel.ResumeLayout(false);
            _userInputTableLayoutPanel.PerformLayout();
            _mainDialogTableLayoutPanel.ResumeLayout(false);
            _mainDialogTableLayoutPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel _buttomControlLayoutpanel;
        private System.Windows.Forms.Button _addButton;
        private System.Windows.Forms.Button _cancelButton;        
        private System.Windows.Forms.Label _requestSignerNameLabel;
        private System.Windows.Forms.Label _signatureAppliedByDateLabel;
        private System.Windows.Forms.ComboBox _intentComboBox;
        private System.Windows.Forms.Label _intentLabel;
        private System.Windows.Forms.TextBox _requestedSignerNameTextBox;
        private System.Windows.Forms.DateTimePicker _dateTimePicker;
        private System.Windows.Forms.TextBox _requestedLocationTextBox;
        private System.Windows.Forms.Label _requestLocationLabel;
        private System.Windows.Forms.TableLayoutPanel _mainDialogTableLayoutPanel;
        private System.Windows.Forms.TableLayoutPanel _userInputTableLayoutPanel;
        private DialogDivider _divider;       
    }
}