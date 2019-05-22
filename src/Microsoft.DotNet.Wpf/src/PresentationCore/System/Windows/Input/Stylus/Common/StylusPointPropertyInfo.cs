// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using MS.Utility;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    /// StylusPointPropertyInfo
    /// </summary>
    public class StylusPointPropertyInfo : StylusPointProperty
    {
        /// <summary>
        /// Instance data
        /// </summary>
        private int                     _min;
        private int                     _max;
        private float                   _resolution;
        private StylusPointPropertyUnit _unit;

        /// <summary>
        /// For a given StylusPointProperty, instantiates a StylusPointPropertyInfo with default values
        /// </summary>
        /// <param name="stylusPointProperty"></param>
        public StylusPointPropertyInfo(StylusPointProperty stylusPointProperty) 
            : base (stylusPointProperty) //base checks for null
        {
            StylusPointPropertyInfo info =
                StylusPointPropertyInfoDefaults.GetStylusPointPropertyInfoDefault(stylusPointProperty);
            _min = info.Minimum;
            _max = info.Maximum;
            _resolution = info.Resolution;
            _unit = info.Unit;
}

        /// <summary>
        /// StylusPointProperty
        /// </summary>
        /// <param name="stylusPointProperty"></param>
        /// <param name="minimum">minimum</param>
        /// <param name="maximum">maximum</param>
        /// <param name="unit">unit</param>
        /// <param name="resolution">resolution</param>
        public StylusPointPropertyInfo(StylusPointProperty stylusPointProperty, int minimum, int maximum, StylusPointPropertyUnit unit, float resolution)
            : base(stylusPointProperty) //base checks for null
        {
            // validate unit
            if (!StylusPointPropertyUnitHelper.IsDefined(unit))
            {
                throw new InvalidEnumArgumentException("unit", (int)unit, typeof(StylusPointPropertyUnit));
            }

            // validate min/max
            if (maximum < minimum)
            {
                throw new ArgumentException(SR.Get(SRID.Stylus_InvalidMax), "maximum");
            }

            // validate resolution
            if (resolution < 0.0f)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidStylusPointPropertyInfoResolution), "resolution");
            }

            _min = minimum;
            _max = maximum;
            _resolution = resolution;
            _unit = unit;
        }

        /// <summary>
        /// Minimum
        /// </summary>
        public int Minimum
        {
            get { return _min; }
        }

        /// <summary>
        /// Maximum
        /// </summary>
        public int Maximum
        {
            get { return _max; }
        }

        /// <summary>
        /// Resolution
        /// </summary>
        public float Resolution
        {
            get { return _resolution; }
            internal set { _resolution = value; }
        }

        /// <summary>
        /// Unit
        /// </summary>
        public StylusPointPropertyUnit Unit
        {
            get { return _unit; }
        }

        /// <summary>
        /// Internal helper method for comparing compat for two StylusPointPropertyInfos
        /// </summary>
        internal static bool AreCompatible(StylusPointPropertyInfo stylusPointPropertyInfo1, StylusPointPropertyInfo stylusPointPropertyInfo2)
        {
            if (stylusPointPropertyInfo1 == null || stylusPointPropertyInfo2 == null)
            {
                throw new ArgumentNullException("stylusPointPropertyInfo");
            }

            Debug.Assert((  stylusPointPropertyInfo1.Id != StylusPointPropertyIds.X &&
                            stylusPointPropertyInfo1.Id != StylusPointPropertyIds.Y &&
                            stylusPointPropertyInfo2.Id != StylusPointPropertyIds.X &&
                            stylusPointPropertyInfo2.Id != StylusPointPropertyIds.Y),
                            "Why are you checking X, Y for compatibility?  They're always compatible");
            //
            // we only take ID and IsButton into account, we don't take metrics into account
            //
            return (stylusPointPropertyInfo1.Id == stylusPointPropertyInfo2.Id &&
                    stylusPointPropertyInfo1.IsButton == stylusPointPropertyInfo2.IsButton);
        }
    }
}
