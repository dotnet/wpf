// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
// 
// Description: This is a MSBuild task which generates a temporary target assembly
//              if current project contains a xaml file with local-type-reference.
//
//              It generates a temporary project file and then call build-engine 
//              to compile it.
//              
//              The new project file will replace all the Reference Items with the 
//              resolved ReferenctPath, add all the generated code file into Compile 
//              Item list.
//
//---------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;

using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Xml;
using System.Xml.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Configuration;

// Since we disable PreSharp warnings in this file, PreSharp warning is unknown to C# compiler.
// We first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace WpfArcadeSdk.Build.Tasks
{
    /// <summary>
    ///   This task is used to generate a temporary target assembly. It generates
    ///   a temporary project file and then compile it.
    /// 
    ///   The generated project file is based on current project file, with below
    ///   modification:
    /// 
    ///       A:  Add the generated code files (.g.cs) to Compile Item list.
    ///       B:  Replace Reference Item list with ReferenctPath item list.
    ///           So that it doesn't need to rerun time-consuming task 
    ///           ResolveAssemblyReference (RAR) again.
    /// 
    /// </summary>
    public sealed class GenerateReferenceAssemblyProjectTask : Task
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor 
        /// </summary>
        public GenerateReferenceAssemblyProjectTask() : base()
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// ITask Execute method
        /// </summary>
        /// <returns></returns>
        /// <remarks>Catching all exceptions in this method is appropriate - it will allow the build process to resume if possible after logging errors</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public override bool Execute()
        {
            bool retValue = true;

            // Verification
            try
            {
                XmlDocument xmlProjectDoc = null;

                xmlProjectDoc = new XmlDocument();
                xmlProjectDoc.Load(CurrentProjectFile);

                //
                // remove all the WinFX specific item lists
                // ApplicationDefinition, Page, MarkupResource and Resource
                //
                RemoveItemsByName(xmlProjectDoc, AppDefName);
                RemoveItemsByName(xmlProjectDoc, PageName);
                RemoveItemsByName(xmlProjectDoc, MarkupResourceName);
                RemoveItemsByName(xmlProjectDoc, ResourceName);
                RemoveItemsByName(xmlProjectDoc, CompileItemName);
                RemoveItemsByName(xmlProjectDoc, EmbeddedResourceName);

                RemovePropertiesByName(xmlProjectDoc, EnableAnalyzersProperty);
                RemovePropertiesByName(xmlProjectDoc, ModuleInitializerTag);
                RemovePropertiesByName(xmlProjectDoc, CompileDependsOnProperty);

                RemoveTagByName(xmlProjectDoc, TargetName);

                RemovePropertiesByName(xmlProjectDoc, EnableDefaultCompileItemsTag);

                // Save the xmlDocument content into the temporary project file.
                xmlProjectDoc.Save(ReferenceAssemblyProjectFile);
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                retValue = false;
            }

            return retValue;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The full path of current project file to base the reference project off of.
        /// </summary>
        [Required]
        public string CurrentProjectFile { get; set; }

        /// <summary>
        /// The full path to the output reference assembly project file.
        /// </summary>
        [Required]
        public string ReferenceAssemblyProjectFile { get; set; }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void RemoveItemsByName(XmlDocument xmlProjectDoc, string itemName)
        {
            RemoveTagByName(xmlProjectDoc, ItemGroupName, itemName);
        }

        private void RemovePropertiesByName(XmlDocument xmlProjectDoc, string propName)
        {
            RemoveTagByName(xmlProjectDoc, PropertyGroupName, propName);
        }

        private void RemoveTagByName(XmlDocument xmlProjectDoc, string tagName)
        {
            if (xmlProjectDoc == null || String.IsNullOrEmpty(tagName))
            {
                // When the parameters are not valid, simply return it, instead of throwing exceptions.
                return;
            }

            XmlNode root = xmlProjectDoc.DocumentElement;

            if (root.HasChildNodes == false)
            {
                // If there is no child element in this project file, just return immediatelly.
                return;
            }

            ArrayList itemToRemove = new ArrayList();

            for (int i = 0; i < root.ChildNodes.Count; i++)
            {
                XmlElement nodeGroup = root.ChildNodes[i] as XmlElement;

                if (nodeGroup != null && String.Compare(nodeGroup.Name, tagName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    itemToRemove.Add(nodeGroup);
                }
            }

            //
            // Now it is the right time to delete the elements.
            //
            if (itemToRemove.Count > 0)
            {
                foreach (object node in itemToRemove)
                {
                    XmlElement item = node as XmlElement;

                    //
                    // Remove this item from its parent node.
                    // the parent node should be nodeGroup.
                    //
                    if (item != null)
                    {
                        root.RemoveChild(item);
                    }
                }
            }
        }

        private void RemoveTagByName(XmlDocument xmlProjectDoc, string parentTagName, string tagName)
        {
            if (xmlProjectDoc == null || String.IsNullOrEmpty(parentTagName) || String.IsNullOrEmpty(tagName))
            {
                // When the parameters are not valid, simply return it, instead of throwing exceptions.
                return;
            }

            //
            // The project file format is always like below:
            //
            //  <Project  xmlns="...">
            //     <ProjectGroup>
            //         ......
            //     </ProjectGroup>
            //
            //     ...
            //     <ItemGroup>
            //         <ItemNameHere ..../>
            //         ....
            //     </ItemGroup>
            //
            //     ....
            //     <Import ... />
            //     ...
            //     <Target Name="xxx" ..../>
            //     
            //      ...
            //
            //  </Project>
            //
            //
            // The order of children nodes under Project Root element is random
            //

            XmlNode root = xmlProjectDoc.DocumentElement;

            if (root.HasChildNodes == false)
            {
                // If there is no child element in this project file, just return immediatelly.
                return;
            }

            for (int i = 0; i < root.ChildNodes.Count; i++)
            {
                XmlElement nodeGroup = root.ChildNodes[i] as XmlElement;

                if (nodeGroup != null && String.Compare(nodeGroup.Name, parentTagName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    //
                    // This is ItemGroup element.
                    //
                    if (nodeGroup.HasChildNodes)
                    {
                        ArrayList itemToRemove = new ArrayList();

                        for (int j = 0; j < nodeGroup.ChildNodes.Count; j++)
                        {
                            XmlElement nodeItem = nodeGroup.ChildNodes[j] as XmlElement;

                            if (nodeItem != null && String.Compare(nodeItem.Name, tagName, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                // This is the item that need to remove.
                                // Add it into the temporary array list.
                                // Don't delete it here, since it would affect the ChildNodes of parent element.
                                //
                                itemToRemove.Add(nodeItem);
                            }
                        }

                        //
                        // Now it is the right time to delete the elements.
                        //
                        if (itemToRemove.Count > 0)
                        {
                            foreach (object node in itemToRemove)
                            {
                                XmlElement item = node as XmlElement;

                                //
                                // Remove this item from its parent node.
                                // the parent node should be nodeGroup.
                                //
                                if (item != null)
                                {
                                    nodeGroup.RemoveChild(item);
                                }
                            }
                        }
                    }

                    //
                    // Removed all the items with given name from this item group.
                    //
                    // Continue the loop for the next ItemGroup.
                }

            }   // end of "for i" statement.

        }

        private void AddNewItems(XmlDocument xmlProjectDoc, string sItemName, ITaskItem[] pItemList)
        {
            if (xmlProjectDoc == null || String.IsNullOrEmpty(sItemName) || pItemList == null)
            {
                // When the parameters are not valid, simply return it, instead of throwing exceptions.
                return;
            }

            XmlNode root = xmlProjectDoc.DocumentElement;

            // Create a new ItemGroup element
            XmlNode nodeItemGroup = xmlProjectDoc.CreateElement(ItemGroupName, root.NamespaceURI);

            // Append this new ItemGroup item into the list of children of the document root.
            root.AppendChild(nodeItemGroup);

            XmlElement embedItem = null;

            for (int i = 0; i < pItemList.Length; i++)
            {
                // Create an element for the given sItemName
                XmlElement nodeItem = xmlProjectDoc.CreateElement(sItemName, root.NamespaceURI);

                // Create an Attribute "Include"
                XmlAttribute attrInclude = xmlProjectDoc.CreateAttribute(IncludeAttrName);

                ITaskItem pItem = pItemList[i];

                // Set the value for Include attribute.
                attrInclude.Value = pItem.ItemSpec;

                // Add the attribute to current item node.
                nodeItem.SetAttributeNode(attrInclude);

                if (True == pItem.GetMetadata(EmbedInteropTypes))
                {
                    embedItem = xmlProjectDoc.CreateElement(EmbedInteropTypes, root.NamespaceURI);
                    embedItem.InnerText = True;
                    nodeItem.AppendChild(embedItem);
                }

                string aliases = pItem.GetMetadata(Aliases);
                if (!String.IsNullOrEmpty(aliases))
                {
                    embedItem = xmlProjectDoc.CreateElement(Aliases, root.NamespaceURI);
                    embedItem.InnerText = aliases;
                    nodeItem.AppendChild(embedItem);
                }

                // Add current item node into the children list of ItemGroup
                nodeItemGroup.AppendChild(nodeItem);
            }
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private const string Aliases = "Aliases";
        private const string ReferenceTypeName = "Reference";
        private const string EmbedInteropTypes = "EmbedInteropTypes";
        private const string AppDefName = "ApplicationDefinition";
        private const string PageName = "Page";
        private const string MarkupResourceName = "MarkupResource";
        private const string ResourceName = "Resource";
        private const string CompileItemName = "Compile";
        private const string EnableAnalyzersProperty = "EnableAnalyzers";
        private const string CompileDependsOnProperty = "CompileDependsOn";
        private const string PropertyGroupName = "PropertyGroup";
        private const string ItemGroupName = "ItemGroup";
        private const string IncludeAttrName = "Include";
        private const string TargetName = "Target";
        private const string EmbeddedResourceName = "EmbeddedResource";
        private const string ModuleInitializerTag = "InjectModuleInitializer";
        private const string True = "True";
        private const string EnableDefaultCompileItemsTag = "EnableDefaultCompileItems";

        #endregion Private Fields

    }
}

