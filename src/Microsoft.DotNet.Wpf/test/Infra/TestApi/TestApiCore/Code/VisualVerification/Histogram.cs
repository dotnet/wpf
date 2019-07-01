// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Microsoft.Test.VisualVerification
{
    /// <summary>
    /// The Histogram class represents a histogram curve, expressed in terms of frequency (proportion of total pixels) 
    /// over brightness (from 0 to 255). In other words, the Histogram class represents the percentage (proportion) of 
    /// pixels that have brightness of 0, 1, etc. <a href="http://en.wikipedia.org/wiki/Image_histogram">This page</a> provides 
    /// a good introduction to <i>image histograms</i>.
    /// <p/>
    /// For testing purposes "brightness" is often equated to "difference". Thus, one is able to construct a "difference
    /// histogram" from a "difference shapshot" and compare that histogram to a histogram of "expected maximum differences" 
    /// (also knows as a "tolerance histogram") in order to determine whether a visual verification test passes or fails.
    /// <p/>
    /// A Histogram object can be loaded from a XML file or generated from a Snapshot object.
    /// </summary>
    public class Histogram
    {
        #region Constructor
        /// <summary>
        /// Creates a zero tolerance histogram.
        /// </summary>
        internal Histogram()
        {
            graph = new double[histogramSize];
            for (int i = 0; i < histogramSize; i++)
            {
                graph[i] = 0;
            }
        }
        #endregion

        #region Static Initializers
        /// <summary>
        /// Creates a Histogram object from an existing Snapshot object.
        /// </summary>
        /// <param name="snapshot">The Snapshot object to derive the Histogram from.</param>
        /// <returns>A new instance of Histogram, based on the provided snapshot.</returns>
        public static Histogram FromSnapshot(Snapshot snapshot)
        {
            Histogram h = new Histogram();
            //Here we count how much each pixel "weighs" to build our histogram
            double contributionPerPixel = 1 / (double)(snapshot.Width * snapshot.Height);

            for (int row = 0; row < snapshot.Height; row++)
            {
                for (int column = 0; column < snapshot.Width; column++)
                {
                    //Scale up the brightness from 0-1 to 0-256 to index the value to a slot on the graph
                    h.graph[(Byte)(snapshot[row, column].GetBrightness() * 256)] += contributionPerPixel;
                }
            }
            return h;
        }

        /// <summary>
        /// Creates a Histogram object from a histogram curve file.
        /// </summary>
        /// <param name="filePath">Name of the file containing the histogram curve.</param>
        /// <returns>A new instance of Histogram, based on the specified file.</returns>
        public static Histogram FromFile(string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            using (Stream s = new FileInfo(filePath).OpenRead())
            {
                xmlDoc.Load(s);
            }

            //Tolerance Node List is a means of containing multiple Tolerance curve profiles within a histogram
            //One and only one tolerance element is supported
            XmlNodeList toleranceNodeList = xmlDoc.DocumentElement.SelectNodes("descendant::Tolerance");
            if (toleranceNodeList.Count != 1)
            {
                throw new XmlException("An unsupported number of Tolerance sets were found.");
            }
            XmlNode toleranceNode = toleranceNodeList[0];

            //Point nodes can be incompletely defined - these will be interpolated in 
            XmlNodeList nodeList = toleranceNode.SelectNodes("Point");
            if (nodeList.Count == 0)
            {
                throw new XmlException("An insufficient number of Points were found");
            }

            SortedDictionary<byte, double> loadedTable = new SortedDictionary<byte, double>();
            for (int t = 0; t < nodeList.Count; t++)
            {
                byte x = byte.Parse(nodeList[t].Attributes["x"].InnerText, NumberFormatInfo.InvariantInfo);
                double y = double.Parse(nodeList[t].Attributes["y"].InnerText, NumberFormatInfo.InvariantInfo);
                VerifyPoint(x, y);
                loadedTable[x] = y;
            }

            // Populate the histogram with the loaded points, and interpolate for any omitted elements.
            Histogram result = new Histogram();
            result.InterpolatePoints(loadedTable);
            return result;
        }
        #endregion

        #region Public Members

        //provide some access to the histogram data.
        /// <summary>
        /// The data of the histogram.
        /// </summary>
        /// <param name="column">Which column of the histogram you want.</param>
        /// <returns>A double value between 0 and 1.</returns>
        public double this[int column]
        {
            get { return graph[column]; }
            set { graph[column] = value; }
        }

        /// <summary>
        /// Create a snapshot to visualize this histogram, and save it to a file.
        /// The graph will be 100 pixels high and 256 columns wide - one for each 'bin' in the histogram.
        /// The snapshot generated will be framed and slightly larger.
        /// </summary>
        public void ToGraph(string filePath, ImageFormat imageFormat)
        {
            Snapshot s = new Snapshot(2 + VisualizationHeight, 4 + histogramSize);//put a border around it

            //clear the rect.
            for (int row = 0; row < s.Height; row++)
            {
                for (int col = 0; col < s.Width; col++)
                {
                    s[row, col] = VisualizationBGColor;
                }
            }

            //frame the data. We could use some more explanatory text perhaps
            s.DrawLine(0, 0, histogramSize - 1, VisualizationMGColor); // left side 

            //ensure that the drawn values are normalized (defensive coding)
            double maxHeight = 0;
            for (int col = 0; col < histogramSize; col++)
            {
                maxHeight = Math.Max(graph[col], maxHeight);
            }

            int heightPrev = 0;
            //Visualize the histogram with a 2d line graph
            for (int col = 0; col < histogramSize - 2; col++)
            {
                //draw a line into the snapshot to represent the height of this histogram bin
                int height = (int)(graph[col] / maxHeight * VisualizationHeight);

                //if (col > 0) { heightPrev = (int)(graph[col - 1] * VisualizationHeight); }
                // draw a vertical line between this columns value and the previous ones
                // that's the near-vertical case taken care of. The loop covers the near-horizontal,
                // so there's no need to code up a full Bresenham line drawing function 
                s.DrawLine(col + 1, Math.Min(heightPrev, height), s.Height, VisualizationMGColor);
                s.DrawLine(col + 1, Math.Min(heightPrev, height) - 1, Math.Max(heightPrev, height), VisualizationFGColor);
                heightPrev = height;
            }

            s.ToFile(filePath, imageFormat);
        }

        /// <summary>
        /// Merges the specified input histogram curve with the current histogram by accumulating the 
        /// per-brightness peak error quantities of two histograms. The Merge operation merges the peak 
        /// values of the two histograms.
        /// </summary>
        /// <param name="histogram">The histogram curve to be merged with.</param>
        /// <returns>A new Histogram object, containing the peak values of both histogram curves.</returns>
        public Histogram Merge(Histogram histogram)
        {
            Histogram result = new Histogram();
            for (int i = 0; i < histogramSize; i++)
            {
                result.graph[i] = Math.Max(this.graph[i], histogram.graph[i]);
            }
            return result;
        }

        /// <summary>
        /// Saves the Histogram object to an XML file representation.
        /// </summary>
        /// <param name="filePath">The path of the XML histogram file to be stored.</param>
        public void ToFile(string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("Histogram");
            {
                //Only one Tolerance node is supported
                XmlNode toleranceNode = xmlDoc.CreateElement("Tolerance");
                for (int i = 0; i < histogramSize; i++)
                {
                    XmlNode pointNode = xmlDoc.CreateElement("Point");

                    XmlAttribute xAttribute = xmlDoc.CreateAttribute("x");
                    xAttribute.InnerText = i.ToString(NumberFormatInfo.InvariantInfo);
                    pointNode.Attributes.Append(xAttribute);

                    XmlAttribute yAttribute = xmlDoc.CreateAttribute("y");
                    yAttribute.InnerText = graph[i].ToString("G17", NumberFormatInfo.InvariantInfo);
                    pointNode.Attributes.Append(yAttribute);

                    toleranceNode.AppendChild(pointNode);
                }
                rootNode.AppendChild(toleranceNode);
            }
            xmlDoc.AppendChild(rootNode);
            xmlDoc.Save(filePath);
        }

        #endregion

        #region Internal Members

        /// <summary>
        /// Evaluates if the frequencies on this histogram curve are less than supplied histogram for all levels of brightness
        /// Note: The 0 brightness level frequency values are not evaluated here - this is where the balance of pixels reside.
        /// </summary>
        /// <param name="tolerance">The histogram curve to be tested against</param>
        /// <returns>True if all brightness frequencies of the tolerance histogram exceed this histogram. False otherwise.</returns>
        internal bool IsLessThan(Histogram tolerance)
        {
            bool result = true;
            //We are intentionally ignoring the 0 intensity - We *want* as many pixels to be there as possible, so this is not a failure condition.
            for (int i = 1; i < histogramSize; i++)
            {
                if (this.graph[i] > tolerance.graph[i])
                {
                    result = false;
                }
            }
            return result;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Linearly Interpolates along the sparse set of points in the LoadTable to produce a fully populated Histogram object.
        /// </summary>
        /// <param name="loadTable">The sparse set of points representing the Histogram.</param>
        private void InterpolatePoints(SortedDictionary<byte, double> loadTable)
        {
            if (!loadTable.ContainsKey(0) || !loadTable.ContainsKey(255))
            {
                throw new InvalidDataException("InterpolatePoints requires an entry for the first (0th) and last (255th) entry to populate the histogram.");
            }

            byte prev = 0;
            foreach (byte curr in loadTable.Keys)
            {
                //For each key after 0, we interpolate along and fill graph values prev+1 to curr
                if (0 != curr)
                {
                    for (byte i = (byte)(prev + 1); i < curr; i++)
                    {
                        graph[i] = LinearInterpolate((curr - prev) / (i - prev), loadTable[prev], loadTable[curr]);
                    }
                }
                //We set current value to be previous and set current value entry onto graph
                prev = curr;
                graph[curr] = loadTable[curr];
            }
        }

        /// <summary>
        /// Provides a sample via linear interpolation between start and end value by specified proportion.
        /// </summary>
        /// <param name="proportion">The proportion of weight to be allocated to the start value (from 0 to 1).</param>
        /// <param name="startValue">The starting value to start interpolating from.</param>
        /// <param name="endValue">The end value to stop interpolation at.</param>
        /// <returns></returns>
        private static double LinearInterpolate(double proportion, double startValue, double endValue)
        {
            return startValue + (endValue - startValue) / proportion;
        }

        /// <summary>
        /// Throws if x or y is invalid.
        /// </summary>
        /// <param name="x">X Coordinate.</param>
        /// <param name="y">Y Coordinate.</param>
        private static void VerifyPoint(byte x, double y)
        {
            //do nothing for x - all possible values of the byte representation are legitimate.
            if (y < 0 || y > 1)
            {
                throw new ArgumentOutOfRangeException("y");
            }
        }

        private Color VisualizationFGColor { get { return Color.Blue; } }
        private Color VisualizationMGColor { get { return Color.Red; } }
        private Color VisualizationBGColor { get { return Color.Gray; } }

        private const int VisualizationHeight = 100;
        private const int histogramSize = 256;
        private double[] graph;

        #endregion
    }
}
