// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Forms;

namespace Microsoft.Test
{
    internal class ProgressWindow : Form
    {
        public ProgressWindow(string windowTitle, int numberOfProgressUnits, string progressUnitLabel)
        {
            StartTime = DateTimeOffset.Now;

            NumberOfProgressUnits = numberOfProgressUnits;
            WindowTitle = windowTitle;
            ProgressUnitLabel = progressUnitLabel;

            ProgressBar = new ProgressBar();
            ProgressBar.Dock = DockStyle.Top;
            ProgressBar.Style = ProgressBarStyle.Continuous;
            ProgressBar.Maximum = NumberOfProgressUnits;
            this.Controls.Add(ProgressBar);

            UnitProgressLabel = new Label();
            UnitProgressLabel.Dock = DockStyle.Bottom;
            UnitProgressLabel.Text = CalculateUnitProgressText();
            this.Controls.Add(UnitProgressLabel);

            TimeProgressLabel = new Label();
            TimeProgressLabel.Dock = DockStyle.Bottom;
            TimeProgressLabel.Text = CalculateTimeProgressText();
            this.Controls.Add(TimeProgressLabel);

            // Yup, those are some magic numbers that were hand crafted to
            // be the right dimensions for the progress window elements. If
            // the layout of the control ever changes, these numbers would
            // have to be recalculated.
            this.Width = 325;
            this.Height = 110;
            this.Text = WindowTitle;

            // I promise I'm using my powers for awesome.
            // So technically you're not supposed to call a winforms control
            // from a different thread than it was created on because they
            // aren't thread safe, but our usage is constrained and safe enough
            // that it makes for much simpler code to do it the way we are.
            // This works just fine normally, but if you run the infrastructure
            // under a debugger it decides to throw if you do this. (Basically
            // this property defaults to Debugger.IsAttached) So by setting it
            // to false we turn off this extra enforcement and we get the same
            // behavior under a debugger as we do normally.
            CheckForIllegalCrossThreadCalls = false;
        }

        public void MoveToLowerRight()
        {
            // These numbers are coupled to the form width/height specified in
            // the constructor. Same recalculation remark applies.
            this.SetDesktopLocation(SystemInformation.PrimaryMonitorSize.Width - 330, SystemInformation.PrimaryMonitorSize.Height - 145);
        }

        public void ReportProgress()
        {
            // If we have hit our max, we no-op. Basically, if you try to report
            // more progress than you told us you would, we ignore excess.
            if (ProgressBar.Value != ProgressBar.Maximum)
            {
                ProgressBar.Value++;
                TimeProgressLabel.Text = CalculateTimeProgressText();
                UnitProgressLabel.Text = CalculateUnitProgressText();
            }
        }

        private string CalculateTimeProgressText()
        {
            TimeSpan elapsedTime = DateTimeOffset.Now - StartTime;
            TimeSpan estimatedTimeRemaining = new TimeSpan();
            if (ProgressBar.Value > 0)
            {
                estimatedTimeRemaining = TimeSpan.FromTicks(elapsedTime.Ticks * ProgressBar.Maximum / ProgressBar.Value) - elapsedTime;
            }

            return String.Format("{0} elapsed, approximately {1} remaining.", ColloquializeTime(elapsedTime), ColloquializeTime(estimatedTimeRemaining));
        }

        private string CalculateUnitProgressText()
        {
            return String.Format("{0} of {1} {2}.", ProgressBar.Value, ProgressBar.Maximum, ProgressUnitLabel);
        }

        private string ColloquializeTime(TimeSpan elapsedTime)
        {
            string unit = String.Empty;
            int amount = 0;

            // Find most significant unit of time and populate unit label/value
            if (elapsedTime.Days > 0)
            {
                unit = "day";
                amount = elapsedTime.Days;
            }
            else if (elapsedTime.Hours > 0)
            {
                unit = "hour";
                amount = elapsedTime.Hours;
            }
            else if (elapsedTime.Minutes > 0)
            {
                unit = "minute";
                amount = elapsedTime.Minutes;
            }
            else if (elapsedTime.Seconds > 0)
            {
                unit = "second";
                amount = elapsedTime.Seconds;
            }

            // If the amount is more than one, pluralize
            if (amount > 1)
            {
                unit += "s";
            }

            // If the amount is zero, timespan is less than the smallest unit
            // of measure we are using (seconds in current implementation)
            // so we'll return '---' to indicate such
            if (amount == 0)
            {
                return "---";
            }

            return String.Format("{0} {1}", amount, unit);
        }

        private int NumberOfProgressUnits { get; set; }
        private ProgressBar ProgressBar { get; set; }
        private string ProgressUnitLabel { get; set; }
        private DateTimeOffset StartTime { get; set; }
        private Label TimeProgressLabel { get; set; }
        private Label UnitProgressLabel { get; set; }
        private string WindowTitle { get; set; }
    }
}