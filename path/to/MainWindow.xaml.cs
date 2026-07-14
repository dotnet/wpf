// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Windows;

namespace DragLeaveCoordBug
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ScrollViewer_DragLeave(object sender, DragEventArgs e)
        {
            if (e.KeyStates.HasFlag(DragDropKeyStates.ShiftKey))
            {
                Debug.WriteLine(new Point(ActualWidth, ActualHeight));
                Debug.WriteLine(e.GetPosition(this));
            }
        }
    }
}