// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.Xml;
    using Microsoft.Test.RenderingVerification.Filters;

    /// <summary>
    /// Tolerance to be used by ImageComparisonResult
    /// </summary>
    public class ImageComparisonSettings
    {
        private const string CUSTOM_TOLERANCE = "CustomTolerance";
        private static readonly XmlNode _defaultToleranceNode = null;

        /// <summary>
        /// Default Tolerance (Build in xml tolerance, no filtering)
        /// </summary>
        public static readonly ImageComparisonSettings Default;
        /// <summary>
        /// Ignore Antialiasing (Build in xml tolerance + Pixelize filter)
        /// </summary>
        public static readonly ImageComparisonSettings IgnoreAntiAliasing;

        private XmlNode _node = null;
        private Filter _filter = null;
        private string _friendlyName = string.Empty;

        static ImageComparisonSettings()
        {
            // HACK : get vscan default tolerance
            XmlDocument xmlDoc = new XmlDocument();
            System.IO.Stream stream = null;
            try
            {
                // Vscan default tolerance lives in the same assembly as we are
                System.Reflection.Assembly self = System.Reflection.Assembly.GetExecutingAssembly();
                stream = self.GetManifestResourceStream("Microsoft.Test.RenderingVerification.RenderingVerification.DefaultTolerance.xml");
                if (stream == null)
                {
                    // May occur if TestRuntime has been build with VS
                    stream = self.GetManifestResourceStream("Code.Microsoft.Test.RenderingVerification.DefaultTolerance.xml");
                }
                xmlDoc.Load(stream);
            }
            finally
            {
                if (stream != null) { stream.Dispose(); stream = null; }
            }

            _defaultToleranceNode = xmlDoc.DocumentElement;

            PixelizeFilter filter = new PixelizeFilter();
            filter.SquareSize = 3;
            filter.ExtendedSize = 3;

            Default = new ImageComparisonSettings(_defaultToleranceNode, null, "Default");
            IgnoreAntiAliasing = new ImageComparisonSettings(_defaultToleranceNode, filter, "IgnoreAntiAliasing");
        }
        private ImageComparisonSettings(XmlNode node, Filter filter, string friendlyName)
        {
            _filter = filter;
            _friendlyName = friendlyName;
            _node = node;
            if (_node == null) { node = _defaultToleranceNode; }
        }
        /// <summary>
        /// Create a new Tolerance 
        /// </summary>
        /// <param name="xmlNode">The xml Tolerance to use</param>
        /// <returns>Returns a new ImageComparisonSettings object</returns>
        public static ImageComparisonSettings CreateCustomTolerance(XmlNode xmlNode)
        {
            return new ImageComparisonSettings(xmlNode, null, CUSTOM_TOLERANCE);
        }
        /// <summary>
        /// Create a new Tolerance 
        /// </summary>
        /// <param name="filter">The Filter to use</param>
        /// <returns>Returns a new ImageComparisonSettings object</returns>
        public static ImageComparisonSettings CreateCustomTolerance(Filter filter)
        {
            return new ImageComparisonSettings(null, filter, CUSTOM_TOLERANCE);
        }
        /// <summary>
        /// Create a new Tolerance 
        /// </summary>
        /// <param name="xmlNode">The xml Tolerance to use</param>
        /// <param name="filter">The Filter to use</param>
        /// <returns>Returns a new ImageComparisonSettings object</returns>
        public static ImageComparisonSettings CreateCustomTolerance(XmlNode xmlNode, Filter filter)
        {
            return new ImageComparisonSettings(xmlNode, filter, CUSTOM_TOLERANCE);
        }
     

        internal Filter Filter
        {
            get { return _filter; }
        }
        internal XmlNode XmlNodeTolerance
        {
            get { return _node; }
        }
    }
}
