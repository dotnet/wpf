// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    #region Namespaces.
        using System;
        using System.IO;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Imaging;
        using Microsoft.Test.RenderingVerification.Filters;
    #endregion Namespaces.

    /// <summary>
    /// The mode of comparison
    /// </summary>
    public enum ChannelCompareMode 
    {
        /// <summary>
        /// Undefined compare mode
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Alpha Red Green Blue should be used
        /// </summary>
        ARGB,
        /// <summary>
        /// Red Green Blue should be used
        /// </summary>
        RGB,
        /// <summary>
        /// Alpha should be used
        /// </summary>
        A,
        /// <summary>
        /// Red should be used
        /// </summary>
        R,
        /// <summary>
        /// Green should be used
        /// </summary>
        G,
        /// <summary>
        /// Blue should be used
        /// </summary>
        B
    };
    
    /// <summary>
    /// The interface to compare images
    /// </summary>
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public interface IImageComparator 
    {
        /// <summary>
        /// The error image resulting from the comparison
        /// </summary>
        /// <param name="errorDiffType">The type of difference to return</param>
        /// <returns></returns>
        IImageAdapter GetErrorDifference(ErrorDifferenceType errorDiffType);
        /// <summary>
        /// The 2d affine transform applied to the source image
        /// </summary>
        Matrix2D            TransformMatrix        {set;get;}
        /// <summary>
        /// The tolerance defining the thresholds for the error levels
        /// </summary>
        Curve          Curve          {get;}
        /// <summary>
        /// The level of low-pass filtering to be applied
        /// </summary>
        int                 FilterLevel      {set;get;}
        /// <summary>
        /// The comparison mode {ARGB,RGB,R,G,B}
        /// </summary>
        ChannelCompareMode  ChannelsInUse    {set;get;}
        /// <summary>
        /// Compares two images, using the different properties: 
        /// filter,mode,transfom,tolerance or the property default value 
        /// if the property was not set
        /// <param name="source"> the source image</param>
        /// <param name="target"> the target image</param>
        /// </summary>
        bool Compare(IImageAdapter source, IImageAdapter target);
    }

    /// <summary>
    /// The image adapter interface, point to struct implementing IColor
    /// </summary>
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public interface IImageAdapter : ICloneable
    {
        /// <summary>
        /// The Width of the image
        /// </summary>
        int Width  { get; }
        /// <summary>
        /// The Height of the image
        /// </summary>
        int Height { get; }
        /// <summary>
        /// The pixel accessor
        /// </summary>
        IColor  this [ int x, int y ] { get; set; }
        /// <summary>
        /// Retrieve a pointer to an object containing the image Metadata information (DPI / Copyright / ...)
        /// </summary>
        /// <value></value>
        IMetadataInfo Metadata { get;}
        /// <summary>
        /// DPI in the X dimension
        /// </summary>
        double DpiX { get;}
        /// <summary>
        /// DPI in the Y dimension
        /// </summary>
        double DpiY { get;}
    }

    /// <summary>
    /// The Metadata information interface, points to object (implementing IMetaData) containing the image metadata information
    /// </summary>
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public interface IMetadataInfo : ICloneable
    {
        /// <summary>
        /// Returns a collection of all PropertyItems associated with this object
        /// </summary>
        /// <value></value>
        PropertyItem[] PropertyItems{get;set;}
    }

    /// <summary>
    /// The interface for the internal color representation
    /// </summary>
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public interface IColor : ICloneable
    {
        /// <summary>
        /// Get/set the all the channels at once
        /// </summary>
        /// <value></value>
        int ARGB { get;set; }
        /// <summary>
        /// Get/set the Red, Green and Blue channels at once
        /// </summary>
        /// <value></value>
        int RGB { get;set; }
        /// <summary>
        /// Get/set the Extended value for the Alpha channel
        /// </summary>
        /// <value></value>
        double ExtendedAlpha { get;set; }
        /// <summary>
        /// Get/set the normalized value for the Alpha channel
        /// </summary>
        /// <value></value>
        double Alpha { get;set; }
        /// <summary>
        /// Get/set the Alpha channel
        /// </summary>
        /// <value></value>
        byte A {get;set;}
        /// <summary>
        /// Get/set the Extended value for the Red channel
        /// </summary>
        /// <value></value>
        double ExtendedRed { get;set;}
        /// <summary>
        /// Get/set the normalized value for the Red channel
        /// </summary>
        /// <value></value>
        double Red { get;set;}
        /// <summary>
        /// Get/set the Red channel
        /// </summary>
        /// <value></value>
        byte R { get;set;}
        /// <summary>
        /// Get/set the Extended value for the Green channel
        /// </summary>
        /// <value></value>
        double ExtendedGreen { get;set; }
        /// <summary>
        /// Get/set the normalized value for the Green channel
        /// </summary>
        /// <value></value>
        double Green { get;set; }
        /// <summary>
        /// Get/set the Green channel
        /// </summary>
        /// <value></value>
        byte G {get;set;}
        /// <summary>
        /// Get/set the Extended value for the Blue channel
        /// </summary>
        /// <value></value>
        double ExtendedBlue { get;set; }
        /// <summary>
        /// Get/set the normalized value for the Blue channel
        /// </summary>
        /// <value></value>
        double Blue { get;set;}
        /// <summary>
        /// Get/set the Blue channel
        /// </summary>
        /// <value></value>
        byte B { get;set;}
        /// <summary>
        /// Convert this Color to the standard "System.Drawing.Color" type
        /// </summary>
        /// <returns></returns>
        System.Drawing.Color ToColor();
        /// <summary>
        /// Get/set the color as Empty 
        /// </summary>
        /// <value></value>
        bool IsEmpty{get;set;}
        /// <summary>
        /// Retrieve if this type can effectively deal with scRGB color (no information loss when filtering)
        /// </summary>
        /// <value></value>
        bool SupportExtendedColor{get;}
        /// <summary>
        /// Get/set the color to scRgb (Gamma 1.0) or not (Gamma 2.2)
        /// </summary>
        /// <value></value>
        bool IsScRgb { get;set;}
        /// <summary>
        /// Get/set the Max value for all channels when normalizing
        /// </summary>
        /// <value></value>
        double MaxChannelValue { get;set;}
        /// <summary>
        /// Get/set the Min value for all channels when normalizing
        /// </summary>
        /// <value></value>
        double MinChannelValue { get;set;}
        /// <summary>
        /// Get/set the Normalization value
        /// </summary>
        /// <value></value>
        double NormalizedValue { get;set;}
    }
}
