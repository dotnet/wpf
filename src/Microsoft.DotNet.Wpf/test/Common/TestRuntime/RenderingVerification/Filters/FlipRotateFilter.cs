// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using
        using System;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Imaging;
        using Microsoft.Test.RenderingVerification;
    #endregion using

    /// <summary>
    /// The angle to apply for the rotation
    /// </summary>
    public enum RotationValue
    { 
        /// <summary>
        /// No rotation
        /// </summary>
        NoRotation = 0,
        /// <summary>
        /// Rotation of 90 degree ( clockwise ) -- ie :  Math.PI * 3/2
        /// </summary>
        Rotation90DegreeClockWise = 90,
        /// <summary>
        /// Rotation of 180 degrees -- ie :  Math.PI
        /// </summary>
        Rotation180Degree = 180,
        /// <summary>
        /// Rotation of 90 degree ( trigometric ) -- ie :  Math.PI / 2
        /// </summary>
        Rotation90DegreeCounterClockWise = 270    // set to 270 instead of -90 to ease avalon interop
    }

    /// <summary>
    /// A Flip and Rotation Filter
    /// </summary>
    public class FlipRotateFilter: Filter
    {
    
        #region Constants
            private const string FLIPVERTICAL = "FlipVertical";
            private const string FLIPHORIZONTAL = "FlipHorizontal";
            private const string ROTATION = "Rotation";
        #endregion Constants

        #region Properties
            #region Private Properties
                private SpatialTransformFilter _innerFilter = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Flip the image horizontally?
                /// </summary>
                public bool FlipVertical
                {
                    get
                    {
                        return (bool)this[FLIPVERTICAL].Parameter;
                    }
                    set
                    {
                        this[FLIPVERTICAL].Parameter = value;
                    }
                }
                /// <summary>
                /// Flip the image vertically?
                /// </summary>
                public bool FlipHorizontal
                {
                    get
                    {
                        return (bool)this[FLIPHORIZONTAL].Parameter;
                    }
                    set
                    {
                        this[FLIPHORIZONTAL].Parameter = value;
                    }
                }
                /// <summary>
                /// The rotation of the image in 90 degree increments
                /// </summary>
                public RotationValue Rotation
                {
                    get
                    {
                        return (RotationValue)this[ROTATION].Parameter;
                    }
                    set
                    {
                        if (value != RotationValue.NoRotation && 
                            value != RotationValue.Rotation90DegreeClockWise && 
                            value != RotationValue.Rotation180Degree && 
                            value != RotationValue.Rotation90DegreeCounterClockWise)
                        {
                            throw new RenderingVerificationException("Rotation must be one of the specified enum !");
                        }

                        this[ROTATION].Parameter = value;
                    }
                }
                /// <summary>
                /// Get the description for this filter
                /// </summary>
                /// <value></value>
                public override string FilterDescription
                {
                    get
                    {
                        return "Flip and rotate an image (90 degree increment) -- MIL filter";
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Flip/Rotate Filter constructor
            /// </summary>
            public FlipRotateFilter()
            {
                _innerFilter = new SpatialTransformFilter();

                FilterParameter flipVertical = new FilterParameter(FLIPVERTICAL, "Verical flip of the image", (bool)false);
                FilterParameter flipHorizontal = new FilterParameter(FLIPHORIZONTAL, "Horizontal Flip of the image", (bool)false);
                FilterParameter rotation = new FilterParameter(ROTATION, "Rotate the image", (RotationValue)RotationValue.NoRotation);

                AddParameter(flipVertical);
                AddParameter(flipHorizontal);
                AddParameter(rotation);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Filter Implementation
                /// </summary>
                protected override IImageAdapter ProcessFilter(IImageAdapter source)
                {
                    _innerFilter.Rotation = (double)Rotation;
                    // Reposition the origin on the top left corner
                    int computeOffset = (int)Rotation;
                    if (computeOffset < 0) { computeOffset = computeOffset % 360 + 360; }
                    switch (computeOffset / 90)
                    { 
                        case 0 : // No offset
                            break;
                        case 1 : 
                            _innerFilter.HorizontalOffset = source.Height - 1;
                            break;
                        case 2 :
                            _innerFilter.HorizontalOffset = source.Width - 1;
                            _innerFilter.VerticalOffset = source.Height - 1;
                            break;
                        case 3 :
                            _innerFilter.VerticalOffset = source.Width - 1;
                            break;
                        default:
                            // Should never occurs as it should be catch by the setter
                            throw new ArgumentOutOfRangeException("Rotation should be a multiple of 90 degree");
                    }
                    _innerFilter.VerticalFlip = FlipVertical;
                    _innerFilter.HorizontalFlip = FlipHorizontal;
                    return _innerFilter.Process(source);
                }
            #endregion Public Methods
        #endregion Methods
    }
}
