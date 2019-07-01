// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Test.Diagnostics;
using Microsoft.Test.Logging;

namespace Microsoft.Test.StateManagement
{
    /// <summary>
    /// COM Server State
    /// </summary>
    public class ComServerState : State<Boolean, object>
    {
        #region Private Data

        private string fileName;
        private string clsid; // CLSID of any of the classes contained in the DLL

        #endregion

        #region Constructors

        /// <summary/>
        public ComServerState()
            : base()
        {
            //Needed for serialization
        }

        /// <summary>
        /// Initializes a ComSvr state given a file name
        /// </summary>
        /// <param name="fileName">Name of DLL to register</param>
        public ComServerState(string fileName, string clsid)
            : base()
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(@"File name must be specified.");
            }
            if (string.IsNullOrEmpty(clsid))
            {
                throw new ArgumentNullException(@"Class ID must be specified.");
            }

            this.fileName = fileName;
            this.clsid = clsid;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Path to the DLL
        /// </summary>
        public string FileName
        {
            get { return fileName; }
        }

        /// <summary>
        /// CLSID
        /// </summary>
        public string CLSID
        {
            get { return clsid; }
        }

        #endregion

        #region Override Members

        /// <summary>
        /// Returns a flag that says if a specific DLL is registered or not
        /// Inherited form State
        /// </summary>
        /// <returns></returns>
        public override Boolean GetValue()
        {
            // First check if our CLSID exists in registry under HKEY_CLASSES_ROOT/CLSID
            string[] clsids = Registry.ClassesRoot.OpenSubKey("CLSID").GetSubKeyNames(); // Registered CLSIDs
            bool isClsidInRegistry = false;
            for (int i = 0; i < clsids.Length; i++)
            {
                if (String.Equals(clsids[i], clsid, StringComparison.InvariantCultureIgnoreCase))
                {
                    isClsidInRegistry = true;
                    break;
                }
            }

            if (!isClsidInRegistry)
            {
                return false;
            }

            // Then check if a server path exists under our CLSID
            RegistryKey inProcServer =
                Registry.ClassesRoot.OpenSubKey("CLSID").OpenSubKey(clsid).OpenSubKey("InprocServer32");
            RegistryKey localServer  =
                Registry.ClassesRoot.OpenSubKey("CLSID").OpenSubKey(clsid).OpenSubKey("LocalServer32");

            if (inProcServer == null)
            {
                if (localServer == null)
                {
                    return false;
                }
                // else, does the path pointed to by LocalServer32 exist?
                if (File.Exists((string)localServer.GetValue(null)))
                {
                    return true;
                }

                return false;
            }
            // else, does the path pointed to by InprocServer32 exist?
            if (File.Exists((string)inProcServer.GetValue(null)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Registers or unregisters a DLL based on value in first parameter
        /// Inherited from State
        /// </summary>
        /// <param name="register">registers if true, unregisters if not.</param>
        /// <param name="action">Unused param</param>
        public override bool SetValue(Boolean register, object action)
        {
            if (register)
            {
                RegistrationHelper.RegisterComServer(fileName);
            }
            else
            {
                // When unregistering, we need to make sure we're using the same binary
                //  to unregister as the one under the server path (InprocServer32 or LocalServer32)
                RegistryKey inProcServer =
                    Registry.ClassesRoot.OpenSubKey("CLSID").OpenSubKey(clsid).OpenSubKey("InprocServer32");
                RegistryKey localServer =
                    Registry.ClassesRoot.OpenSubKey("CLSID").OpenSubKey(clsid).OpenSubKey("LocalServer32");
                if (inProcServer != null)
                {
                    fileName = (string)inProcServer.GetValue(null);
                }
                else
                {
                    if (localServer != null)
                    {
                        fileName = (string)localServer.GetValue(null);
                    }
                }
                RegistrationHelper.UnregisterComServer(fileName);
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            ComServerState rhs = obj as ComServerState;
            if (rhs == null)
            {
                return false;
            }

            return (String.Equals(clsid, rhs.CLSID, StringComparison.InvariantCultureIgnoreCase) &
                String.Equals(fileName, rhs.FileName, StringComparison.InvariantCultureIgnoreCase));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

    }
}
