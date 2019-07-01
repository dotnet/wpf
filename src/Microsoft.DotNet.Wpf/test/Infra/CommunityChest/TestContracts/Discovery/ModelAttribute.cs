// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Discovery
{
    /// <summary>
    /// Model attribute.
    /// </summary>
    // Code Analysis says we should seal it, but can't for legacy support.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class ModelAttribute : TestAttribute
    {
        #region Constructors 

        /// <summary>
        /// Constructor.
        /// </summary>
        public ModelAttribute(string xtcFileName, int priority, string subarea)
            : this(xtcFileName, priority, subarea, String.Empty) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ModelAttribute(string xtcFileName, int priority, string subarea, string name)
            : this(xtcFileName, priority, subarea, TestCaseSecurityLevel.Unset, name) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ModelAttribute(string xtcFileName, int priority, string subarea, TestCaseSecurityLevel securityLevel, string name)
            : base(name)
        {
            Priority = priority;
            SubArea = subarea;
            SecurityLevel = securityLevel;
            _xtcFileName = xtcFileName;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ModelAttribute(string xtcFileName, int modelStart, int modelEnd, int priority, string subarea, string name)
			: this(xtcFileName, modelStart, modelEnd, priority, subarea, TestCaseSecurityLevel.Unset, name) { }

		/// <summary>
        /// Constructor that takes everything.
        /// </summary>
        public ModelAttribute(string xtcFileName, int modelStart, int modelEnd, int priority, string subarea, TestCaseSecurityLevel securityLevel, string name)
            : base(name)
		{
            Priority = priority;
            SubArea = subarea;
            SecurityLevel = securityLevel;
            _xtcFileName = xtcFileName;
            
            _modelStart= modelStart;
            _modelEnd = modelEnd;
        }

        #endregion

        #region Public properties 

        /// <summary>
        /// Expand each test case in the provided XTC into separate test cases.
        /// </summary>
        public bool ExpandModelCases 
        {
            get { return _expandModelCases; }
            set { _expandModelCases = value; }
        }
        private bool _expandModelCases = false;

        /// <summary>
        ///  Get the value for title.
        /// </summary>
        public string XtcFileName
        {
            get { return this._xtcFileName; }
        }

        /// <summary>
        /// Get the value for Params.
        /// </summary>
        public int ModelStart
        {
            get {return this._modelStart;}
        }

        /// <summary>
        /// Get if the test case will run under SEE.
        /// </summary>
        public int ModelEnd
        {
            get {return this._modelEnd;}
        }

        #endregion

        #region Private members

        /// <summary>
        /// Our title.
        /// </summary>
        private string _xtcFileName = String.Empty;

        /// <summary>
        /// Hold disabled value.
        /// </summary>
        private int _modelStart = -1;

        /// <summary>
        /// Hold the Area.
        /// </summary>
        private int _modelEnd = -1;

        #endregion
    }

}
