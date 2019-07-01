// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Xml.XPath;

namespace Microsoft.Test.Utilities.VariationEngine
{
	/// <summary>
	/// Summary description for Scenario.
	/// </summary>
    internal class Scenario
    {
        #region Memeber variables
        string caseid;
        internal Hashtable nodevariationList;
        internal Hashtable textVariationList;
        internal List<AttributeVariation> attributeVariationList;
        internal RootNodeVariation rootnodevariation = null;

        internal List<XmlNode> unrecognizednodeslist = null;

        Random autorand;

        /// <summary>
        /// Use this for caching scenario and use this only when 
        /// scenario needs to be applied.
        /// </summary>
        XmlNode currentscenarionode = null;

        #endregion

        #region Internal Methods

        /// <summary>
        /// Constructor
        /// </summary>
        internal Scenario()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize member variables.
        /// </summary>
        internal virtual void Initialize()
        {
            nodevariationList = new Hashtable();
            textVariationList = new Hashtable();

            attributeVariationList = new List<AttributeVariation>();
        }

        /// <summary>
        /// Get data from a Scenario XmlNode.
        /// </summary>
        /// <param name="scenarionode"></param>
        internal Scenario(XmlNode scenarionode)
        {
            Initialize();
            // Todo: Removed - Read(scenarionode);

            if (scenarionode == null || scenarionode.Name != Constants.ScenarioElement)
            {
                throw new ArgumentNullException("Input Xml node is null");
            }

            if (scenarionode.Attributes[Constants.CaseAttribute] == null)
            {
                throw new ArgumentException("Scenario without Case attribute found.");
            }

            caseid = scenarionode.Attributes[Constants.CaseAttribute].Value;

            currentscenarionode = scenarionode;
        }

        /// <summary>
        /// Reads all variations specified in current scenario.
        /// Setup to read only when required when the default tree is being parsed for example.
        /// </summary>
        internal virtual void ReadVariations()
        {
            if (currentscenarionode == null)
            {
                UtilsLogger.LogDiagnostic = "Current scenario is null, there is no variations to read.";
                return;
            }

            if (currentscenarionode.HasChildNodes == false)
            {
                UtilsLogger.LogDiagnostic = "Current scenario does not contain any Variations";
                return;
            }

            // In Scenario elements nested Variation elements are not allowed. 
            // Thus the loop here only looks for child elements of the Scenario node.
            // If there are nested Variation elements an Exception will be thrown when the 
            // variation is initialized.
            // If a non-variation element is found that element is stored in a list for processing
            // by later processing.
            for (int i = 0; i < currentscenarionode.ChildNodes.Count; i++)
            {
                if (currentscenarionode.ChildNodes[i] == null)
                {
                    continue;
                }

                switch (currentscenarionode.ChildNodes[i].Name)
                {
                    case Constants.NodeVariationElement:
                        NodeVariation newnodevariation = new NodeVariation();
                        newnodevariation.Initialize(currentscenarionode.ChildNodes[i]);
                        nodevariationList.Add(newnodevariation.ID, newnodevariation);

//						if (nodevariationList.ContainsKey(newnodevariation.ID))
//						{
//							nodevariationList.Add(Convert.ToInt32(GenerateRandomNumber()) + "_" + newnodevariation.ID, newnodevariation);
//						}
//						else
//						{
//							nodevariationList.Add(newnodevariation.ID, newnodevariation);
//						}
                        break;

                    case Constants.AttributeVariationElement:
                        AttributeVariation newattribvariation = new AttributeVariation();
                        newattribvariation.Initialize(currentscenarionode.ChildNodes[i]);
                        attributeVariationList.Add(newattribvariation);
                        break;

                    case Constants.TextVariationElement:
                        TextVariation newtextvariation = new TextVariation();
                        newtextvariation.Initialize(currentscenarionode.ChildNodes[i]);
                        textVariationList.Add(newtextvariation.ID, newtextvariation);
                        break;

                    case Constants.RootNodeVariationElement:
                        if (rootnodevariation != null)
                        {
                            throw new ApplicationException("No more than one RootNodeVariation can be specified in the document.");
                        }

                        rootnodevariation = new RootNodeVariation();
                        rootnodevariation.Initialize(currentscenarionode.ChildNodes[i]);
                        break;

                    default:
                        if (unrecognizednodeslist == null)
                        {
                            unrecognizednodeslist = new List<XmlNode>();
                        }

                        unrecognizednodeslist.Add(currentscenarionode.ChildNodes[i]);
                        break;
                }
            }

            if (nodevariationList.Count == 0 && attributeVariationList.Count == 0 && textVariationList.Count == 0)
            {
                // Todo: Removed this exception to allow changing only filename for a variation.
                //throw new NotSupportedException("Current scenario does not contain any supported variations.");
            }
        }

        /// <summary>
        /// Current Scenario Case/ID.
        /// </summary>
        /// <value></value>
        internal string Case
        {
            get
            {
                return caseid;
            }
            set
            {
                caseid = value;
            }
        }

        /// <summary>
        /// Gets a particular Attribute Variation based on the ID.
        /// This was implemented to allow for cases when more than one Attribute Variation
        /// needs to be applied on the Default Attribute Variation.
        /// </summary>
        /// <param name="caseid"></param>
        /// <returns></returns>
        internal AttributeVariation GetAttributeVariation(string caseid)
        {
            if (String.IsNullOrEmpty(caseid))
            {
                return null;
            }

            if (attributeVariationList.Count == 0)
            {
                return null;
            }

            // If the key is not duplicated then a simple check if the key is 
            // contained in the Hashtable should be sufficient.

            for (int i = 0; i < attributeVariationList.Count; i++)
            {
                AttributeVariation currentvariation = (AttributeVariation)attributeVariationList[i];
                if (currentvariation == null)
                {
                    continue;
                }

                if (currentvariation.ID == caseid)
                {
                    return currentvariation;
                }
            }

            return null;
        }

        /// <summary>
        /// Similar to GetAttributeVariation, RemoveAttributeVariation is used to remove
        /// an attribute variation when it's duplicated.
        /// </summary>
        /// <param name="caseid"></param>
        internal void RemoveAttributeVariation(string caseid)
        {
            if (String.IsNullOrEmpty(caseid))
            {
                return;
            }

            if (attributeVariationList.Count == 0)
            {
                return;
            }

            for (int i = 0; i < attributeVariationList.Count; i++)
            {
                AttributeVariation currentvariation = (AttributeVariation)attributeVariationList[i];
                if (currentvariation == null)
                {
                    continue;
                }

                if (currentvariation.ID == caseid)
                {
                    attributeVariationList.RemoveAt(i);
                    return;
                }
            }

//			if (attributeVariationList.ContainsKey(caseid))
//			{
//				attributeVariationList.Remove(caseid);
//				return;
//			}
//
//			AttributeVariation currentvariation = null;
//
//			IDictionaryEnumerator enumerator = attributeVariationList.GetEnumerator();
//			while (enumerator.MoveNext())
//			{
//				currentvariation = (AttributeVariation)enumerator.Value;
//
//				if (currentvariation.ID.Contains("_") == false)
//				{
//					currentvariation = null;
//					continue;
//				}
//
//				int index = currentvariation.ID.IndexOf('_');
//				
//				// As the contains("_") already took care of check if _ exists or not 
//				// index should always be >= 0
//				string currentid = currentvariation.ID.Substring(index + 1);
//				if (currentid.CompareTo(caseid) < 0)
//				{
//					currentvariation = null;
//					continue;
//				}
//				else
//				{
//					attributeVariationList.Remove(currentvariation.ID);
//					break;
//				}
//			}
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Random number Generator, used to add Variations with same case ID.
        /// </summary>
        /// <returns></returns>
        private double GenerateRandomNumber()
        {
            if (autorand == null)
            {
                autorand = new Random(4);
            }

            return autorand.NextDouble() * 1000;
        }
        #endregion

    }

}
