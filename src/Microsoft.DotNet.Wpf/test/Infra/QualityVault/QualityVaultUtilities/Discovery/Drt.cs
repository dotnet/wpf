// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.Test.Discovery
{
    /// <summary/>
    [Serializable]
    public class Drt
    {
        private string executable;
        private string architecture;
        private string os;
        private string owner;
        private string team;
        private string args;
        private int timeout;
        private List<string> deployments;
        private List<string> supportFiles;

        /// <summary/>
        [XmlAttribute]
        public string Executable
        {
            get { return executable; }
            set { executable = value; }
        }

        /// <summary/>
        [XmlAttribute]
        public string Architecture
        {
            get { return architecture; }
            set { architecture = value; }
        }

        /// <summary/>
        [XmlAttribute]
        public string OS
        {
            get { return os; }
            set { os = value; }
        }

        /// <summary/>
        [XmlAttribute]
        public string Owner
        {
            get { return owner; }
            set { owner = value; }
        }

        /// <summary/>
        [XmlAttribute]
        public string Team
        {
            get { return team; }
            set { team = value; }
        }

        /// <summary/>
        [XmlAttribute]
        public string Args
        {
            get { return args; }
            set { args = value; }
        }

        /// <summary/>
        [XmlAttribute]
        public int Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }

        /// <summary>
        /// Collection of support files needed for this testcase
        /// </summary>
        public List<string> SupportFiles
        {
            get
            {
                if (supportFiles == null)
                    supportFiles = new List<string>();
                return supportFiles;
            }
        }
        /// <summary>
        /// Collection of deployments needed by this testcase
        /// </summary>
        public List<string> Deployments
        {
            get
            {
                if (deployments == null)
                    deployments = new List<string>();
                return deployments;
            }
        }
    }
}
