// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Security.Permissions;
using Microsoft.Win32;

namespace Microsoft.Test.Globalization
{
   /// <summary>
   /// Helps in Determining if a specific WPF languageuage Pack is Installed.
   /// </summary>
    [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
    public static class LanguagePackHelper
    {
        #region Public Members

        /// <summary>
        /// Determines if the WPF language pack is installed
        /// E.g: IsLanguagePackInstalled(new CultureInfo("en-US");
        ///      IsLanguagePackInstalled(System.Globalization.CultureInfo.CurrentCulture);
        /// The language dlls come installed by default in Vista.. As such no registry entries for the dlls are made..
        /// This is reflected in the logic which searches for the specific dlls in the system directory for the Vista OS
        /// We do not anticipate this function being called for Vista as it is redundant
        /// </summary>
        /// <param name="culture">culture info corresponding to the languagepack</param>
        /// <returns></returns>
        public static bool IsWpfLanguagePackInstalled(CultureInfo culture)
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                string languageFolder = culture.IetfLanguageTag.Split('-')[0];

                try
                {
                    if ((int)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\V3.0\Setup\Windows Presentation Foundation\" + languageFolder, "InstallSuccess", -1) == 1)
                    {
                        return true;
                    }
                }
                catch (NullReferenceException) { }
            }

            string languageResourcesFolder;

            //The registry key search should work for XP
            //the below is just a backup incase the registry gets corrupted
            //OR in the case of english languageuage
            if (Environment.OSVersion.Version.Major < 6)
            {
                languageResourcesFolder = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework", "InstallRoot", string.Empty).ToString()+@"\v3.0\WPF";
            }
            else
            {
                languageResourcesFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            }
            if (Directory.Exists(languageResourcesFolder))
            {
                string languageDllNumber = "";
                switch (culture.Parent.Name)
                {

                    case "en":
                        languageDllNumber = "0009.dll";
                        break;

                    case "de":
                        languageDllNumber = "0007.dll";
                        break;

                    case "fr":
                        languageDllNumber = "000c.dll";
                        break;

                    case "es":
                        languageDllNumber = "000a.dll";
                        break;
                }
                if ((File.Exists(languageResourcesFolder + @"\NlsData" + languageDllNumber)) &&
                    (File.Exists(languageResourcesFolder + @"\NlsLexicons" + languageDllNumber)))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Determines what the current WPF UICulture is.
        /// Checking CultureInfo.CurrentUICulture is not good enough because locale will not be applied in WPF unless the lang pack for that locale is actually installed.
        /// In the event that the WPF lang pack that matches the system cultture is not installed, the WPF ui culture will fallback to English.  
        /// </summary>        
        /// <returns>CultureInfo</returns>
        public static CultureInfo CurrentWPFUICulture()
        {           
            CultureInfo fallbackCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
            string cultureKeyValue = "Install";

            if (currentUICulture.Name == fallbackCulture.Name)
            {
                return currentUICulture;
            }

            // Will need to expand this logic to include future major releases.
            System.Collections.ArrayList cultureKeyStringList = new System.Collections.ArrayList();
            if (System.Environment.Version.Major == 4)
            {
                cultureKeyStringList.Add(string.Format(@"Software\Microsoft\NET Framework Setup\NDP\v4\Full\{0}", System.Globalization.CultureInfo.CurrentUICulture.LCID));
                cultureKeyStringList.Add(string.Format(@"Software\Microsoft\NET Framework Setup\NDP\v4\Client\{0}", System.Globalization.CultureInfo.CurrentUICulture.LCID));
            }
            else if (System.Environment.Version.Major < 4)
            {
                cultureKeyValue = "InstallSuccess";
                cultureKeyStringList.Add(string.Format(@"Software\Microsoft\NET Framework Setup\NDP\v3.0\Setup\Windows Presentation Foundation\{0}", System.Globalization.CultureInfo.CurrentUICulture.LCID));
            }

            // Check each registry key.  If a match is found we will assume that the lang pack is installed.
            foreach (object cultureKeyString in cultureKeyStringList)
            {
                RegistryKey cultureKey = Registry.LocalMachine.OpenSubKey(cultureKeyString.ToString());
                if (cultureKey != null)
                {
                    object installValue = cultureKey.GetValue(cultureKeyValue);
                    if (installValue != null)
                    {
                        if ((int)installValue == 1)
                        {
                            return currentUICulture;
                        }
                    }
                }
            }
            return fallbackCulture;   
        }

        #endregion
    }
}
