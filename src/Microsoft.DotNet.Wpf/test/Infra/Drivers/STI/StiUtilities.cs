// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
#if !TESTBUILD_IA64
    using System.Windows.Threading;
#endif
using System.Xml;
using System.Diagnostics;

namespace Microsoft.Test
{
    public static class StiUtilities
    {
        #region Private Methods

        [SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.EndsWith(System.String)")]
        private static void WriteXMLConfigFile(string fileName, string MSBuildVersion)
        {
            if (!MSBuildVersion.EndsWith(".0.0"))
            {
                MSBuildVersion += ".0.0";
            }
            XmlTextWriter xtw = new XmlTextWriter(fileName, null);
            xtw.Formatting = Formatting.Indented;
            xtw.WriteStartDocument();
            xtw.WriteStartElement("configuration");
            xtw.WriteStartElement("runtime");
            xtw.WriteStartElement("assemblyBinding", "urn:schemas-microsoft-com:asm.v1");
            xtw.WriteStartElement("dependentAssembly");
            xtw.WriteStartElement("assemblyIdentity");
            xtw.WriteAttributeString("name", "Microsoft.Build.Framework");
            xtw.WriteAttributeString("publicKeyToken", "b03f5f7f11d50a3a");
            xtw.WriteAttributeString("culture", "neutral");
            xtw.WriteEndElement();
            xtw.WriteStartElement("bindingRedirect");
            xtw.WriteAttributeString("oldVersion", "0.0.0.0-99.9.9.9");
            xtw.WriteAttributeString("newVersion", MSBuildVersion);
            xtw.WriteEndElement();//bindingRedirect
            xtw.WriteEndElement();//dependentAssembly
            xtw.WriteStartElement("dependentAssembly");
            xtw.WriteStartElement("assemblyIdentity");
            xtw.WriteAttributeString("name", "Microsoft.Build.Engine");
            xtw.WriteAttributeString("publicKeyToken", "b03f5f7f11d50a3a");
            xtw.WriteAttributeString("culture", "neutral");
            xtw.WriteEndElement();
            xtw.WriteStartElement("bindingRedirect");
            xtw.WriteAttributeString("oldVersion", "0.0.0.0-99.9.9.9");
            xtw.WriteAttributeString("newVersion", MSBuildVersion);
            xtw.WriteEndElement();//bindingRedirect
            xtw.WriteEndElement();//dependentAssembly
            xtw.WriteEndElement();//assemblyBinding
            xtw.WriteEndElement();//runtime
            xtw.WriteEndElement();//configuration
            xtw.WriteEndDocument();
            xtw.Flush();
            xtw.Close();
        }
        #endregion

        /// <summary>
        /// Gets the assembly's strong name signature
        /// </summary>
        /// <param name="asm"></param>
        /// <returns></returns>
        private static StrongName GetAssemblyStrongName(Assembly asm)
        {
            AssemblyName name = asm.GetName();
            return new StrongName(new StrongNamePublicKeyBlob(name.GetPublicKey()), name.Name, name.Version);
        }

#if TESTBUILD_CLR20
        /// <summary>
        /// Apply "FullTrust" security policy or a partial trust policy.
        /// </summary>
        /// <param name="securityLevel">"FullTrust" to set full trust, otherwise partial trust.</param>
        public static void ApplySecurityPolicy(TestCaseSecurityLevel securityLevel)
        {
            // Apply the security policy for the test case.
            PermissionSet permissionSet = null;
            if (securityLevel != TestCaseSecurityLevel.PartialTrust)
            {
                //GlobalLog.LogStatus("Setting full trust security permissions.");
                permissionSet = new PermissionSet(PermissionState.Unrestricted);
            }
            else
            {
                //GlobalLog.LogStatus("Setting partial trust security permissions.");
                permissionSet = GetSEEPermissionSet();
            }
            AppDomain.CurrentDomain.SetAppDomainPolicy(CreatePolicyLevel(permissionSet));
        }

        /// <summary>
        /// Creates an AppDomain PolicyLevel that will restrict untrusted Code to a specified PermissionSet
        /// </summary>
        /// <param name="defaultPermissions">PermissionSet that will be granted to untrusted code</param>
        /// <returns>AppDomain PolicyLevel</returns>
        private static PolicyLevel CreatePolicyLevel(PermissionSet defaultPermissions)
        {
            PolicyLevel policyLevel = PolicyLevel.CreateAppDomainLevel();
            policyLevel.RootCodeGroup = new FirstMatchCodeGroup(new AllMembershipCondition(), new PolicyStatement(new PermissionSet(PermissionState.None)));

            //All assemblies signed with the CLR Framework key will be granted FullTrust
            //            TrustAssembliesSignedWithKey(policyLevel, Microsoft.Internal.BuildInfo.CLR_PUBLIC_KEY_STRING);
            TrustAssembliesSignedWithKey(policyLevel, "2411111111111111111111111111111111111112");

            string WCP_PUBLIC_KEY_STRING = Get_WCP_PUBLIC_KEY_STRING();

            //All assemblies signed with the Avalon key will be granted FullTrust

            TrustAssembliesSignedWithKey(policyLevel, WCP_PUBLIC_KEY_STRING); //Microsoft.Internal.BuildInfo.WCP_PUBLIC_KEY_STRING);

            //Create a security Element that will grant FullTrust to all assemblies signed with the Microsoft key
            TrustAssembliesSignedWithKey(policyLevel, "002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293");

            //Create a security Element that will grant FullTrust to all assemblies signed with the Longhorn API key
            //            TrustAssembliesSignedWithKey(policyLevel, Microsoft.Internal.BuildInfo.WINDOWS_PUBLIC_KEY_STRING);
            TrustAssembliesSignedWithKey(policyLevel, "00240000048000009400000006020000002400005253413100040000010001003f8c902c8fe7ac83af7401b14c1bd103973b26dfafb2b77eda478a2539b979b56ce47f36336741b4ec52bbc51fecd51ba23810cec47070f3e29a2261a2d1d08e4b2b4b457beaa91460055f78cc89f21cd028377af0cc5e6c04699b6856a1e49d5fad3ef16d3c3d6010f40df0a7d6cc2ee11744b5cfb42e0f19a52b8a29dc31b0");

            //Create a security Element that will grant FullTrust to all assemblies signed with the Windows Client Test Trusted key
            //            TrustAssembliesSignedWithKey(policyLevel, Microsoft.Internal.BuildInfo.WINDOWS_TEST_PUBLIC_KEY_STRING);
            TrustAssembliesSignedWithKey(policyLevel, "00240000048000009400000006020000002400005253413100040000010001007fdb0774cdfefc88aaa6613332e5be12a11bd32adb7e2ead4c049cffcc6284fa975cd55b0291738247984cfcf4074970c44c1da29b07201b0f90fb7b2c60d2a604c4aba9fbc106ad3d1838dad496780b0e4518d045fe70c4de4e9663354989cf9a2e4f9add41bfef82437da35c8b1c1a0d6dacb521456c4bb18bada2be7407c3");

            //Create a statment for the PermissionSet and set the RootCodeGroup of the policy
            FirstMatchCodeGroup untrustedCodeGroup = new FirstMatchCodeGroup(new AllMembershipCondition(), new PolicyStatement(defaultPermissions));
            untrustedCodeGroup.Description = "Grants all non-trusted code a specific set of permissions";
            policyLevel.RootCodeGroup.AddChild(untrustedCodeGroup);

            return policyLevel;
        }

        private static string Get_WCP_PUBLIC_KEY_STRING()
        {
            Assembly assembly = typeof(Dispatcher).Assembly;
            Type type = assembly.GetType("Microsoft.Internal.BuildInfo");

            FieldInfo fieldInfo = type.GetField("WCP_PUBLIC_KEY_STRING",
                                                BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
            string WCP_PUBLIC_KEY_STRING = (string)fieldInfo.GetValue(type);

            if (String.IsNullOrEmpty(WCP_PUBLIC_KEY_STRING))
            {
                throw new InvalidOperationException();
            }
            return WCP_PUBLIC_KEY_STRING;
        }

        private static void TrustAssembliesSignedWithKey(PolicyLevel policyLevel, string publicKey)
        {
            StrongNameMembershipCondition membership = CreateStrongNameMC(publicKey);
            FirstMatchCodeGroup trustedCodeGroup = new FirstMatchCodeGroup(membership, new PolicyStatement(new PermissionSet(PermissionState.Unrestricted)));
            trustedCodeGroup.Description = "Grants system signed code full trust even if not in the GAC";
            policyLevel.RootCodeGroup.AddChild(trustedCodeGroup);
        }

        //Creates a StrongNameMembershipCondition for a publicKeyBlob
        private static StrongNameMembershipCondition CreateStrongNameMC(string pubKeyBlob)
        {
            SecurityElement se = new SecurityElement("IMembershipCondition");
            Hashtable atts = new Hashtable();
            atts.Add("class", "StrongNameMembershipCondition");
            atts.Add("version", "1");
            atts.Add("PublicKeyBlob", pubKeyBlob);
            se.Attributes = atts;

            StrongNamePublicKeyBlob keyBlob = new StrongNamePublicKeyBlob(typeof(StiUtilities).Assembly.GetName().GetPublicKey()); //create a tempory keyblob
            StrongNameMembershipCondition snmc = new StrongNameMembershipCondition(keyBlob, null, null);  //use a fake keyblob to create the SNMC
            snmc.FromXml(se); //parse the securityelement to get the real condition

            return snmc;
        }
#endif
    }
}