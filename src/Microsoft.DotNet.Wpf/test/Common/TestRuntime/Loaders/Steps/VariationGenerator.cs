// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.Test.Security.Wrappers;

namespace Microsoft.Test.Utilities.VariationEngine
{
	/// <summary>
	/// VariationGenerator Abstract class.
	/// </summary>
	//    public abstract class VariationGenerator
    public abstract class VariationGenerator : IDisposable
    {
        #region Member variables
        internal TemplateData _dd = null;
        internal Scenarios scenarios = null;
        internal List<Scenario> _scenariolist = null;

        Scenario currentScenario = null;

        internal XmlDocumentSW canvasdoc = null;
     
        internal XmlNode placeholdernode = null;

        const string PlaceHolderElement = "PlaceHolder";

        internal string tokendocumentnamespace = null;

        internal Hashtable variationids;

        #endregion Member variables.

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        internal VariationGenerator()
        {
            Init();
        }

        /// <summary>
        /// Dispose objects that are not required anymore.
        /// </summary>
        public virtual void Dispose()
        {
            _dd = null;
            scenarios = null;
            _scenariolist = null;
            currentScenario = null;

            canvasdoc = null;
            placeholdernode = null;
            variationids = null;
        }

        /// <summary>
        /// Read Variation data based on xmlfile.
        /// </summary>
        /// <param name="xmldatafile"></param>
        /// <returns></returns>
        public virtual bool Read(string xmldatafile)
        {
            bool returnvalue = false;

            xmldatafile = CommonHelper.VerifyFileExists(xmldatafile);
            if (String.IsNullOrEmpty(xmldatafile))
            {
                throw new System.IO.FileNotFoundException(xmldatafile + " could not be found");
            }

            try
            {
                Console.WriteLine("Reading {0} file", xmldatafile);
                XmlDocumentSW defaultDoc = new XmlDocumentSW();
                defaultDoc.Load(xmldatafile);

                returnvalue = this.Read((XmlNodeSW)defaultDoc);
                UtilsLogger.LogDiagnostic = "Read returned value = " + returnvalue;
            }
            catch (XmlException xex)
            {
                UtilsLogger.LogDiagnostic = "Reading data file caused an Exception";
                throw xex;
            }

            return returnvalue;
        }

        /// <summary>
        /// Read data from a Xml Node.
        /// </summary>
        /// <param name="datanode"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public bool Read(XmlNodeSW datanode)
        {
            try
            {
                XmlDocumentSW defaultDoc = new XmlDocumentSW();
                defaultDoc.LoadXml(datanode.OuterXml);

                if (defaultDoc.DocumentElement.Name != Constants.XMLVariationTemplateElement)
                {
                    throw new ApplicationException(defaultDoc.DocumentElement.Name + " is not a supported root element");
                }

                // Check for Include Elements first
                // In this case if there are default data and Scenario elements they get ignored.
                if (defaultDoc.HasChildNodes == false && defaultDoc.DocumentElement.HasChildNodes)
                {
                    UtilsLogger.LogError = "Empty document specified";
                    return false;
                }

                bool defaultdatafilespecified = false;
                bool scenariosfilespecified = false;

                // Loop through the elements under the Document element and get information.
                // If there are duplicates found or there are overrides found do the right thing.
                for (int i = 0; i < defaultDoc.DocumentElement.ChildNodes.Count; i++)
                {
                    XmlNode currentnode = defaultDoc.DocumentElement.ChildNodes[i];
                    switch (currentnode.Name)
                    {
                        case Constants.IncludeElement:
                            // There are 2 types of Includes.
                            // Include - TemplateData & Scenarios.
                            // If both these are specified parse the files they point to.
                            // If duplicates are specified or more than one error out.
                            if (currentnode.Attributes.Count > 0)
                            {
                                if (currentnode.Attributes[Constants.TypeAttribute] == null)
                                {
                                    UtilsLogger.LogError = "Include element does not have Attribute Type";
                                    return false;
                                }

                                string file = null;

                                // Read the Item attribute on the element which means the file to include.
                                if (currentnode.Attributes[Constants.TypeAttribute].Value == Constants.TemplateDataElement ||
                                    currentnode.Attributes[Constants.TypeAttribute].Value == Constants.ScenariosElement)
                                {
                                    // If current node contains attribute "TemplateData" or "Scenarios" get 
                                    // value of "Item" attribute which is a path to an xml file and load the xml.
                                    string includefile = currentnode.Attributes["Item"].Value;
                                    if (includefile == null)
                                    {
                                        UtilsLogger.LogError = "Item attribute not specified on Include element";
                                        return false;
                                    }

                                    file = CommonHelper.VerifyFileExists(includefile);
                                    if (file == null)
                                    {
                                        // Filenot found
                                        throw new FileNotFoundException("Could not find file - " + includefile);
                                    }
                                }
                                else
                                {
                                    UtilsLogger.LogError = "Use Either TemplateData or Scenarios.";
                                    throw new NotSupportedException("Unsupported type - " + currentnode.Attributes[Constants.TypeAttribute].Value + " value.");
                                }

                                // Depending on Type attribute value get the defaultdatalist or scenariosdoc.
                                switch (currentnode.Attributes[Constants.TypeAttribute].Value)
                                {
                                    case Constants.TemplateDataElement:
                                        if (defaultdatafilespecified == false)
                                        {
                                            ReadTemplateData(file);
                                            defaultdatafilespecified = true;
                                        }
                                        else
                                        {
                                            throw new NotSupportedException("Cannot have multipe TemplateData include elements");
                                        }
                                        break;

                                    case Constants.ScenariosElement:
                                        if (scenariosfilespecified == false)
                                        {
                                            ReadScenarios(file);
                                            scenariosfilespecified = true;
                                        }
                                        else
                                        {
                                            throw new NotSupportedException("Cannot have multiple Scenario include elements");
                                        }
                                        break;

                                    default:
                                        Console.WriteLine("Include attribute equals {0}", currentnode.Attributes[Constants.TypeAttribute].Value);
                                        throw new NotSupportedException("Include attribute Type does not equal \"TemplateData\" or \"Scenarios\".");
                                }
                            }
                            else
                            {
                                // Time for another error.
                                Console.WriteLine("No attributes found on the Include element");
                                return false;
                            }
                            break;

                        case Constants.TemplateDataElement:
                            // Check if there was an include that already read the default data.
                            
                            if (defaultdatafilespecified == false)
                            {
                                ReadTemplateData(currentnode);
                            }
                            else
                            {
                                Console.WriteLine("Ignoring TemplateData section in Xml document");
                            }
                            break;

                        case Constants.ScenariosElement:
                            // Check if there was an include that already read the Scenarios.
                            if (scenariosfilespecified == false)
                            {
                                ReadScenarios(currentnode);
                            }
                            else
                            {
                                Console.WriteLine("Ignoring Scenarios section in Xml document");
                            }
                            break;
                    }
                }

                if (_dd == null)
                {
                    throw new ApplicationException("No TemplateData node was found.");
                }

                if (scenarios == null)
                {
                    throw new ApplicationException("No Scenarios node was found.");
                }

                if (_dd != null && scenarios != null)
                {
                    DoDeepCopy();
                }

            }
            catch (XmlException ex)
            {
                // Get Exception Message and print.
                UtilsLogger.DisplayExceptionInformation(ex);
                return false;
            }

            return true;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Apply Scenario and all variations specified under the scenario.
        /// </summary>
        /// <param name="scenarioid"></param>
        /// <returns></returns>
        internal bool ApplyScenario(string scenarioid)
        {
            return ApplyScenario(scenarioid, null);
        }

        /// <summary>
        /// Apply a specific scenario to the current default data document.
        /// </summary>
        /// <param name="scenarioid"></param>
        /// <param name="desiredvariationids"></param>
        /// <returns></returns>
        internal bool ApplyScenario(string scenarioid, string[] desiredvariationids)
        {
            if (scenarioid == null)
            {
                UtilsLogger.LogDiagnostic = "Scenario ID is null";
                return false;
            }

            if (_dd == null)
            {
                UtilsLogger.LogDiagnostic = "TemplateData object is null";
                return false;
            }

            if (_dd.templatedataelement == null)
            {
                // Todo: ---
                UtilsLogger.LogDiagnostic = "TemplateData has not been tokenized";
                return false;
            }

            if (scenarios == null)
            {
                UtilsLogger.LogDiagnostic = "Scenarios object is null";
                return false;
            }

            canvasdoc = new XmlDocumentSW();
            string rootxml = null;
            if (_dd.rootelement != null)
            {
                tokendocumentnamespace = _dd.rootelement.NamespaceURI;

                rootxml = "<" + Constants.TemplateDataElement;
                rootxml += " xmlns=\"" + tokendocumentnamespace + "\">";
                rootxml += "</" + Constants.TemplateDataElement + ">";
            }
            else
            {
                tokendocumentnamespace = "";
                rootxml = "<" + Constants.TemplateDataElement + ">";                
                rootxml += "</" + Constants.TemplateDataElement + ">";
            }

            if (_dd.bcontainsrootnodeelement )
            {

                if (_dd.templatedataelement.ChildNodes.Count != 1)
                {
                    throw new ApplicationException("No more than one RootNodeVariation can be specified");
                }

                tokendocumentnamespace = _dd.templatedataelement.ChildNodes[0].NamespaceURI;
            }

            canvasdoc.LoadXml(rootxml);
            canvasdoc.DocumentElement.InnerXml = _dd.templatedataelement.InnerXml;

            // Find the scenario ID from scenariolist.
            for (int i = 0; _scenariolist != null && i < _scenariolist.Count; i++)
            {
                if (_scenariolist[i].Case == scenarioid)
                {
                    currentScenario = _scenariolist[i];
                    break;
                }
            }

            if (currentScenario == null)
            {
                throw new ApplicationException("Could not find scenario with ID = " + scenarioid);                
            }

            // Read variations relevant to that scenario.
            currentScenario.ReadVariations();

            variationids = new Hashtable();

			if (desiredvariationids != null && desiredvariationids.Length != 0)
			{
				for (int i = 0; i < desiredvariationids.Length; i++)
				{
					variationids.Add(desiredvariationids[i], i);
				}
			}
			else
			{
				int count = 0;
				IDictionaryEnumerator enumerator = currentScenario.nodevariationList.GetEnumerator();
				while (enumerator.MoveNext())
				{
					variationids.Add(((BaseVariation)enumerator.Value).ID, count++);					
				}

				enumerator = currentScenario.textVariationList.GetEnumerator();
				while (enumerator.MoveNext())
				{
					variationids.Add(((BaseVariation)enumerator.Value).ID, count++);
				}

				for (int i = 0; i < currentScenario.attributeVariationList.Count; i++)
				{
					variationids.Add(((BaseVariation)currentScenario.attributeVariationList[i]).ID, count++);
				}
			}

            // Start applying those variations.
            if (canvasdoc.DocumentElement.ChildNodes.Count > 1)
            {
                List<XmlNode> nodelist = new List<XmlNode>(canvasdoc.DocumentElement.ChildNodes.Count);
                for (int i = 0; i < canvasdoc.DocumentElement.ChildNodes.Count; i++)
                {
                    nodelist.Add(canvasdoc.DocumentElement.ChildNodes[i]);
                }

                for (int i = 0; i < nodelist.Count; i++)
                {
                    RecurseTree(nodelist[i]);
                }

                nodelist = null;
            }
            else
            {
                RecurseTree(canvasdoc.DocumentElement);
            }

            if (currentScenario.unrecognizednodeslist != null && currentScenario.unrecognizednodeslist.Count > 0)
            {
                for (int i = 0; i < currentScenario.unrecognizednodeslist.Count; i++)
                {
                    if (ProcessUnRecognizedElements(currentScenario.unrecognizednodeslist[i]))
                    {
                        currentScenario.unrecognizednodeslist.Remove(currentScenario.unrecognizednodeslist[i]);
                        i--; // Reset back to one as one node was removed.
                    }
                }

                // Todo : Add code to specify that unrecognized code was found.
            }

            Cleanup();
            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize member variables.
        /// </summary>
        private void Init()
        {
            _scenariolist = new List<Scenario>();
        }

        /// <summary>
        /// Remove any place holder element left around in the document that was modified to apply the variations.
        /// </summary>
        private void Cleanup()
        {
            if (canvasdoc != null)
            {
                XmlNodeList placeholders = canvasdoc.GetElementsByTagName(PlaceHolderElement);
                for (int i = 0; i < placeholders.Count; i++)
                {
                    canvasdoc.DocumentElement.RemoveChild(placeholders[i]);
                }
            }
        }

        /// <summary>
        /// Read Default Data from a file.
        /// </summary>
        /// <param name="xmlfile"></param>
        private void ReadTemplateData(string xmlfile)
        {
            _dd = new TemplateData(xmlfile);
        }

        /// <summary>
        /// Read Default Data from a xaml node.
        /// </summary>
        /// <param name="ddnode"></param>
        private void ReadTemplateData(XmlNode ddnode)
        {
            if (ddnode.SelectNodes("//" + Constants.TemplateDataElement).Count == 0 || ddnode.SelectNodes("//" + Constants.TemplateDataElement).Count > 1)
            {
                throw new NotSupportedException("There was no TemplateData element found in the current node.");
            }

            _dd = new TemplateData(ddnode);
        }

        /// <summary>
        /// Read Scenarios from a file.
        /// </summary>
        /// <param name="xmlfile"></param>
        private void ReadScenarios(string xmlfile)
        {
            xmlfile = CommonHelper.VerifyFileExists(xmlfile);
            if (String.IsNullOrEmpty(xmlfile))
            {
                throw new FileNotFoundException(xmlfile + " could not be found");
            }

            XmlDocumentSW scenariosdoc = new XmlDocumentSW();
            scenariosdoc.Load(xmlfile);

            Console.WriteLine("Reading include Scenarios file {0}", xmlfile);

            ReadScenarios((XmlNode)scenariosdoc.DocumentElement);
        }

        /// <summary>
        /// Read Scenarios from a Xmlnode.
        /// </summary>
        /// <param name="scenariosnode"></param>
        private void ReadScenarios(XmlNode scenariosnode)
        {
            if (scenariosnode.SelectNodes("//" + Constants.ScenariosElement).Count == 0 || scenariosnode.SelectNodes("//" + Constants.ScenariosElement).Count > 1)
            {
                throw new Exception("There was no Scenarios element found in the current node.");
            }

            scenarios = new Scenarios(scenariosnode.SelectNodes("//" + Constants.ScenariosElement)[0]);
            _scenariolist = scenarios.scenariolist;

            if (scenarios.defaultsnode != null && scenarios.defaultsnode.HasChildNodes)
            {
                for (int i = 0; i < scenarios.defaultsnode.ChildNodes.Count; i++)
                {
                    ReadDefaults(scenarios.defaultsnode.ChildNodes[i]);
                }
            }
        }

        /// <summary>
        /// Need to copy all variations so that when remove operations 
        /// are done the actual data does not get lost.
        /// </summary>
        private void DoDeepCopy()
        {
            if (_dd == null)
            {
                throw new ArgumentNullException("TemplateData deep copy failed as default data object is null");
            }

            if (scenarios == null)
            {
                throw new ArgumentNullException("Scenarios deep copy failed as scenarios object is null");
            }

            _scenariolist = new List<Scenario>(scenarios.scenariolist);

        }

        /// <summary>
        /// When a variation cannot be found or cannot be applied to the defaults tree it needs to be restored.
        /// This method restores the default tree. While restoring it calls NodeVariationApplied method 
        /// to allow derived classes to retreive information from the changes made or not made.
        /// Finally once the node has been restored, remove the variation from the scenario's list.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="attributestable"></param>
        private void RestoreDefaults(XmlNode node, Hashtable attributestable)
        {
            // Now it's just a matter of moving the node after the variation element and deleting the variation element.
            if (node == null)
            {
                return;
            }

            if (node.Attributes[Constants.IDAttribute] == null)
            {
                return;
            }

            // Create Place holder node if it's not already created and set innerxml of current node to placeholder.
            if (placeholdernode == null)
            {
                CreatePlaceHolder();
            }

            placeholdernode.InnerXml = "";
            placeholdernode.InnerXml = node.InnerXml;

            // The above steps facilitates calling NodeVariationApplied method to allow derived classes which 
            // override the behavior to store derived class specific information.
            NodeVariationApplied(node.ParentNode, placeholdernode, attributestable);

            // Take all the children under placeholder node and insert them after the 
            // current variation node that is being changed and then finally remove the node and it's children.
            XmlNode parentnode = node.ParentNode;
            XmlNode refchild = node;
            while (placeholdernode.ChildNodes.Count > 0)
            {
                refchild = parentnode.InsertAfter(placeholdernode.ChildNodes[0], refchild);
            }

            parentnode.RemoveChild(node);

            // Get the current node id and remove it from scenario list.
            string id = node.Attributes[Constants.IDAttribute].Value;

            switch (node.Name)
            {
                case Constants.NodeVariationElement:
                case Constants.TextVariationElement:
                    currentScenario.nodevariationList.Remove(id);
                    break;

                case Constants.AttributeVariationElement:
                    currentScenario.RemoveAttributeVariation(id);
                    break;
            }

            CleanupPlaceHolder();
        }

        /// <summary>
        /// Apply Node variation specified in default template.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool ApplyNodeVariation(XmlNode node)
        {
            // Get variation ID from element.
            // Check if variation is all or selected numbers. If all apply all variations.
            // If selected only do selected variations.

            if (currentScenario == null)
            {
                // There nothing to do.
                return false;
            }

            // Using Default Node variation instead of Node variation as it supports having
            // nested NodeVariation or AttributeVariation/TextVariation combinations.
            DefaultNodeVariation temp = new DefaultNodeVariation();
            temp.Initialize(node);

            // Check if the Node is in the list of variation ids in the scenario.
            string nodeid = temp.ID;
            if (variationids.Count != 0)
            {
                if (variationids.Contains(nodeid) == false)
                {
                    RestoreDefaults(node, temp.attributestable);
                    return true;
                }
            }
            else if (variationids.Contains(nodeid) == false)
            {
                RestoreDefaults(node, temp.attributestable);
                return false;
            }

            //			NodeVariation nodevariation = currentScenario.GetNodeVariation(nodeid);
            //			if (nodevariation == null)
            //			{
            //				RestoreDefaults(node, temp.attributestable);
            //				temp = null;
            //				return false;
            //			}

            // Check if the id is in the list of variations in the default variations list.
            // If not restore the default.
            if (currentScenario.nodevariationList.Contains(nodeid) == false)
            {
                RestoreDefaults(node, temp.attributestable);
                temp = null;
                return false;
            }

            // First create a placeholder and update the placeholder xml.
            // Call NodeVariationApplied method to have derived classes extract information relevant to them.
            // Finally apply the xml to the default template.
            NodeVariation nodevariation = (NodeVariation)currentScenario.nodevariationList[nodeid];

            // Create PlaceHolder.
            if (placeholdernode == null)
            {
                CreatePlaceHolder();
            }

            if (String.IsNullOrEmpty(nodevariation.VariationChildrenXml) == false)
            {
                placeholdernode.InnerXml = nodevariation.VariationChildrenXml;
            }
            else
            {
                placeholdernode.InnerXml = "";
            }

            NodeVariationApplied(node.ParentNode, placeholdernode, nodevariation.attributestable);

            // Get Parent of current node.
            XmlNode parentnode = node.ParentNode;
            XmlNode refchild = node;
            while (placeholdernode.ChildNodes.Count > 0)
            {
                refchild = parentnode.InsertAfter(placeholdernode.ChildNodes[0], refchild);
            }

            // Remove Variation element & variation id from list of variations to be applied.
            parentnode.RemoveChild(node);
            variationids.Remove(nodeid);

            CleanupPlaceHolder();

            return true;

        }

        /// <summary>
        /// Apply Node variation specified in default template.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool ApplyRootNodeVariation(XmlNode node)
        {
            // Get variation ID from element.
            // Check if variation is all or selected numbers. If all apply all variations.
            // If selected only do selected variations.

            if (currentScenario == null)
            {
                // There nothing to do.
                return false;
            }

            // Using Default Node variation instead of Node variation as it supports having
            // nested NodeVariation or AttributeVariation/TextVariation combinations.
            RootNodeVariation temp = new RootNodeVariation();
            temp.Initialize(node);

            // Check if the Node is in the list of variation ids in the scenario.
            string nodeid = temp.ID;
            if (variationids.Count != 0)
            {
                if (variationids.Contains(nodeid) == false)
                {
                    RestoreDefaults(node, temp.attributestable);
                    return true;
                }
            }
            else if (variationids.Contains(nodeid) == false)
            {
                RestoreDefaults(node, temp.attributestable);
                return false;
            }

            // Check if the id is in the list of variations in the default variations list.
            // If not restore the default.
            //if (currentScenario.nodevariationList.Contains(nodeid) == false)
            //{
            //    RestoreDefaults(node, temp.attributestable);
            //    temp = null;
            //    return false;
            //}

            // First create a placeholder and update the placeholder xml.
            // Call NodeVariationApplied method to have derived classes extract information relevant to them.
            // Finally apply the xml to the default template.
            RootNodeVariation rootnodevariation = currentScenario.rootnodevariation;
            //rootnodevariation.ElementValue 

            NodeVariationApplied(node.ParentNode, placeholdernode, rootnodevariation.attributestable);

            if (String.IsNullOrEmpty(rootnodevariation.ElementValue))
            {
                throw new ApplicationException("RootNodeVariation elementvalue cannot be null");
            }

            // Create a new Element here.
            // Copy all the existing root element attributes if none have to be changed.
            // Assign existing innerxml to the new element.
            XmlNode newnode = node.OwnerDocument.CreateElement(rootnodevariation.ElementValue,this.tokendocumentnamespace);
            node.ParentNode.AppendChild(newnode);

            XmlNode rootnode = node.ChildNodes[0];
            for (int i = 0; i < rootnode.Attributes.Count; i++ )
            {
                newnode.Attributes.Append(rootnode.Attributes[i]);
            }

            for (int i = 0; i < rootnode.ChildNodes.Count; i++)
            {
                newnode.AppendChild(rootnode.ChildNodes[i]);
            }

            // Get Parent of current node.
            XmlNode parentnode = node.ParentNode;
            parentnode.RemoveChild(node);

            // Remove Variation element & variation id from list of variations to be applied.
            variationids.Remove(nodeid);

            return true;

        }

        /// <summary>
        /// Attribute variation is a little special as there can be multiple of them specified in the
        /// Scenario to change multiple Attributes on a element.
        /// For this we keep looping until all the variations are processed.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool ApplyAttributeVariation(XmlNode node)
        {
            if (currentScenario == null)
            {
                // There nothing to do.
                return false;
            }

            if (node.HasChildNodes == false)
            {
                // Todo: Attribute variation has to have one child node. Throw exception
                return false;
            }

            if (node.ChildNodes.Count > 1)
            {
                // Todo: Only one allowed. Throw exception
                return false;
            }

            AttributeVariation temp = new AttributeVariation();
            temp.Initialize(node);

            string nodeid = temp.ID;
            if (variationids.Count != 0)
            {
                if (variationids.Contains(nodeid) == false)
                {
                    RestoreDefaults(node, temp.attributestable);
                    return true;
                }
            }
            else if (variationids.Contains(nodeid) == false)
            {
                RestoreDefaults(node, temp.attributestable);
                return false;
            }

            AttributeVariation attributevariation = currentScenario.GetAttributeVariation(nodeid);
            if (attributevariation == null)
            {
                RestoreDefaults(node, temp.attributestable);
                return true;
            }

            // Loop until there are no more attributevariations with the same id to process.
            XmlNode nodetomodify = null;
            do
            {
                if (nodetomodify == null)
                {
                    nodetomodify = node.ChildNodes[0];
                }

                if (nodetomodify.Attributes.Count == 0)
                {
                    // Nothing to change.
                    attributevariation = null;
                    RestoreDefaults(node, temp.attributestable);
                    return true;
                }

                // Change the attribute if it is found on the node.
                XmlAttribute attrib = nodetomodify.Attributes[attributevariation.AttributeName];
                if (attrib != null)
                {
                    if (attributevariation.RemoveAttribute == false)
                    {
                        attrib.Value = attributevariation.AttributeValue;
                    }
                    else
                    {
                        XmlNode parentnode = (XmlNode)attrib.OwnerElement;
                        parentnode.Attributes.Remove(attrib);
                    }
                }
                else
                {
                    attrib = nodetomodify.OwnerDocument.CreateAttribute(attributevariation.AttributeName);                    
                    attrib.Value = attributevariation.AttributeValue;
                    nodetomodify.Attributes.Append(attrib);
                }

                NodeVariationApplied(node.ParentNode, nodetomodify, attributevariation.attributestable);

                currentScenario.RemoveAttributeVariation(nodeid);

                // Delete the node and the variation id from the list only when there are no more 
                // variations with the same ID to apply.
                attributevariation = currentScenario.GetAttributeVariation(nodeid);
                if (attributevariation == null)
                {
                    XmlNode parentnode = node.ParentNode;
                    parentnode.InsertAfter(nodetomodify, node);

                    parentnode.RemoveChild(node);

                    variationids.Remove(nodeid);
                }

            } while (attributevariation != null);

            CleanupPlaceHolder();

            return true;
        }

        /// <summary>
        /// Similar to NodeVariation only Xml Text content is changed.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool ApplyTextVariation(XmlNode node)
        {
            if (currentScenario == null)
            {
                // There nothing to do.
                return false;
            }

            if (node.HasChildNodes == false)
            {
                // Todo: Attribute variation has to have one child node Throw exception
                return false;
            }

            if (node.ChildNodes.Count > 1)
            {
                // Todo: Only one allowed. Throw exception
                return false;
            }

            DefaultTextVariation temp = new DefaultTextVariation();
            temp.Initialize(node);

            string nodeid = temp.ID;
            if (variationids.Count != 0)
            {
                if (variationids.Contains(nodeid) == false)
                {
                    RestoreDefaults(node, temp.attributestable);
                    return false;
                }
            }
            else if (variationids.Contains(nodeid) == false)
            {
                RestoreDefaults(node, temp.attributestable);
                return false;
            }

            if (currentScenario.textVariationList.Contains(nodeid) == false)
            {
                RestoreDefaults(node, temp.attributestable);
                return false;
            }

            XmlNode nodetomodify = node.ChildNodes[0];
            if (nodetomodify == null)
            {
                // Nothing to change.
                RestoreDefaults(node, temp.attributestable);
                return false;
            }

            TextVariation textvariation = (TextVariation)currentScenario.textVariationList[nodeid];
            nodetomodify.InnerText = textvariation.Text;

            NodeVariationApplied(node.ParentNode, nodetomodify, textvariation.attributestable);

            // Get Parent of current node.
            XmlNode parentnode = node.ParentNode;
            parentnode.InsertAfter(nodetomodify, node);

            parentnode.RemoveChild(node);
            variationids.Remove(nodeid);

            CleanupPlaceHolder();

            return true;
        }

        /// <summary>
        /// Method Recurses an Xml element tree specifically looking for Variation elements.
        /// Once an element is found the respective Apply variation method is called on that element.
        /// </summary>
        /// <param name="element"></param>
        private void RecurseTree(XmlNode element)
        {
            if (element == null)
            {
                return;
            }

            if (element.HasChildNodes)
            {
                if (element.HasChildNodes)
                {
                    List<XmlNode> childnodes = new List<XmlNode>(element.ChildNodes.Count);
                    for (int i = 0; i < element.ChildNodes.Count; i++)
                    {
                        childnodes.Add(element.ChildNodes[i]);
                    }

                    for (int i = 0; i < childnodes.Count; i++)
                    {
                        RecurseTree(childnodes[i]);
                    }

                    childnodes = null;
                }
            }

            switch (element.Name)
            {
                case Constants.NodeVariationElement:
                    ApplyNodeVariation(element);
                    break;

                case Constants.AttributeVariationElement:
                    ApplyAttributeVariation(element);
                    break;

                case Constants.TextVariationElement:
                    ApplyTextVariation(element);
                    break;

                case Constants.RootNodeVariationElement:
                    ApplyRootNodeVariation(element);
                    break;
            }

        }

        /// <summary>
        /// Creates a PlaceHolder element under the template element.
        /// </summary>
        private void CreatePlaceHolder()
        {
            placeholdernode = canvasdoc.CreateElement(PlaceHolderElement);
            XmlNode lastchild = canvasdoc.DocumentElement.LastChild;
            canvasdoc.DocumentElement.InsertAfter(placeholdernode, lastchild);
        }

        /// <summary>
        /// Clean the placeholder element for further processing.
        /// </summary>
        private void CleanupPlaceHolder()
        {
            if (placeholdernode == null)
            {
                return;
            }

            placeholdernode.InnerXml = "";
            placeholdernode.Attributes.RemoveAll();

            XmlNode parentnode = placeholdernode.ParentNode;
            parentnode.RemoveChild(placeholdernode);

            parentnode = null;
            placeholdernode = null;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Virtual methods to process Defaults element in Scenarios element
        /// </summary>
        /// <param name="node"></param>
        protected virtual void ReadDefaults(XmlNode node)
        {
            // Abstract definition.
            // Variation Generator does not do anything.
        }

        /// <summary>
        /// Virtual method used to Process unrecongnized elements under Scenario element.
        /// </summary>
        /// <param name="unrecognizednode"></param>
        /// <returns></returns>
        protected virtual bool ProcessUnRecognizedElements(XmlNode unrecognizednode)
        {
            // None here, derived classes can override this.
            return false;
        }

        /// <summary>
        /// Virtual method used to get information from an element which has been transformed.
        /// </summary>
        /// <param name="actualnode"></param>
        /// <param name="modifiednode"></param>
        /// <param name="attributestable"></param>
        protected virtual void NodeVariationApplied(XmlNode actualnode, XmlNode modifiednode, Hashtable attributestable)
        {
        }

        #endregion

    }
}
