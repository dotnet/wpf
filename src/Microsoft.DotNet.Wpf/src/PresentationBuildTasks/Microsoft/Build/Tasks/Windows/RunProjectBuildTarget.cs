// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    #region RunProjectBuildTarget Task class

    /// <summary>
    /// </summary>
    public sealed class RunProjectBuildTarget : Task
    {
        #region Constructors

        /// <summary>
        /// Constructor 
        /// </summary>
        public RunProjectBuildTarget()
            : base(SR.SharedResourceManager)
        {
        }   
        
        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// ITask Execute method
        /// </summary>
        /// <returns></returns>
        /// <remarks>Catching all exceptions in this method is appropriate - it will allow the build process to resume if possible after logging errors</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public override bool Execute()
        {
            System.Diagnostics.Debugger.Launch();

            bool retValue = true;

            // Verification
            try
            {
                // Run project build target
                retValue = BuildEngine.BuildProjectFile(ProjectName, new string[] { BuildTarget }, null, null);
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                retValue = false;
            }

            return retValue;
        }

        #endregion Public Methods
        
        #region Public Properties

        [Required]
        public string ProjectName 
        { get; set; }

        [Required]
        public string BuildTarget 
        { get; set; }

        #endregion Public Properties
    }
    
    #endregion RunProjectBuildTarget Task class
}
