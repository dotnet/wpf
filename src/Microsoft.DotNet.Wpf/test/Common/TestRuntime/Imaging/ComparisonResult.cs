// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Imaging
{
    #region Namespaces.

    using System;
    using System.Collections;
    using System.Drawing;

    #endregion Namespaces.

    /// <summary>
    /// This class is used to represent the result of an image matching
    /// operation.
    /// </summary>
    public class ComparisonResult
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors.

        /// <summary>Creates a new MatchResult instance.</summary>
        public ComparisonResult()
        {
            this.identical = true;
            this.criteriaMet = true;
            this.differenceList = new ArrayList();
        }

        #endregion Constructors.

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public methods.

        /// <summary>Highlights differences on the specified bitmap.</summary>
        public void HighlightDifferences(Bitmap bitmap)
        {
            // Group points in rectangles.
            PointRectangleGrouper grouper = new PointRectangleGrouper();
            for (int i = 0; i < differences.Length; i++)
            {
                grouper.AddPoint(differences[i].PixelPosition);
            }

            // Highlight the rectangles.
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Rectangle[] rects = grouper.GetRectangles();
                for (int i = 0; i < rects.Length; i++)
                {
                    Rectangle r = rects[i];
                    Rectangle grown = Rectangle.FromLTRB(r.Left - 1, r.Top - 1,
                        r.Right + 1, r.Bottom + 1);
                    g.DrawRectangle(Pens.Red, grown);
                }
            }
        }

        /// <summary>Returns a string representation of this object.</summary>
        /// <returns>A string representation of this object.</returns>
        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(
                (1 + differences.Length) * 32);
            sb.Append(ToStringBrief());
            ComparisonDifference lastDifference = null;
            ComparisonDifference lastContiguous = null;
            for (int i = 0; i < differences.Length; i++)
            {
                bool inContiguousRange = lastContiguous != null;

                ComparisonDifference difference = differences[i];
                if (difference.IsSimilarTo(lastDifference))
                {
                    if (difference.IsContiguousTo(lastDifference))
                    {
                        lastContiguous = difference;
                    }
                    else
                    {
                        if (inContiguousRange)
                        {
                            DescribeContiguous(lastContiguous, sb);
                            lastContiguous = null;
                        }
                        sb.AppendFormat(",({0};{1})", difference.PixelPosition.X,
                            difference.PixelPosition.Y);
                    }
                }
                else
                {
                    if (inContiguousRange)
                    {
                        DescribeContiguous(lastContiguous, sb);
                        lastContiguous = null;
                    }
                    if (i != 0) sb.Append("\r\n");
                    sb.AppendFormat("  {0}", difference.ToString());
                }
                lastDifference = difference;
            }
            if (lastContiguous != null)
            {
                DescribeContiguous(lastContiguous, sb);
            }
            sb.Append("\r\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns a string representation of this object, without logging
        /// every difference.
        /// </summary>
        /// <returns>A string representation of this object.</returns>
        public string ToStringBrief()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
            sb.AppendFormat("Criteria met: {0}\r\n", CriteriaMet);
            sb.AppendFormat("Identical: {0}\r\n", Identical);
            sb.AppendFormat("Master image pixel count: {0}\r\n", masterImagePixelCount);
            sb.AppendFormat("Max error proportion (to pixel count): {0}\r\n",
                (float) masterImagePixelCount * errorProportion);
            sb.AppendFormat("Differences [{0}]\r\n", differences.Length);
            return sb.ToString();
        }

        #endregion Public methods.

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public properties.

        /// <summary>Whether the comparison criteria was met.</summary>
        public bool CriteriaMet
        {
            get { return this.criteriaMet; }
        }

        /// <summary>Whether the images are identical.</summary>
        public bool Identical
        {
            get { return this.identical; }
        }

        /// <summary>Gets the list of differences.</summary>
        /// <returns>An array of ComparisonDifference objects.</returns>
        public ComparisonDifference[] GetDifferences()
        {
            return this.differences;
        }

        #endregion Public properties.

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal methods.

        /// <summary>
        /// Adds a difference to the difference list.
        /// </summary>
        /// <param name="difference">Difference to add.</param>
        internal void AddDifference(ComparisonDifference difference)
        {
            if (difference != null)
                differenceList.Add(difference);
        }

        /// <summary>
        /// Sets the value for the CriteriaMet property. This must be set
        /// to false when the criteria is not met.
        /// </summary>
        /// <param name="metValue">Value for the CriteriaMet property.</param>
        internal void SetCriteriaMet(bool metValue)
        {
            this.criteriaMet = metValue;
        }

        /// <summary>
        /// Sets the value for the Identical property. This must be set
        /// when a difference is detected but does not pass the tolerance
        /// threshold.
        /// </summary>
        internal void SetIdentical(bool identicalValue)
        {
            this.identical = identicalValue;
        }

        /// <summary>Sets the pixel count for the Master Image used.</summary>
        internal void SetMasterImagePixelCount(long countValue)
        {
            this.masterImagePixelCount = countValue;
        }

        /// <summary>Sets the maximum error proportion used.</summary>
        internal void SetErrorProportion(float errorValue)
        {
            this.errorProportion = errorValue;
        }

        /// <summary>
        /// This lets the class know that no more differences will be added.
        /// Normally, the ComparisonOperation object is the one that calls
        /// this method.
        /// </summary>
        internal void FinishedComparison()
        {
            differences = new ComparisonDifference[differenceList.Count];
            for (int i = 0; i < differences.Length; i++)
            {
                differences[i] = (ComparisonDifference) differenceList[i];
            }
        }

        #endregion Internal methods.

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private methods.

        /// <summary>
        /// Describes the second part of a contiguous range of pixel
        /// differences by appending into the specified
        /// StringBuilder.
        /// </summary>
        private void DescribeContiguous(ComparisonDifference lastContiguous,
            System.Text.StringBuilder sb)
        {
            System.Diagnostics.Debug.Assert(lastContiguous != null);
            System.Diagnostics.Debug.Assert(sb != null);

            sb.Append("-(");
            sb.Append(lastContiguous.PixelPosition.X);
            sb.Append(";");
            sb.Append(lastContiguous.PixelPosition.Y);
            sb.Append(")");
        }

        #endregion Private methods.

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private fields.

        private bool criteriaMet;
        private ComparisonDifference[] differences;
        private ArrayList differenceList;
        private bool identical;
        private long masterImagePixelCount;
        private float errorProportion;

        #endregion Private fields.

        //------------------------------------------------------
        //
        //  Inner Classes
        //
        //------------------------------------------------------

        #region Inner classes.

        /// <summary>
        /// Groups points in bounding rectangles.
        /// </summary>
        private class PointRectangleGrouper
        {
            /// <summary>List of possibly disjoint rectangles.</summary>
            private ArrayList rects = new ArrayList();

            [Flags()]
            private enum Edge { Left = 1, Top = 2, Right = 4, Bottom = 8 };

            /// <summary>
            /// Gets the edges of rectangle r on which point p lies.
            /// </summary>
            private static Edge GetEdges(Rectangle r, Point p)
            {
                Edge result = 0;
                if ((r.Left - 1) == p.X && (r.Top <= p.Y && p.Y <= r.Bottom))
                    result |= Edge.Left;
                if ((r.Right + 1) == p.X && (r.Top <= p.Y && p.Y <= r.Bottom))
                    result |= Edge.Right;
                if ((r.Top - 1) == p.Y && (r.Left <= p.X && p.X <= r.Right))
                    result |= Edge.Top;
                if ((r.Bottom + 1) == p.Y && (r.Left <= p.X && p.X <= r.Right))
                    result |= Edge.Bottom;
                return result;
            }

            /// <summary>
            /// Adds a point to an existing rectangle or to a new rectangle.
            /// </summary>
            public void AddPoint(Point p)
            {
                for (int i = 0; i < rects.Count; i++)
                {
                    Rectangle r = (Rectangle) rects[i];
                    if (r.Contains(p))
                        return;
                    Edge edge = GetEdges(r, p);
                    if ((edge & Edge.Left) != 0)
                        rects[i] = Rectangle.FromLTRB(r.Left - 1, r.Top, r.Right, r.Bottom);
                    if ((edge & Edge.Top) != 0)
                        rects[i] = Rectangle.FromLTRB(r.Left, r.Top - 1, r.Right, r.Bottom);
                    if ((edge & Edge.Right) != 0)
                        rects[i] = Rectangle.FromLTRB(r.Left, r.Top, r.Right + 1, r.Bottom);
                    if ((edge & Edge.Bottom) != 0)
                        rects[i] = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom + 1);
                    if ((int)edge != 0)
                        return;
                }
                rects.Add(new Rectangle(p.X, p.Y, 0, 0));
            }

            public Rectangle[] GetRectangles()
            {
                return (Rectangle[]) rects.ToArray(typeof(Rectangle));
            }
        }

        #endregion Inner classes.
    }

    #region Differences.

    /// <summary>
    /// This class is the base class for differences found in comparsion
    /// operations.
    /// </summary>
    /// <author sdk="True" alias="mruiz">Marcelo</author>
    public abstract class ComparisonDifference
    {
        private Point pixelPosition;

        /// <summary>
        /// Pixel position where difference was found.
        /// </summary>
        public Point PixelPosition
        {
            get { return this.pixelPosition; }
            set { this.pixelPosition = value; }
        }

        /// <summary>Marks the different pixel with a bounding box.</summary>
        /// <param name="graphics">Graphics surface to draw pixel on.</param>
        public void MarkPixel(Graphics graphics)
        {
            MarkPixel(graphics, Color.Red);
        }

        /// <summary>
        /// Marks the different pixel with a bounding box.
        /// </summary>
        /// <param name="graphics">Graphics surface to draw pixel on.</param>
        /// <param name="mark">Color to mark pixel with.</param>
        public void MarkPixel(Graphics graphics, Color mark)
        {
            if (!PixelPosition.IsEmpty)
            {
                graphics.DrawRectangle(new Pen(mark),
                    PixelPosition.X - 1, PixelPosition.Y - 1, 2, 2);
            }
        }

        /// <summary>
        /// Whether this difference is similar to a difference in
        /// another position.
        /// </summary>
        internal virtual bool IsSimilarTo(ComparisonDifference difference)
        {
            if (this == difference)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Whether this difference is adjanct to a difference in
        /// another position.
        /// </summary>
        internal bool IsContiguousTo(ComparisonDifference difference)
        {
            if (difference == null) return false;
            if (this == difference) return false;
            return (difference.pixelPosition.X == this.pixelPosition.X + 1 ||
                difference.pixelPosition.X == this.pixelPosition.X - 1);
        }
    }

    /// <summary>Describes how a value was compared.</summary>
    /// <author sdk="True" alias="mruiz">Marcelo</author>
    public enum ValueComparison
    {
        /// <summary>The compared value was below a reference value.</summary>
        BelowValue,
        /// <summary>The compared value was exactly at a reference value.</summary>
        AtValue,
        /// <summary>The compared value was above a reference value.</summary>
        AboveValue,
        /// <summary>The compared value was within a reference range.</summary>
        InRange
    };

    /// <summary>
    /// This class is used to represent a difference in size between
    /// compared images.
    /// </summary>
    /// <author sdk="True" alias="mruiz">Marcelo</author>
    public class SizeDifference: ComparisonDifference
    {
        private Size expected;
        private Size found;

        internal SizeDifference(Size expected, Size found)
        {
            this.expected = expected;
            this.found = found;
        }

        /// <summary>
        /// Returns a string that describes the difference.
        /// </summary>
        /// <returns>A string that describes the difference.</returns>
        public override string ToString()
        {
            return String.Format("Expected size was ({0}) and found size was {1}",
                expected, found);
        }
    }

    /// <summary>
    /// This class is used to represent a difference in a pixel color between
    /// compared images.
    /// </summary>
    /// <author sdk="True" alias="mruiz">Marcelo</author>
    public class PixelColorDifference: ComparisonDifference
    {
        /// <summary>Expected color.</summary>
        private Color expected;
        /// <summary>Color found.</summary>
        private Color found;

        internal PixelColorDifference(int x, int y, Color expected, Color found)
        {
            this.expected = expected;
            this.found = found;
            PixelPosition = new Point(x, y);
        }

        internal PixelColorDifference(int x, int y,
            byte expectedRed, byte expectedGreen, byte expectedBlue,
            byte foundRed, byte foundGreen, byte foundBlue)
        {
            this.expected = Color.FromArgb(expectedRed, expectedGreen, expectedBlue);
            this.found = Color.FromArgb(foundRed, foundGreen, foundBlue);
            PixelPosition = new Point(x, y);
        }

        /// <summary>
        /// Returns a string that describes the difference.
        /// </summary>
        /// <returns>A string that describes the difference.</returns>
        public override string ToString()
        {
            return String.Format(
                "Expected color at pixel ({0};{1}) was ({2} {3} {4}) and found color was ({5} {6} {7}).",
                PixelPosition.X, PixelPosition.Y,
                expected.R, expected.G, expected.B,
                found.R, found.G, found.B);
        }

        /// <summary>
        /// Whether this difference is similar to a difference in
        /// another position.
        /// </summary>
        internal override bool IsSimilarTo(ComparisonDifference difference)
        {
            PixelColorDifference d = difference as PixelColorDifference;
            if (d == null) return false;
            if (d.expected == this.expected && d.found == this.found) return true;
            return false;
        }
    }

    /// <summary>
    /// This color is used to represent a color difference in relation
    /// to a tolerable color distance.
    /// </summary>
    /// <author sdk="True" alias="mruiz">Marcelo</author>
    public class ColorDistanceDifference: ComparisonDifference
    {
        private float expected;
        private float found;
        private ValueComparison comparison;

        internal ColorDistanceDifference(int x, int y, float expected, float found,
            ValueComparison comparison)
        {
            PixelPosition = new Point(x, y);
            this.expected = expected;
            this.found = found;
            this.comparison = comparison;
        }

        /// <summary>
        /// Returns a string that describes the difference.
        /// </summary>
        /// <returns>A string that describes the difference.</returns>
        public override string ToString()
        {
            return String.Format(
                "Expected color distance {0} {1} found {4} at ({2};{3})",
                comparison, expected, PixelPosition.X, PixelPosition.Y, found);
        }

        /// <summary>
        /// Whether this difference is similar to a difference in
        /// another position.
        /// </summary>
        internal override bool IsSimilarTo(ComparisonDifference difference)
        {
            ColorDistanceDifference d = difference as ColorDistanceDifference;
            if (d == null) return false;
            if (d.expected == this.expected && d.found == this.found) return true;
            return false;
        }
    }

    #endregion Differences.
}
