// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: PaginationProgress event.
//
//

namespace System.Windows.Documents 
{
    /// <summary>
    /// PaginationProgress event handler.
    /// </summary>
    public delegate void PaginationProgressEventHandler(object sender, PaginationProgressEventArgs e);

    /// <summary>
    /// Event arguments for the PaginationProgress event.
    /// </summary>
    public class PaginationProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="start">Zero-based page number for this first page that has been paginated.</param>
        /// <param name="count">Number of continuous pages paginated.</param>
        public PaginationProgressEventArgs(int start, int count)
        {
            _start = start; 
            _count = count;
        }

        /// <summary>
        /// Zero-based page number for this first page that has been paginated.
        /// </summary>
        public int Start
        {
            get { return _start; }
        }

        /// <summary>
        /// Number of continuous pages paginated.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Zero-based page number for this first page that has been paginated.
        /// </summary>
        private readonly int _start;

        /// <summary>
        /// Number of continuous pages paginated.
        /// </summary>
        private readonly int _count;
    }
}
