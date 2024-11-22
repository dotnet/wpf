// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Documents
{
    partial class ProgressDialog
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
            _label = new System.Windows.Forms.Label();
            _timer = new System.Windows.Forms.Timer();
            SuspendLayout();
            // 
            // label1
            // 
            _label.AutoSize = true;            
            _label.Location = new System.Drawing.Point(20, 9);
            _label.Margin = new System.Windows.Forms.Padding(10);
            _label.Name = "label1";            
            _label.TabIndex = 0;
            // 
            // timer1
            // 
            _timer.Enabled = true;
            _timer.Interval = _timerInterval;
            _timer.Tick += new System.EventHandler(OnTimerTick);
            // 
            // ProgressDialog
            //             
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;            
            ControlBox = false;
            Controls.Add(_label);            
            MaximizeBox = false;
            MinimizeBox = false;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            Name = "ProgressDialog";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            TopLevel = true;
            ResumeLayout(false);
            PerformLayout();            
        }

        #endregion

        private System.Windows.Forms.Label _label;
        private System.Windows.Forms.Timer _timer;
    }
}