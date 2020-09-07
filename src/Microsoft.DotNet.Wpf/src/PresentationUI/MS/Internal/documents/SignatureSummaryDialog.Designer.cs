// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Documents
{
    partial class SignatureSummaryDialog
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
            _buttonDone = new System.Windows.Forms.Button();
            _flowpanelBottomControls = new System.Windows.Forms.FlowLayoutPanel();
            _flowpanelMiddleControls = new System.Windows.Forms.FlowLayoutPanel();
            _buttonSign = new System.Windows.Forms.Button();
            _buttonViewCert = new System.Windows.Forms.Button();
            _buttonRequestDelete = new System.Windows.Forms.Button();
            _buttonRequestAdd = new System.Windows.Forms.Button();
            _divider = new DialogDivider();
            _columnHeaderPanel = new System.Windows.Forms.FlowLayoutPanel();
            _listBoxSummary = new System.Windows.Forms.ListBox();
            _listboxMainPanel = new System.Windows.Forms.Panel();
            _listboxItemPanel = new System.Windows.Forms.Panel();
            _mainLayoutTable = new System.Windows.Forms.TableLayoutPanel();          
            _flowpanelBottomControls.SuspendLayout();
            _listboxMainPanel.SuspendLayout();
            _listboxItemPanel.SuspendLayout();
            _mainLayoutTable.SuspendLayout();  
            SuspendLayout();
            // 
            // _buttonDone
            // 
            _buttonDone.AutoSize = true;
            _buttonDone.Location = new System.Drawing.Point(247, 13);
            _buttonDone.Name = "_buttonDone";
            _buttonDone.Size = new System.Drawing.Size(75, 23);
            _buttonDone.TabIndex = 2;
            _buttonDone.Click += new System.EventHandler(_buttonDone_Click);
            //
            // _divider
            //      
            _divider.Anchor = System.Windows.Forms.AnchorStyles.Left;            
            _divider.Name = "_divider";
            // 
            // _flowpanelMiddleControls
            //             
            _flowpanelMiddleControls.Controls.Add(_buttonViewCert);
            _flowpanelMiddleControls.Controls.Add(_buttonSign);
            _flowpanelMiddleControls.Controls.Add(_buttonRequestAdd);
            _flowpanelMiddleControls.Controls.Add(_buttonRequestDelete);            
            _flowpanelMiddleControls.AutoSize = true;
            _flowpanelMiddleControls.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _flowpanelMiddleControls.Dock = System.Windows.Forms.DockStyle.Bottom;
            _flowpanelMiddleControls.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            _flowpanelMiddleControls.Location = new System.Drawing.Point(297, 0);
            _flowpanelMiddleControls.Name = "_flowpanelMiddleControls";
            _flowpanelMiddleControls.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3);
            _flowpanelMiddleControls.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            _flowpanelMiddleControls.Size = new System.Drawing.Size(335, 49);            
            // 
            // _flowpanelBottomControls
            // 
            _flowpanelBottomControls.Controls.Add(_buttonDone);
            _flowpanelBottomControls.AutoSize = true;
            _flowpanelBottomControls.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _flowpanelBottomControls.Dock = System.Windows.Forms.DockStyle.Bottom;
            _flowpanelBottomControls.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _flowpanelBottomControls.Location = new System.Drawing.Point(297, 0);
            _flowpanelBottomControls.Margin = new System.Windows.Forms.Padding(0);
            _flowpanelBottomControls.Name = "_flowpanelBottomControls";
            _flowpanelBottomControls.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            _flowpanelBottomControls.Size = new System.Drawing.Size(335, 49);            
            // 
            // _buttonSign
            // 
            _buttonSign.AutoSize = true;
            _buttonSign.Location = new System.Drawing.Point(166, 13);
            _buttonSign.Name = "_buttonSign";
            _buttonSign.Size = new System.Drawing.Size(75, 23);
            _buttonSign.TabIndex = 1;
            _buttonSign.Click += new System.EventHandler(_buttonSign_Click);
            // 
            // _buttonViewCert
            // 
            _buttonViewCert.AutoSize = true;
            _buttonViewCert.Enabled = false;
            _buttonViewCert.Location = new System.Drawing.Point(85, 13);
            _buttonViewCert.Name = "_buttonViewCert";
            _buttonViewCert.Size = new System.Drawing.Size(75, 23);
            _buttonViewCert.TabIndex = 0;
            _buttonViewCert.Click += new System.EventHandler(_buttonViewCert_Click);
            // 
            // _buttonRequestDelete
            // 
            _buttonRequestDelete.AutoSize = true;
            _buttonRequestDelete.Enabled = false;
            _buttonRequestDelete.Location = new System.Drawing.Point(4, 13);
            _buttonRequestDelete.Name = "_buttonRequestDelete";
            _buttonRequestDelete.Size = new System.Drawing.Size(75, 23);
            _buttonRequestDelete.TabIndex = 1;
            // 
            // _buttonRequestAdd
            // 
            _buttonRequestAdd.AutoSize = true;
            _buttonRequestAdd.Location = new System.Drawing.Point(247, 42);
            _buttonRequestAdd.Name = "_buttonRequestAdd";
            _buttonRequestAdd.Size = new System.Drawing.Size(75, 23);
            _buttonRequestAdd.TabIndex = 0;
            _buttonRequestAdd.Visible = false;
            _buttonRequestAdd.Click += new System.EventHandler(_buttonRequestAdd_Click);
            // 
            // _columnHeaderPanel
            // 
            _columnHeaderPanel.BackColor = System.Drawing.SystemColors.Control;
            _columnHeaderPanel.Dock = System.Windows.Forms.DockStyle.Top;
            _columnHeaderPanel.AutoSize = true;
            _columnHeaderPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _columnHeaderPanel.Location = new System.Drawing.Point(0, 0);
            _columnHeaderPanel.Name = "_columnHeaderPanel";
            _columnHeaderPanel.Size = new System.Drawing.Size(600, 25);            
            // 
            // _listBoxSummary
            //                            
            _listBoxSummary.BackColor = System.Drawing.SystemColors.Window;
            _listBoxSummary.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _listBoxSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            _listBoxSummary.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            _listBoxSummary.FormattingEnabled = true;
            _listBoxSummary.Location = new System.Drawing.Point(0, 0);
            _listBoxSummary.Name = "_listBoxSummary";
            _listBoxSummary.Size = new System.Drawing.Size(600, 217);
            _listBoxSummary.TabIndex = 3;
            _listBoxSummary.TabStop = false;
            _listBoxSummary.DrawItem += new System.Windows.Forms.DrawItemEventHandler(_listBoxSummary_DrawItem);
            _listBoxSummary.Resize += new System.EventHandler(_listBoxSummary_Resize);
            _listBoxSummary.SelectedIndexChanged += new System.EventHandler(_listBoxSummary_SelectedIndexChanged);
            _listBoxSummary.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(_listBoxSummary_MeasureItem);
            // 
            // _listboxMainPanel
            //            
            _listboxMainPanel.BackColor = System.Drawing.SystemColors.Info;
            _listboxMainPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            _listboxMainPanel.Controls.Add(_listboxItemPanel);
            _listboxMainPanel.Controls.Add(_columnHeaderPanel);
            _listboxMainPanel.Location = new System.Drawing.Point(15, 15);
            _listboxMainPanel.Name = "_listboxMainPanel";
            _listboxMainPanel.Size = new System.Drawing.Size(602, 272);            
            // 
            // _listboxItemPanel
            // 
            _listboxItemPanel.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            _listboxItemPanel.Controls.Add(_listBoxSummary);
            _listboxItemPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            _listboxItemPanel.Location = new System.Drawing.Point(0, 53);
            _listboxItemPanel.Name = "_listboxItemPanel";
            _listboxItemPanel.Size = new System.Drawing.Size(600, 217);            
            // 
            // _mainLayoutTable
            // 
            _mainLayoutTable.AutoSize = true;
            _mainLayoutTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _mainLayoutTable.ColumnCount = 1;
            _mainLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            _mainLayoutTable.Controls.Add(_listboxMainPanel, 0, 1);
            _mainLayoutTable.Controls.Add(_flowpanelMiddleControls, 0, 2);
            _mainLayoutTable.Controls.Add(_divider, 0, 3);
            _mainLayoutTable.Controls.Add(_flowpanelBottomControls, 0, 4);
            _mainLayoutTable.Location = new System.Drawing.Point(6, 6);
            _mainLayoutTable.Margin = new System.Windows.Forms.Padding(5);
            _mainLayoutTable.Name = "_mainLayoutTable";
            _mainLayoutTable.RowCount = 3;
            _mainLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _mainLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _mainLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _mainLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());                
            // 
            // SignatureSummaryDialog
            //            
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            CancelButton = _buttonDone;
            Controls.Add(_mainLayoutTable);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new System.Drawing.Size(629, 378);
            Visible = false;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Name = "SignatureSummaryDialog";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            _flowpanelBottomControls.ResumeLayout(false);
            _flowpanelBottomControls.PerformLayout();
            _listboxMainPanel.ResumeLayout(false);
            _listboxItemPanel.ResumeLayout(false);
            _mainLayoutTable.ResumeLayout(false);
            _mainLayoutTable.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        #region Private Fields
        //------------------------------------------------------    
        //    
        //  Private Fields
        //    
        //------------------------------------------------------

        private System.Windows.Forms.ListBox _listBoxSummary;
        private System.Windows.Forms.Button _buttonDone;
        private System.Windows.Forms.Button _buttonSign;
        private System.Windows.Forms.Button _buttonViewCert;
        private System.Windows.Forms.Button _buttonRequestAdd;
        private System.Windows.Forms.Button _buttonRequestDelete;        
        private System.Windows.Forms.FlowLayoutPanel _flowpanelMiddleControls;
        private System.Windows.Forms.FlowLayoutPanel _flowpanelBottomControls;
        private System.Windows.Forms.Panel _listboxMainPanel;
        private System.Windows.Forms.FlowLayoutPanel _columnHeaderPanel;
        private System.Windows.Forms.Panel _listboxItemPanel;
        private System.Windows.Forms.TableLayoutPanel _mainLayoutTable;
        private DialogDivider _divider;

        #endregion Private Fields
        
    }
}

