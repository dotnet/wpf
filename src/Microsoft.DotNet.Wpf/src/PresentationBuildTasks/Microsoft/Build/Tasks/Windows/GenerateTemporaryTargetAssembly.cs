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
        /// CurrentProject 
        ///    The full path of current project file.
        /// </summary>
        [Required]
        public string  CurrentProject
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
        /// CompileTargetName
        /// 
        /// The msbuild target name which is used to generate assembly from source code files.
        /// Usually it is "CoreCompile"
        /// 
        /// </summary>
        [Required]
        public string CompileTargetName
        { get; set; }

        public bool Restore
        { get; set; }

        [Required]
        public string TemporaryTargetAssemblyProjectName 
        { get; set; }

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
        { get; set; } = false;

        #endregion Public Properties
  
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private const string targetAssemblyProjectNamePropertyName = "_TargetAssemblyProjectName";

        #endregion Private Fields

    }
    
    #endregion GenerateTemporaryTargetAssembly Task class
}
