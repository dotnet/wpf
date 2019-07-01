// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Test.Discovery
{
    /// <summary>
    /// Attribute representing test case metadata.
    /// </summary>
    // TODO: Remove obsolete ctors, Dev10 Bug 587668.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class TestAttribute : Attribute
    {
        #region Magic Numbers

        /// <summary>
        /// Because attributes don't support nullable types, we have to use
        /// 'magic' numbers to indicate unset.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720")]
        public static readonly int UnsetInt = -1;

        /// <summary>
        /// Obsolete message that is used by the various obsolete attributes on deprecated ctors.
        /// I'd prefer this type be a static readonly, but attributes are picky about the types
        /// permitted in them - and one restriction is that static readonlys are not permitted.
        /// </summary>
        public const string ObsoleteConstructorMessage = "The constructor with Name as the only positional argument and all others as named arguments should be used.";

        #endregion

        #region Constructors

        /// <summary>
        /// This is the only constructor that should be used.
        /// </summary>
        /// <param name="name">Test case name, should be unique.</param>
        public TestAttribute(string name)
        {
            this.Name = name;

            // Because attributes don't support nullable types, we have to use
            // 'magic' numbers to indicate unset. Priority and Timeout use -1,
            // SecurityLevel fortunately has an Unset value, and MtaThread
            // and Disabled's default value of false ends up working out ok.
            // Bool is really dangerous since it isn't tri-state, but as long
            // as we craft them so the user would only want to set them to true
            // and stamping the inherited value is ok, we can get away with it
            // for now. Dealing with unset will be part of a Discovery rework.
            this.Priority = UnsetInt;
            this.Timeout = UnsetInt;
            this.SecurityLevel = TestCaseSecurityLevel.Unset;
            this.ExecutionGroupingLevel = ExecutionGroupingLevel.InternalDefault_DontUseThisLevel;
        }

        /// <summary>
        /// Constructor, name will come from test class name.
        /// This is obsolete, but not enforced by compiler, as 3.5 Build is being harsh wrt Warnings as Errors on feature code.
        /// </summary>
        //[Obsolete(ObsoleteConstructorMessage)]
        public TestAttribute(int priority, string subArea)
            : this(priority, subArea, TestCaseSecurityLevel.Unset, String.Empty) { }

        /// <summary>
        /// Constructor.
        /// This is obsolete, but not enforced by compiler, as 3.5 Build is being harsh wrt Warnings as Errors on feature code.
        /// </summary>
        //[Obsolete(ObsoleteConstructorMessage)]
        public TestAttribute(int priority, string subArea, string name)
            : this(priority, subArea, TestCaseSecurityLevel.Unset, name) { }
        
        /// <summary>
        /// Constructor that takes everything.
        /// This is obsolete, but not enforced by compiler, as 3.5 Build is being harsh wrt Warnings as Errors on feature code.
        /// </summary>
        //[Obsolete(ObsoleteConstructorMessage)]
        public TestAttribute(int priority, string subArea, TestCaseSecurityLevel securityLevel, string name)
            : this(subArea, securityLevel, name)
        {
            this.Priority = priority;
        }

        /// <summary>
        /// Constructor that leaves priority at default. Allows defaults attribute to specify 
        /// priority for entire class or assembly.
        /// This is obsolete, but not enforced by compiler, as 3.5 Build is being harsh wrt Warnings as Errors on feature code.
        /// </summary>
        //[Obsolete(ObsoleteConstructorMessage)]
        public TestAttribute(string subArea, TestCaseSecurityLevel securityLevel, string name)
        {
            // Since this() constructor calls go from root type to immediate parent,
            // this constructor is oldest in the chain so we can safely set
            // priority to the -1 unset magic number. Any calling constructor
            // will have it's chance to set priority and override this value.
            this.Priority = UnsetInt;
            this.Timeout = UnsetInt;

            if (String.IsNullOrEmpty(subArea))
            {
                Trace.TraceWarning("TestAttribute SubArea should not be empty.");
            }

            this.SubArea = subArea;
            this.SecurityLevel = securityLevel;
            this.Name = name;
            this.ExecutionGroupingLevel = ExecutionGroupingLevel.InternalDefault_DontUseThisLevel;
        }

        #endregion

        #region Public and Protected Members

        /// <summary>
        /// Get the value for this Priority.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Test Case Area.
        /// </summary>
        public string Area { get; set; }

        /// <summary>
        /// Get the value for this SubArea.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public string SubArea { get; set; }

        /// <summary>
        /// Get the test case security level.
        /// </summary>
        public TestCaseSecurityLevel SecurityLevel { get; set; }

        /// <summary>
        /// Describes the desired degree of Execution Grouping.
        /// </summary>
        public ExecutionGroupingLevel ExecutionGroupingLevel { get; set; }

        /// <summary>
        /// Set to execute case in multithreaded apartment. Only used for
        /// AppDomain cases.
        /// </summary>
        public bool MtaThread { get; set; }

        /// <summary>
        /// Get test case name.
        /// </summary>
        public string Name { get; set; }
       
        /// <summary>
        /// Test case description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// By default, the test class will be searched a method called Run.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Get test case method parameters. These are passed to the test's Run method, not the constructor.
        /// </summary>
        public string MethodParameters { get; set; }

        /// <summary>
        /// Test case timeout in seconds.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Get disabled state.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Comma separated list of support files.
        /// </summary>
        public string SupportFiles { get; set; }

        /// <summary>
        /// Space separated list of keys and key=value pairs that will be passed in 
        /// TestInfo.DriverParams. TestParameters override all other driver params.
        /// <code>[Test(1, "myArea", "my test", TestParameters = "CtorParams=arg1,arg2 SomethingUsedByTest")]</code>
        /// </summary>
        public string TestParameters { get; set; }

        /// <summary>
        /// key/key=value string that can be used to store data on a test. 
        /// This is obsolete, but not enforced by compiler, as 3.5 Build is being harsh wrt Warnings as Errors on feature code.
        /// </summary>
        //[Obsolete("Test arguments should be supplied via driver arguments.")]
        public string Variables { get; set; }
        
        /// <summary>
        /// Comma separated list of versions.
        /// </summary>
        public string Versions { get; set; }

        /// <summary>
        /// Comma separated list of bugs in the DevDiv database.
        /// </summary>
        public string DevDivBugs { get; set; }

        /// <summary>
        /// Comma separated list of accepted configurations.
        /// </summary>
        public string Configurations { get; set; }

        /// <summary>
        /// Comma separated list of deployment files.
        /// </summary>
        public string Deployments { get; set; }
        
        /// <summary>
        /// Comma separated list of keywords.
        /// </summary>
        public string Keywords { get; set; }       
 
        #endregion
    }
}
