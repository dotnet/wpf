// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Threading;

using System.Runtime.Serialization;
using System.IO;
using Microsoft.Test.Win32;

namespace Microsoft.Test.Modeling
{

    ///<summary>
    /// This class is the Modeling traversal that is loaded from a XTC. This enqueue the test cases transitions for later
    /// executing.  It is implemented was on a Push and Pull Model to get the transitions, the pull model happens when
    /// the executing enters to a nested pump.  The pull model don't happen automatically, the user needs to know how
    /// request for the next item.
    ///</summary>
    public class XtcTestCaseLoader : TestCaseLoader
    {

        ///<summary>
        /// Constructor that takes the XtcFile and what range of cases you want to execute
        ///</summary>
        public XtcTestCaseLoader(string xtc) : this(xtc, 0, -1) { }

        ///<summary>
        /// Constructor that takes the XtcFile and what range of cases you want to execute
        ///</summary>
        public XtcTestCaseLoader(string xtc, int firstCase, int lastCase)
            : base()
        {

            if (xtc == null)
                throw new ArgumentNullException("The XTC file or XTC content.");

            if (firstCase > 0)
            {
                _firstCase = firstCase;
            }

            if (lastCase >= 1)
            {
                _lastCase = lastCase;
            }

            _xtc = xtc;
        }


        ///<summary>
        /// Create the queue of test cases from a xtcfile
        ///</summary>
        protected override ActionsQueue CreateActionQueue()
        {
            _xtcParser = new XtcParser(_xtc, Models, _firstCase, _lastCase);
            return _xtcParser.CreateActionsQueue();
        }

        string _xtc = String.Empty;
        XtcParser _xtcParser = null;
        int _firstCase = 1;
        int _lastCase = -1;


    }

}
