// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows.Controls
{
    /// <summary>
    /// struct used by IHierarchicalVirtualizationAndScrollInfo to represt the
    /// the cumulative desired sizes of items.
    /// </summary>
    public struct HierarchicalVirtualizationItemDesiredSizes
    {
        #region Constructors

        public HierarchicalVirtualizationItemDesiredSizes(Size logicalSize,
            Size logicalSizeInViewport,
            Size logicalSizeBeforeViewport,
            Size logicalSizeAfterViewport,
            Size pixelSize,
            Size pixelSizeInViewport,
            Size pixelSizeBeforeViewport,
            Size pixelSizeAfterViewport)
        {
            _logicalSize = logicalSize;
            _logicalSizeInViewport = logicalSizeInViewport;
            _logicalSizeBeforeViewport = logicalSizeBeforeViewport;
            _logicalSizeAfterViewport = logicalSizeAfterViewport;
            _pixelSize = pixelSize;
            _pixelSizeInViewport = pixelSizeInViewport;
            _pixelSizeBeforeViewport = pixelSizeBeforeViewport;
            _pixelSizeAfterViewport = pixelSizeAfterViewport;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Describes the items size in logical units and is needed to 
        /// support item scrolling in hierarchical scenarios. (Eg. TreeView, Grouping)
        /// </summary>
        public Size LogicalSize
        {
            get
            {
                return _logicalSize;
            }
        }

        /// <summary>
        /// Describes the item size in viewport in logical units
        /// </summary>
        public Size LogicalSizeInViewport
        {
            get
            {
                return _logicalSizeInViewport;
            }
        }

        /// <summary>
        /// Describes the item size in cache before viewport in logical units
        /// </summary>
        public Size LogicalSizeBeforeViewport
        {
            get
            {
                return _logicalSizeBeforeViewport;
            }
        }

        /// <summary>
        /// Describes the item size in cache after viewport in logical units
        /// </summary>
        public Size LogicalSizeAfterViewport
        {
            get
            {
                return _logicalSizeAfterViewport;
            }
        }

        /// <summary>
        /// Describes the item size in pixel units
        /// </summary>
        public Size PixelSize
        {
            get
            {
                return _pixelSize;
            }
        }

        /// <summary>
        /// Describes the item size in viewport in pixel units
        /// </summary>
        public Size PixelSizeInViewport
        {
            get
            {
                return _pixelSizeInViewport;
            }
        }

        /// <summary>
        /// Describes the item size in cache before viewport in pixel units
        /// </summary>
        public Size PixelSizeBeforeViewport
        {
            get
            {
                return _pixelSizeBeforeViewport;
            }
        }

        /// <summary>
        /// Describes the item size in cache after viewport in pixel units.
        /// </summary>
        public Size PixelSizeAfterViewport
        {
            get
            {
                return _pixelSizeAfterViewport;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Overloaded operator, compares 2 HierarchicalVirtualizationItemDesiredSizes's.
        /// </summary>
        /// <param name="itemDesiredSizes1">first HierarchicalVirtualizationItemDesiredSizes to compare.</param>
        /// <param name="itemDesiredSizes2">second HierarchicalVirtualizationItemDesiredSizes to compare.</param>
        /// <returns>true if specified HierarchicalVirtualizationItemDesiredSizess have same logical 
        /// and pixel sizes.</returns>
        public static bool operator ==(HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes1, HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes2)
        {
            return ((itemDesiredSizes1.LogicalSize == itemDesiredSizes2.LogicalSize) &&
                (itemDesiredSizes1.LogicalSizeInViewport == itemDesiredSizes2.LogicalSizeInViewport) &&
                (itemDesiredSizes1.LogicalSizeBeforeViewport == itemDesiredSizes2.LogicalSizeBeforeViewport) &&
                (itemDesiredSizes1.LogicalSizeAfterViewport == itemDesiredSizes2.LogicalSizeAfterViewport) &&
                (itemDesiredSizes1.PixelSize == itemDesiredSizes2.PixelSize) &&
                (itemDesiredSizes1.PixelSizeInViewport == itemDesiredSizes2.PixelSizeInViewport) &&
                (itemDesiredSizes1.PixelSizeBeforeViewport == itemDesiredSizes2.PixelSizeBeforeViewport) &&
                (itemDesiredSizes1.PixelSizeAfterViewport == itemDesiredSizes2.PixelSizeAfterViewport));
        }

        /// <summary>
        /// Overloaded operator, compares 2 HierarchicalVirtualizationItemDesiredSizes's.
        /// </summary>
        /// <param name="itemDesiredSizes1">first HierarchicalVirtualizationItemDesiredSizes to compare.</param>
        /// <param name="itemDesiredSizes2">second HierarchicalVirtualizationItemDesiredSizes to compare.</param>
        /// <returns>true if specified HierarchicalVirtualizationItemDesiredSizess have either different logical or 
        /// pixel sizes.</returns>
        public static bool operator !=(HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes1, HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes2)
        {
            return ((itemDesiredSizes1.LogicalSize != itemDesiredSizes2.LogicalSize) ||
                (itemDesiredSizes1.LogicalSizeInViewport != itemDesiredSizes2.LogicalSizeInViewport) ||
                (itemDesiredSizes1.LogicalSizeBeforeViewport != itemDesiredSizes2.LogicalSizeBeforeViewport) ||
                (itemDesiredSizes1.LogicalSizeAfterViewport != itemDesiredSizes2.LogicalSizeAfterViewport) ||
                (itemDesiredSizes1.PixelSize != itemDesiredSizes2.PixelSize) ||
                (itemDesiredSizes1.PixelSizeInViewport != itemDesiredSizes2.PixelSizeInViewport) ||
                (itemDesiredSizes1.PixelSizeBeforeViewport != itemDesiredSizes2.PixelSizeBeforeViewport) ||
                (itemDesiredSizes1.PixelSizeAfterViewport != itemDesiredSizes2.PixelSizeAfterViewport));
        }

        /// <summary>
        /// Compares this instance of HierarchicalVirtualizationItemDesiredSizes with another object.
        /// </summary>
        /// <param name="oCompare">Reference to an object for comparison.</param>
        /// <returns><c>true</c>if this HierarchicalVirtualizationItemDesiredSizes instance has the same logical 
        /// and pixel sizes as oCompare.</returns>
        override public bool Equals(object oCompare)
        {
            if (oCompare is HierarchicalVirtualizationItemDesiredSizes)
            {
                HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes = (HierarchicalVirtualizationItemDesiredSizes)oCompare;
                return (this == itemDesiredSizes);
            }
            else
                return false;
        }

        /// <summary>
        /// Compares this instance of HierarchicalVirtualizationItemDesiredSizes with another instance.
        /// </summary>
        /// <param name="comparisonItemSizes">Header desired size instance to compare.</param>
        /// <returns><c>true</c>if this HierarchicalVirtualizationItemDesiredSizes instance has the same logical 
        /// and pixel sizes as comparisonHeaderSizes.</returns>
        public bool Equals(HierarchicalVirtualizationItemDesiredSizes comparisonItemSizes)
        {
            return (this == comparisonItemSizes);
        }

        /// <summary>
        /// <see cref="Object.GetHashCode"/>
        /// </summary>
        /// <returns><see cref="Object.GetHashCode"/></returns>
        public override int GetHashCode()
        {
            return (_logicalSize.GetHashCode() ^
                _logicalSizeInViewport.GetHashCode() ^
                _logicalSizeBeforeViewport.GetHashCode() ^
                _logicalSizeAfterViewport.GetHashCode() ^
                _pixelSize.GetHashCode() ^
                _pixelSizeInViewport.GetHashCode() ^
                _pixelSizeBeforeViewport.GetHashCode() ^
                _pixelSizeAfterViewport.GetHashCode());
        }

        #endregion

        #region Data

        Size _logicalSize;
        Size _logicalSizeInViewport;
        Size _logicalSizeBeforeViewport;
        Size _logicalSizeAfterViewport;
        Size _pixelSize;
        Size _pixelSizeInViewport;
        Size _pixelSizeBeforeViewport;
        Size _pixelSizeAfterViewport;

        #endregion
    }
}

