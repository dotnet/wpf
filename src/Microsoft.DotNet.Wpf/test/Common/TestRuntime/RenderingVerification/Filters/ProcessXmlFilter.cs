// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Filters
{
    #region using 
        using System;
        using System.Xml;
        using System.Drawing;
        using System.Reflection;
        using System.Collections;
        using System.Globalization;
        using Microsoft.Test.RenderingVerification;
    #endregion using
    /*
    /// <summary>
    /// Summary description for FilterToXmlToFilter.
    /// </summary>
    public class FilterToXml
    {
        public FilterToXml()
        {
        }
    }
*/
    /// <summary>
    /// Summary description for FilterToXmlToFilter.
    /// </summary>
    internal class ProcessXmlFilter
    {
        #region Properties
            #region Private Properties
                private XmlNode _node = null;
                private ImageUtility _bitmap = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get / set the root
                /// </summary>
                /// <value></value>
                public XmlNode NodeXml
                { 
                    get 
                    {
                        return _node;
                    }
                    set 
                    {
                        if (value == null)
                        { 
                            throw new ArgumentNullException("The value of the NodeXml property cannot be set to null");
                        }
                        _node = value;
                    }
                }
                /// <summary>
                /// Get/set the mapping between "SNAPSHOT" tag and image
                /// </summary>
                /// <value></value>
                public object Snapshot
                { 
                    get 
                    {
                        return _bitmap;
                    }
                    set 
                    {
                        if (value.GetType() == typeof(Bitmap))
                        {
                            _bitmap = new ImageUtility((Bitmap)value);
                        }
                        else
                        {
                            if (value.GetType() == typeof(string))
                            {
                                _bitmap = new ImageUtility((string)value);
                            }
                            else
                            {
                                if (value.GetType() == typeof(ImageUtility))
                                {
                                    _bitmap = (ImageUtility)value;
                                }
                                else
                                {
                                    throw new ArgumentException("Wrong type passed in. Supported type are String, Bitmap and ImageUtility");
                                }
                            }
                        }
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            private ProcessXmlFilter() // Block default instantiation
            {
            }
            /// <summary>
            /// Instanciate an instance of the ProcessXmlFilter Class
            /// </summary>
            /// <param name="node">The Root of the xml docuement</param>
            public ProcessXmlFilter(XmlNode node) : this()
            {
                if (node == null)
                {
                    throw new ArgumentNullException("node", "The XmlNode passed in cannot be null");
                }
                _node = node;
            }
            /// <summary>
            /// Instanciate an instance of the ProcessXmlFilter Class
            /// </summary>
            /// <param name="xmlFile">The XML file containing the Filter Decription</param>
            public ProcessXmlFilter(string xmlFile) : this()
            { 
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFile);
                _node = (XmlNode)xmlDoc.DocumentElement;
            }

        #endregion Constructors

        #region Methods
            #region Private Methods
                private void ProcessFilters(XmlNode node, ref Hashtable aliasCollection)
                    {
                        // Check params
                        if (node == null || node.NodeType != XmlNodeType.Element)
                        {
                            return;
                        }

                        // Recurse thru children -- need to process the inner Filter first (most nested one).
                        if (((XmlElement)node).HasChildNodes)
                        {
                            foreach (XmlNode childNode in node)
                            {
                                ProcessFilters(childNode, ref aliasCollection);
                            }
                        }

                        // This is a Filter, needs to be applied to the image (IImageAdapter)
                        if (((XmlElement)node).Name == "Filter")
                        {
                            IImageAdapter source = null;
                            Hashtable parameters = new Hashtable();

                            // Get input file (parameters)
                            XmlNodeList inputParam = node.SelectNodes("INPUT/*");

                            if (inputParam == null || inputParam.Count == 0)
                            {
                                throw new XmlException("<INPUT> Tag not found or does not have a(ny) child(ren), invalid xml format");
                            }

                            foreach (XmlElement xmlElement in inputParam)
                            {
                                if (xmlElement.LocalName == "IIMAGEADAPTERSOURCE")
                                {
                                    if (xmlElement.HasChildNodes == false)
                                    {
                                        throw new XmlException("<IIMAGEADAPTERSOURCE> Tag doesn't have any child, invalid xml format");
                                    }

                                    // BUGBUG : Invalid cast exception if child of <IIMAGEADAPTERSOURCE> is text.
                                    // This is an invalid input but we should handle this instead of thrwoing an InvalidCastExcpetion.
                                    source = (ImageAdapter)GetParamValue((XmlElement)xmlElement.FirstChild, aliasCollection, true);
                                }
                                else
                                {
                                    object paramValue = GetParamValue(xmlElement, aliasCollection, false);

                                    parameters.Add(xmlElement.LocalName, paramValue);
                                }
                            }

                            // Create filter 
            //                Assembly asm = Assembly.LoadFrom(CombinePaths(Directory.GetCurrentDirectory(), VISUALSCANENGINE));
            //                Type filterType = asm.GetType(FILTERNAMESPACE + "." + node.Attributes["name"].Value, false, true);
                            Assembly asm = Assembly.GetExecutingAssembly();
                            Type filterType = asm.GetType(this.GetType().Namespace + "." + node.Attributes["name"].Value, false, true);
                            Filter filter = asm.CreateInstance(filterType.ToString(), true) as Filter;

                            // Set all Filter Parameters defined in XML
                            IEnumerator iter = parameters.Keys.GetEnumerator();

                            while (iter.MoveNext())
                            {
                                string key = (string)iter.Current;
                                object val = parameters[key];
                                object convertedValue = null;

                                // Get FilterParam associated with this XmlNode
                                FilterParameter filterParam = (FilterParameter)filterType.InvokeMember("Item", BindingFlags.GetProperty | BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase, null, filter, new object[] { key });

                                // Convert Xml string to expected type.
                                Type paramType = filterParam.Parameter.GetType();

                                if (paramType.GetInterface(typeof(IConvertible).ToString()) != null)
                                {
                                        // Try to Call "Parse" with culture invariant (double / float / ...)
                                    const string PARSE = "Parse";
                                    Type[] typeToPass = new Type[] { val.GetType(), CultureInfo.InvariantCulture.GetType() };
                                    object[] paramToPass = new object[] { val, CultureInfo.InvariantCulture };
                                    BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod;
                                    MethodInfo mi = paramType.GetMethod(PARSE, flags, null, typeToPass, null);

                                    if (mi == null)
                                    {
                                        // Not supported, call "Parse" without culture involved
                                        typeToPass = new Type[] { val.GetType() };
                                        paramToPass = new object[] { val };
                                        mi = paramType.GetMethod(PARSE, flags, null, typeToPass, null);
                                        if (mi == null)
                                        {
                                            // What's going on here ? A type that implement IConvertible but not Parse ? Custom ?
                                            throw new ApplicationException("The type ('" + filterType.FullName + "')does not implement the Parse Method");
                                        }
                                    }

                                    convertedValue = paramType.InvokeMember(PARSE, flags, null, paramType, paramToPass);
                                }
                                else
                                {
                                    if (paramType == typeof(ImageAdapter))
                                    {
                                        // Ok to Set type directly
                                        convertedValue = val;
                                    }
                                    else
                                    {
                                        throw new ApplicationException("Unsupported type, Check your filter file. If you are sure it is correct contact the 'avalon test tool team'");
                                    }
                                }

                                filterParam.Parameter = convertedValue;
                            }

                            // Apply filter
                            IImageAdapter imageProcessed = filter.Process(source);

                            // Add return value and ouput to AliasCollection
                            if (node.Attributes["alias"] == null)
                            {
                                throw new XmlException("Filter tag must have an 'alias' attribute set");
                            }

                            string alias = node.Attributes["alias"].Value;

                            if (aliasCollection.Contains(alias))
                            {
                                throw new XmlException("Duplicate alias found ('" + alias + "'); 'alias' attribute must be unique");
                            }

                            aliasCollection.Add(alias, imageProcessed);

                            // Retrieve OUTPUT params and add to the AliasCollection
                            XmlNodeList outputList = node.SelectNodes("OUTPUT/*");

                            foreach (XmlElement output in outputList)
                            {
                                string param = output.LocalName;

                                if (output.Attributes["alias"] == null)
                                {
                                    throw new XmlException("All child of the OUTPUT tag must have an 'alias' attribute set");
                                }

                                string outputAlias = output.Attributes["alias"].Value;
                                FilterParameter FilterParam = (FilterParameter)filterType.InvokeMember("Item", BindingFlags.GetProperty | BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public, null, filter, new object[] { param });

                                if (FilterParam == null)
                                {
                                    throw new RenderingVerificationException("This filter does not implement the specified property", new MissingMemberException(filterType.FullName, param));
                                }

                                if (aliasCollection.Contains(outputAlias))
                                {
                                    throw new XmlException("Duplicate alias found ('" + outputAlias + "'); 'alias' attribute must be unique");
                                }

                                aliasCollection.Add(outputAlias, FilterParam.Parameter as IImageAdapter);
                            }
                        }
                    }

                private object GetParamValue(XmlElement xmlElement, Hashtable aliasCollection, bool acceptTokenOnly)
                {
                    object retVal = null;

                    switch (xmlElement.LocalName)
                    {
                        case "SNAPSHOT" :
                            retVal = new ImageAdapter(((ImageUtility)Snapshot).Bitmap32Bits);
                            break;

                        case "FILE" :
                            retVal = new ImageAdapter(new Bitmap(xmlElement.InnerText));
                            break;

                        case "ALIAS" :
                            retVal = (ImageAdapter)aliasCollection[xmlElement.InnerText];
                            break;

                        case "Filter" :
                            retVal = (ImageAdapter)aliasCollection[xmlElement.Attributes["alias"].Value];
                            break;

                        default :
                            if (acceptTokenOnly)
                            {
                                throw new XmlException("Expected token not found (SNAPSHOT / ALIAS / FILE / Filter)");
                            }

                            if (xmlElement.HasChildNodes)
                            {
                                if (xmlElement.FirstChild.NodeType == XmlNodeType.Text)
                                {
                                    return xmlElement.FirstChild.InnerText;
                                }
                                else
                                {
                                    if (xmlElement.FirstChild.NodeType != XmlNodeType.Element)
                                    {
                                        // BUGBUG : a <!--Comment--> might cause this to happen
                                        throw new XmlException("Unexpected tag");
                                    }
                                }

                                return GetParamValue((XmlElement)xmlElement.FirstChild, aliasCollection, acceptTokenOnly);
                            }
                            else
                            {
                                retVal = xmlElement.InnerText;
                            }

                            break;
                    }
                    return retVal;
                }

            #endregion Private Methods
            #region Public Methods
                /// <summary>
                /// Process the filters
                /// </summary>
                /// <returns>An IImageAdpater representing the filtered image</returns>
                public IImageAdapter PerformFiltering()
                {
                    XmlNode node = _node.SelectSingleNode("descendant::ReturnValue");

                    if (node == null)
                    {
                        throw new XmlException("<ReturnValue> tag not found in the xml");
                    }

                    if (node.NodeType != XmlNodeType.Text)
                    {
                        throw new XmlException("<ReturnValue> should enclosed only text, not another tag nor comment nor an empty string");
                    }

                    string aliasName = node.InnerText.Trim();

                    if (aliasName == string.Empty)
                    {
                        throw new XmlException("<ReturnValue> Tag cannot be empty");
                    }

                    return PerformFiltering(aliasName);
                }

                /// <summary>
                /// Process the filters
                /// </summary>
                /// <param name="aliasName">The IImage adapter to be used as returned value</param>
                /// <returns>An IImageAdpater representing the filtered image</returns>
                public IImageAdapter PerformFiltering(string aliasName)
                {
                    Hashtable aliasCollection = new Hashtable();

                    ProcessFilters(_node, ref aliasCollection);
                    if (aliasCollection.Contains(aliasName) == false)
                    {
                        throw new XmlException("The alias '" + aliasName + "' was not found in the xmlNode");
                    }

                    return (IImageAdapter)aliasCollection[aliasName];
                }

            #endregion Public Methods
        #endregion Methods
    }

}
