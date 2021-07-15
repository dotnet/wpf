// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines various constants used throughout DocumentViewer,
//              DocumentGrid and other related code.
//


namespace MS.Internal.Documents
{
    internal static class DocumentViewerConstants
    {
        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        /// <summary>
        /// The minimum allowed value for the Zoom property on DocumentViewer
        /// </summary>
        public static double MinimumZoom
        {
            get { return _minimumZoom; }
        }

        /// <summary>
        /// The maximum allowed value for the Zoom property on DocumentViewer
        /// </summary>
        public static double MaximumZoom
        {
            get { return _maximumZoom; }
        }

        /// <summary>
        /// The minimum allowed value for the Scale property on DocumentGrid
        /// for normal views.
        /// </summary>
        public static double MinimumScale
        {
            get { return _minimumZoom / 100.0; }
        }

        /// <summary>
        /// The minimum allowed value for the Scale property on DocumentGrid
        /// for a Thumbnails view.
        /// </summary>
        public static double MinimumThumbnailsScale
        {
            get { return _minimumThumbnailsZoom / 100.0; }
        }

        /// <summary>
        /// The maximum allowed value for the Scale property on DocumentGrid        
        /// </summary>
        public static double MaximumScale
        {
            get { return _maximumZoom / 100.0; }
        }

        /// <summary>
        /// The maximum allowed value for the MaxPagesAcross property on DocumentViewer
        /// </summary>
        public static int MaximumMaxPagesAcross
        {
            get { return _maximumMaxPagesAcross; }
        }

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------
        private const double _minimumZoom = 5.0;
        private const double _minimumThumbnailsZoom = 12.5;
        private const double _maximumZoom = 5000.0;
        private const int _maximumMaxPagesAcross = 32;
    }
}
