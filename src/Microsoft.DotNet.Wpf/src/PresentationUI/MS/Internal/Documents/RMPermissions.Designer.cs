// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Documents
{
    partial class RMPermissionsDialog
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
            this.mainContentFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.authenticatedAsFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.authenticatedAsTextLabel = new System.Windows.Forms.Label();
            this.authenticatedAsLabel = new System.Windows.Forms.Label();
            this.permissionsFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.permissionsHeldLabel = new System.Windows.Forms.Label();
            this.contactFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.requestFromTextLabel = new System.Windows.Forms.Label();
            this.requestFromLabel = new System.Windows.Forms.LinkLabel();
            this.expirationFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.expiresOnTextLabel = new System.Windows.Forms.Label();
            this.expiresOnLabel = new System.Windows.Forms.Label();
            this.divider = new DialogDivider();
            this.actionsFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.closeButton = new System.Windows.Forms.Button();
            this.mainContentFlowPanel.SuspendLayout();
            this.authenticatedAsFlowPanel.SuspendLayout();
            this.permissionsFlowPanel.SuspendLayout();
            this.expirationFlowPanel.SuspendLayout();
            this.contactFlowPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainContentFlowPanel
            // 
            this.mainContentFlowPanel.AutoSize = true;
            this.mainContentFlowPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mainContentFlowPanel.Controls.Add(this.authenticatedAsFlowPanel);
            this.mainContentFlowPanel.Controls.Add(this.permissionsFlowPanel);
            this.mainContentFlowPanel.Controls.Add(this.contactFlowPanel);
            this.mainContentFlowPanel.Controls.Add(this.expirationFlowPanel);
            this.mainContentFlowPanel.Controls.Add(this.divider);
            this.mainContentFlowPanel.Controls.Add(this.actionsFlowPanel);
            this.mainContentFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.mainContentFlowPanel.Padding = new System.Windows.Forms.Padding(5);
            this.mainContentFlowPanel.Name = "mainContentFlowPanel";
            // 
            // authenticatedAsFlowPanel
            // 
            this.authenticatedAsFlowPanel.AutoSize = true;
            this.authenticatedAsFlowPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.authenticatedAsFlowPanel.Controls.Add(this.authenticatedAsTextLabel);
            this.authenticatedAsFlowPanel.Controls.Add(this.authenticatedAsLabel);
            this.authenticatedAsFlowPanel.Name = "authenticatedAsFlowPanel";
            this.authenticatedAsFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            // 
            // authenticatedAsTextLabel
            // 
            this.authenticatedAsTextLabel.AutoSize = true;
            this.authenticatedAsTextLabel.Name = "authenticatedAsTextLabel";
            // 
            // authenticatedAsLabel
            // 
            this.authenticatedAsLabel.AutoSize = true;
            this.authenticatedAsLabel.Margin = new System.Windows.Forms.Padding(13, 3, 3, 3);
            this.authenticatedAsLabel.Name = "authenticatedAsLabel";
            // 
            // permissionsFlowPanel
            // 
            this.permissionsFlowPanel.AutoSize = true;
            this.permissionsFlowPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.permissionsFlowPanel.Controls.Add(this.permissionsHeldLabel);
            this.permissionsFlowPanel.Name = "permissionsFlowPanel";
            this.permissionsFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            // 
            // permissionsHeldLabel
            // 
            this.permissionsHeldLabel.AutoSize = true;
            this.permissionsHeldLabel.Name = "permissionsHeldLabel";
            // 
            // contactFlowPanel
            // 
            this.contactFlowPanel.AutoSize = true;
            this.contactFlowPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.contactFlowPanel.Controls.Add(this.requestFromTextLabel);
            this.contactFlowPanel.Controls.Add(this.requestFromLabel);
            this.contactFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.contactFlowPanel.Name = "contactFlowPanel";
            // 
            // requestFromTextLabel
            // 
            this.requestFromTextLabel.AutoSize = true;
            this.requestFromTextLabel.Name = "requestFromTextLabel";
            // 
            // requestFromLabel
            // 
            this.requestFromLabel.AutoSize = true;
            this.requestFromLabel.Margin = new System.Windows.Forms.Padding(13, 3, 3, 3);
            this.requestFromLabel.Name = "requestFromLabel";
            this.requestFromLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // expirationFlowPanel
            // 
            this.expirationFlowPanel.AutoSize = true;
            this.expirationFlowPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.expirationFlowPanel.Controls.Add(this.expiresOnTextLabel);
            this.expirationFlowPanel.Controls.Add(this.expiresOnLabel);
            this.expirationFlowPanel.Name = "expirationFlowPanel";
            // 
            // expiresOnTextLabel
            // 
            this.expiresOnTextLabel.AutoSize = true;
            this.expiresOnTextLabel.Name = "expiresOnTextLabel";
            // 
            // expiresOnLabel
            // 
            this.expiresOnLabel.AutoSize = true;
            this.expiresOnLabel.Name = "expiresOnLabel";
            // 
            // actionsFlowPanel
            // 
            this.actionsFlowPanel.AutoSize = true;
            this.actionsFlowPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.actionsFlowPanel.Controls.Add(this.closeButton);
            this.actionsFlowPanel.Name = "actionsFlowPanel";
            this.actionsFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.actionsFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            // 
            // closeButton
            // 
            this.closeButton.AutoSize = true;
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Name = "closeButton";

            // Tab Definitions
            // These are defined on all elements for consistency.  To enable an
            // actual tabstop ensure the control has TabStop=true.
            this.mainContentFlowPanel.TabIndex      = 0;
            this.authenticatedAsFlowPanel.TabIndex  = 1;
            this.authenticatedAsTextLabel.TabIndex  = 2;
            this.authenticatedAsLabel.TabIndex      = 3;
            this.permissionsFlowPanel.TabIndex      = 4;
            this.permissionsHeldLabel.TabIndex      = 5;
            this.contactFlowPanel.TabIndex          = 6;
            this.requestFromTextLabel.TabIndex      = 7;
            this.requestFromLabel.TabIndex          = 8;
            this.expirationFlowPanel.TabIndex       = 9;
            this.expiresOnTextLabel.TabIndex        = 10;
            this.expiresOnLabel.TabIndex            = 11;
            this.divider.TabIndex                   = 12;
            this.actionsFlowPanel.TabIndex          = 13;
            this.closeButton.TabIndex               = 14;


            // 
            // RMPermissions
            // 
            this.AcceptButton = closeButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = closeButton;
            this.ClientSize = new System.Drawing.Size(196, 155);
            this.Controls.Add(this.mainContentFlowPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RMPermissions";
            this.mainContentFlowPanel.ResumeLayout(false);
            this.mainContentFlowPanel.PerformLayout();
            this.authenticatedAsFlowPanel.ResumeLayout(false);
            this.authenticatedAsFlowPanel.PerformLayout();
            this.permissionsFlowPanel.ResumeLayout(false);
            this.permissionsFlowPanel.PerformLayout();
            this.contactFlowPanel.ResumeLayout(false);
            this.contactFlowPanel.PerformLayout();
            this.expirationFlowPanel.ResumeLayout(false);
            this.expirationFlowPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

            this.closeButton.Select();

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel mainContentFlowPanel;
        private System.Windows.Forms.FlowLayoutPanel authenticatedAsFlowPanel;
        private System.Windows.Forms.Label authenticatedAsTextLabel;
        private System.Windows.Forms.Label authenticatedAsLabel;
        private System.Windows.Forms.FlowLayoutPanel permissionsFlowPanel;
        private System.Windows.Forms.Label permissionsHeldLabel;
        private System.Windows.Forms.FlowLayoutPanel contactFlowPanel;
        private System.Windows.Forms.Label requestFromTextLabel;
        private System.Windows.Forms.LinkLabel requestFromLabel;
        private System.Windows.Forms.FlowLayoutPanel expirationFlowPanel;
        private System.Windows.Forms.Label expiresOnTextLabel;
        private System.Windows.Forms.Label expiresOnLabel;
        private DialogDivider divider;
        private System.Windows.Forms.FlowLayoutPanel actionsFlowPanel;
        private System.Windows.Forms.Button closeButton;
    }
}