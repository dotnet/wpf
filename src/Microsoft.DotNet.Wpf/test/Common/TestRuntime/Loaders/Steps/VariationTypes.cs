// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;

namespace Microsoft.Test.Utilities.VariationEngine
{

    /// <summary>
    /// Variation class inherits from BaseVariation.
    /// Setups up unsupported variation list and then parses variation xml node.
    /// </summary>
    internal class DefaultNodeVariation : BaseVariation
    {
        /// <summary>
        /// Base constructor to setup Unsupported variations list.
        /// </summary>
        public DefaultNodeVariation()
            : base()
        {
            string[] temp = {
				Constants.ScenarioElement, 
				Constants.ScenariosElement
			};

            base.NotSupportedNestedElements = temp;
        }

        /// <summary>
        /// Initializer, check for element name and then call base Initialize method.
        /// </summary>
        /// <param name="varnode"></param>
        internal override void Initialize(XmlNode varnode)
        {
            UtilsLogger.LogDiagnostic = "Variation:Initialize";

            if (varnode == null || varnode.Name != Constants.NodeVariationElement)
            {
                UtilsLogger.LogDiagnostic = "Variation node passed in is null, exiting";
                return;
            }

            base.Initialize(varnode);

            base.UnrecognizedAttributes(varnode);
        }
    }

    /// <summary>
    /// RootNodeVariation definition.
    /// </summary>
    internal class RootNodeVariation : BaseVariation
    {
        string elementvalue = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RootNodeVariation()
            : base()
        {
            string[] temp = {
				Constants.ScenarioElement, 
				Constants.ScenariosElement
			};

            base.NotSupportedNestedElements = temp;
        }

        /// <summary>
        /// Initialize the RootNodeVariation.
        /// </summary>
        /// <param name="varnode"></param>
        internal override void Initialize(XmlNode varnode)
        {
            UtilsLogger.LogDiagnostic = "RootNodeVariation:Initialize";

            if (varnode == null || varnode.Name != Constants.RootNodeVariationElement)
            {
                UtilsLogger.LogDiagnostic = "RootNodeVariation node passed in is null, exiting";
                return;
            }

            if (varnode.ChildNodes.Count > 1)
            {
                throw new ApplicationException("RootNodeVariation node cannot have more than one child.");
            }

            elementvalue = varnode.InnerText;

            base.Initialize(varnode);

            base.UnrecognizedAttributes(varnode);
        }

        /// <summary>
        /// RootNodeVariation new element name.
        /// </summary>
        internal string ElementValue
        {
            get
            {
                return elementvalue;
            }
        }
    }

    /// <summary>
    /// NodeVariation definition.
    /// </summary>
    internal class NodeVariation : DefaultNodeVariation
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public NodeVariation()
            : base()
        {
            string[] temp = {
				Constants.NodeVariationElement, 
				Constants.AttributeVariationElement, 
				Constants.TextVariationElement,
			};

            base.NotSupportedNestedElements = temp;
        }

        /// <summary>
        /// Overrides base Initializer.        
        /// </summary>
        /// <param name="varnode">Xmlnode that is of type NodeVariation</param>
        internal override void Initialize(XmlNode varnode)
        {
            base.Initialize(varnode);

            UtilsLogger.Log = "NodeVariation:Initialize";

            if (String.IsNullOrEmpty(varnode.InnerXml) == false && CommonHelper.IsSpecialValue(varnode.InnerXml))
            {
                string szretvalue = CommonHelper.DeriveSpecialValues(varnode.InnerXml, varnode);
                if (String.IsNullOrEmpty(szretvalue) == false)
                {
                    this.innerxml = szretvalue;
                }
            }

            UtilsLogger.LogDiagnostic = "Calling base initialize class";
        }
    }

    /// <summary>
    /// BaseAttributeVariation class inheriting from BaseVariation class.
    /// The difference between BaseAttributeVariation and Variation class is 
    /// that BaseAttributeVariation cannot have any children and is defined only 
    /// to change certain attribute values of an existing element.
    /// BaseAttributeVariation describes three Properties -
    ///		AttributeName - The name of the attribute to be changed.
    ///		AttributeValue - The value of the attribute specified by AttributeName property.
    ///		Type - 
    /// </summary>
    internal class AttributeVariation : BaseVariation
    {
        string attributename;
        string attributevalue;
        bool removeattribute;

        /// <summary>
        /// Add unsupported types to base class.
        /// </summary>
        public AttributeVariation()
            : base()
        {
            string[] temp = {
				Constants.NodeVariationElement, 
				Constants.AttributeVariationElement, 
				Constants.ScenarioElement, 
				Constants.ScenariosElement,
				Constants.TextVariationElement
			};

            base.NotSupportedNestedElements = temp;
        }

        /// <summary>
        /// Overrides base Initializer.
        /// Abstracts attributename, attributevalue and type information from element.
        /// </summary>
        /// <param name="attribnode">Xmlnode that is of type AttributeVariation</param>
        internal override void Initialize(XmlNode attribnode)
        {
            UtilsLogger.Log = "BaseAttributeVariation:Initialize";

            base.Initialize(attribnode);

            if (attribnode == null || attribnode.Name != Constants.AttributeVariationElement)
            {
                UtilsLogger.Log = "Input param was null or not of type AttributeVariation, exiting.";
                return;
            }

            if (attribnode.Attributes.Count == 0)
            {
                UtilsLogger.Log = "AttributeVariation specified was empty with no attributes, ignoring";
                return;
            }

            if ( attribnode.Attributes[Constants.RemoveAttribute] != null )
            {
                if (attribnode.Attributes[Constants.AttributeNameAttribute] == null)
                {
                    throw new ApplicationException("AttributeName should be specified when Remove is specified");
                }

                this.attributename = attribnode.Attributes[Constants.AttributeNameAttribute].Value;
                if (String.IsNullOrEmpty(this.attributename))
                {
                    throw new ApplicationException("AttributeName cannot be null for a AttributeVariation");
                }

                if (String.IsNullOrEmpty(attribnode.Attributes[Constants.RemoveAttribute].Value))
                {
                    throw new ApplicationException("Remove value cannot be blank");
                }

                removeattribute = Convert.ToBoolean(attribnode.Attributes[Constants.RemoveAttribute].Value);
            }
            else
            {
                if (attribnode.Attributes[Constants.AttributeNameAttribute] != null)
                {
                    string szretvalue = attribnode.Attributes[Constants.AttributeNameAttribute].Value;
                    if (CommonHelper.IsSpecialValue(szretvalue))
                    {
                        szretvalue = CommonHelper.DeriveSpecialValues(szretvalue, attribnode);
                        if (String.IsNullOrEmpty(szretvalue) == false)
                        {
                            this.attributename = szretvalue;
                        }
                    }
                    else
                    {
                        this.attributename = attribnode.Attributes[Constants.AttributeNameAttribute].Value;
                    }

                    szretvalue = null;

                    if (String.IsNullOrEmpty(this.attributename))
                    {
                        this.attributename = attribnode.Attributes[Constants.AttributeNameAttribute].Value;
                    }
                    attribnode.Attributes.Remove(attribnode.Attributes[Constants.AttributeNameAttribute]);
                }

                if (attribnode.Attributes[Constants.AttributeValueAttribute] != null)
                {
                    string szretvalue = attribnode.Attributes[Constants.AttributeValueAttribute].Value;
                    if (CommonHelper.IsSpecialValue(szretvalue))
                    {
                        szretvalue = CommonHelper.DeriveSpecialValues(szretvalue, attribnode);
                        if (String.IsNullOrEmpty(szretvalue) == false)
                        {
                            this.attributevalue = szretvalue;
                        }
                    }
                    else
                    {
                        this.attributevalue = attribnode.Attributes[Constants.AttributeValueAttribute].Value;
                    }

                    szretvalue = null;

                    if (String.IsNullOrEmpty(this.attributevalue))
                    {
                        this.attributename = attribnode.Attributes[Constants.AttributeValueAttribute].Value;
                    }

                    attribnode.Attributes.Remove(attribnode.Attributes[Constants.AttributeValueAttribute]);
                }
            }
            
            UtilsLogger.LogDiagnostic = "Calling base initialize class";

            base.UnrecognizedAttributes(attribnode);

        }

        /// <summary>
        /// Value of the attribute to be modified.
        /// </summary>
        /// <value></value>
        public string AttributeValue
        {
            get
            {
                return attributevalue;
            }
        }

        /// <summary>
        /// Name of attribute to be modified.
        /// </summary>
        /// <value></value>
        public string AttributeName
        {
            get
            {
                return attributename;
            }
        }

        /// <summary>
        /// Flag specified to mark a attribute as removeable.
        /// </summary>
        public bool RemoveAttribute
        {
            get
            {
                return removeattribute;
            }
        }
    }

    internal class TextVariation : BaseVariation
    {
        string innertext;

        public TextVariation()
            : base()
        {
        }

        /// <summary>
        /// Checks if the current TextVariation child is an XmlText type.
        /// </summary>
        /// <param name="varnode"></param>
        internal override void Initialize(XmlNode varnode)
        {
            base.Initialize(varnode);

            if (varnode == null)
            {
                return;
            }

            bool bexit = false;
            XmlNode parentnode = varnode.ParentNode;
            while (parentnode != null && bexit == false)
            {
                switch (parentnode.Name)
                {
                    case Constants.TemplateDataElement:
                        bexit = true;
                        break;

                    case Constants.ScenarioElement:
                        bexit = true;
                        break;

                    case "PropertyGroup":
                        bexit = true;
                        break;

                    default:
                        parentnode = parentnode.ParentNode;
                        break;
                }
            }

            if (parentnode.Name == Constants.ScenarioElement)
            {
                if (varnode.ChildNodes.Count > 1)
                {
                    throw new NotSupportedException("TextVariation should have only 1 child Xml Node " + this.ID);
                }

                // If there are no childnodes that means the Element text needs to be blank.
                if (varnode.ChildNodes.Count > 0)
                {
                    if (varnode.ChildNodes[0].GetType() != typeof(XmlText))
                    {
                        throw new NotSupportedException("TextVariation cannot contain anything thing other than XmlText");
                    }

                    if (String.Compare(varnode.InnerText, varnode.InnerXml) != 0)
                    {
                        throw new NotSupportedException("TextVariation cannot have Xml Content inside them " + this.ID);
                    }
                }

                // Insert autodata info here.
                //innertext = varnode.InnerText;

                if (CommonHelper.IsSpecialValue(varnode.InnerText))
                {
                    string szretvalue = CommonHelper.DeriveSpecialValues(varnode.InnerText, varnode);
                    if (String.IsNullOrEmpty(szretvalue) == false)
                    {
                        innertext = szretvalue;
                    }
                }

                if (String.IsNullOrEmpty(innertext))
                {
                    innertext = varnode.InnerText;
                }
            }
            else if (parentnode.Name == Constants.TemplateDataElement)
            {
                if (varnode.ChildNodes.Count > 1)
                {
                    throw new NotSupportedException("TextVariation should have only 1 child Xml Node " + this.ID);
                }

                innertext = varnode.ChildNodes[0].InnerText;
            }

            base.UnrecognizedAttributes(varnode);

        }

        /// <summary>
        /// The InnerText value to be set on an XmlElement.
        /// </summary>
        /// <value></value>
        public string Text
        {
            get
            {
                return innertext;
            }
        }

    }

    internal class DefaultTextVariation : BaseVariation
    {
        string innertext;

        public DefaultTextVariation()
            : base()
        {
        }

        /// <summary>
        /// Checks if the current TextVariation child is an XmlText type.
        /// </summary>
        /// <param name="varnode"></param>
        internal override void Initialize(XmlNode varnode)
        {
            base.Initialize(varnode);

            if (varnode != null)
            {
                if (varnode.ChildNodes.Count > 1)
                {
                    throw new NotSupportedException("TextVariation should have only 1 child Xml Node " + this.ID);
                }

                innertext = varnode.InnerText;

                base.UnrecognizedAttributes(varnode);
            }
        }

        /// <summary>
        /// The InnerText value to be set on an XmlElement.
        /// </summary>
        /// <value></value>
        public string Text
        {
            get
            {
                return innertext;
            }
        }
    }

}
