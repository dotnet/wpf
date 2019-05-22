// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



namespace System.Windows.Controls
{
    /// <summary>
    ///     Struct used by IHierarchicalVirtualizationAndScrollInfo
    ///     to specify constraints.
    /// </summary>
    public struct HierarchicalVirtualizationConstraints
    {
        #region Constructors

        public HierarchicalVirtualizationConstraints(VirtualizationCacheLength cacheLength,
            VirtualizationCacheLengthUnit cacheLengthUnit,
            Rect viewport)
        {
            _cacheLength = cacheLength;
            _cacheLengthUnit = cacheLengthUnit;
            _viewport = viewport;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns teh dimensions of the cache before and aftet the viewport.
        /// </summary>
        public VirtualizationCacheLength CacheLength
        {
            get
            {
                return _cacheLength;
            }
        }

        /// <summary>
        /// Returns the unit for the CacheLength of the cache before and after
        /// the viewport
        /// </summary>
        public VirtualizationCacheLengthUnit CacheLengthUnit
        {
            get
            {
                return _cacheLengthUnit;
            }
        }

        /// <summary>
        /// Returns the constraint viewport.
        /// </summary>
        public Rect Viewport
        {
            get
            {
                return _viewport;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Overloaded operator, compares 2 HierarchicalVirtualizationConstraints's.
        /// </summary>
        /// <param name="constraints1">first HierarchicalVirtualizationConstraints to compare.</param>
        /// <param name="constraints2">second HierarchicalVirtualizationConstraints to compare.</param>
        /// <returns>true if specified HierarchicalVirtualizationConstraintss have same CacheLength, CacheLengthUnit and Viewport.</returns>
        public static bool operator ==(HierarchicalVirtualizationConstraints constraints1, HierarchicalVirtualizationConstraints constraints2)
        {
            return ((constraints1.CacheLength == constraints2.CacheLength) &&
                (constraints1.CacheLengthUnit == constraints2.CacheLengthUnit) &&
                (constraints2.Viewport == constraints2.Viewport));
        }

        /// <summary>
        /// Overloaded operator, compares 2 HierarchicalVirtualizationConstraints's.
        /// </summary>
        /// <param name="constraints1">first HierarchicalVirtualizationConstraints to compare.</param>
        /// <param name="constraints2">second HierarchicalVirtualizationConstraints to compare.</param>
        /// <returns>true if specified HierarchicalVirtualizationConstraintss have either different CacheLength or 
        /// CacheLengthUnit or Viewport.</returns>
        public static bool operator !=(HierarchicalVirtualizationConstraints constraints1, HierarchicalVirtualizationConstraints constraints2)
        {
            return ((constraints1.CacheLength != constraints2.CacheLength) ||
                (constraints1.CacheLengthUnit != constraints2.CacheLengthUnit) ||
                (constraints1.Viewport != constraints2.Viewport));
        }

        /// <summary>
        /// Compares this instance of HierarchicalVirtualizationConstraints with another object.
        /// </summary>
        /// <param name="oCompare">Reference to an object for comparison.</param>
        /// <returns><c>true</c>if this HierarchicalVirtualizationConstraints instance has the same CacheLength, CacheLengthUnit 
        /// and Viewport as oCompare.</returns>
        override public bool Equals(object oCompare)
        {
            if (oCompare is HierarchicalVirtualizationConstraints)
            {
                HierarchicalVirtualizationConstraints constraints = (HierarchicalVirtualizationConstraints)oCompare;
                return (this == constraints);
            }
            else
                return false;
        }

        /// <summary>
        /// Compares this instance of HierarchicalVirtualizationConstraints with another instance.
        /// </summary>
        /// <param name="comparisonConstraints">Header desired size instance to compare.</param>
        /// <returns><c>true</c>if this HierarchicalVirtualizationConstraints instance has the same CacheLength, CacheLengthUnit 
        /// and Viewport as comparisonConstraints.</returns>
        public bool Equals(HierarchicalVirtualizationConstraints comparisonConstraints)
        {
            return (this == comparisonConstraints);
        }

        /// <summary>
        /// <see cref="Object.GetHashCode"/>
        /// </summary>
        /// <returns><see cref="Object.GetHashCode"/></returns>
        public override int GetHashCode()
        {
            return (_cacheLength.GetHashCode() ^ _cacheLengthUnit.GetHashCode() ^ _viewport.GetHashCode());
        }

        #endregion

        #region Data

        VirtualizationCacheLength _cacheLength;
        VirtualizationCacheLengthUnit _cacheLengthUnit;
        Rect _viewport;

        #endregion
    }
}

