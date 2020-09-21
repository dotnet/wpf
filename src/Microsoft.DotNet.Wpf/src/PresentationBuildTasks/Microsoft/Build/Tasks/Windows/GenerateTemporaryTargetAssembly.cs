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
            bool retValue = true;

            // Verification
            try
            {
                // Add AssemblyName, IntermediateOutputPath and _TargetAssemblyProjectName to the global property list
                // Note that _TargetAssemblyProjectName is not defined as a property with Output attribute - that doesn't do us much 
                // good here. We need _TargetAssemblyProjectName to be a well-known property in the new (temporary) project
                // file, and having it be available in the current MSBUILD process is not useful.
                Hashtable globalProperties = new Hashtable(3);

                globalProperties[nameof(IntermediateOutputPath)] = IntermediateOutputPath;
                globalProperties[nameof(AssemblyName)] = AssemblyName;
                globalProperties[targetAssemblyProjectNamePropertyName] = Path.GetFileNameWithoutExtension(CurrentProject);

                // Compile the project
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
        /// TemporaryTargetAssemblyProjectName 
        ///    The name of the randomly generated project name for the temporary assembly 
        /// </summary>
        [Required]
        public string TemporaryTargetAssemblyProjectName 
        { get; set; }

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

        #endregion Public Properties
  
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

        private const string targetAssemblyProjectNamePropertyName = "_TargetAssemblyProjectName";

        #endregion Private Fields

    }
    
    #endregion GenerateTemporaryTargetAssembly Task class
}
