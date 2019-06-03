// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Definition of text embedded object
//
//  Spec:      http://avalon/text/DesignDocsAndSpecs/Text%20Formatting%20API.doc
//
//


using System;
using System.Collections;
using System.Windows;
using System.Windows.Media;
using MS.Internal.TextFormatting;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// Provide definition for a kind of text content in which measuring, hittesting 
    /// and drawing of the entire content is done in whole. Example of that kind of 
    /// content is a button in the middle of the line.
    /// </summary>
    public abstract class TextEmbeddedObject : TextRun
    {
        /// <summary>
        /// Line break condition before text object
        /// </summary>
        public abstract LineBreakCondition BreakBefore
        { get; }


        /// <summary>
        /// Line break condition after text object
        /// </summary>
        public abstract LineBreakCondition BreakAfter
        { get; }


        /// <summary>
        /// Flag indicates whether text object has fixed size regardless of where 
        /// it is placed within a line
        /// </summary>
        public abstract bool HasFixedSize
        { get; }


        /// <summary>
        /// Get text object measurement metrics that will fit within the specified
        /// remaining width of the paragraph
        /// </summary>
        /// <param name="remainingParagraphWidth">remaining paragraph width</param>
        /// <returns>text object metrics</returns>
        public abstract TextEmbeddedObjectMetrics Format(
            double  remainingParagraphWidth
            );


        /// <summary>
        /// Get computed bounding box of text object
        /// </summary>
        /// <param name="rightToLeft">run is drawn from right to left</param>
        /// <param name="sideways">run is drawn with its side parallel to baseline</param>
        /// <returns>computed bounding box size of text object</returns>
        public abstract Rect ComputeBoundingBox(
            bool    rightToLeft,
            bool    sideways
            );


        /// <summary>
        /// Draw text object
        /// </summary>
        /// <param name="drawingContext">drawing context</param>
        /// <param name="origin">origin where the object is drawn</param>
        /// <param name="rightToLeft">run is drawn from right to left</param>
        /// <param name="sideways">run is drawn with its side parallel to baseline</param>
        public abstract void Draw(
            DrawingContext      drawingContext,
            Point               origin,
            bool                rightToLeft,
            bool                sideways
            );
    }



    /// <summary>
    /// Text object properties
    /// </summary>
    public class TextEmbeddedObjectMetrics
    {
        private double          _width;
        private double          _height;
        private double          _baseline;


        /// <summary>
        /// Construct a text object size
        /// </summary>
        /// <param name="width">object width</param>
        /// <param name="height">object height</param>
        /// <param name="baseline">object baseline in ratio relative to run height</param>
        public TextEmbeddedObjectMetrics(
            double          width,
            double          height,
            double          baseline
            )
        {
            _width = width;
            _height = height;
            _baseline = baseline;
        }


        /// <summary>
        /// Object width
        /// </summary>
        public double Width
        {
            get { return _width; }
        }


        /// <summary>
        /// Object height
        /// </summary>
        /// <value></value>
        public double Height
        {
            get { return _height; }
        }


        /// <summary>
        /// Object baseline in ratio relative to run height
        /// </summary>
        public double Baseline
        {
            get { return _baseline; }
        }
    }
}
