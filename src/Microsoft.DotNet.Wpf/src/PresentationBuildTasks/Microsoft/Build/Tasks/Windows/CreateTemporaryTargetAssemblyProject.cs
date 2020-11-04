// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Xml;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MS.Utility;
using MS.Internal.Tasks;

// Since we disable PreSharp warnings in this file, PreSharp warning is unknown to C# compiler.
// We first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace Microsoft.Build.Tasks.Windows
{
    /// <summary>
    ///   This task is used to generate a temporary target assembly project. 
    /// 
    ///   The generated project file is based on current project file, with below
    ///   modification:
    /// 
    ///       A:  Add the generated code files (.g.cs) to Compile Item list.
    ///       B:  Replace Reference Item list with ReferencePath item list.
    ///           So that it doesn't need to rerun time-consuming task 
    ///           ResolveAssemblyReference (RAR) again.
    /// 
    /// </summary>
    public sealed class CreateTemporaryTargetAssemblyProject : Task
    {
        /// <summary>
        /// Constructor 
        /// </summary>
        public CreateTemporaryTargetAssemblyProject()
            : base(SR.SharedResourceManager)
        {
        }   

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
                // Create a random file name
                // This can fix the problem of project cache in VS.NET environment.
                //
                // GetRandomFileName( ) could return any possible file name and extension
                // Since this temporary file will be used to represent an MSBUILD project file, 
                // we will use the same extension as that of the current project file
                //
                string currentProjectName = Path.GetFileNameWithoutExtension(CurrentProject);
                string currentProjectExtension = Path.GetExtension(CurrentProject);

                string randomFileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                string tempProjPrefix = string.Join("_", currentProjectName, randomFileName, WPFTMP);
                TemporaryTargetAssemblyProjectName = tempProjPrefix  + currentProjectExtension;

                XmlDocument xmlProjectDoc = null;

                xmlProjectDoc = new XmlDocument( );
                xmlProjectDoc.Load(CurrentProject);

                // remove all the WinFX specific item lists
                // ApplicationDefinition, Page, MarkupResource and Resource
                RemoveItemsByName(xmlProjectDoc, APPDEFNAME);
                RemoveItemsByName(xmlProjectDoc, PAGENAME);
                RemoveItemsByName(xmlProjectDoc, MARKUPRESOURCENAME);
                RemoveItemsByName(xmlProjectDoc, RESOURCENAME);

                // Add properties required for temporary assembly compilation
                var properties = new List<(string PropertyName, string PropertyValue)> 
                {
                    ( nameof(AssemblyName), AssemblyName ),
                    ( nameof(IntermediateOutputPath), IntermediateOutputPath ),
                    ( "AppendTargetFrameworkToOutputPath", "false"),
                    ( "_TargetAssemblyProjectName", Path.GetFileNameWithoutExtension(CurrentProject)),
                    ( nameof(MSBuildProjectExtensionsPath), MSBuildProjectExtensionsPath),
                    ( nameof(Analyzers), Analyzers)
                };

                AddNewProperties(xmlProjectDoc, properties);

                // Replace the Reference Item list with ReferencePath
                RemoveItemsByName(xmlProjectDoc, REFERENCETYPENAME);
                AddNewItems(xmlProjectDoc, ReferencePathTypeName, ReferencePath);

                // Add GeneratedCodeFiles to Compile item list.
                AddNewItems(xmlProjectDoc, CompileTypeName, GeneratedCodeFiles);

                // Save the xmlDocument content into the temporary project file.
                xmlProjectDoc.Save(TemporaryTargetAssemblyProjectName);
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                retValue = false;
            }

            return retValue;
        }

        /// <summary>
        /// CurrentProject 
        ///    The full path of current project file.
        /// </summary>
        [Required]
        public string  CurrentProject
        { get; set; }

        /// <summary>
        /// GeneratedCodeFiles
        ///    A list of generated code files, it could be empty.
        ///    The list will be added to the Compile item list in new generated project file.
        /// </summary>
        public ITaskItem[] GeneratedCodeFiles
        { get; set; }

        /// <summary>
        /// CompileTypeName
        ///   The appropriate item name which can be accepted by managed compiler task.
        ///   It is "Compile" for now.
        ///   
        ///   Adding this property is to make the type name configurable, if it is changed, 
        ///   No code is required to change in this task, but set a new type name in project file.
        /// </summary>
        [Required]
        public string CompileTypeName
        { get; set; }

        /// <summary>
        /// ReferencePath
        ///    A list of resolved reference assemblies.
        ///    The list will replace the original Reference item list in generated project file.
        /// </summary>
        public ITaskItem[] ReferencePath
        { get; set; }

        /// <summary>
        /// ReferencePathTypeName
        ///   The appropriate item name which is used to keep the Reference list in managed compiler task.
        ///   It is "ReferencePath" for now.
        ///   
        ///   Adding this property is to make the type name configurable, if it is changed, 
        ///   No code is required to change in this task, but set a new type name in project file.
        /// </summary>
        [Required]
        public string ReferencePathTypeName
        { get; set; }

        /// <summary>
        /// IntermediateOutputPath
        /// 
        /// The value which is set to IntermediateOutputPath property in current project file.
        /// 
        /// Passing this value explicitly is to make sure to generate temporary target assembly 
        /// in expected directory.  
        /// </summary>
        [Required]
        public string IntermediateOutputPath
        { get; set; }

        /// <summary>
        /// AssemblyName
        /// 
        /// The value which is set to AssemblyName property in current project file.
        /// Passing this value explicitly is to make sure to generate the expected 
        /// temporary target assembly.
        /// 
        /// </summary>
        [Required]
        public string AssemblyName
        { get; set; }

        /// <summary>
        /// MSBuildProjectExtensionsPath 
        /// 
        /// Required for NuGet PackageReferences (*.nuget.g.props/targets, project.assets, etc.)
        /// 
        /// </summary>
        [Required]
        public string MSBuildProjectExtensionsPath
        { get; set; }

        /// <summary>
        /// Analyzers 
        /// 
        /// Required for Source Generator support. May be null.
        /// 
        /// </summary>
        public string Analyzers 
        { get; set; }

        [Output]
        public string TemporaryTargetAssemblyProjectName 
        { get; set; }

        //
        // Remove specific items from project file. Every item should be under an ItemGroup.
        //
        private void RemoveItemsByName(XmlDocument xmlProjectDoc, string sItemName)
        {
            if (xmlProjectDoc == null || String.IsNullOrEmpty(sItemName))
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

                if (nodeGroup != null && String.Compare(nodeGroup.Name, ITEMGROUP_NAME, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    //
                    // This is ItemGroup element.
                    //
                    if (nodeGroup.HasChildNodes)
                    {
                        List<XmlElement> itemToRemove = new List<XmlElement>();

                        for (int j = 0; j < nodeGroup.ChildNodes.Count; j++)
                        {
                            XmlElement nodeItem = nodeGroup.ChildNodes[j] as XmlElement;

                            if (nodeItem != null && String.Compare(nodeItem.Name, sItemName, StringComparison.OrdinalIgnoreCase) == 0)
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
                            foreach (XmlElement item in itemToRemove)
                            {
                                // Remove this item from its parent node.
                                // the parent node should be nodeGroup.
                                nodeGroup.RemoveChild(item);
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

        //
        // Add a list of files into an Item in the project file, the ItemName is specified by sItemName.
        //
        private void AddNewItems(XmlDocument xmlProjectDoc, string sItemName, ITaskItem[] pItemList)
        {
            if (xmlProjectDoc == null || String.IsNullOrEmpty(sItemName) || pItemList == null)
            {
                // When the parameters are not valid, simply return it, instead of throwing exceptions.
                return;
            }

            XmlNode root = xmlProjectDoc.DocumentElement;

            // Create a new ItemGroup element
            XmlNode nodeItemGroup = xmlProjectDoc.CreateElement(ITEMGROUP_NAME, root.NamespaceURI);

            // Append this new ItemGroup item into the list of children of the document root.
            root.AppendChild(nodeItemGroup);

            XmlElement embedItem = null;

            for (int i = 0; i < pItemList.Length; i++)
            {
                // Create an element for the given sItemName
                XmlElement nodeItem = xmlProjectDoc.CreateElement(sItemName, root.NamespaceURI);

                // Create an Attribute "Include"
                XmlAttribute attrInclude = xmlProjectDoc.CreateAttribute(INCLUDE_ATTR_NAME);

                ITaskItem pItem = pItemList[i];

                // Set the value for Include attribute.
                attrInclude.Value = pItem.ItemSpec;

                // Add the attribute to current item node.
                nodeItem.SetAttributeNode(attrInclude);

                if (TRUE == pItem.GetMetadata(EMBEDINTEROPTYPES))
                {
                    embedItem = xmlProjectDoc.CreateElement(EMBEDINTEROPTYPES, root.NamespaceURI);
                    embedItem.InnerText = TRUE;
                    nodeItem.AppendChild(embedItem);
                }

                string aliases = pItem.GetMetadata(ALIASES);
                if (!String.IsNullOrEmpty(aliases))
                {
                    embedItem = xmlProjectDoc.CreateElement(ALIASES, root.NamespaceURI);
                    embedItem.InnerText = aliases;
                    nodeItem.AppendChild(embedItem);
                }

                // Add current item node into the children list of ItemGroup
                nodeItemGroup.AppendChild(nodeItem);
            }
        }

        private void AddNewProperties(XmlDocument xmlProjectDoc, List<(string PropertyName, string PropertyValue)> properties )
        {
            if (xmlProjectDoc == null || properties == null )
            {
                // When the parameters are not valid, simply return it, instead of throwing exceptions.
                return;
            }

            XmlNode root = xmlProjectDoc.DocumentElement;

            // Create a new ItemGroup element
            XmlNode nodeItemGroup = xmlProjectDoc.CreateElement("PropertyGroup", root.NamespaceURI);
            root.InsertAfter(nodeItemGroup, root.FirstChild);

            // Append this new ItemGroup item into the list of children of the document root.
            foreach(var property in properties)
            {
                // Skip empty properties
                if (!string.IsNullOrEmpty(property.PropertyValue))
                {
                    // Create an element for the given propertyName
                    XmlElement nodeItem = xmlProjectDoc.CreateElement(property.PropertyName, root.NamespaceURI);
                    nodeItem.InnerText = property.PropertyValue;

                    // Add current item node into the PropertyGroup 
                    nodeItemGroup.AppendChild(nodeItem);
                }
            }

        }

        private const string ALIASES = "Aliases";
        private const string REFERENCETYPENAME = "Reference";
        private const string EMBEDINTEROPTYPES = "EmbedInteropTypes";
        private const string APPDEFNAME = "ApplicationDefinition";
        private const string PAGENAME = "Page";
        private const string MARKUPRESOURCENAME = "MarkupResource";
        private const string RESOURCENAME = "Resource";

        private const string ITEMGROUP_NAME = "ItemGroup";
        private const string INCLUDE_ATTR_NAME = "Include";

        private const string TRUE = "True";
        private const string WPFTMP = "wpftmp";
    }
}
