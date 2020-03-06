// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Documents
{
    internal partial class SigningDialog
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
            _userInputFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            _signatureControlInputLayoutTable = new System.Windows.Forms.TableLayoutPanel();
            _addDigSigCheckBox = new System.Windows.Forms.CheckBox();
            _signerlabel = new System.Windows.Forms.Label();
            _addDocPropCheckBox = new System.Windows.Forms.CheckBox();
            _reasonLabel = new System.Windows.Forms.Label();            
            _reasonComboBox = new System.Windows.Forms.ComboBox();
            _actionlabel = new System.Windows.Forms.Label();
            _locationLabel = new System.Windows.Forms.Label();
            _locationTextBox = new System.Windows.Forms.TextBox();
            _divider = new DialogDivider();
            _buttonflowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            _cancelButton = new System.Windows.Forms.Button();
            _signButton = new System.Windows.Forms.Button();
            _signSaveAsButton = new System.Windows.Forms.Button();            
            _mainLayoutTable = new System.Windows.Forms.TableLayoutPanel();            
            _userInputFlowPanel.SuspendLayout();
            _signatureControlInputLayoutTable.SuspendLayout();
            _buttonflowLayoutPanel.SuspendLayout();            
            _mainLayoutTable.SuspendLayout();            
            SuspendLayout();
            
            // 
            // _userInputFlowPanel
            // 
            _userInputFlowPanel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _userInputFlowPanel.AutoSize = true;
            _userInputFlowPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _userInputFlowPanel.Controls.Add(_signatureControlInputLayoutTable);
            _userInputFlowPanel.Location = new System.Drawing.Point(3, 71);
            _userInputFlowPanel.Name = "_userInputFlowPanel";
            _userInputFlowPanel.Size = new System.Drawing.Size(475, 195);                        
            // 
            // _signatureControlInputLayoutTable
            // 
            _signatureControlInputLayoutTable.AutoSize = true;
            _signatureControlInputLayoutTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _signatureControlInputLayoutTable.ColumnCount = 1;
            _signatureControlInputLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            _signatureControlInputLayoutTable.Controls.Add(_divider, 0, 8);
            _signatureControlInputLayoutTable.Controls.Add(_addDigSigCheckBox, 0, 7);
            _signatureControlInputLayoutTable.Controls.Add(_signerlabel, 0, 0);
            _signatureControlInputLayoutTable.Controls.Add(_addDocPropCheckBox, 0, 6);
            _signatureControlInputLayoutTable.Controls.Add(_reasonLabel, 0, 1);            
            _signatureControlInputLayoutTable.Controls.Add(_reasonComboBox, 0, 2);
            _signatureControlInputLayoutTable.Controls.Add(_actionlabel, 0, 5);
            _signatureControlInputLayoutTable.Controls.Add(_locationLabel, 0, 3);
            _signatureControlInputLayoutTable.Controls.Add(_locationTextBox, 0, 4);
            _signatureControlInputLayoutTable.Location = new System.Drawing.Point(6, 19);
            _signatureControlInputLayoutTable.Name = "_signatureControlInputLayoutTable";
            _signatureControlInputLayoutTable.RowCount = 9;
            _signatureControlInputLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _signatureControlInputLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _signatureControlInputLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _signatureControlInputLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _signatureControlInputLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _signatureControlInputLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _signatureControlInputLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _signatureControlInputLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _signatureControlInputLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _signatureControlInputLayoutTable.Size = new System.Drawing.Size(466, 173);            
            // 
            // _addDigSigCheckBox
            // 
            _addDigSigCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _addDigSigCheckBox.AutoSize = true;
            _addDigSigCheckBox.Location = new System.Drawing.Point(3, 136);
            _addDigSigCheckBox.Name = "_addDigSigCheckBox";
            _addDigSigCheckBox.Size = new System.Drawing.Size(15, 14);
            _addDigSigCheckBox.TabIndex = 6;
            // 
            // _signerlabel
            // 
            _signerlabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _signerlabel.AutoSize = true;
            _signerlabel.Location = new System.Drawing.Point(3, 20);
            _signerlabel.Margin = new System.Windows.Forms.Padding(3, 10, 3, 20);
            _signerlabel.Name = "_signerlabel";
            _signerlabel.Size = new System.Drawing.Size(0, 0);            
            // 
            // _addDocPropCheckBox
            // 
            _addDocPropCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _addDocPropCheckBox.AutoSize = true;
            _addDocPropCheckBox.Checked = true;
            _addDocPropCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            _addDocPropCheckBox.Location = new System.Drawing.Point(3, 116);
            _addDocPropCheckBox.Name = "_addDocPropCheckBox";
            _addDocPropCheckBox.Size = new System.Drawing.Size(15, 14);
            _addDocPropCheckBox.TabIndex = 5;
            // 
            // _reasonLabel
            // 
            _reasonLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _reasonLabel.AutoSize = true;
            _reasonLabel.Location = new System.Drawing.Point(3, 40);
            _reasonLabel.Name = "_reasonLabel";
            _reasonLabel.Size = new System.Drawing.Size(0, 0);            
            // 
            // _reasonComboBox
            // 
            _reasonComboBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _reasonComboBox.FormattingEnabled = true;
            _reasonComboBox.Location = new System.Drawing.Point(3, 43);
            _reasonComboBox.MaxLength = _maxIntentLength;
            _reasonComboBox.Name = "_reasonComboBox";
            _reasonComboBox.Size = new System.Drawing.Size(460, 21);
            _reasonComboBox.TabIndex = 0;
            // 
            // _actionlabel
            // 
            _actionlabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _actionlabel.AutoSize = true;
            _actionlabel.Location = new System.Drawing.Point(3, 113);
            _actionlabel.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
            _actionlabel.Name = "_actionlabel";
            _actionlabel.Size = new System.Drawing.Size(0, 0);            
            // 
            // _locationLabel
            // 
            _locationLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _locationLabel.AutoSize = true;
            _locationLabel.Location = new System.Drawing.Point(3, 77);
            _locationLabel.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
            _locationLabel.Name = "_locationLabel";
            _locationLabel.Size = new System.Drawing.Size(0, 0);            
            // 
            // _locationTextBox
            // 
            _locationTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _locationTextBox.Location = new System.Drawing.Point(3, 80);
            _locationTextBox.MaxLength = _maxLocationLength;
            _locationTextBox.Name = "_locationTextBox";
            _locationTextBox.Size = new System.Drawing.Size(460, 20);
            _locationTextBox.TabIndex = 1;
            //
            // _divider
            //
            _divider.Anchor = System.Windows.Forms.AnchorStyles.Left;            
            _divider.Margin = new System.Windows.Forms.Padding(0, 15, 0, 0);
            _divider.Name = "_divider";          
            // 
            // _buttonflowLayoutPanel
            // 
            _buttonflowLayoutPanel.AutoSize = true;
            _buttonflowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _buttonflowLayoutPanel.Controls.Add(_cancelButton);
            _buttonflowLayoutPanel.Controls.Add(_signButton);
            _buttonflowLayoutPanel.Controls.Add(_signSaveAsButton);
            _buttonflowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            _buttonflowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _buttonflowLayoutPanel.Location = new System.Drawing.Point(3, 272);
            _buttonflowLayoutPanel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            _buttonflowLayoutPanel.Name = "_buttonflowLayoutPanel";
            _buttonflowLayoutPanel.Padding = new System.Windows.Forms.Padding(5, 5, 0, 5);
            _buttonflowLayoutPanel.Size = new System.Drawing.Size(475, 17);            
            // 
            // _cancelButton
            // 
            _cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            _cancelButton.AutoSize = true;
            _cancelButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            _cancelButton.Location = new System.Drawing.Point(434, 8);            
            _cancelButton.Name = "_cancelButton";
            _cancelButton.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            _cancelButton.Size = new System.Drawing.Size(36, 6);
            _cancelButton.TabIndex = 4;
            _cancelButton.Click += new System.EventHandler(_cancelButton_Click);
            // 
            // _signButton
            // 
            _signButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            _signButton.AutoSize = true;
            _signButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _signButton.Enabled = false;
            _signButton.Location = new System.Drawing.Point(392, 8);
            _signButton.Name = "_signButton";
            _signButton.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            _signButton.Size = new System.Drawing.Size(36, 6);
            _signButton.TabIndex = 3;
            _signButton.Click += new System.EventHandler(_signSaveButton_Click);
            // 
            // _signSaveAsButton
            // 
            _signSaveAsButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            _signSaveAsButton.AutoSize = true;
            _signSaveAsButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _signSaveAsButton.Enabled = false;
            _signSaveAsButton.Location = new System.Drawing.Point(350, 8);
            _signSaveAsButton.Name = "_signSaveAsButton";
            _signSaveAsButton.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            _signSaveAsButton.Size = new System.Drawing.Size(36, 6);
            _signSaveAsButton.TabIndex = 2;
            _signSaveAsButton.Click += new System.EventHandler(_signSaveAsButton_Click);            
            // 
            // _mainLayoutTable
            // 
            _mainLayoutTable.AutoSize = true;
            _mainLayoutTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _mainLayoutTable.ColumnCount = 1;
            _mainLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            _mainLayoutTable.Controls.Add(_userInputFlowPanel, 0, 1);
            _mainLayoutTable.Controls.Add(_buttonflowLayoutPanel, 0, 2);            
            _mainLayoutTable.Location = new System.Drawing.Point(6, 6);
            _mainLayoutTable.Margin = new System.Windows.Forms.Padding(5);
            _mainLayoutTable.Name = "_mainLayoutTable";
            _mainLayoutTable.RowCount = 3;
            _mainLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _mainLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());            
            _mainLayoutTable.Size = new System.Drawing.Size(481, 292);            
            // 
            // SigningDialog
            //
            AcceptButton = _signSaveAsButton;
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            CancelButton = _cancelButton;
            ClientSize = new System.Drawing.Size(492, 303);
            Controls.Add(_mainLayoutTable);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SigningDialog";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;            
            _userInputFlowPanel.ResumeLayout(false);
            _userInputFlowPanel.PerformLayout();
            _signatureControlInputLayoutTable.ResumeLayout(false);
            _signatureControlInputLayoutTable.PerformLayout();
            _buttonflowLayoutPanel.ResumeLayout(false);
            _buttonflowLayoutPanel.PerformLayout();                        
            _mainLayoutTable.ResumeLayout(false);
            _mainLayoutTable.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        #region Private Fields
        //------------------------------------------------------    
        //    
        //  Private Fields
        //    
        //------------------------------------------------------
        private System.Windows.Forms.FlowLayoutPanel _userInputFlowPanel;
        private System.Windows.Forms.Label _reasonLabel;
        private System.Windows.Forms.Label _signerlabel;
        private System.Windows.Forms.Label _actionlabel;
        private System.Windows.Forms.TextBox _locationTextBox;
        private System.Windows.Forms.Label _locationLabel;
        private System.Windows.Forms.ComboBox _reasonComboBox;
        private System.Windows.Forms.CheckBox _addDigSigCheckBox;
        private System.Windows.Forms.CheckBox _addDocPropCheckBox;        
        private System.Windows.Forms.FlowLayoutPanel _buttonflowLayoutPanel;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button _signButton;
        private System.Windows.Forms.Button _signSaveAsButton;                        
        private System.Windows.Forms.TableLayoutPanel _mainLayoutTable;
        private System.Windows.Forms.TableLayoutPanel _signatureControlInputLayoutTable;
        private DialogDivider _divider;

        #endregion Private Fields

    }

}