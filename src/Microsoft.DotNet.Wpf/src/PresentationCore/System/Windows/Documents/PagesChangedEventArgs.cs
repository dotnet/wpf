// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: PagesChanged event.
//
//

namespace System.Windows.Documents 
{
    /// <summary>
    /// PagesChanged event handler.
    /// </summary>
    public delegate void PagesChangedEventHandler(object sender, PagesChangedEventArgs e);

    /// <summary>
    /// Event arguments for the PagesChanged event.
    /// </summary>
    public class PagesChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="start">Zero-based page number for this first page that has changed.</param>
        /// <param name="count">Number of continuous pages changed.</param>
        public PagesChangedEventArgs(int start, int count)
        {
            _start = start; 
            _count = count;
        }

        /// <summary>
        /// Zero-based page number for this first page that has changed.
        /// </summary>
        public int Start
        {
            get { return _start; }
        }

        /// <summary>
        /// Number of continuous pages changed. If the number of pages affected is 
        /// unknown, then this value will be Integer.MaxValue.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Zero-based page number for this first page that has changed.
        /// </summary>
        private readonly int _start;

        /// <summary>
        /// Number of continuous pages changed. If the number of pages affected is 
        /// unknown, then this value will be Integer.MaxValue.
        /// </summary>
        private readonly int _count;
    }
}
