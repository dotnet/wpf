// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Xml;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Test.Utilities.Reflection;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// Security class has methods to use for security tests
    /// </summary>
    [System.Security.Permissions.FileIOPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted = true)]
    internal class SecurityElementBuilder
    {
        /// <summary>
        /// Creates a SecurityElement based on an xml representation of a PermissionSet
        /// </summary>
        /// <param name="InputReader">stream containing an xml representation of a PermissionSet</param>
        /// <returns>the created SecurityElement</returns>
        internal static SecurityElement SecurityElementFromXml(TextReader InputReader)
        {
            // create the security element
            SecurityElement secelem = null;

            // create the xml reader
            XmlTextReader reader = new XmlTextReader(InputReader);

            // read the PermissionSet tag and its attributes
            reader.Read();
            secelem = new SecurityElement(reader.Name);
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    secelem.AddAttribute(reader.Name, reader.Value);
                }
            }

            // read the IPermission's contained in the PermissionSet and its attributes
            while (reader.Read())
            {
                if (reader.Name == "IPermission")
                {
                    SecurityElement child = new SecurityElement(reader.Name);
                    if (reader.HasAttributes)
                    {
                        while (reader.MoveToNextAttribute())
                        {
                            child.AddAttribute(reader.Name, reader.Value);
                        }
                    }
                    secelem.AddChild(child);
                }
            }

            // close the xml reader
            reader.Close();

            // return the SecurityElement
            return (secelem);
        }


        /// <summary>
        /// Creates a SecurityElement based on a file containing a xml representation of a PermissionSet
        /// </summary>
        /// <param name="File">path of the file</param>
        /// <returns>the created SecurityElement</returns>
        internal static SecurityElement SecurityElementFromXmlFile(string File)
        {
            FileStream fs = new FileStream(File, FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(fs);
            SecurityElement secelem = SecurityElementFromXml(reader);
            reader.Close();
            fs.Close();
            return (secelem);
        }
    }


    /// <summary>
    /// StrongNameMembershipConditionBuilder is an utility class for constructing membership conditions based on strong names
    /// </summary>
    internal class StrongNameMembershipConditionBuilder
    {
        internal static StrongNameMembershipCondition StrongNameMembershipConditionFromPublicKeyBlob(string publicKeyBlob)
        {
            // create a security IMembershipCondition element. goal is to construct an xml like this:
            // <IMembershipCondition class="StrongNameMembershipCondition"
            //        version="1"
            //        PublicKeyBlob="012456789" />
            SecurityElement se = new SecurityElement("IMembershipCondition");
            se.AddAttribute("class", "StrongNameMembershipCondition");
            se.AddAttribute("version", "1");
            se.AddAttribute("PublicKeyBlob", publicKeyBlob);

            // create a StrongNamePublicKeyBlob. it doesn't matter the key
            StrongNamePublicKeyBlob keyBlob = new StrongNamePublicKeyBlob(new byte[] { 0x00 });

            // create the StrongNameMembershipCondition to return
            StrongNameMembershipCondition snmc = new StrongNameMembershipCondition(keyBlob, null, null);

            // fill the StrongNameMembershipCondition from the built xml
            snmc.FromXml(se);

            // return the StrongNameMembershipCondition
            return (snmc);
        }
    }


    /// <summary>
    /// Allows sandboxed execution of assemblies
    /// </summary>
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted = true)]
    internal class Sandbox
    {
        private AppDomain sandbox;


        /// <summary>
        /// Creates a Sandbox object based on the xml'ed permissions in a file
        /// </summary>
        /// <param name="permSetPath">file with an xml'ed permission set</param>
        internal Sandbox(string permSetPath)
        {
            // get a handler of the permission set file
            FileHandler permissionSet = new FileHandler(permSetPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // create the sandbox
            BuildSandbox(permissionSet.StreamReader, null);
        }


        /// <summary>
        /// Creates a Sandbox object based on the xml'ed permissions and a list of full trust public key blobs
        /// </summary>
        /// <param name="permSetPath">Path of the file containing the permission set</param>
        /// <param name="fullTrustAsmPath">The file containing the public key blob. Expected format is:
        /// <code>
        /// <FullTrustAssemblies>
        ///     <PublicKeyBlob>123</PublicKeyBlob>
        ///     <PublicKeyBlob>456</PublicKeyBlob>
        ///     ...
        /// </FullTrustAssemblies>
        /// </code>
        /// </param>
        internal Sandbox(string permSetPath, string fullTrustAsmPath)
        {
            StreamReader permSetReader = null;
            FileHandler permissionSet = null;

            try
            {
                // get a handler of the permission set file
                permissionSet = new FileHandler(permSetPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                permSetReader = permissionSet.StreamReader;
            }
            catch (ArgumentNullException)
            {
                // set the reader to null, BuildSandbox will take care of this
                permSetReader = null;
            }

            // create the sandbox
            BuildSandbox(permSetReader, fullTrustAsmPath);
        }


        /// <summary>
        /// Executes a method sandboxed in an appdomain
        /// </summary>
        /// <param name="FnToExecute">The method to execute</param>
        internal void Execute(System.CrossAppDomainDelegate FnToExecute)
        {
            sandbox.DoCallBack(FnToExecute);
        }


        /// <summary>
        /// Executes an exe sandboxed in ann appdomain
        /// </summary>
        /// <param name="AsmToExecute">The exe file to execute</param>
        internal void Execute(string AsmToExecute)
        {
            sandbox.ExecuteAssembly(AsmToExecute);
        }


        /// <summary>
        /// Creates a sandboxed instance of a type
        /// </summary>
        /// <param name="AssemblyName">The assembly where the type lives</param>
        /// <param name="TypeName">The name of the type to construct</param>
        /// <returns>An ObjectHandle of the constructed type</returns>
        internal ObjectHandle CreateInstance(string AssemblyName, string TypeName)
        {
            try
            {
                ObjectHandle oh = sandbox.CreateInstance(AssemblyName, TypeName, false, System.Reflection.BindingFlags.CreateInstance, null, new object[] { }, null, null, null);
                return (oh);
            }
            catch (Exception e)
            {
                GlobalLog.LogDebug(e.ToString());
                return (null);
            }
        }


        /// <summary>
        /// Adds full trust assemblies to a policy level
        /// </summary>
        /// <param name="policyLevel">The policy level to add full trust assemblies</param>
        /// <param name="fullTrustAssembliesPath">The file containing the public key blob</param>
        private void AddFullTrustAssemblies(PolicyLevel policyLevel, string fullTrustAssembliesPath)
        {
            // create the query to get the membership conditions
            XmlQuery membershipConditions = new XmlQuery();
            membershipConditions.Load(fullTrustAssembliesPath);
            membershipConditions.Select("/FullTrustAssemblies/PublicKeyBlob");

            // for each mc found...
            while (membershipConditions.Iterator.MoveNext())
            {
                // create a mc object
                StrongNameMembershipCondition fullTrustedMC = StrongNameMembershipConditionBuilder.StrongNameMembershipConditionFromPublicKeyBlob(membershipConditions.Iterator.Current.ToString());

                // add the full trust mc
                //error CS0618: 'System.Security.Policy.PolicyLevel.AddFullTrustAssembly(System.Security.Policy.StrongNameMembershipCondition)' is obsolete: 'Because all GAC assemblies always get full trust, the full trust list is no longer meaningful. You should install any assemblies that are used in security policy in the GAC to ensure they are trusted.'
                //policyLevel.AddFullTrustAssembly(fullTrustedMC);
            }
        }


        /// <summary>
        /// Creates a Sandbox object based on a TextReader containing an xml'ed permission set
        /// </summary>
        /// <param name="permissionSetStream">a text reader representing a xml'ed permission set</param>
        /// <param name="fullTrustAssembliesPath" />
        private void BuildSandbox(TextReader permissionSetStream, string fullTrustAssembliesPath)
        {
            PermissionSet permSet = null;

            // if there isn't a stream with a permission set, no sandbox => full trust permission set
            if (permissionSetStream != null)
            {
                //create permission set from stream
                SecurityElement secelem = SecurityElementBuilder.SecurityElementFromXml(permissionSetStream);
                permSet = new PermissionSet(PermissionState.None);
                permSet.FromXml(secelem);
            }
            else
            {
                permSet = new PermissionSet(PermissionState.Unrestricted);
            }

            //create sandbox app domain. This method doesn't use the full trust assemblies, they are used by the policy level
            CreateSandboxedDomain(permSet, fullTrustAssembliesPath);
        }


        /// <summary>
        /// Creates a sandboxed app domain
        /// </summary>
        /// <param name="Permissions">The PermissionSet to apply to the new app domain</param>
        /// <param name="fullTrustAssembliesPath">Stream containing the full trust public key blobs</param>
        private void CreateSandboxedDomain(PermissionSet Permissions, string fullTrustAssembliesPath)
        {
            //create policy level
            PolicyLevel polLevel = PolicyLevel.CreateAppDomainLevel();

            //assign permission set to the policy level
            polLevel.RootCodeGroup.PolicyStatement = new PolicyStatement(Permissions);

            // add full trust assemblies to the policy level
            if (fullTrustAssembliesPath != null)
            {
                AddFullTrustAssemblies(polLevel, fullTrustAssembliesPath);
            }

            //create appdomain
            sandbox = AppDomain.CreateDomain("sandbox appDomain");

            //apply security policy
            sandbox.SetAppDomainPolicy(polLevel);
        }
    }


    /// <summary>
    /// SecurityInfo exposes functionality to access useful info regarding the security of an application
    /// </summary>
    internal class SecurityInfo
    {
        /// <summary>
        /// Returns the permission set granted to the caller assembly
        /// </summary>
        /// <returns>the granted permission set</returns>
        internal static PermissionSet GetGrantSet()
        {
            // load the mscorlib
            AssemblyProxy ap = new AssemblyProxy();

            ap.Load("mscorlib, Version=1.2.3400.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, ProcessorArchitecture=Neutral");

            // get the permission set granted to this assembly
            ap.Parameters.Add(new PermissionSet(PermissionState.None));
            ap.Parameters.Add(new PermissionSet(PermissionState.None));
            ap.Invoke("System.AppDomain", "nGetGrantSet", AppDomain.CurrentDomain);
            PermissionSet pset = (PermissionSet)ap.Parameters[0];
            ap.Parameters.Clear();
            return (pset);
        }

        /// <summary>
        /// GetCurrentUserSID
        /// </summary>
        /// <returns></returns>
        internal static string GetCurrentUserSID()
        {
            return (System.Security.Principal.WindowsIdentity.GetCurrent().User.Value);
        }
    }

    /// <summary>
    /// PermissionSetBuilder provides ways to construct PermissionSet objects
    /// </summary>
    internal class PermissionSetBuilder
    {
        /// <summary>
        /// Creates a PermissionSet from its xml'ed description
        /// </summary>
        /// <param name="s">xml string</param>
        /// <returns>a new PermissionSet</returns>
        internal static PermissionSet PermissionSetFromString(string s)
        {
            StringReader reader = new StringReader(s);
            SecurityElement secelem = SecurityElementBuilder.SecurityElementFromXml(reader);
            PermissionSet perm = new PermissionSet(PermissionState.None);
            perm.FromXml(secelem);
            return (perm);
        }
    }

    /// <summary>
    /// TransparentSecurityHelper exposes helper functions for partially trusted apps.
    /// METHODS OF THIS CLASS MUST NOT ASSERT ANY PERMISSION. THIS WOULD MODIFY EXPECTED RESULTS.
    /// </summary>
    internal class TransparentSecurityHelper
    {
        /// <summary>
        /// check whether the given permission is granted
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        internal static bool IsPermissionGranted(IPermission p)
        {
            try
            {
                p.Demand();

                // permission is granted
                return (true);
            }
            catch (SecurityException)
            {
                // permission is not granted
                return (false);
            }
        }

        /// <summary>
        /// Make a Demand of the given permission set
        /// </summary>
        /// <param name="p"></param>
        internal static void Demand(PermissionSet p)
        {
            p.Demand();
        }

        /// <summary>
        /// Make a Demand of the given permission
        /// </summary>
        /// <param name="p"></param>
        internal static void Demand(CodeAccessPermission p)
        {
            p.Demand();
        }

        /// <summary>
        /// LinkDemandFullTrust
        /// </summary>
        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        internal static void LinkDemandFullTrust()
        {
        }

        /// <summary>
        /// IsEmpty
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        internal static bool IsEmpty(CodeAccessPermission p)
        {
            PermissionSet empty = new PermissionSet(PermissionState.None);
            PermissionSet ps = new PermissionSet(PermissionState.None);
            ps.AddPermission(p);
            bool isEmpty = empty.Union(ps).Equals(empty);
            return (isEmpty);
        }

        /// <summary>
        /// DemandSuccedsAfterAssert
        /// </summary>
        /// <param name="toAssert"></param>
        /// <param name="toDemand"></param>
        /// <returns>true if assert stopped demand; false if assert didn't stop demand</returns>
        internal static bool DemandSuccedsAfterAssert(PermissionSet toAssert, PermissionSet toDemand)
        {
            toAssert.Assert();
            try
            {
                TransparentSecurityHelper.Demand(toDemand);
                return (true);
            }
            catch (SecurityException)
            {
                return (false);
            }
        }

        /// <summary>
        /// TestAssert
        /// </summary>
        /// <param name="toPermitOnly"></param>
        /// <param name="toDemand"></param>
        /// <returns>true if assert stopped demand; false if assert didn't stop demand</returns>
        internal static bool DemandSuccedsAfterPermitOnly(PermissionSet toPermitOnly, PermissionSet toDemand)
        {
            // assert full trust first
            new PermissionSet(PermissionState.Unrestricted).Assert();

            // permit only given permission set
            toPermitOnly.PermitOnly();
            try
            {
                TransparentSecurityHelper.Demand(toDemand);
                return (true);
            }
            catch (SecurityException)
            {
                return (false);
            }
        }

        /// <summary>
        /// DemandSuccedsAfterDeny
        /// </summary>
        /// <param name="toDeny"></param>
        /// <returns></returns>
        internal static bool DemandSuccedsAfterDeny(PermissionSet toDeny)
        {
            toDeny.Deny();
            try
            {
                TransparentSecurityHelper.Demand(toDeny);
                return (true);
            }
            catch (SecurityException)
            {
                return (false);
            }
        }
    }

    /// <summary>
    /// LinkDemandElement is an element that makes a link demand for full trust in its default constructor
    /// Used by testcases/testcase2
    /// </summary>
    internal class LinkDemandFullTrustElement
    {
        /// <summary>
        /// Element default constructor. It makes a link demand for full trust
        /// </summary>
        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        internal LinkDemandFullTrustElement()
        {
        }
    }

    /// <summary>
    /// HwndUtils lets partial trust tests access certain Hwnd-related items it normally can't reach
    /// Note: this is a security hole.  Don't do this in shipping Avalon code!
    /// </summary>
    internal class HwndUtils
    {
        //let partial trust test get an HwndSource
        internal static HwndSource GetHwndSource(Visual v)
        {
            HwndSource hws = null;

            new PermissionSet(PermissionState.Unrestricted).Assert();
            try
            {
                hws = PresentationSource.FromVisual(v) as HwndSource;
            }
            finally
            {
                CodeAccessPermission.RevertAll();
            }
            return hws;
        }

        //let partial trust get an InputManager
        internal static InputManager GetInputManager()
        {
            InputManager im = null;

            new PermissionSet(PermissionState.Unrestricted).Assert();
            try
            {
                im = InputManager.Current;
            }
            finally
            {
                CodeAccessPermission.RevertAll();
            }
            return im;
        }
    }
}

