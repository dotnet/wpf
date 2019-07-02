// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    #region Using directives
        using System;
        using System.Xml;
        using System.Globalization;
        using System.Collections.Generic;
    #endregion

    /// <summary>
    /// Summary description for CurveTolerance
    /// </summary>
    public class CurveTolerance : ICloneable
    {
        #region Properties
            private Dictionary<float, SortedDictionary<byte, double>> _mapping = new Dictionary<float, SortedDictionary<byte, double>>();
            private float _currentDpiRatio = 1f;
            /// <summary>
            /// Get/set the dpi ratio user want to set ( ratio = Math.Max(a,b) / Math.Min(a,b) )
            /// </summary>
            public float DpiRatio
            {
                get { return _currentDpiRatio; }
                set 
                {
                    if (value <= 0f) { throw new ArgumentOutOfRangeException("DpiRatio", value, "Ratio must be strictly positive"); }
                    _currentDpiRatio = value;
                }
            }
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create a neww CurveTolerance object
            /// </summary>
            public CurveTolerance() 
            {
            }
            /// <summary>
            /// Create a neww CurveTolerance object
            /// </summary>
            /// <param name="ratio"></param>
            public CurveTolerance(float ratio) : this() 
            { 
                DpiRatio = ratio;
            }
        #endregion Constructors

        #region Methods
            
            /// <summary>
            /// Get the Entries (ControlPoints) for the current Dpiratio
            /// Nore : returns null, if no Entries for the seelcted DpiRatio
            /// </summary>
            [CLSCompliant(false)]
            public SortedDictionary<byte, double> Entries
            {
                get
                {
                    if (_mapping.ContainsKey(DpiRatio) == false) 
                    {
                        _mapping.Add(DpiRatio, new SortedDictionary<byte, double>());
                    }
                    return _mapping[DpiRatio];
                }
            }
            /// <summary>
            /// Get the tolerance percentage for this channel value (and for this DpiRatio).
            /// Note : returns double.NaN if there is no Entries for this DpiRatio
            /// </summary>
            /// <param name="channelValue"></param>
            /// <returns></returns>
            public double InterpolatedValue(byte channelValue)
            {
                if (_mapping.ContainsKey(DpiRatio) == false || _mapping[DpiRatio].Keys.Count == 0) { return double.NaN; }

                SortedDictionary<byte, double> sortedDico = _mapping[DpiRatio];
                SortedDictionary<byte, double>.KeyCollection keyCollection = sortedDico.Keys;
                double retVal = 1.0;
                foreach (byte keyValue in keyCollection)
                {
                    if (keyValue > channelValue) { break; }
                    retVal = sortedDico[keyValue];
                }
                return retVal;
            }

            /// <summary>
            /// Load a tolerance from the specified file
            /// </summary>
            /// <param name="toleranceFileName">The file containing the tolerance</param>
            /// <returns></returns>
            public void LoadTolerance(string toleranceFileName)
            {
                XmlDocument xmlDoc = null;
                if (toleranceFileName == null || toleranceFileName.Trim() == string.Empty)
                {
                    throw new RenderingVerificationException("Cannot pass a null or empty string as Tolerance file name");
                }
                try
                {
                    xmlDoc = new XmlDocument();
                    xmlDoc.Load(toleranceFileName);
                }
                catch (Exception e)
                {
                    throw new RenderingVerificationException("The file passed in is not a valid XML file, see inner Exception for more details", e);
                }
                LoadTolerance(xmlDoc.DocumentElement);
            }
            /// <summary>
            /// Load a tolerance from the specified Xml Node
            /// </summary>
            /// <param name="node">The xml node containing the tolerance</param>
            public void LoadTolerance(XmlNode node)
            {
                if (node == null) { throw new ArgumentNullException("node", "Argument must be set to a valid insance of an object (null passed in)"); }

                _mapping.Clear();

                float dpiRatio = 1;
                XmlNodeList toleranceNodeList = node.SelectNodes("descendant::Tolerance");
                if (toleranceNodeList == null || toleranceNodeList.Count == 0) 
                {
                    // Backward support.
                    if (node.Name != "Tolerance") { throw new XmlException("Unexpected xml format"); }
                    toleranceNodeList = node.OwnerDocument.SelectNodes("descendant::Tolerance");
                    if (node.Attributes.GetNamedItem("dpiRatio") == null)
                    {
                        XmlAttribute attribute = node.OwnerDocument.CreateAttribute("dpiRatio");
                        attribute.InnerText = dpiRatio.ToString(NumberFormatInfo.InvariantInfo);
                        node.Attributes.Append(attribute);
                    }
                    
                }

                foreach(XmlNode toleranceNode in toleranceNodeList)
                {
                    // Get DpiRatio Info
                    XmlNode ratioAttributeNode = toleranceNode.Attributes.GetNamedItem("dpiRatio");
                    if (ratioAttributeNode == null) { throw new XmlException("Xml file not formatted as expected"); }
                    this.DpiRatio = (float)double.Parse(ratioAttributeNode.InnerText, NumberFormatInfo.InvariantInfo);

                    // Get Entries for this Dpi ratio
                    XmlNodeList nodeList = toleranceNode.SelectNodes("Point");
                    if (nodeList.Count == 0) { throw new XmlException("Xml file not formatted as expected"); }
                    double x = double.NaN;
                    double y = double.NaN;
                    
                    for (int t = 0; t < nodeList.Count; t++)
                    {
                        x = double.Parse(nodeList[t].Attributes["x"].InnerText, NumberFormatInfo.InvariantInfo);
                        y = double.Parse(nodeList[t].Attributes["y"].InnerText, NumberFormatInfo.InvariantInfo);
                        if (x != (byte)x) { throw new RenderingVerificationException("x value in Tolerance out of bounds (must be a integer between 0 and 255 -- both value included)"); }
                        if (y < 0.0 || y > 1.0) { throw new RenderingVerificationException("y value in Tolerance out of bounds (must be a double between 0.0 and 1.0 -- both value included)"); }
                        this.Entries.Add((byte)x, y);
                    }
                }
            }
            /// <summary>
            /// Write the Tolerance to disk
            /// </summary>
            /// <param name="fileName">The name of the file to create</param>
            public void WriteTolerance(string fileName)
            {
                if (fileName == null || fileName.Trim() == string.Empty)
                {
                    throw new ArgumentNullException("File name cannot be null / emtpy string / whitespaces");
                }
                XmlNode xmlNode = WriteToleranceToNode();
                xmlNode.OwnerDocument.Save(fileName);
            }
            /// <summary>
            /// Serialize the Tolerance to xmlNode
            /// </summary>
            /// <return>The node containing the Tolerance</return>
            public XmlNode WriteToleranceToNode()
            {
                XmlDocument xmlDoc = new XmlDocument();
                XmlNode retVal = xmlDoc.CreateElement("CurveTolerances");
                foreach(float dpiRatio in _mapping.Keys)
                {
                    // create &lt;Tolerance dpiRatio='...'&gt; node
                    XmlNode toleranceNode = xmlDoc.CreateElement("Tolerance");
                    XmlAttribute dpiRatioAttribute = xmlDoc.CreateAttribute("dpiRatio");
                    dpiRatioAttribute.InnerText = dpiRatio.ToString(NumberFormatInfo.InvariantInfo);
                    toleranceNode.Attributes.Append(dpiRatioAttribute);

                    // create all &lt;Point x='...' y ='...'/&gt; node
                    SortedDictionary<byte, double> toleranceEntries = _mapping[dpiRatio];
                    foreach (byte x in toleranceEntries.Keys)
                    {
                        XmlNode pointNode = xmlDoc.CreateElement("Point");
                        XmlAttribute xAttribute = xmlDoc.CreateAttribute("x");
                        xAttribute.InnerText = x.ToString(NumberFormatInfo.InvariantInfo);
                        pointNode.Attributes.Append(xAttribute);
                        XmlAttribute yAttribute = xmlDoc.CreateAttribute("y");
                        yAttribute.InnerText = toleranceEntries[x].ToString("G17", NumberFormatInfo.InvariantInfo);
                        pointNode.Attributes.Append(yAttribute);
                        toleranceNode.AppendChild(pointNode);
                    }
                    retVal.AppendChild(toleranceNode);
                }
                xmlDoc.AppendChild(retVal);
                return retVal;
            }
        #endregion Methods

        #region ICloneable Members
            /// <summary>
            /// Returns a deep copy of a CurveTolerance object
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                CurveTolerance retVal = new CurveTolerance();
                retVal._currentDpiRatio = this._currentDpiRatio;
                retVal._mapping = new Dictionary<float, SortedDictionary<byte, double>>();
                foreach (float dpiRation in this._mapping.Keys)
                {
                    SortedDictionary<byte, double> thisDico = this._mapping[dpiRation];
                    SortedDictionary<byte, double> newDico = new SortedDictionary<byte, double>();
                    foreach (byte x in thisDico.Keys)
                    {
                        newDico.Add(x, thisDico[x]);
                    }
                    retVal._mapping.Add(dpiRation, newDico);
                }
                return retVal;
            }
        #endregion ICloneable Members
    }
}
