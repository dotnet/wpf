// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

namespace System.Windows.Media.Imaging
{
    #region DownloadProgressEventArgs

    /// <summary>
    /// Event args for the DownloadProgress event.
    /// </summary>
    public class DownloadProgressEventArgs : EventArgs
    {
        // Internal constructor
        internal DownloadProgressEventArgs(int percentComplete)
        {
            _percentComplete = percentComplete;
        }

        /// <summary>
        /// Returns the progress between 1-100
        /// </summary>
        public int Progress
        {
            get
            {
                return _percentComplete;
            }
        }

        int _percentComplete;
    }

    #endregion
}

