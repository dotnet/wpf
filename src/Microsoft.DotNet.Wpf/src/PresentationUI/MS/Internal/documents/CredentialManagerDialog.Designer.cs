// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    CredentialManagerDialog is the Forms dialog that allows users to select RM Creds.

namespace MS.Internal.Documents
{
    partial class CredentialManagerDialog
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
            this._mainDialogTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this._buttonFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this._cancelButton = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this._userInputtableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this._listButtonFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this._addButton = new System.Windows.Forms.Button();
            this._removeButton = new System.Windows.Forms.Button();
            this._credListBox = new System.Windows.Forms.ListBox();
            this._instructionLabel = new System.Windows.Forms.Label();
            this._mainDialogTableLayoutPanel.SuspendLayout();
            this._buttonFlowLayoutPanel.SuspendLayout();
            this._userInputtableLayoutPanel.SuspendLayout();
            this._listButtonFlowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _mainDialogTableLayoutPanel
            // 
            this._mainDialogTableLayoutPanel.AutoSize = true;
            this._mainDialogTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._mainDialogTableLayoutPanel.ColumnCount = 1;
            this._mainDialogTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._mainDialogTableLayoutPanel.Controls.Add(this._buttonFlowLayoutPanel, 0, 2);
            this._mainDialogTableLayoutPanel.Controls.Add(this._userInputtableLayoutPanel, 0, 1);
            this._mainDialogTableLayoutPanel.Controls.Add(this._instructionLabel, 0, 0);
            this._mainDialogTableLayoutPanel.Location = new System.Drawing.Point(8, 8);
            this._mainDialogTableLayoutPanel.Name = "_mainDialogTableLayoutPanel";
            this._mainDialogTableLayoutPanel.RowCount = 3;
            this._mainDialogTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._mainDialogTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._mainDialogTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._mainDialogTableLayoutPanel.Size = new System.Drawing.Size(398, 157);
            this._mainDialogTableLayoutPanel.TabIndex = 0;
            // 
            // _buttonFlowLayoutPanel
            // 
            this._buttonFlowLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonFlowLayoutPanel.AutoSize = true;
            this._buttonFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._buttonFlowLayoutPanel.Controls.Add(this._cancelButton);
            this._buttonFlowLayoutPanel.Controls.Add(this._okButton);
            this._buttonFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this._buttonFlowLayoutPanel.Name = "_buttonFlowLayoutPanel";
            this._buttonFlowLayoutPanel.TabIndex = 3;
            // 
            // _cancelButton
            //
            this._cancelButton.AutoSize = true;
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 1;
            // 
            // _okButton
            // 
            this._okButton.AutoSize = true;
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 0;
            this._okButton.Click += new System.EventHandler(this._okButton_Click);
            // 
            // _userInputtableLayoutPanel
            // 
            this._userInputtableLayoutPanel.ColumnCount = 2;
            this._userInputtableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._userInputtableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this._userInputtableLayoutPanel.Controls.Add(this._listButtonFlowLayoutPanel, 1, 0);
            this._userInputtableLayoutPanel.Controls.Add(this._credListBox, 0, 0);
            this._userInputtableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._userInputtableLayoutPanel.Name = "_userInputtableLayoutPanel";
            this._userInputtableLayoutPanel.RowCount = 1;
            this._userInputtableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._userInputtableLayoutPanel.TabIndex = 1;
            // 
            // _listButtonFlowLayoutPanel
            // 
            this._listButtonFlowLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this._listButtonFlowLayoutPanel.AutoSize = true;
            this._listButtonFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._listButtonFlowLayoutPanel.Controls.Add(this._addButton);
            this._listButtonFlowLayoutPanel.Controls.Add(this._removeButton);
            this._listButtonFlowLayoutPanel.Name = "_listButtonFlowLayoutPanel";
            this._listButtonFlowLayoutPanel.TabIndex = 1;
            // 
            // _addButton
            // 
            this._addButton.AutoSize = true;
            this._addButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this._addButton.Name = "_addButton";
            this._addButton.TabIndex = 0;
            this._addButton.Click += new System.EventHandler(this._addButton_Click);
            // 
            // _removeButton
            //
            this._removeButton.AutoSize = true;
            this._removeButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this._removeButton.Enabled = false;
            this._removeButton.Name = "_removeButton";
            this._removeButton.TabIndex = 1;
            this._removeButton.Click += new System.EventHandler(this._removeButton_Click);
            // 
            // _credListBox
            // 
            this._credListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._credListBox.FormattingEnabled = true;
            this._credListBox.Location = new System.Drawing.Point(3, 3);
            this._credListBox.Name = "_credListBox";
            this._credListBox.Size = new System.Drawing.Size(299, 82);
            this._credListBox.TabIndex = 0;
            this._credListBox.SelectedIndexChanged += new System.EventHandler(this._credListBox_SelectedIndexChanged);
            // 
            // _instructionLabel
            // 
            this._instructionLabel.AutoSize = true;
            this._instructionLabel.Location = new System.Drawing.Point(3, 0);
            this._instructionLabel.MaximumSize = new System.Drawing.Size(400, 0);
            this._instructionLabel.Name = "_instructionLabel";
            this._instructionLabel.Size = new System.Drawing.Size(0, 0);
            this._instructionLabel.TabIndex = 0;
            // 
            // CredentialManagerDialog
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this._cancelButton; 
            this.ClientSize = new System.Drawing.Size(409, 168);
            this.Controls.Add(this._mainDialogTableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CredentialManagerDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this._mainDialogTableLayoutPanel.ResumeLayout(false);
            this._mainDialogTableLayoutPanel.PerformLayout();
            this._buttonFlowLayoutPanel.ResumeLayout(false);
            this._userInputtableLayoutPanel.ResumeLayout(false);
            this._userInputtableLayoutPanel.PerformLayout();
            this._listButtonFlowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel _mainDialogTableLayoutPanel;
        private System.Windows.Forms.FlowLayoutPanel _buttonFlowLayoutPanel;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.TableLayoutPanel _userInputtableLayoutPanel;
        private System.Windows.Forms.FlowLayoutPanel _listButtonFlowLayoutPanel;
        private System.Windows.Forms.Button _addButton;
        private System.Windows.Forms.Button _removeButton;
        private System.Windows.Forms.ListBox _credListBox;
        private System.Windows.Forms.Label _instructionLabel;
    }
}
