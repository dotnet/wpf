// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Microsoft.Test.VisualVerification
{
    /// <summary>
    /// This class will consist of just the set of data points loaded out of the Tolerance.xml. 
    /// </summary>
    public class ToleranceCurve
    {
        /// <summary>
        ///  Creates a zero legacy tolerance histogram.
        /// </summary>
        public ToleranceCurve()
        {
            graph = new SortedDictionary<byte, double>();
            tolerances = new List<ToleranceLegacy>();
        }

        /// <summary>
        /// Creates a ToleranceCurve object from a histogram curve file.
        /// </summary>
        /// <param name="filePath">Name of the file containing the histogram curve.</param>
        /// <returns>A new instance of ToleranceCurve, based on the specified file.</returns>
        public static ToleranceCurve FromFile(string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            using (Stream s = new FileInfo(filePath).OpenRead())
            {
                try
                {
                    xmlDoc.Load(s);
                }
                catch (XmlException)
                {
                    throw new XmlException("Tolerance file is not a right xml format");
                }
            }

            //Tolerance Node List is a means of containing multiple Tolerance curve profiles within a histogram
            XmlNodeList toleranceNodeList = xmlDoc.DocumentElement.SelectNodes("descendant::Tolerance");
            if (toleranceNodeList.Count == 0)
            {
                throw new XmlException("There is no tolerance node in Tolerance file");
            }

            ToleranceCurve toleranceCurve = new ToleranceCurve();

            for (int i = 0; i < toleranceNodeList.Count; i++)
            {
                ToleranceLegacy toleranceLegacy = new ToleranceLegacy();
                XmlAttribute dpiRatio = toleranceNodeList[i].Attributes["dpiRatio"];
                if (dpiRatio == null)
                {
                    throw new XmlException("It is a invalid Tolerance file.\nThere isn't dpiRadio attribute in tolerance node.");
                }

                toleranceLegacy.DpiRatio = Convert.ToDouble(dpiRatio.InnerText, NumberFormatInfo.InvariantInfo);

                XmlNodeList pointNodeList = toleranceNodeList[i].SelectNodes("Point");

                if (pointNodeList.Count == 0)
                {
                    toleranceCurve.SetGraph(0, 0);
                    toleranceCurve.SetGraph(255, 0);
                    return toleranceCurve;
                }

                List<TolerancePoint> points = new List<TolerancePoint>();
                for (int j = 0; j < pointNodeList.Count; j++)
                {
                    XmlAttribute pointX = pointNodeList[j].Attributes["x"];
                    XmlAttribute pointY = pointNodeList[j].Attributes["y"];
                    if (pointX == null || pointY == null)
                    {
                        throw new XmlException("It is a invalid Tolerance file.\nThere isn't x or y attribute in Point node.");
                    }

                    if (toleranceLegacy.DpiRatio == defaultDpiRatio)
                    {
                        byte x = Convert.ToByte(pointX.InnerText, NumberFormatInfo.InvariantInfo);
                        double y = Convert.ToDouble(pointY.InnerText, NumberFormatInfo.InvariantInfo);
                        toleranceCurve.SetGraph(x, y);
                    }
                    else
                    {
                        TolerancePoint point = new TolerancePoint();
                        point.X = pointX.InnerText;
                        point.Y = pointY.InnerText;
                        points.Add(point);
                    }
                }

                if (toleranceLegacy.DpiRatio != defaultDpiRatio)
                {
                    toleranceLegacy.Points = points;
                    toleranceCurve.AddTolerance(toleranceLegacy);
                }
            }

            return toleranceCurve;
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

        /// <summary>
        /// Linearly Interpolates along the sparse set of points in the LoadTable to produce a fully populated Histogram object.
        /// </summary>
        /// <param name="loadTable">The sparse set of points representing the Histogram.</param>
        internal Histogram InterpolatePoints(SortedDictionary<byte, double> loadTable)
        {
            Histogram result = new Histogram();
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
                        result[i] = LinearInterpolate((double)(curr - prev) / (i - prev), loadTable[prev], loadTable[curr]);
                    }
                }
                //We set current value to be previous and set current value entry onto graph
                prev = curr;
                result[curr] = loadTable[curr];
            }

            return result;
        }

        /// <summary>
        /// Get a copy of the points generated from Tolerance.xml
        /// </summary>
        /// <returns></returns>  
        public SortedDictionary<byte, double> GetGraph()
        {
            SortedDictionary<byte, double> result = new SortedDictionary<byte, double>();
            KeyValuePair<byte, double>[] array = new KeyValuePair<byte, double>[graph.Count];

            graph.CopyTo(array, 0);
            for (int i = 0; i < array.Length; i++)
            {
                byte x = array[i].Key;
                result[x] = array[i].Value;
            }

            return result;
        }

        /// <summary>
        /// Set new value for Tolerance.xml
        /// </summary>
        /// <param name="x">The x Coordinate of brightness</param>
        /// <param name="y">The new value of specified brightness</param>
        public void SetGraph(byte x, double y)
        {
            VerifyPoint(x, y);
            graph[x] = y;
        }

        /// <summary>
        /// Get those tolerance nodes that dpiRatio is not 1 for save
        /// </summary>
        /// <returns>A list of tolerance nodes</returns>
        public List<ToleranceLegacy> GetTolerances()
        {
            return tolerances;
        }

        /// <summary>
        /// Store a tolerance node from Tolerance.xml to a array
        /// </summary>
        /// <param name="legacy">The tolerance node to add</param>
        public void AddTolerance(ToleranceLegacy legacy)
        {
            tolerances.Add(legacy);
        }

        /// <summary>
        /// Saves the ToleranceCurve object to an XML file representation.
        /// </summary>
        /// <param name="filePath">The path of the XML histogram file to be stored.</param>
        public void ToFile(string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("ToleranceCurve.ToFile:file path is null or empty");
            }

            XmlDocument xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("CurveTolerances");

            XmlNode toleranceNode = xmlDoc.CreateElement("Tolerance");

            XmlAttribute dpiRatio = xmlDoc.CreateAttribute("dpiRatio");
            dpiRatio.InnerText = defaultDpiRatio.ToString();
            toleranceNode.Attributes.Append(dpiRatio);
            SortedDictionary<byte, double> loadTable = GetGraph();
            foreach (byte x in loadTable.Keys)
            {
                if ((x == 0 || x == 255) && loadTable[x] == 0)
                {
                    continue;
                }

                XmlNode pointNode = xmlDoc.CreateElement("Point");

                XmlAttribute xAttribute = xmlDoc.CreateAttribute("x");
                xAttribute.InnerText = x.ToString(NumberFormatInfo.InvariantInfo);
                pointNode.Attributes.Append(xAttribute);

                XmlAttribute yAttribute = xmlDoc.CreateAttribute("y");
                yAttribute.InnerText = loadTable[x].ToString(NumberFormatInfo.InvariantInfo);
                pointNode.Attributes.Append(yAttribute);

                toleranceNode.AppendChild(pointNode);
            }

            rootNode.AppendChild(toleranceNode);

            for (int i = 0; i < tolerances.Count; i++)
            {
                toleranceNode = xmlDoc.CreateElement("Tolerance");
                dpiRatio = xmlDoc.CreateAttribute("dpiRatio");
                dpiRatio.InnerText = tolerances[i].DpiRatio.ToString();
                toleranceNode.Attributes.Append(dpiRatio);
                for (int j = 0; j < tolerances[i].Points.Count; j++)
                {
                    XmlNode pointNode = xmlDoc.CreateElement("Point");

                    XmlAttribute xAttribute = xmlDoc.CreateAttribute("x");
                    xAttribute.InnerText = tolerances[i].Points[j].X;
                    pointNode.Attributes.Append(xAttribute);

                    XmlAttribute yAttribute = xmlDoc.CreateAttribute("y");
                    yAttribute.InnerText = tolerances[i].Points[j].Y;
                    pointNode.Attributes.Append(yAttribute);

                    toleranceNode.AppendChild(pointNode);
                }

                rootNode.AppendChild(toleranceNode);
            }

            xmlDoc.AppendChild(rootNode);
            xmlDoc.Save(filePath);
        }

        /// <summary>
        /// Generate a histogram with fully 256 points by interpolate points between those points loaded from Tolerance.xml
        /// </summary>
        /// <returns></returns>
        public Histogram ToHistogram()
        {
            SortedDictionary<byte, double> loadTable = GetGraph();
            if (!loadTable.ContainsKey(0))
            {
                loadTable[0] = 0;
            }

            if (!loadTable.ContainsKey(255))
            {
                loadTable[255] = 0;
            }

            return InterpolatePoints(loadTable);
        }

        private const int histogramSize = 256;
        private const double defaultDpiRatio = 1;
        private SortedDictionary<byte, double> graph;
        private List<ToleranceLegacy> tolerances;
    }

    /// <summary>
    /// For saving those tolerance nodes that dpiRatio is not 1
    /// </summary>
    public class ToleranceLegacy
    {
        /// <summary>
        /// For saving the dpiRatio attribute
        /// </summary>
        public double DpiRatio { get; set; }

        /// <summary>
        /// For saving point nodes
        /// </summary>
        public List<TolerancePoint> Points { get; set; }
    }

    /// <summary>
    /// For saving those point nodes of a tolerance node that dpiRatio is not 1
    /// </summary>
    public class TolerancePoint
    {
        /// <summary>
        /// For saving the x attribute of point node
        /// </summary>
        public string X { get; set; }

        /// <summary>
        /// For saving the y attribute of point node
        /// </summary>
        public string Y { get; set; }
    }
}
