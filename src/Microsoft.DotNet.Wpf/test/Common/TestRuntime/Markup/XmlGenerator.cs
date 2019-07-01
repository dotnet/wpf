// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Test.Markup
{
    /// <summary>
    /// Creates xml files or streams based on a schema and parameters 
    /// for the number of allowed children, attributes, and tree depth.
    /// </summary>
    public class XmlGenerator
    {
        #region Constructor

        /// <summary>
        /// Empty constructor.
        /// </summary>
        protected XmlGenerator()
        {
        }

        /// <summary>
        /// Accepts schema stream that will direct the kinds of xmls to create.
        /// </summary>
        /// <param name="mainSchema">The schema stream to use.</param>
        public XmlGenerator(Stream mainSchema)
        {
            lock (_syncObject)
            {
                _Initialize(mainSchema, new Stream[0]);
            }
        }

        /// <summary>
        /// Accepts schema stream(s) that will direct the kinds of xmls to create.
        /// </summary>
        /// <param name="mainSchema">The schema stream to use.</param>
        /// <param name="importSchemas">Add-on schemas to merge with the main schema.</param>
        public XmlGenerator(Stream mainSchema, params Stream[] importSchemas)
        {
            lock (_syncObject)
            {
                _Initialize(mainSchema, importSchemas);
            }
        }

        /// <summary>
        /// Accepts schema filename that will direct the kinds of xmls to create.
        /// </summary>
        /// <param name="mainSchema">The schema file to use.</param>
        public XmlGenerator(string mainSchema)
        {
            if (!File.Exists(mainSchema))
            {
                throw new ArgumentException("Schema file does not exist.", "mainSchema");
            }

            lock (_syncObject)
            {
                FileStream fileStream = new FileStream(mainSchema, FileMode.Open, FileAccess.Read);

                _Initialize(fileStream, new Stream[0]);

                fileStream.Close();
            }
        }

        /// <summary>
        /// Accepts schema filename(s) that will direct the kinds of xmls to create.
        /// </summary>
        /// <param name="mainSchema">The main schema file to use.</param>
        /// <param name="importSchemas">Add-on schemas to merge with the main schema.</param>
        public XmlGenerator(string mainSchema, params string[] importSchemas)
        {
            if (!File.Exists(mainSchema))
            {
                throw new ArgumentException("Schema file does not exist.", "mainSchema");
            }

            lock (_syncObject)
            {
                // Get stream for main schema.
                FileStream mainStream = new FileStream(mainSchema, FileMode.Open, FileAccess.Read);

                Stream[] importStreams = new Stream[importSchemas.Length];

                try
                {
                    // Get streams for import schemas.
                    for (int i = 0; i < importStreams.Length; i++)
                    {
                        string filePath = importSchemas[i];

                        if (!File.Exists(filePath))
                        {
                            throw new ArgumentException("Add-on schema file does not exist:" + filePath, "importSchemas");
                        }

                        FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                        importStreams[i] = fileStream;
                    }

                    // Merge and compile schemas.
                    _Initialize(mainStream, importStreams);
                }
                finally
                {
                    // Close streams if necessary

                    if (mainStream != null)
                        mainStream.Close();

                    for (int i = 0; i < importStreams.Length; i++)
                    {
                        Stream importStream = importStreams[i];

                        if (importStream != null)
                            importStreams[i].Close();
                    }
                }
            }
        }

        /// <summary>
        /// Accepts schema stream(s) that will direct the kinds of xmls to create.
        /// </summary>
        /// <param name="mainSchema">The main schema stream to use.</param>
        /// <param name="importSchemas">Add-on schemas to merge with the main schema.</param>
        [CLSCompliant(false)]
        protected void _Initialize(Stream mainSchema, Stream[] importSchemas)
        {
            if (mainSchema == null)
            {
                throw new ArgumentNullException("mainSchema");
            }

            _schema = XmlSchema.Read(mainSchema, null);

            //
            // Loop through import schemas, adding each one to the main 
            // schema's Includes collection.
            //
            for (int i = 0; i < importSchemas.Length; i++)
            {
                Stream stream = importSchemas[i];

                if (stream == null)
                {
                    throw new ArgumentNullException("stream in importSchemas is null.", "importSchemas");
                }

                // Read custom schema.
                XmlSchema customSchema = XmlSchema.Read(stream, null);

                // Set custom schema in import object.
                XmlSchemaImport import = new XmlSchemaImport();

                import.Schema = customSchema;
                import.Namespace = customSchema.TargetNamespace;

                // Add import object to xaml schema, and compile xaml schema.
                _schema.Includes.Add(import);
            }


            #pragma warning disable 618
            _schema.Compile(new ValidationEventHandler(_OnValidate));
            #pragma warning restore 618

            // Store array of available elements.
            ICollection coll = _schema.Elements.Values;

            _xmlElements = new object[coll.Count];

            coll.CopyTo(_xmlElements, 0);
        }

        /// <summary>
        /// Used by schema compiler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnValidate(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Error)
            {
                Trace.WriteLine("Xml Schema Error: " + args.Message);
                throw new Exception("Error occurred while compiling xml schema:\r\n\r\n" + args.Message);
            }
            else
            {
                Trace.WriteLine("Xml Schema Warning: " + args.Message);
            }
        }

        #endregion Constructor

        /// <summary>
        /// Returns the random number generator.
        /// </summary>
        /// <value></value>
        public Random Random
        {
            get
            {
                return _random;
            }
        }

        /// <summary>
        /// Recreates the internal random number generator with a new seed value.
        /// </summary>
        /// <remarks>
        /// The random number generation starts from a seed value. If the same seed is used 
        /// repeatedly, the same series of numbers is generated. The default seed value is 
        /// derived from the system clock.
        /// </remarks>
        public void Reset()
        {
            _random = new Random();
        }

        /// <summary>
        /// Recreates the internal random number generator with the given seed value.
        /// </summary>
        /// <param name="seed">The value used to initialize the random number generator.</param>
        /// <remarks>
        /// The random number generation starts from a seed value. If the same seed is used 
        /// repeatedly, the same series of numbers is generated. The default seed value is 
        /// derived from the system clock.
        /// </remarks>
        public void Reset(int seed)
        {
            _random = new Random(seed);
        }

        #region CreateStatus event

        /// <summary>
        /// Fires when a new xml is created.
        /// </summary>
        public event GeneratorStatusEventHandler GeneratorStatus;

        private void _NotifyCreateStatus(String info, bool xmlCreated)
        {
            if (GeneratorStatus != null)
                GeneratorStatus(null, new GeneratorStatusEventArgs(info, xmlCreated));
        }
        #endregion

        #region Create Xmls

        /// <summary>
        /// Registers a delegate that may be called to generate or monitor new elements.
        /// </summary>
        /// <param name="helper">Delegate that will be called.</param>
        /// <param name="elementNames">Names of the elements to handle.  If none are given, the helper is registered for all elements.</param>
        /// <remarks>Each call to register a helper will replace prior helper registrations of the same element name.</remarks>
        public void RegisterElementHelper(ElementHelper helper, params string[] elementNames)
        {
            if (helper == null)
            {
                throw new ArgumentNullException("helper");
            }

            // If no names were given, register helper for all elements.
            if (elementNames.Length == 0)
            {
                _elementHelpers.Clear();
                _elementHelpers[String.Empty] = helper;
            }
            // Register helper for each element given.
            else
            {
                for (int i = 0; i < elementNames.Length; i++)
                {
                    _elementHelpers[elementNames[i]] = helper;
                }
            }
        }

        /// <summary>
        /// Registers a delegate that may be called to generate text content under generated elements.
        /// </summary>
        /// <param name="helper">Delegate that will be called to generate text content.</param>
        /// <param name="elementNames">Names of the elements to handle.  If none are given, the helper is registered for all elements.</param>
        /// <remarks>Each call to register a helper will replace prior helper registrations of the same element name.</remarks>
        public void RegisterTextHelper(TextContentHelper helper, params string[] elementNames)
        {
            if (helper == null)
            {
                throw new ArgumentNullException("helper");
            }

            // If no names were given, register helper for all elements.
            if (elementNames.Length == 0)
            {
                _textContentHelpers.Clear();
                _textContentHelpers[String.Empty] = helper;
            }
            // Register helper for each element given.
            else
            {
                for (int i = 0; i < elementNames.Length; i++)
                {
                    _textContentHelpers[elementNames[i]] = helper;
                }
            }
        }

        /// <summary>
        /// Registers a delegate that may be called to generate attribute values for certain attributes.
        /// </summary>
        /// <param name="helper">Delegate that will be called to generate attribute values.</param>
        /// <param name="attributeNames">Names of the attributes to handle.  If none are given, the helper is registered for all attributes.</param>
        /// <remarks>Each call to register a helper will replace prior helper registrations of the same attribute name.</remarks>
        public void RegisterAttributeHelper(AttributeHelper helper, params string[] attributeNames)
        {
            if (helper == null)
            {
                throw new ArgumentNullException("helper");
            }

            // If no names were given, register helper for all attributes.
            if (attributeNames.Length == 0)
            {
                _attributeHelpers.Clear();
                _attributeHelpers[String.Empty] = helper;
            }
            // Register helper for each attribute given.
            else
            {
                for (int i = 0; i < attributeNames.Length; i++)
                {
                    _attributeHelpers[attributeNames[i]] = helper;
                }
            }
        }

        /// <summary>
        /// Creates multiple xml files.  The total number depends on
        /// the user parameters and the schema.
        /// </summary>
        /// <param name="maxDepth">The maximum depth of each xml tree</param>
        /// <param name="maxAttributes">The maximum number of attribute for each node</param>
        /// <param name="maxChildren">The maximum number of children and complex syntax properties for each node</param>
        public void CreateFiles(int maxDepth, int maxAttributes, int maxChildren)
        {
            this.CreateFiles(maxDepth, maxAttributes, maxChildren, Int32.MaxValue, String.Empty, String.Empty, String.Empty);
        }

        /// <summary>
        /// Creates multiple xml files.  The total number depends on
        /// the user parameters and the schema.
        /// </summary>
        /// <param name="maxDepth">The maximum depth of each xml tree</param>
        /// <param name="maxAttributes">The maximum number of attribute for each node</param>
        /// <param name="maxChildren">The maximum number of children and complex syntax properties for each node</param>
        /// <param name="maxFiles">The maximum number of files to create.</param>
        /// <param name="outDirectory">The directory to save generated xml files.</param>
        /// <param name="filePrefix">The prefix to put on every saved xml filename.</param>
        /// <param name="fileExtension">The extension to user on every saved xml filename.</param>
        public void CreateFiles(int maxDepth, int maxAttributes, int maxChildren, int maxFiles, string outDirectory, string filePrefix, string fileExtension)
        {
            // Validate and assign contraint values.
            _SetConstraints(maxDepth, maxAttributes, maxChildren);

            if (maxFiles < 0)
            {
                throw new ArgumentException("Number of files cannot be less than 0.", "maxFiles");
            }

            if (!Directory.Exists(outDirectory))
            {
                throw new ArgumentException(outDirectory + " does not exist.", "outDirectory");
            }

            // Make sure outDirectory ends with a directory separator.
            if (!outDirectory.EndsWith("\\"))
            {
                outDirectory += "\\";
            }

            // Check the file extension
            if (fileExtension == String.Empty)
            {
                fileExtension = ".xml";
            }
            else if (!fileExtension.StartsWith("."))
            {
                fileExtension = "." + fileExtension;
            }

            this._NotifyCreateStatus("Creating xml files...", false);

            // Create xml files up to the number specified.        
            for (_xmlSequence = 0; _xmlSequence < maxFiles; _xmlSequence++)
            {
                this._NotifyCreateStatus("Creating xml file " + _xmlSequence.ToString() + "...", false);

                //Prepare file stream.
                string xmlFileName = outDirectory + filePrefix + "_" + _xmlSequence.ToString() + fileExtension;
                FileStream fileStream = new FileStream(xmlFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

                try
                {
                    XmlTextWriter writer = new XmlTextWriter(fileStream, Encoding.Unicode);
                    writer.Formatting = Formatting.Indented;

                    // Create xml.
                    _CreateXml(writer);

                    writer.Close();
                }
                catch (Exception err)
                {
                    // Close the stream if an exception occurred.
                    fileStream.Close();

                    throw new Exception("Exception occurred while generating xml.", err);
                }

                //Show progress
                this._NotifyCreateStatus("Created xml file: " + xmlFileName, true);
            }

            this._NotifyCreateStatus("Created " + maxFiles.ToString() + " xml files.", false);
        }

        /// <summary>
        /// Creates an xml to a stream.
        /// </summary>
        /// <returns></returns>
        public Stream CreateStream(int maxDepth, int maxAttributes, int maxChildren)
        {
            // Validate and assign contraint values.
            _SetConstraints(maxDepth, maxAttributes, maxChildren);

            // Save xml to stream.
            Stream stream = new MemoryStream();

            try
            {
                XmlTextWriter writer = new XmlTextWriter(stream, Encoding.Unicode);
                writer.Formatting = Formatting.Indented;

                // Create xml.
                _CreateXml(writer);

                // Reposition stream at beginning.
                stream.Seek(0, SeekOrigin.Begin);

                return stream;
            }
            catch (Exception err)
            {
                // Close the stream if an exception occurred.
                stream.Close();

                throw new Exception("Exception occurred while generating xml.", err);
            }
        }

        /// <summary>
        /// Validates constraints before setting private fields.
        /// </summary>
        private void _SetConstraints(int maxDepth, int maxAttributes, int maxChildren)
        {
            if (maxDepth < 0)
            {
                throw new ArgumentException("Maximum tree depth cannot be less than 0.", "maxDepth");
            }

            if (maxAttributes < 0)
            {
                throw new ArgumentException("Maximum number of attributes per element cannot be less than 0.", "maxAttributes");
            }

            if (maxChildren < 0)
            {
                throw new ArgumentException("Maximum number of children per element cannot be less than 0.", "maxChildren");
            }

            _maxDepth = maxDepth;
            _maxAttributes = maxAttributes;
            _maxChildren = maxChildren;
        }

        /// <summary>
        /// Callback for derived classes to insert extra content at the beginning of the xml.
        /// </summary>
        /// <param name="xmlDocument"></param>
        protected virtual void CreateXmlCore(XmlDocument xmlDocument)
        {
        }

        /// <summary>
        /// Creates an xml document.
        /// </summary>
        private void _CreateXml(XmlTextWriter writer)
        {
            // Add schema namespaces to NameTable.
            XmlNameTable nameTable = new NameTable();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(nameTable);

            Hashtable prefixes = _ReadNamespaces();

            foreach (object key in prefixes.Keys)
            {
                string prefix = (string)prefixes[key];
                nsmgr.AddNamespace(prefix, (string)key);
            }

            // Create XmlDocument.
            XmlDocument xmlDocument = new XmlDocument(nameTable);

            // Allow derived classes to insert content at beginning of generated xml.
            this.CreateXmlCore(xmlDocument);

            //Get the type of root node
            XmlSchemaElement element = _ReadARootNodeType();

            if (element == null)
            {
                throw new Exception("The schema does not specify a root element.");
            }

            _CreateElement(xmlDocument, element, 0);

            xmlDocument.WriteTo(writer);
            writer.Flush();
        }

        #endregion CreatXmls

        #region Private methods to create attributes and children

        #region Attributes

        /// <summary>
        /// For a certain type, get the possible simple syntax properties 
        /// and write some of them. The total number of properties depends
        /// on the user parameters and the schema's minOccurs and maxOccurs
        /// specs.
        /// </summary>
        private void _CreateAttributes(XmlElement parentElement, XmlSchemaElement element, XmlSchemaType schemaType)
        {
            int cntWritten = 0;

            //
            // Get available attributes.
            //
            ArrayList attributes = _ReadAttributes(schemaType);

            //
            // Write all required attributes first.
            // Also, remove all prohibited attributes.
            //
            for (int i = 0; i < attributes.Count; )
            {
                XmlSchemaAttribute attribute = attributes[i] as XmlSchemaAttribute;

                // If attribute is required, write it now, and remove it from list.
                if (attribute.Use == XmlSchemaUse.Required)
                {
                    attributes.Remove(attribute);
                    _WriteAttribute(parentElement, element, attribute);
                    cntWritten++;
                }
                // If attribute is prohibited, remove it from list.
                else if (attribute.Use == XmlSchemaUse.Prohibited)
                {
                    attributes.Remove(attribute);
                }
                else
                {
                    i++;
                }
            }

            //
            // Choose random number of attributes to write up to some 
            // specified maximum.
            //
            int maxAttribsToWrite = _ThreadSafeRandomNext(_maxAttributes + 1);

            for (int i = cntWritten; i < maxAttribsToWrite && attributes.Count > 0; i++)
            {
                int attribIndex = _ThreadSafeRandomNext(attributes.Count);
                XmlSchemaAttribute attribute = attributes[attribIndex] as XmlSchemaAttribute;

                attributes.RemoveAt(attribIndex);

                _WriteAttribute(parentElement, element, attribute);
            }
        }

        /// <summary>
        /// Takes a schema attribute, finds possible values as specified in the
        /// schema, and writes a name="value" pair to an XmlNode.
        /// </summary>
        private void _WriteAttribute(XmlElement parentElement, XmlSchemaElement element, XmlSchemaAttribute attribute)
        {
            string attributeName = attribute.QualifiedName.Name;
            string attributeNamespace = attribute.QualifiedName.Namespace;
            string attributeValue = String.Empty;
            XmlAttribute newAttrib = null;

            // Create new attribute
            if (attributeNamespace != _schema.TargetNamespace)
            {
                string prefix = (string)_prefixes[attributeNamespace];
                newAttrib = parentElement.OwnerDocument.CreateAttribute(prefix, attributeName, attributeNamespace);
            }
            else
            {
                newAttrib = parentElement.OwnerDocument.CreateAttribute(attributeName);
            }

            //
            // Get attribute value if possible.
            // Try registered helpers first.
            // If that doesn't work, read from schema.
            //
            HandledLevel handledLevel = HandledLevel.None;

            if (_attributeHelpers.Contains(String.Empty))
            {
                // Get attribute value from helper.
                AttributeHelper helper = (AttributeHelper)_attributeHelpers[String.Empty];

                handledLevel = helper(parentElement, newAttrib);
            }
            else if (_attributeHelpers.Contains(attributeName))
            {
                // Get attribute value from helper.
                AttributeHelper helper = (AttributeHelper)_attributeHelpers[attributeName];

                handledLevel = helper(parentElement, newAttrib);
            }

            // If helper wasn't called or didn't handle the attribute,
            // generate a value using the schema.
            // Note: If helper returned HandledLevel.Partial, treat it like HandledLevel.Complete.
            if (handledLevel == HandledLevel.None)
            {
                if (attribute.AttributeSchemaType.Content is XmlSchemaSimpleTypeRestriction)
                {
                    XmlSchemaSimpleTypeRestriction restriction = attribute.AttributeSchemaType.Content as XmlSchemaSimpleTypeRestriction;

                    if (restriction == null)
                    {
                        throw new NotSupportedException("Only restriction nodes are supported for simpleType nodes in the schema.");
                    }

                    XmlSchemaObjectCollection facets = restriction.Facets;

                    if (facets.Count > 0)
                    {
                        int index = _ThreadSafeRandomNext(facets.Count);
                        XmlSchemaObject facet = facets[index];

                        if (facet is XmlSchemaEnumerationFacet)
                        {
                            XmlSchemaEnumerationFacet enumFacet = facet as XmlSchemaEnumerationFacet;

                            attributeValue = enumFacet.Value;
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException("Only simple type restrictions are supported for attribute values.");
                }

                // Set attribute value and add to parent
                newAttrib.Value = attributeValue;
                parentElement.SetAttributeNode(newAttrib);
            }
        }

        #endregion Attributes

        #region Children

        /// <summary>
        /// Generates and writes text strings for the given element.
        /// </summary>
        private void _WriteTextContent(XmlNode parentNode, bool isMixedContent)
        {
            string elementName = parentNode.Name;
            TextContentHelper helper = null;

            // Get registered helper.
            if (_textContentHelpers.Contains(String.Empty))
            {
                helper = _textContentHelpers[String.Empty] as TextContentHelper;
            }
            else
            {
                helper = _textContentHelpers[elementName] as TextContentHelper;
            }

            // Let helper add text if it wants.
            // No need to hold HandledLevel returned by helper.
            if (isMixedContent && helper != null)
            {
                helper(parentNode);
            }
        }

        /// <summary>
        /// Takes a new node and checks registered ElementHelper delegates for it.
        /// Helpers may completely handle the new node, which causes the generator to
        /// skip ahead, or they may partially handle it, which causes the generator to
        /// continue generating attributes and children for it.
        /// </summary>
        private HandledLevel _WriteElement(XmlNode parentNode, XmlElement newElement, bool isStart)
        {
            ElementHelper helper = null;
            string elementName = newElement.Name;

            if (_elementHelpers.Contains(String.Empty))
            {
                helper = (ElementHelper)_elementHelpers[String.Empty];
            }
            else if (_elementHelpers.Contains(elementName))
            {
                // Get attribute value from helper.
                helper = (ElementHelper)_elementHelpers[elementName];
            }

            if (helper != null)
                return helper(parentNode, newElement, isStart);
            else
                return HandledLevel.None;
        }


        /// <summary>
        /// For a certain type, get the possible children, and write some of them. 
        /// The total number of children depends on the user parameters and the 
        /// schema.
        /// </summary>
        private void _CreateChildren(XmlElement parentElement, XmlSchemaElement element, XmlSchemaType schemaType, int currentLayer)
        {
            // Check to make sure we're not growing out of control.
            if (currentLayer >= _maxDepth + CYCLE_TOLERANCE)
            {
                throw new Exception("The depth of the generated xml is growing nonstop.  Possibly, the schema is not properly setting the minOccurs attribute on an element.");
            }

            //
            // Get list of possible children.
            //
            ArrayList children = _ReadAvailableChildren(element, schemaType);

            if (children.Count == 0)
            {
                return;
            }

            //
            // Choose random number of children to write up to some 
            // specified maximum.
            //
            decimal maxToWrite = (decimal)_ThreadSafeRandomNext(_maxChildren + 1);


            //
            // Check whether or not element accepts mixed content.
            //
            XmlSchemaComplexType ct = schemaType as XmlSchemaComplexType;
            bool isMixedContent = false;
            if (ct != null)
                isMixedContent = ct.IsMixed;


            //
            // Choose a child from the available collection, and create its subtree.
            //
            _CreateRandomChild(parentElement, children, currentLayer, 0, maxToWrite, isMixedContent);
        }

        /// <summary>
        /// Write children specified in a &lt;sequence&gt; node.
        /// </summary>
        private decimal _CreateSequence(XmlElement parentElement, XmlSchemaSequence sequence, int currentLayer, decimal cTotalWritten, decimal maxToWrite, bool isMixedContent)
        {
            // Read all nested children under this sequence with MinOccurs > 0.
            ArrayList children = _ReadAvailableChildren(sequence, null);

            if (children.Count == 0)
                return 0;

            decimal cNewWritten = 0;

            // Write minimum required sequences.
            for (int i = 0; i < sequence.MinOccurs; i++)
            {
                // Within each sequence, write children in order.
                for (int j = 0; j < children.Count; j++)
                {
                    // Attempt to write text under element.
                    _WriteTextContent(parentElement, isMixedContent);

                    XmlSchemaObject schemaObject = (XmlSchemaObject)children[j];

                    cNewWritten += _CreateChild(parentElement, schemaObject, currentLayer, cTotalWritten, maxToWrite, isMixedContent);
                }
            }

            // Attempt to write text under element.
            _WriteTextContent(parentElement, isMixedContent);

            // Skip this choice now if we've reached the max layers.
            if (currentLayer >= _maxDepth)
            {
                return cNewWritten;
            }

            // Compute a random maximum number of children to write.
            decimal maxSequenceToWrite = sequence.MinOccurs + (decimal)_ThreadSafeRandomNextDouble() * (sequence.MaxOccurs - sequence.MinOccurs);


            // Write children again until we reach maximum allowed sequences or total.
            for (decimal i = sequence.MinOccurs;
                 i < maxSequenceToWrite &&
                 cTotalWritten + cNewWritten < maxToWrite;
                 i++)
            {
                // Within each sequence, write children in order.
                for (int j = 0; j < children.Count; j++)
                {
                    // Attempt to write text under element.
                    _WriteTextContent(parentElement, isMixedContent);

                    XmlSchemaObject schemaObject = (XmlSchemaObject)children[j];

                    cNewWritten += _CreateChild(parentElement, schemaObject, currentLayer, cTotalWritten, maxToWrite, isMixedContent);

                }
            }

            // Attempt to write text under element.
            _WriteTextContent(parentElement, isMixedContent);

            return cNewWritten;
        }

        /// <summary>
        /// Write children specified in a &lt;choice&gt; node.  Choice means
        /// that just one of the children will be chosen.  If the Choice has
        /// a MinOccurs and/or MaxOccurs, we will make separate choices from
        /// MinOccurs to MaxOccurs times.
        /// </summary>
        private decimal _CreateChoice(XmlElement parentElement, XmlSchemaChoice choice, int currentLayer, decimal cTotalWritten, decimal maxToWrite, bool isMixedContent)
        {
            // Read all nested children under this choice with MinOccurs > 0.
            ArrayList children = _ReadAvailableChildren(choice, null);

            if (children.Count == 0)
                return 0;

            decimal cNewWritten = 0;

            // Write minimum required choices.
            for (int i = 0;
                i < choice.MinOccurs &&
                children.Count > 0;
                i++)
            {
                // Attempt to write text under element.
                _WriteTextContent(parentElement, isMixedContent);

                cNewWritten += _CreateRandomChild(parentElement, (ArrayList)children.Clone(), currentLayer, cTotalWritten + cNewWritten, maxToWrite, isMixedContent);
            }

            // Attempt to write text under element.
            _WriteTextContent(parentElement, isMixedContent);

            // Skip this choice now if we've reached the max layers.
            if (currentLayer >= _maxDepth)
            {
                return cNewWritten;
            }

            // Compute a random maximum number of children to write.
            decimal maxChoiceToWrite = choice.MinOccurs + (decimal)_ThreadSafeRandomNextDouble() * (choice.MaxOccurs - choice.MinOccurs);

            // Write children again until we reach maximum allowed choices or total.
            for (decimal i = choice.MinOccurs;
                 i < maxChoiceToWrite &&
                 cTotalWritten + cNewWritten < maxToWrite &&
                 children.Count > 0;
                 i++)
            {
                // Attempt to write text under element.
                _WriteTextContent(parentElement, isMixedContent);

                cNewWritten += _CreateRandomChild(parentElement, (ArrayList)children.Clone(), currentLayer, cTotalWritten + cNewWritten, maxToWrite, isMixedContent);
            }

            // Attempt to write text under element.
            _WriteTextContent(parentElement, isMixedContent);

            return cNewWritten;
        }

        /// <summary>
        /// Looks through available children.  Randomly choose one to add to the parent.
        /// If none can be added, return 0.
        /// </summary>
        private decimal _CreateChild(XmlElement parentElement, XmlSchemaObject schemaObject, int currentLayer, decimal cTotalWritten, decimal maxToWrite, bool isMixedContent)
        {
            decimal cNewWritten = 0;

            if (schemaObject is XmlSchemaChoice)
            {
                cNewWritten = _CreateChoice(parentElement, (XmlSchemaChoice)schemaObject, currentLayer, cTotalWritten, maxToWrite, isMixedContent);
            }
            else if (schemaObject is XmlSchemaSequence)
            {
                cNewWritten = _CreateSequence(parentElement, (XmlSchemaSequence)schemaObject, currentLayer, cTotalWritten, maxToWrite, isMixedContent);
            }
            else if (schemaObject is XmlSchemaElement)
            {
                // Randomly choose child from available collection.
                XmlSchemaElement child = (XmlSchemaElement)schemaObject;

                for (int i = 0; i < child.MinOccurs; i++)
                {
                    //recursively create children, including complex properties
                    _CreateElement(parentElement, child, currentLayer + 1);
                    cNewWritten++;
                }

                // Compute a random maximum number of the child to write.
                decimal maxChildToWrite = child.MinOccurs + (decimal)_ThreadSafeRandomNextDouble() * (child.MaxOccurs - child.MinOccurs);

                for (decimal i = child.MinOccurs;
                    i < maxChildToWrite &&
                    cTotalWritten + cNewWritten < maxToWrite;
                    i++)
                {
                    //recursively create children, including complex properties
                    _CreateElement(parentElement, child, currentLayer + 1);
                    cNewWritten++;
                }
            }
            else
            {
                throw new NotImplementedException("Child is not a <choice>, <sequence>, or <element>.");
            }

            return cNewWritten;
        }

        /// <summary>
        /// Looks through available children.  Randomly choose one to add to the parent.
        /// If none can be added, return 0.
        /// </summary>
        private decimal _CreateRandomChild(XmlElement parentElement, ArrayList children, int currentLayer, decimal cTotalWritten, decimal maxToWrite, bool isMixedContent)
        {
            decimal cNewWritten = 0;

            // Loop through children until one is created or until
            // all of them are removed from the available collection.
            while (cNewWritten == 0 && children.Count > 0)
            {
                object childObj = children[_ThreadSafeRandomNext(children.Count)];

                cNewWritten = _CreateChild(parentElement, (XmlSchemaObject)childObj, currentLayer, cTotalWritten, maxToWrite, isMixedContent);

                children.Remove(childObj);
            }

            return cNewWritten;
        }

        /// <summary>
        /// Returns a list of children available under the given object.
        /// </summary>
        /// <param name="schemaObject">Can be a XmlSchemaElement or XmlSchemaGroupBase.</param>
        /// <param name="schemaType">If schemaObject is an XmlSchemaElement, this is its corresponding type.</param>
        /// <returns></returns>
        private ArrayList _ReadAvailableChildren(XmlSchemaObject schemaObject, XmlSchemaType schemaType)
        {
            ArrayList available = new ArrayList();

            if (schemaObject is XmlSchemaGroupBase)
            {
                XmlSchemaGroupBase groupBase = (XmlSchemaGroupBase)schemaObject;

                //
                // Look at all group items.  Add all immediate element items and
                // element items that are nested under another choice.
                //
                for (int i = 0; i < groupBase.Items.Count; i++)
                {
                    object item = groupBase.Items[i];

                    if (item is XmlSchemaElement)
                    {
                        XmlSchemaElement child = item as XmlSchemaElement;

                        available.Add(child);
                    }
                    else if (item is XmlSchemaChoice)
                    {
                        XmlSchemaChoice choice = item as XmlSchemaChoice;

                        available.Add(choice);
                    }
                    else if (item is XmlSchemaSequence)
                    {
                        XmlSchemaSequence sequence = item as XmlSchemaSequence;

                        available.Add(sequence);
                    }
                }
            }
            else if (schemaObject is XmlSchemaElement)
            {
                if (schemaType == null)
                {
                    throw new ArgumentNullException("schemaType", "The schemaType cannot be null if the schemaObject is and XmlSchemaElement.");
                }

                XmlSchemaElement element = (XmlSchemaElement)schemaObject;

                if (schemaType is XmlSchemaComplexType)
                {
                    XmlSchemaComplexType complexType = (XmlSchemaComplexType)schemaType;

                    if (complexType != null)
                    {
                        // We support only <sequence> and <choice> nodes now.
                        // If the type has one of those, add it.  Otherwise, throw.
                        if (complexType.ContentTypeParticle is XmlSchemaSequence ||
                         complexType.ContentTypeParticle is XmlSchemaChoice)
                        {
                            available.Add((XmlSchemaGroupBase)complexType.ContentTypeParticle);
                        }
                        else if (complexType.ContentTypeParticle is XmlSchemaAny)
                        {
                            throw new Exception("<any> nodes are not supported yet.");
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException("XmlSchemaType is not XmlSchemaComplexType. Non-complex element types are not supported yet.");
                }
            }
            else
            {
                throw new NotImplementedException("XmlSchemaType is not XmlSchemaGroupBase or XmlSchemaElement.");
            }

            return available;
        }

        /// <summary>
        /// Returns a list of schema types that are derived from a given complex type.
        /// </summary>
        private ArrayList _ReadDerivedTypes(XmlSchemaComplexType complexType)
        {
            if (_derivedTypeCache.ContainsKey(complexType))
            {
                return _derivedTypeCache[complexType];
            }

            lock (_derivedTypeCache)
            {
                // Check again if the cache contains the item since it could have
                // been added while we waited for the lock (another thread was in
                // the section, blocking us).
                if (_derivedTypeCache.ContainsKey(complexType))
                {
                    return _derivedTypeCache[complexType];
                }

                ArrayList derivedTypes = new ArrayList();

                // Get all of the types in the schema.
                XmlSchemaObjectTable schemaTypes = _schema.SchemaTypes;

                // Copy types to array
                object[] values = new object[schemaTypes.Values.Count];
                schemaTypes.Values.CopyTo(values, 0);

                // For each type, look through its parent chain.  If our target
                // type is in the chain, we know that this type is a sub-type
                // of it, so we add it to the derivedTypes collection.
                XmlSchemaComplexType schemaType = null;
                XmlSchemaComplexType baseType = null;
                for (int i = 0; i < values.Length; i++)
                {
                    schemaType = values[i] as XmlSchemaComplexType;

                    if (schemaType == null || schemaType.IsAbstract)
                        continue;

                    baseType = schemaType.BaseXmlSchemaType as XmlSchemaComplexType;

                    while (baseType != null)
                    {
                        if (baseType == complexType)
                        {
                            derivedTypes.Add(schemaType);
                            break;
                        }

                        baseType = baseType.BaseXmlSchemaType as XmlSchemaComplexType;
                    }
                }

                // Cache the derived types for the given complexType.
                _derivedTypeCache[complexType] = derivedTypes;

                // Finally, return the collection of derived types.
                return derivedTypes;
            }
        }
        /// <summary>
        /// Create a child. If this type can have a child, recursively 
        /// call _CreateChildren().
        /// </summary>
        private void _CreateElement(XmlNode parentNode, XmlSchemaElement element, int currentLayer)
        {
            string elementName = element.QualifiedName.Name;
            string elementNamespace = element.QualifiedName.Namespace;
            Hashtable prefixes = _ReadNamespaces();
            XmlDocument xmlDocument = null;
            XmlElement newElement = null;

            // Get XmlDocument from parentNode.
            if (parentNode is XmlDocument)
                xmlDocument = parentNode as XmlDocument;
            else
                xmlDocument = parentNode.OwnerDocument;

            // Create the new node.
            if (elementNamespace != _schema.TargetNamespace)
            {
                string prefix = (string)_prefixes[elementNamespace];
                newElement = xmlDocument.CreateElement(prefix, elementName, elementNamespace);
            }
            else
            {
                newElement = xmlDocument.CreateElement(elementName, elementNamespace);
            }

            // Write default xmlns (has empty prefix) if this is the root node.
            if (currentLayer == 0)
            {
                XmlAttribute xmlns = xmlDocument.CreateAttribute("xmlns", "http://www.w3.org/2000/xmlns/");
                xmlns.Value = _schema.TargetNamespace;
                newElement.SetAttributeNode(xmlns);

                foreach (string ns in _prefixes.Keys)
                {
                    string prefix = (string)_prefixes[ns];

                    if (prefix == "")
                        continue;

                    xmlns = xmlDocument.CreateAttribute("xmlns:" + prefix);
                    xmlns.Value = ns;
                    newElement.SetAttributeNode(xmlns);
                }
            }

            // Give helper a chance to handle the new node.
            if (_WriteElement(parentNode, newElement, true) == HandledLevel.Complete)
                return;

            // Add the new node.
            parentNode.AppendChild(newElement);


            // Get schema element's schema type.
            XmlSchemaType schemaType = _GetSchemaType(element);


            // Write attributes of element.
            _CreateAttributes(newElement, element, schemaType);


            // Write children of element.
            _CreateChildren(newElement, element, schemaType, currentLayer);


            // Give helper a chance to modify/monitor the new node.
            _WriteElement(parentNode, newElement, false);
        }

        #endregion Children

        /// <summary>
        /// Reads specified namespaces from the schema.
        /// </summary>
        /// <returns>Collection of namespaces w/ prefixes.</returns>
        private Hashtable _ReadNamespaces()
        {
            if (_prefixes != null)
            {
                return _prefixes;
            }

            lock (_syncObject)
            {
                // Check again if the cache is null since it could have been created
                // while we waited for the lock (another thread was in
                // the section, blocking us).
                if (_prefixes != null)
                {
                    return _prefixes;
                }

                //
                // Store namespace prefixes.
                //
                Hashtable prefixes = new Hashtable();

                // Set prefix of main schema's TargetNamespace to empty string.
                prefixes[_schema.TargetNamespace] = String.Empty;

                _ReadNamespaces(_schema, prefixes);

                _prefixes = prefixes;
            }

            return _prefixes;
        }

        /// <summary>
        /// Reads specified namespaces from a schema and its imported schemas.  Recursive.
        /// </summary>
        private void _ReadNamespaces(XmlSchema schema, Hashtable prefixes)
        {
            // Store other namespace prefixes.
            XmlQualifiedName[] namespaces = schema.Namespaces.ToArray();
            for (int i = 0; i < namespaces.Length; i++)
            {
                XmlQualifiedName qualifiedName = namespaces[i];

                // Store namespace prefix for all namespaces.
                // Only the main schema's TargetNamespace gets an empty string prefix,
                // so if a namespace has an empty string prefix at this point,
                // we should generate one.
                if (!prefixes.Contains(qualifiedName.Namespace))
                {
                    if (qualifiedName.Name != String.Empty)
                        prefixes[qualifiedName.Namespace] = qualifiedName.Name;
                    else
                        prefixes[qualifiedName.Namespace] = "ns" + prefixes.Count;
                }
            }

            // Store namespace prefixes for namespaces on imported schemas.
            for (int i = 0; i < schema.Includes.Count; i++)
            {
                XmlSchemaExternal ext = schema.Includes[i] as XmlSchemaExternal;

                if (ext != null && ext.Schema != null)
                {
                    _ReadNamespaces(ext.Schema, prefixes);
                }
            }
        }

        // Thread-safe wrapper around Random.Next().
        // The Random class is not thread safe.  It can become corrupted
        // and begin returning 0 every call.
        private int _ThreadSafeRandomNext(int max)
        {
            lock (_random)
            {
                return _random.Next(max);
            }
        }

        // Thread-safe wrapper around Random.NextDouble().
        // The Random class is not thread safe.  It can become corrupted
        // and begin returning 0 every call.
        private double _ThreadSafeRandomNextDouble()
        {
            lock (_random)
            {
                return _random.NextDouble();
            }
        }

        /// <summary>
        /// Simple private utility function for reading values form the schema.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        private string _GetAttributeValue(XmlNode node, string attributeName)
        {
            XmlAttributeCollection attributes = node.Attributes;
            XmlAttribute attribute = attributes[attributeName];

            if (null == attribute)
            {
                return null;
            }

            return attribute.Value;
        }

        /// <summary>
        /// Reads possible content children (choice, sequence, or any) from the schema.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private XmlSchemaGroupBase _ReadContentChild(XmlSchemaElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            XmlSchemaGroupBase child = null;

            //
            // Get schema element's schema type.
            //
            XmlSchemaType schemaType = element.ElementSchemaType;

            if (schemaType is XmlSchemaComplexType)
            {
                XmlSchemaComplexType complexType = schemaType as XmlSchemaComplexType;

                if (complexType.ContentTypeParticle is XmlSchemaSequence ||
                     complexType.ContentTypeParticle is XmlSchemaChoice)
                {
                    child = complexType.ContentTypeParticle as XmlSchemaGroupBase;
                }
                else if (complexType.ContentTypeParticle is XmlSchemaAny)
                {
                    throw new Exception("XmlSchemaAny are not supported yet.");
                }
            }
            else
            {
                throw new Exception("XmlSchemaType is not XmlSchemaComplexType. Non-complex element types are not supported yet.");
            }

            return child;
        }

        /// <summary>
        /// Reads possible attributes that can be on an element.
        /// </summary>
        /// <param name="schemaType"></param>
        /// <returns></returns>
        private ArrayList _ReadAttributes(XmlSchemaType schemaType)
        {
            if (schemaType == null)
            {
                throw new ArgumentNullException("schemaType");
            }

            ArrayList available = new ArrayList();

            //
            // Using the element's schema type, read the attributes.
            //
            if (schemaType is XmlSchemaSimpleType)
            {
                // TODO: Check if we need to look for attributes on simple types.
                return available;
            }
            else
            {
                XmlSchemaComplexType complexType = (XmlSchemaComplexType)schemaType;

                if (complexType != null)
                {
                    XmlSchemaObjectTable elementAttribs = complexType.AttributeUses;

                    // Read all attributes, including those nested in groups/sub-groups.
                    _ReadAttributesCore(available, elementAttribs);
                }
            }

            return available;
        }
        /// <summary>
        /// Returns a XmlSchemaType for the given XmlSchemaElement. This will 
        /// find a derived type if the given type is an abstract complex type.
        /// </summary>
        private XmlSchemaType _GetSchemaType(XmlSchemaElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            XmlSchemaType schemaType = (XmlSchemaType)element.ElementSchemaType;

            if (schemaType is XmlSchemaComplexType)
            {
                XmlSchemaComplexType complexType = (XmlSchemaComplexType)schemaType;

                // If the type is abstract, find a derived type and replace
                // the type with the derived one.
                if (complexType.IsAbstract)
                {
                    ArrayList derivedTypes = _ReadDerivedTypes(complexType);

                    if (derivedTypes.Count == 0)
                    {
                        // The type is abstract and there are no derived types,
                        // so let's return now.
                        return null;
                    }

                    // Randomly choose one of the available derived types.
                    int index = _ThreadSafeRandomNext(derivedTypes.Count);
                    complexType = derivedTypes[index] as XmlSchemaComplexType;
                }

                schemaType = complexType;
            }

            return schemaType;
        }
        /// <summary>
        /// Implementation of _ReadAttributes() that runs recursively.
        /// </summary>
        /// <param name="available">Attributes are appended to this list.</param>
        /// <param name="elementAttribs">The root collection of attributes to begin the search.</param>
        private void _ReadAttributesCore(ArrayList available, XmlSchemaObjectTable elementAttribs)
        {
            //
            // Copy attributes to array.
            //
            XmlSchemaAttribute[] values = new XmlSchemaAttribute[elementAttribs.Values.Count];
            elementAttribs.Values.CopyTo(values, 0);

            //
            // For each attribute in the array, add it to the 'available' list.
            //
            for (int i = 0; i < values.Length; i++)
            {
                available.Add(values[i]);
            }
        }

        /// <summary>
        /// Reads a top-level element from the schema.
        /// </summary>
        /// <returns></returns>
        private XmlSchemaElement _ReadARootNodeType()
        {
            XmlSchemaElement element = null;

            if (_xmlElements.Length > 0)
            {
                int index = _ThreadSafeRandomNext(_xmlElements.Length);

                element = _xmlElements[index] as XmlSchemaElement;
            }

            return element;
        }

        #endregion Private methods to create attributes and children

        // Maximum depth of node tree in xml
        int _maxDepth = 0;

        // Maximum number of attributes
        int _maxAttributes;

        // Maximum number of children per node
        int _maxChildren;

        // Sequence number for the current xml to create.
        // Note: This persists across multiple xml generation calls.
        int _xmlSequence = 0;

        // Random number generator
        Random _random = new Random();

        // Xml namepace prefixes cache.
        Hashtable _prefixes = null;

        // Sync object.
        static object _syncObject = new object();

        /// <summary>
        /// Schema object that spans the lifetime of this helper.
        /// </summary>
        [CLSCompliant(false)]
        protected XmlSchema _schema = null;

        // Cache of elements defined in the schema.
        object[] _xmlElements = null;

        // Handlers for helping to generate mixed content.
        Hashtable _textContentHelpers = new Hashtable();

        // Handlers for helping to generate attribute values.
        Hashtable _attributeHelpers = new Hashtable();

        // Handlers for helping to generate elements.
        Hashtable _elementHelpers = new Hashtable();

        // Cache of derived types for XmlSchemaComplexTypes.
        Dictionary<XmlSchemaComplexType, ArrayList> _derivedTypeCache = new Dictionary<XmlSchemaComplexType, ArrayList>();

        const int CYCLE_TOLERANCE = 100;
    }

    #region Internal Class

    /// <summary>
    /// Used for handling Mapping PI's (Mapping processing instructions).
    /// </summary>
    internal class MappingPIComponent
    {
        public string ClrNamespace
        {
            get
            {
                return _clrNamespace;
            }
            set
            {
                _clrNamespace = value;
            }
        }
        public string XmlNamespace
        {
            get
            {
                return _xmlNamespace;
            }
            set
            {
                _xmlNamespace = value;
            }
        }
        public string Assembly
        {
            get
            {
                return _assembly;
            }
            set
            {
                _assembly = value;
            }
        }

        string _clrNamespace;
        string _xmlNamespace;
        string _assembly;
    }

    #endregion Internal Class

    #region Helper delegates

    /// <summary>
    /// The type of helper required for generating text content.
    /// </summary>
    /// <param name="parentNode">Element that will get the content.</param>
    /// <returns>Whether or not the text content was handled.</returns>
    public delegate HandledLevel TextContentHelper(XmlNode parentNode);

    /// <summary>
    /// The type of helper required for overriding generating attribute values.
    /// </summary>
    /// <param name="parentNode">Element that will get the attribute.</param>
    /// <param name="attribute">Attribute that will get the value.</param>
    /// <returns>Whether or not the attribute was handled.  Both Partial 
    /// and Complete are considered "handled" by the generator.  The
    /// generator will create and assign an attribute value itself only if the 
    /// return value is HandledLevel.None.</returns>
    public delegate HandledLevel AttributeHelper(XmlNode parentNode, XmlAttribute attribute);

    /// <summary>
    /// The type of helper required for overriding or tracking element generation.
    /// </summary>
    /// <param name="parentNode">Node that will get the new element.</param>
    /// <param name="newNode">New element.</param>
    /// <param name="isStart">True if the creation of the element is starting. False if ending.</param>
    /// <returns>A HandledLevel. Partial means the helper did some work, but the generator should continue adding attributes and inserting the new element.</returns>
    public delegate HandledLevel ElementHelper(XmlNode parentNode, XmlNode newNode, bool isStart);

    /// <summary>
    /// Indicates the level that a helper has handled text, an attribute, or a node.
    /// </summary>
    public enum HandledLevel
    {
        /// <summary>
        /// Indicates that the helper did not change a node at all.
        /// </summary>
        None,
        /// <summary>
        /// Indicates that the helper changed something in a node, but the generator should continue as usual.
        /// </summary>
        Partial,
        /// <summary>
        /// Indicates that the helper changed something in a node, and the generator should skip its current step.
        /// </summary>
        Complete
    }

    #endregion Helper delegates

    /// <summary>
    /// The type of handler required for listening to the GeneratorStatus event.
    /// </summary>
    public delegate void GeneratorStatusEventHandler(object sender, GeneratorStatusEventArgs e);

    /// <summary>
    /// Holds message for create xml status.
    /// </summary>
    public class GeneratorStatusEventArgs : EventArgs
    {
        string _message = String.Empty;

        bool _xmlCreated = false;

        /// <summary>
        /// Creates event args.
        /// </summary>
        /// <param name="message">Any message for listeners.</param>
        /// <param name="xmlCreated">Indicates whether or not an xml (file or stream) as just been created.</param>
        public GeneratorStatusEventArgs(string message, bool xmlCreated)
            : base()
        {
            _message = message;
            _xmlCreated = xmlCreated;
        }

        /// <summary>
        /// The event's message.
        /// </summary>
        /// <value></value>
        public string Message
        {
            get
            {
                return _message;
            }
        }

        /// <summary>
        /// Indicates whether or not an xml (file or stream) as just been created.
        /// </summary>
        /// <value></value>
        public bool XmlCreated
        {
            get
            {
                return _xmlCreated;
            }
        }
    }
}
