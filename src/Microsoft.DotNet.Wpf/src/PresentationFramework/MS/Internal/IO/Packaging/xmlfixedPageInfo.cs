// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//              Implements a DOM-based subclass of the FixedPageInfo abstract class.
//              The class functions as an array of XmlGlyphRunInfo's in markup order.
//

using System;
using System.Windows;                   // For ExceptionStringTable
using System.Xml;                       // For DOM objects
using System.Diagnostics;               // For Assert
using System.Globalization;             // For CultureInfo

namespace MS.Internal.IO.Packaging
{
    internal class XmlFixedPageInfo : MS.Internal.FixedPageInfo
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        /// <summary>
        /// Initialize object from DOM node.
        /// </summary>
        /// <remarks>
        /// The DOM node is assumed to be a XAML FixedPage element. Its namespace URI
        /// is subsequently used to look for its nested Glyphs elements (see private property NodeList).
        /// </remarks>
        internal XmlFixedPageInfo(XmlNode fixedPageNode)
        {
            _pageNode = fixedPageNode;
            Debug.Assert(_pageNode != null);
            if (_pageNode.LocalName != _fixedPageName || _pageNode.NamespaceURI != ElementTableKey.FixedMarkupNamespace)
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedXmlNodeInXmlFixedPageInfoConstructor, 
                    _pageNode.NamespaceURI, _pageNode.LocalName,
                    ElementTableKey.FixedMarkupNamespace, _fixedPageName));
            }
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods
        /// <summary>
        /// Get the glyph run at zero-based position 'position'.
        /// </summary>
        /// <remarks>
        /// Returns null for a nonexistent position. No exception raised.
        /// </remarks>
        internal override GlyphRunInfo GlyphRunAtPosition(int position)
        {
            if (position < 0 || position >= GlyphRunList.Length)
            {
                return null;
            }
            if (GlyphRunList[position] == null)
            {
                GlyphRunList[position] = new XmlGlyphRunInfo(NodeList[position]);
            }
            return GlyphRunList[position];
        }
        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties
        /// <summary>
        /// Indicates the number of glyph runs on the page.
        /// </summary>
        internal override int GlyphRunCount 
        { 
            get
            {
                return GlyphRunList.Length;
            }
        }
        #endregion Internal Properties

        //------------------------------------------------------
        //
        //   Private Properties
        //
        //------------------------------------------------------

        #region Private Properties
        /// <summary>
        /// Lazily initialize _glyphRunList, an array of XmlGlyphInfo objects,
        /// using the NodeList private property.
        /// </summary>
        /// <remarks>
        /// When using Visual Studio to step through code using this property, make sure the option
        /// "Allow property evaluation in variables windows" is unchecked.
        /// </remarks>
        private XmlGlyphRunInfo[] GlyphRunList
        {
            get
            {
                if (_glyphRunList == null)
                {
                    _glyphRunList = new XmlGlyphRunInfo[NodeList.Count];
                }
                return _glyphRunList;
            }
        }

        /// <summary>
        /// Lazily initialize the list of Glyphs elements on the page using XPath.
        /// </summary>
        /// <remarks>
        /// When using Visual Studio to step through code using this property, make sure the option
        /// "Allow property evaluation in variables windows" is unchecked.
        /// </remarks>
        private XmlNodeList NodeList
        {
            get
            {
                if (_nodeList == null)
                {
                    string glyphRunQuery = String.Format(CultureInfo.InvariantCulture, ".//*[namespace-uri()='{0}' and local-name()='{1}']",
                        ElementTableKey.FixedMarkupNamespace,
                        _glyphRunName);
                    _nodeList = _pageNode.SelectNodes(glyphRunQuery);
                }
                return _nodeList;
            }
        }
        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        #region Constants
        private const string _fixedPageName = "FixedPage";
        private const string _glyphRunName = "Glyphs";
        #endregion Constants

        private XmlNode     _pageNode;
        private XmlNodeList _nodeList = null;
        private XmlGlyphRunInfo[] _glyphRunList = null;
        #endregion Private Fields

    }
}
