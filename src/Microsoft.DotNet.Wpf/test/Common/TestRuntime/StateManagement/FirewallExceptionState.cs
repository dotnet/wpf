// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Reflection;
using System.Security.Principal;
using System.Xml;
using Microsoft.Test.Diagnostics;

namespace Microsoft.Test.StateManagement
{
    /// <summary>
    /// Firewall Exception State
    /// </summary>
    public class FirewallExceptionState : State<FirewallRule, object>
    {
        #region Private Data

        private string programName;

        #endregion

        #region Constructors

        /// <summary/>
        public FirewallExceptionState()
            : base()
        {
            //Needed for serialization
        }

        /// <summary>
        /// Initializes a firewall state given a program name
        /// </summary>
        /// <param name="programName">Program to be used as firewall filter exception.</param>
        public FirewallExceptionState(string programName)
            : base()
        {
            if (string.IsNullOrEmpty(programName))
            {
                throw new ArgumentNullException("programName");
            }

            if (!Path.IsPathRooted(programName))
            {
                throw new ArgumentException("Program Name must be fully qualified.", "programName");
            }

            this.programName = programName;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Name of the program
        /// </summary>
        public String ProgramName
        {
            get { return programName; }
            set { programName = value; }
        }

        #endregion

        #region Override Members

        /// <summary>
        /// Returns the value given the key and value name
        /// </summary>
        /// <returns></returns>
        public override FirewallRule GetValue()
        {
            return FirewallHelper.GetFirewallRuleForApplication(programName);
        }

        /// <summary>
        /// Sets a value for the given key and value name
        /// </summary>
        /// <param name="value"></param>
        /// <param name="action">Action to perform when setting the value</param>
        public override bool SetValue(FirewallRule value, object action)
        {
            switch (value)
            {
                case FirewallRule.None:
                {
                    FirewallHelper.EnableFirewallForExecutingApplication(programName, true);
                    break;
                }
                case FirewallRule.Exist:
                {
                    FirewallHelper.EnableFirewallForExecutingApplication(programName, false);
                    break;
                }
                case FirewallRule.Enabled:
                {
                    FirewallHelper.DisableFirewallForExecutingApplication(programName);
                    break;
                }
                default:
                {
                    throw new ArgumentException("Value is not a valid enum value.", "value");
                }
            }

            return true;
        }

        /// <summary/>
        public override bool Equals(object obj)
        {
            FirewallExceptionState otherFirewallExceptionState = obj as FirewallExceptionState;
            if (Object.Equals(otherFirewallExceptionState, null))
            {
                return false;
            }

            return (String.Equals(programName, otherFirewallExceptionState.programName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary/>
        public override int GetHashCode()
        {
            return programName.GetHashCode();
        }

        #endregion
    }

}
