// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Virtualization cache length implementation
//

using MS.Internal;
using System.ComponentModel;
using System.Globalization;

namespace System.Windows.Controls
{
    /// <summary>
    /// VirtualizationCacheLengthUnit enum is used to indicate what kind of value the 
    /// VirtualizationCacheLength is holding.
    /// </summary>
    // Note: Keep the VirtualizationCacheLengthUnit enum in sync with the string representation 
    //       of units (VirtualizationCacheLengthConverter._unitString). 
    public enum VirtualizationCacheLengthUnit 
    {
        /// <summary>
        /// The value is expressed as a pixel.
        /// </summary>
        Pixel, 
        /// <summary>
        /// The value is expressed as an item.
        /// </summary>
        Item,
        /// <summary>
        /// The value is expressed as a page full of item.
        /// </summary>
        Page,
    }

    /// <summary>
    /// VirtualizationCacheLength is the type used for length of the cache used by VirtualizingPanels
    /// </summary>
    [TypeConverter(typeof(VirtualizationCacheLengthConverter))]
    public struct VirtualizationCacheLength : IEquatable<VirtualizationCacheLength>
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor, initializes the CacheBeforeViewport and the CacheAfterViewport to their sizes. Units are specified as a seperate VisualizationCacheLengthUnit property.
        /// </summary>
        /// <param name="cacheBeforeAndAfterViewport">Value to be stored by this VirtualizationCacheLength 
        /// instance as the cacheBeforeViewport and cacheAfterViewport.</param>
        /// <exception cref="ArgumentException">
        /// If <c>cacheBeforeAndAfterViewport</c> parameter is <c>double.NaN</c>
        /// or <c>cacheBeforeAndAfterViewport</c> parameter is <c>double.NegativeInfinity</c>
        /// or <c>cacheBeforeAndAfterViewport</c> parameter is <c>double.PositiveInfinity</c>.
        /// </exception>
        public VirtualizationCacheLength(double cacheBeforeAndAfterViewport)
            : this(cacheBeforeAndAfterViewport, cacheBeforeAndAfterViewport)
        {
        }

        public VirtualizationCacheLength(double cacheBeforeViewport, double cacheAfterViewport)
        {
            if (DoubleUtil.IsNaN(cacheBeforeViewport))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidCtorParameterNoNaN, "cacheBeforeViewport"));
            }

            if (DoubleUtil.IsNaN(cacheAfterViewport))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidCtorParameterNoNaN, "cacheAfterViewport"));
            }

            _cacheBeforeViewport = cacheBeforeViewport;
            _cacheAfterViewport = cacheAfterViewport;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Overloaded operator, compares 2 VirtualizationCacheLength's.
        /// </summary>
        /// <param name="cl1">first VirtualizationCacheLength to compare.</param>
        /// <param name="cl2">second VirtualizationCacheLength to compare.</param>
        /// <returns>true if specified VirtualizationCacheLengths have same value 
        /// and unit type.</returns>
        public static bool operator ==(VirtualizationCacheLength cl1, VirtualizationCacheLength cl2)
        {
            return (cl1.CacheBeforeViewport == cl2.CacheBeforeViewport
                    && cl1.CacheAfterViewport == cl2.CacheAfterViewport);
        }

        /// <summary>
        /// Overloaded operator, compares 2 VirtualizationCacheLength's.
        /// </summary>
        /// <param name="cl1">first VirtualizationCacheLength to compare.</param>
        /// <param name="cl2">second VirtualizationCacheLength to compare.</param>
        /// <returns>true if specified VirtualizationCacheLengths have either different value or 
        /// unit type.</returns>
        public static bool operator !=(VirtualizationCacheLength cl1, VirtualizationCacheLength cl2)
        {
            return (cl1.CacheBeforeViewport != cl2.CacheBeforeViewport
                              || cl1.CacheAfterViewport != cl2.CacheAfterViewport);
        }

        /// <summary>
        /// Compares this instance of VirtualizationCacheLength with another object.
        /// </summary>
        /// <param name="oCompare">Reference to an object for comparison.</param>
        /// <returns><c>true</c>if this VirtualizationCacheLength instance has the same value 
        /// and unit type as oCompare.</returns>
        override public bool Equals(object oCompare)
        {
            if (oCompare is VirtualizationCacheLength)
            {
                VirtualizationCacheLength l = (VirtualizationCacheLength)oCompare;
                return (this == l);
            }
            else
                return false;
        }

        /// <summary>
        /// Compares this instance of VirtualizationCacheLength with another instance.
        /// </summary>
        /// <param name="cacheLength">Cache length instance to compare.</param>
        /// <returns><c>true</c>if this VirtualizationCacheLength instance has the same value 
        /// and unit type as cacheLength.</returns>
        public bool Equals(VirtualizationCacheLength cacheLength)
        {
            return (this == cacheLength);
        }

        /// <summary>
        /// <see cref="Object.GetHashCode"/>
        /// </summary>
        /// <returns><see cref="Object.GetHashCode"/></returns>
        public override int GetHashCode()
        {
            return ((int)_cacheBeforeViewport + (int)_cacheAfterViewport);
        }

        /// <summary>
        /// Returns cacheBeforeViewport part of this VirtualizationCacheLength instance.
        /// </summary>
        public double CacheBeforeViewport { get { return _cacheBeforeViewport; } }

        /// <summary>
        /// Returns cacheAfterViewport part of this VirtualizationCacheLength instance.
        /// </summary>
        public double CacheAfterViewport { get { return _cacheAfterViewport; } }

        /// <summary>
        /// Returns the string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return VirtualizationCacheLengthConverter.ToString(this, CultureInfo.InvariantCulture);
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        private double _cacheBeforeViewport;
        private double _cacheAfterViewport;
        #endregion Private Fields
    }
}

