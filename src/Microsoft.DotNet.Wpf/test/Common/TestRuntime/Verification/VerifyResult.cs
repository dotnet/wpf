// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using Microsoft.Test.Logging;

namespace Microsoft.Test.Verification
{
    /// <summary>
    /// Default implementation for IVerifyResult, the result of verifying using IVerifier.
    /// </summary>
    public class VerifyResult : IVerifyResult
    {
        #region Private Data

        private string _message;

        private TestResult _result;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor. 
        /// </summary>
        public VerifyResult() : this(TestResult.Unknown, "") { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="result">Result of verification.</param>
        /// <param name="message">Message (error or not) you may want to pass along.</param>
        public VerifyResult(TestResult result, string message)
        {
            _result = result;
            _message = message;
        }

        #endregion

        #region Public and Protected Members

        /// <summary>
        /// Message (error or not) you may want to associate with the result of the verification.
        /// </summary>
        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        /// <summary>
        /// Result of the verification.
        /// </summary>
        public TestResult Result
        {
            get { return _result; }
            set { _result = value; }
        }

        #endregion

    }
}
