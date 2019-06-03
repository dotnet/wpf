// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



namespace System.Windows.Controls
{
    /// <summary>
    /// struct used by IHierarchicalVirtualizationAndScrollInfo to
    /// represt the desired sizes of the header element.
    /// </summary>
    public struct HierarchicalVirtualizationHeaderDesiredSizes
    {
        #region Constructors

        public HierarchicalVirtualizationHeaderDesiredSizes(Size logicalSize, Size pixelSize)
        {
            _logicalSize = logicalSize;
            _pixelSize = pixelSize;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Logical size of the header.
        /// </summary>
        public Size LogicalSize
        {
            get
            {
                return _logicalSize;
            }
        }

        /// <summary>
        /// Pixel size of the header
        /// </summary>
        public Size PixelSize
        {
            get
            {
                return _pixelSize;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Overloaded operator, compares 2 HierarchicalVirtualizationHeaderDesiredSizes's.
        /// </summary>
        /// <param name="headerDesiredSizes1">first HierarchicalVirtualizationHeaderDesiredSizes to compare.</param>
        /// <param name="headerDesiredSizes2">second HierarchicalVirtualizationHeaderDesiredSizes to compare.</param>
        /// <returns>true if specified HierarchicalVirtualizationHeaderDesiredSizess have same logical 
        /// and pixel sizes.</returns>
        public static bool operator ==(HierarchicalVirtualizationHeaderDesiredSizes headerDesiredSizes1, HierarchicalVirtualizationHeaderDesiredSizes headerDesiredSizes2)
        {
            return (headerDesiredSizes1.LogicalSize == headerDesiredSizes2.LogicalSize
                    && headerDesiredSizes1.PixelSize == headerDesiredSizes2.PixelSize);
        }

        /// <summary>
        /// Overloaded operator, compares 2 HierarchicalVirtualizationHeaderDesiredSizes's.
        /// </summary>
        /// <param name="headerDesiredSizes1">first HierarchicalVirtualizationHeaderDesiredSizes to compare.</param>
        /// <param name="headerDesiredSizes2">second HierarchicalVirtualizationHeaderDesiredSizes to compare.</param>
        /// <returns>true if specified HierarchicalVirtualizationHeaderDesiredSizess have either different logical or 
        /// pixel sizes.</returns>
        public static bool operator !=(HierarchicalVirtualizationHeaderDesiredSizes headerDesiredSizes1, HierarchicalVirtualizationHeaderDesiredSizes headerDesiredSizes2)
        {
            return (headerDesiredSizes1.LogicalSize != headerDesiredSizes2.LogicalSize
                              || headerDesiredSizes1.PixelSize != headerDesiredSizes2.PixelSize);
        }

        /// <summary>
        /// Compares this instance of HierarchicalVirtualizationHeaderDesiredSizes with another object.
        /// </summary>
        /// <param name="oCompare">Reference to an object for comparison.</param>
        /// <returns><c>true</c>if this HierarchicalVirtualizationHeaderDesiredSizes instance has the same logical 
        /// and pixel sizes as oCompare.</returns>
        override public bool Equals(object oCompare)
        {
            if (oCompare is HierarchicalVirtualizationHeaderDesiredSizes)
            {
                HierarchicalVirtualizationHeaderDesiredSizes headerDesiredSizes = (HierarchicalVirtualizationHeaderDesiredSizes)oCompare;
                return (this == headerDesiredSizes);
            }
            else
                return false;
        }

        /// <summary>
        /// Compares this instance of HierarchicalVirtualizationHeaderDesiredSizes with another instance.
        /// </summary>
        /// <param name="comparisonHeaderSizes">Header desired size instance to compare.</param>
        /// <returns><c>true</c>if this HierarchicalVirtualizationHeaderDesiredSizes instance has the same logical 
        /// and pixel sizes as comparisonHeaderSizes.</returns>
        public bool Equals(HierarchicalVirtualizationHeaderDesiredSizes comparisonHeaderSizes)
        {
            return (this == comparisonHeaderSizes);
        }

        /// <summary>
        /// <see cref="Object.GetHashCode"/>
        /// </summary>
        /// <returns><see cref="Object.GetHashCode"/></returns>
        public override int GetHashCode()
        {
            return (_logicalSize.GetHashCode() ^ _pixelSize.GetHashCode());
        }

        #endregion

        #region Data

        private Size _logicalSize;
        private Size _pixelSize;

        #endregion
    }
}

