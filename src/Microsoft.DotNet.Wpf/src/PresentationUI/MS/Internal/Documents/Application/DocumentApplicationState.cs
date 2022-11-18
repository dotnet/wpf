// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Represents the current UI state of the application.


using MS.Internal.PresentationUI;
using System;

namespace MS.Internal.Documents.Application
{
    [Serializable]
    [FriendAccessAllowed]
    internal struct DocumentApplicationState
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="zoom">Zoom value</param>
        /// <param name="horizontalOffset">Horizontal offset within the document</param>
        /// <param name="verticalOffset">Vertical offset within the document</param>
        /// <param name="maxPagesAcross">Number of adjacent pages displayed</param>
        public DocumentApplicationState(double zoom, double horizontalOffset, double verticalOffset,
            int maxPagesAcross)
        {
            // set local values
            _zoom = zoom;
            _horizontalOffset = horizontalOffset;
            _verticalOffset = verticalOffset;
            _maxPagesAcross = maxPagesAcross;
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Zoom value
        /// </summary>
        public double Zoom
        {
            get
            {
                return _zoom;
            }
        }

        /// <summary>
        /// Horizontal offset within the document
        /// </summary>
        public double HorizontalOffset
        {
            get
            {
                return _horizontalOffset;
            }
        }

        /// <summary>
        /// Vertical offset within the document
        /// </summary>
        public double VerticalOffset
        {
            get
            {
                return _verticalOffset;
            }
        }

        /// <summary>
        /// Number of adjacent pages displayed
        /// </summary>
        public int MaxPagesAcross
        {
            get
            {
                return _maxPagesAcross;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        private double      _zoom;
        private double      _horizontalOffset;
        private double      _verticalOffset;
        private int         _maxPagesAcross;
        #endregion Private Fields
    }

}
