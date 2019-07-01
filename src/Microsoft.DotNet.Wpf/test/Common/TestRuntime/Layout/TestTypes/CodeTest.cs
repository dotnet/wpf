// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Layout.TestTypes
{
    /// <summary>
    /// Base Class for all Layout Code Tests.
    /// </summary>
    public class CodeTest : LayoutTest
    {
        /// <summary>
        /// Contructor
        /// </summary>
        public CodeTest()
        { }

        /// <summary>
        /// Test Content for test window.
        /// </summary>
        /// <returns>Framework Element</returns>
        public virtual FrameworkElement TestContent()
        {
            return null;
        }

        /// <summary>
        /// Test Actions for test case. 
        /// </summary>
        public virtual void TestActions()
        { }

        /// <summary>
        /// Validation steps for test case.
        /// </summary>
        public virtual void TestVerify()
        { }
    }
}