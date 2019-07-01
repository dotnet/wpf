// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// XmlQuery is a class that encapsulates functionality to query xml files
    /// </summary>
    [System.Security.Permissions.FileIOPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted = true)]
    internal class XmlQuery
    {
        XmlDocument xmlDoc = null;
        XPathNodeIterator iterator = null;

        /// <summary>
        /// returns the XPathNodeIterator object to access the results of the query
        /// </summary>
        /// <value>XPathNodeIterator</value>
        internal XPathNodeIterator Iterator
        {
            get
            {
                return (iterator);
            }
        }

        /// <summary>
        /// Construct an XmlQuery object
        /// </summary>
        internal XmlQuery()
        {
            xmlDoc = new XmlDocument();
        }

        /// <summary>
        /// Loads a document to work with
        /// </summary>
        /// <param name="xmlFile">File path</param>
        internal void Load(string xmlFile)
        {
            xmlDoc.Load(xmlFile);
        }

        /// <summary>
        /// Loads a document from the given string
        /// </summary>
        /// <param name="xml"></param>
        internal void LoadXml(string xml)
        {
            xmlDoc.LoadXml(xml);
        }

        /// <summary>
        /// Performs the query
        /// </summary>
        /// <param name="query">XPath format query</param>
        internal void Select(string query)
        {
            XPathNavigator xmlNav = xmlDoc.CreateNavigator();
            xmlNav.MoveToRoot();
            XPathExpression xmlExpr = xmlNav.Compile(query);
            iterator = xmlNav.Select(xmlExpr);
        }

        /// <summary>
        /// Selects a set of nodes from a file given an XPath query
        /// </summary>
        /// <param name="file">the file to select nodes from</param>
        /// <param name="statement">the XPath query</param>
        /// <returns>string[] with the node's string representation</returns>
        internal static string[] Select(string file, string statement)
        {
            // create the query
            XmlQuery query = new XmlQuery();

            // load the file
            query.Load(file);

            // return strings on the query
            ArrayList result = new ArrayList();
            query.Select(statement);
            while (query.Iterator.MoveNext())
            {
                result.Add(query.Iterator.Current.Value);
            }
            return ((string[])result.ToArray(typeof(string)));
        }
    }

    /// <summary>
    /// Settings class wraps the functionality of easily get parameters from a file such as the following:
    /// <code>
    /// <Variables>
    ///     <Variable Name="x" Value="1" />
    ///     <Variable Name="x" Value="2" />
    ///     <Variable Name="x" Value="3" />
    ///     <Variable Name="y" Value="4" />
    ///     <Variable Name="z" Value="5" />
    /// </Variables>
    /// </code>
    /// </summary>
    internal class XmlVariablesFile
    {
        internal DuplicatesHashtable Read(string file)
        {
            // query the given file
            XmlQuery q = new XmlQuery();
            q.Load(file);
            const string query = @"/Variables/Variable[@Name and @Value]";
            q.Select(query);

            // build the table with the values
            DuplicatesHashtable t = new DuplicatesHashtable();

            while (q.Iterator.MoveNext())
            {
                // add (name, val) to the table
                const string nameAttrib = "Name";
                const string valueAttrib = "Value";
                t.Add(q.Iterator.Current.GetAttribute(nameAttrib, String.Empty), q.Iterator.Current.GetAttribute(valueAttrib, String.Empty));
            }

            // return the table
            return (t);
        }
    }
}
