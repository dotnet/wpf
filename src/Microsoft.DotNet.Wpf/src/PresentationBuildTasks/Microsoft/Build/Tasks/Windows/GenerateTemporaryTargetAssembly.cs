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
//              resolved ReferencePath, add all the generated code file into Compile 
//              Item list.
//
//---------------------------------------------------------------------------

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
    #region GenerateTemporaryTargetAssembly Task class

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
    public sealed class GenerateTemporaryTargetAssembly : Task
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
        public GenerateTemporaryTargetAssembly()
            : base(SR.SharedResourceManager)
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
            if (string.Compare(IncludePackageReferencesDuringMarkupCompilation, "false", StringComparison.OrdinalIgnoreCase) != 0)
            {
                return ExecuteGenerateTemporaryTargetAssemblyWithPackageReferenceSupport();
            }
            else
            {
                return ExecuteLegacyGenerateTemporaryTargetAssembly();
            }
        }

        /// <summary>
        /// ExecuteLegacyGenerateTemporaryTargetAssembly 
        ///
        /// Creates a project file based on the parent project and compiles a temporary assembly. 
        ///
        /// Passes IntermediateOutputPath, AssemblyName, and TemporaryTargetAssemblyName as global properties.
        ///
        /// </summary>
        /// <returns></returns>
        /// <remarks>Catching all exceptions in this method is appropriate - it will allow the build process to resume if possible after logging errors</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool ExecuteLegacyGenerateTemporaryTargetAssembly()
        {
            bool retValue = true;

            // Verification
            try
            {
                XmlDocument xmlProjectDoc = null;

                xmlProjectDoc = new XmlDocument( );
                xmlProjectDoc.Load(CurrentProject);

                //
                // remove all the WinFX specific item lists
                // ApplicationDefinition, Page, MarkupResource and Resource
                //

                RemoveItemsByName(xmlProjectDoc, APPDEFNAME);
                RemoveItemsByName(xmlProjectDoc, PAGENAME);
                RemoveItemsByName(xmlProjectDoc, MARKUPRESOURCENAME);
                RemoveItemsByName(xmlProjectDoc, RESOURCENAME);

                // Replace the Reference Item list with ReferencePath

                RemoveItemsByName(xmlProjectDoc, REFERENCETYPENAME);
                AddNewItems(xmlProjectDoc, ReferencePathTypeName, ReferencePath);

                // Add GeneratedCodeFiles to Compile item list.
                AddNewItems(xmlProjectDoc, CompileTypeName, GeneratedCodeFiles);

                string currentProjectName = Path.GetFileNameWithoutExtension(CurrentProject);
                string currentProjectExtension = Path.GetExtension(CurrentProject);

                // Create a random file name
                // This can fix the problem of project cache in VS.NET environment.
                //
                // GetRandomFileName( ) could return any possible file name and extension
                // Since this temporary file will be used to represent an MSBUILD project file, 
                // we will use the same extension as that of the current project file
                //
                string randomFileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

                // Don't call Path.ChangeExtension to append currentProjectExtension. It will do 
                // odd things with project names that already contains a period (like System.Windows.
                // Contols.Ribbon.csproj). Instead, just append the extension - after all, we already know
                // for a fact that this name (i.e., tempProj) lacks a file extension.
                string tempProjPrefix = string.Join("_", currentProjectName, randomFileName, WPFTMP);
                string tempProj = tempProjPrefix  + currentProjectExtension;


                // Save the xmlDocument content into the temporary project file.
                xmlProjectDoc.Save(tempProj);

                //
                // Invoke MSBUILD engine to build this temporary project file.
                //

                Hashtable globalProperties = new Hashtable(3);

                // Add AssemblyName, IntermediateOutputPath and _TargetAssemblyProjectName to the global property list
                // Note that _TargetAssemblyProjectName is not defined as a property with Output attribute - that doesn't do us much 
                // good here. We need _TargetAssemblyProjectName to be a well-known property in the new (temporary) project
                // file, and having it be available in the current MSBUILD process is not useful.
                globalProperties[intermediateOutputPathPropertyName] = IntermediateOutputPath;

                globalProperties[assemblyNamePropertyName] = AssemblyName;
                globalProperties[targetAssemblyProjectNamePropertyName] = currentProjectName;

                retValue = BuildEngine.BuildProjectFile(tempProj, new string[] { CompileTargetName }, globalProperties, null);

                // Delete the temporary project file and generated files unless diagnostic mode has been requested
                if (!GenerateTemporaryTargetAssemblyDebuggingInformation)
                {
                    try
                    {
                        File.Delete(tempProj);

                        DirectoryInfo intermediateOutputPath = new DirectoryInfo(IntermediateOutputPath);
                        foreach (FileInfo temporaryProjectFile in intermediateOutputPath.EnumerateFiles(string.Concat(tempProjPrefix, "*")))
                        {
                            temporaryProjectFile.Delete();
                        }
                    }
                    catch (IOException e)
                    {
                        // Failure to delete the file is a non fatal error
                        Log.LogWarningFromException(e);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                retValue = false;
            }

            return retValue;
        }

        /// <summary>
        /// ExecuteGenerateTemporaryTargetAssemblyWithPackageReferenceSupport 
        ///
        /// Creates a project file based on the parent project and compiles a temporary assembly. 
        ///
        /// Receives the temporary project name as a parameter and writes properties in to the project file itself.
        ///
        /// No global properties are set.  
        ///
        /// </summary>
        /// <returns></returns>
        /// <remarks>Catching all exceptions in this method is appropriate - it will allow the build process to resume if possible after logging errors</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool ExecuteGenerateTemporaryTargetAssemblyWithPackageReferenceSupport()
        {
            bool retValue = true;

            //
            // Create the temporary target assembly project
            // 
            try
            {
                XmlDocument xmlProjectDoc = null;

                xmlProjectDoc = new XmlDocument( );
                xmlProjectDoc.Load(CurrentProject);

                // remove all the WinFX specific item lists
                // ApplicationDefinition, Page, MarkupResource and Resource
                RemoveItemsByName(xmlProjectDoc, APPDEFNAME);
                RemoveItemsByName(xmlProjectDoc, PAGENAME);
                RemoveItemsByName(xmlProjectDoc, MARKUPRESOURCENAME);
                RemoveItemsByName(xmlProjectDoc, RESOURCENAME);

                // Replace the Reference Item list with ReferencePath
                RemoveItemsByName(xmlProjectDoc, REFERENCETYPENAME);
                AddNewItems(xmlProjectDoc, ReferencePathTypeName, ReferencePath);

                // Add GeneratedCodeFiles to Compile item list.
                AddNewItems(xmlProjectDoc, CompileTypeName, GeneratedCodeFiles);

                // Replace implicit SDK imports with explicit SDK imports
                ReplaceImplicitImports(xmlProjectDoc); 

                // Add properties required for temporary assembly compilation
                var properties = new List<(string PropertyName, string PropertyValue)> 
                {
                    ( nameof(AssemblyName), AssemblyName ),
                    ( nameof(IntermediateOutputPath), IntermediateOutputPath ),
                    ( nameof(BaseIntermediateOutputPath), BaseIntermediateOutputPath ),
                    ( "_TargetAssemblyProjectName", Path.GetFileNameWithoutExtension(CurrentProject)),
                    ( nameof(Analyzers), Analyzers )
                };

                AddNewProperties(xmlProjectDoc, properties);

                // Save the xmlDocument content into the temporary project file.
                xmlProjectDoc.Save(TemporaryTargetAssemblyProjectName);

                // Disable conflicting Arcade SDK workaround that imports NuGet props/targets
                Hashtable globalProperties = new Hashtable(1);
                globalProperties["_WpfTempProjectNuGetFilePathNoExt"] = "";

                //
                //  Compile the temporary target assembly project
                //
                retValue = BuildEngine.BuildProjectFile(TemporaryTargetAssemblyProjectName, new string[] { CompileTargetName }, globalProperties, null);

                // Delete the temporary project file and generated files unless diagnostic mode has been requested
                if (!GenerateTemporaryTargetAssemblyDebuggingInformation)
                {
                    try
                    {
                        File.Delete(TemporaryTargetAssemblyProjectName);

                        DirectoryInfo intermediateOutputPath = new DirectoryInfo(IntermediateOutputPath);
                        foreach (FileInfo temporaryProjectFile in intermediateOutputPath.EnumerateFiles(string.Concat(Path.GetFileNameWithoutExtension(TemporaryTargetAssemblyProjectName), "*")))
                        {
                            temporaryProjectFile.Delete();
                        }
                    }
                    catch (IOException e)
                    {
                        // Failure to delete the file is a non fatal error
                        Log.LogWarningFromException(e);
                    }
                }

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
        /// CurrentProject 
        ///    The full path of current project file.
        /// </summary>
        [Required]
        public string  CurrentProject
        {
            get { return _currentProject; }
            set { _currentProject = value; }
        }

        /// <summary>
        /// MSBuild Binary Path.
        ///   This is required for Project to work correctly.
        /// </summary>
        [Required]
        public string MSBuildBinPath
        {
            get { return _msbuildBinPath; }
            set { _msbuildBinPath = value; }
        }
        
        /// <summary>
        /// GeneratedCodeFiles
        ///    A list of generated code files, it could be empty.
        ///    The list will be added to the Compile item list in new generated project file.
        /// </summary>
        public ITaskItem[] GeneratedCodeFiles
        {
            get { return _generatedCodeFiles; }
            set { _generatedCodeFiles = value; }
        }

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
        {
            get { return _compileTypeName; }
            set { _compileTypeName = value; }
        }


        /// <summary>
        /// ReferencePath
        ///    A list of resolved reference assemblies.
        ///    The list will replace the original Reference item list in generated project file.
        /// </summary>
        public ITaskItem[] ReferencePath
        {
            get { return _referencePath; }
            set { _referencePath = value; }
        }

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
        {
            get { return _referencePathTypeName; }
            set { _referencePathTypeName = value; }
        }


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
        {
            get { return _intermediateOutputPath; }
            set { _intermediateOutputPath = value; }
        }

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
        {
            get { return _assemblyName; }
            set { _assemblyName = value; }
        }

        /// <summary>
        /// CompileTargetName
        /// 
        /// The msbuild target name which is used to generate assembly from source code files.
        /// Usually it is "CoreCompile"
        /// 
        /// </summary>
        [Required]
        public string CompileTargetName
        {
            get { return _compileTargetName; }
            set { _compileTargetName = value; }
        }

        /// <summary>
        /// Optional <see cref="Boolean"/> task parameter
        /// 
        /// When <code>true</code>, debugging information is enabled for the <see cref="GenerateTemporaryTargetAssembly"/>
        /// Task. At this time, the only debugging information that is generated consists of the temporary project that is 
        /// created to generate the temporary target assembly. This temporary project is normally deleted at the end of this
        /// MSBUILD task; when <see cref="GenerateTemporaryTargetAssemblyDebuggingInformation"/> is enable, this temporary project 
        /// will be retained for inspection by the developer. 
        ///
        /// This is a diagnostic parameter, and it defaults to <code>false</code>.
        /// </summary>
        public bool GenerateTemporaryTargetAssemblyDebuggingInformation 
        { 
            get { return _generateTemporaryTargetAssemblyDebuggingInformation; }
            set { _generateTemporaryTargetAssemblyDebuggingInformation = value; } 
        }

        /// <summary>
        /// Analyzers 
        /// 
        /// Required for Source Generator support. May be null.
        /// 
        /// </summary>
        public string Analyzers 
        { get; set; }

        /// <summary>
        /// BaseIntermediateOutputPath
        /// 
        /// Required for Source Generator support. May be null.
        /// 
        /// </summary>
        public string BaseIntermediateOutputPath
        {
            get; set;
        }

        /// <summary>
        /// IncludePackageReferencesDuringMarkupCompilation 
        /// 
        /// Required for Source Generator support. May be null.
        ///
        /// Set this property to 'false' to use the .NET Core 3.0 behavior for this task. 
        ///
        /// </summary>
        public string IncludePackageReferencesDuringMarkupCompilation 
        { get; set; }

        /// <summary>
        /// TemporaryTargetAssemblyProjectName 
        ///
        /// Required for PackageReference support.
        ///
        /// This property may be null if 'IncludePackageReferencesDuringMarkupCompilation' is 'false'.
        ///
        /// The file name with extension of the randomly generated project name for the temporary assembly
        ///
        /// </summary>
        public string TemporaryTargetAssemblyProjectName 
        { get; set; }


        #endregion Public Properties
  
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

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
                        ArrayList itemToRemove = new ArrayList();

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

            // Create a new PropertyGroup element
            XmlNode nodeItemGroup = xmlProjectDoc.CreateElement("PropertyGroup", root.NamespaceURI);
            root.PrependChild(nodeItemGroup);

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

        //
        // Replace implicit SDK imports with explicit imports 
        //
        static private void ReplaceImplicitImports(XmlDocument xmlProjectDoc)
        {
            if (xmlProjectDoc == null)
            {
                // When the parameters are not valid, simply return it, instead of throwing exceptions.
                return;
            }

            XmlNode root = xmlProjectDoc.DocumentElement;
          
            for (int i = 0; i < root.Attributes.Count; i++)
            {
                XmlAttribute xmlAttribute = root.Attributes[i] as XmlAttribute;

                if (xmlAttribute.Name.Equals("Sdk", StringComparison.OrdinalIgnoreCase))
                {
                    // <Project Sdk="Microsoft.NET.Sdk">

                    // Remove Sdk attribute
                    var sdkValue = xmlAttribute.Value;
                    root.Attributes.Remove(xmlAttribute);

                    //
                    // Add explicit top import
                    //
                    //  <Import Project = "Sdk.props" Sdk="Microsoft.NET.Sdk" />
                    //
                    XmlNode nodeImportProps = xmlProjectDoc.CreateElement("Import", root.NamespaceURI);
                    XmlAttribute projectAttribute = xmlProjectDoc.CreateAttribute("Project", root.NamespaceURI);
                    projectAttribute.Value = "Sdk.props";
                    nodeImportProps.Attributes.Append(projectAttribute);
                    nodeImportProps.Attributes.Append(xmlAttribute);

                    // Prepend this Import to the root of the XML document
                    root.PrependChild(nodeImportProps);

                    //
                    // Add explicit bottom import
                    //
                    //  <Import Project = "Sdk.targets" Sdk="Microsoft.NET.Sdk" 
                    //                
                    XmlNode nodeImportTargets = xmlProjectDoc.CreateElement("Import", root.NamespaceURI);
                    XmlAttribute projectAttribute2 = xmlProjectDoc.CreateAttribute("Project", root.NamespaceURI);
                    projectAttribute2.Value = "Sdk.targets";
                    XmlAttribute projectAttribute3 = xmlProjectDoc.CreateAttribute("Sdk", root.NamespaceURI);
                    projectAttribute3.Value = sdkValue;
                    nodeImportTargets.Attributes.Append(projectAttribute2);
                    nodeImportTargets.Attributes.Append(projectAttribute3);

                    // Append this Import to the end of the XML document
                    root.AppendChild(nodeImportTargets);
                }
            }
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private string _currentProject = String.Empty;

        private ITaskItem[] _generatedCodeFiles;
        private ITaskItem[] _referencePath;

        private string _referencePathTypeName;
        private string _compileTypeName;

        private string _msbuildBinPath;

        private string  _intermediateOutputPath;
        private string  _assemblyName;
        private string  _compileTargetName;
        private bool _generateTemporaryTargetAssemblyDebuggingInformation = false;

        private const string intermediateOutputPathPropertyName = "IntermediateOutputPath";
        private const string assemblyNamePropertyName = "AssemblyName";
        private const string targetAssemblyProjectNamePropertyName = "_TargetAssemblyProjectName";

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

        #endregion Private Fields

    }
    
    #endregion GenerateProjectForLocalTypeReference Task class
}

