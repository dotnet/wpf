// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections;
using System.Globalization;
using System.Text;

namespace Microsoft.Test.Utilities.VariationEngine
{
    /// <summary>
    /// Helper for Version strings.
    /// </summary>
    public class VersionHelper
    {
        string AssemblyVersion = null;
        string AssemblyCulture = null;
        string AssemblyPublickeytoken = null;
        static string _presentationassemblyfullname = null;

        /// <summary>
        /// 
        /// </summary>
        public static string PresentationAssemblyFullName
        {
            set
            {
                // typeof(System.Windows.FrameworkElement).Assembly
                if (String.IsNullOrEmpty(value))
                {
                    return;
                }

                _presentationassemblyfullname = value;

                //string[] assemblyinfo = value.Split(new char[] { ',' });
                //if (assemblyinfo.Length != 4)
                //{
                //    throw new ApplicationException("Assembly Full Name incorrect");
                //}

                //DeriveAssemblyInfo(assemblyinfo);
                //assemblyinfo = null;
            }
        }

        /// <summary>
        /// Seperates a string array information into different assembly data.
        /// </summary>
        /// <param name="assemblyinfo">Assembly full name delimited by Assembly Full Name</param>
        private void DeriveAssemblyInfo(string[] assemblyinfo)
        {
            for (int i = 0; i < assemblyinfo.Length; i++)
            {
                if (assemblyinfo[i].Contains("=") == false)
                {
                    if (i == 0)
                    {
                        continue;
                    }
                    else
                    {
                        throw new ApplicationException("Assembly Full name is incorrect");
                    }
                }

                switch (i)
                {
                    case 1:
                        AssemblyVersion = assemblyinfo[i];
                        break;

                    case 2:
                        AssemblyCulture = assemblyinfo[i];
                        break;

                    case 3:
                        AssemblyPublickeytoken = assemblyinfo[i];
                        break;
                }
            }
        }

        /// <summary>
        /// Based on current variation contents returns Version string for either 
        /// PresentationFramework version info or system info.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal string GetVersionInfo(string text)
        {
            int index = -1;
            string versiontype = "av";

            if (String.IsNullOrEmpty(text) == false)
            {

                if (text.Contains("[") || text.Contains("]"))
                {
                    index = text.IndexOf("[");
                    if (index < 0)
                    {
                        index = text.IndexOf("]");
                        versiontype = text.Substring(0, index);
                    }
                    else
                    {
                        if (text.Contains("]"))
                        {
                            versiontype = text.Substring(index + 1, text.IndexOf("]") - index - 1);
                        }
                    }
                }
            
                index = text.IndexOf(':');
                if (index < 0)
                {
                    return null;
                }

                text = text.Substring(index + 1, text.Length - index - 1);
            }

            string[] assemblyinfo = null;
            //if (assemblyinfo.Length != 4)
            //{
            //    throw new ApplicationException("Assembly Full Name incorrect");
            //}

            //DeriveAssemblyInfo(assemblyinfo);
            //assemblyinfo = null;

            switch (versiontype.ToLowerInvariant())
            {
                case "av" :
                    assemblyinfo = _presentationassemblyfullname.Split(new char[] { ',' });
                    break;

                case "system":
                    assemblyinfo = typeof(object).Assembly.FullName.Split(new char[] { ',' });
                    break;
            }

            if (assemblyinfo.Length != 4)
            {
                throw new ApplicationException("Assembly Full Name incorrect");
            }

            DeriveAssemblyInfo(assemblyinfo);
            assemblyinfo = null;

            if (String.IsNullOrEmpty(text))
            {
                text = "full";
            }

            switch (text.ToLowerInvariant())
            {
                case "full":
                    text = "," + AssemblyVersion + "," + AssemblyCulture + "," + AssemblyPublickeytoken;
                    break;

                case "assemblyversion":
                    text = "," + AssemblyVersion;
                    break;

                case "assemblyculture":
                    text = "," + AssemblyCulture;
                    break;

                case "assemblypublictoken":
                    text = "," + AssemblyVersion + "," + AssemblyPublickeytoken;
                    break;
            }

            return text;
        }
    }
}
