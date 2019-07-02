// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    using System;
    using Microsoft.Win32;
    using Microsoft.Test.Diagnostics;

    internal sealed class MasterDimensionIndexableAttribute : Attribute
    {
        private bool isIndexable = false;
        public MasterDimensionIndexableAttribute(bool isIndexableDimension)
        {
            isIndexable = isIndexableDimension;
        }
        public bool IsIndexable
        {
            get { return isIndexable; }
        }
    }

    /// <summary>
    /// Contract for any object representing a machine state 
    /// </summary>
    public interface IMasterDimension
    {
        /// <summary>
        /// Direct the object to detect its own settings
        /// </summary>
        string GetCurrentValue();
    }

    [MasterDimensionIndexable(false)]
    internal class BuildNumberDimension : IMasterDimension
    {
        public string GetCurrentValue()
        {
            return SystemInformation.Current.BuildNumber.ToString();
        }
    }

    [MasterDimensionIndexable(false)]
    internal class CpuArchitectureDimension : IMasterDimension
    {
        public string GetCurrentValue()
        {
            return SystemInformation.Current.ProcessorArchitecture.ToString();

        }
    }

    [MasterDimensionIndexable(true)]
    internal class CurrentUICultureDimension : IMasterDimension
    {
        public string GetCurrentValue()
        {
            return System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        }
    }

    [MasterDimensionIndexable(true)]
    internal class DpiDimension : IMasterDimension
    {
        public string GetCurrentValue()
        {
            return Microsoft.Test.Display.Monitor.Dpi.x.ToString();
        }
    }

    [MasterDimensionIndexable(true)]
    internal class DwmDimension : IMasterDimension
    {
        public string GetCurrentValue()
        {
            if (Microsoft.Test.Display.DisplayConfiguration.IsDwmCapable &&
                Microsoft.Test.Display.DisplayConfiguration.IsDwmCompositionSupported &&
                Microsoft.Test.Display.DisplayConfiguration.IsDwmEnabled)
            {
                return "On";
            }
            return "Off";
        }
    }

    [MasterDimensionIndexable(false)]
    internal class HostTypeDimension : IMasterDimension
    {
        public string GetCurrentValue()
        {
            return "unknown";
        }
    }

    [MasterDimensionIndexable(true)]
    internal class IeVersionDimension : IMasterDimension
    {
        // Won't change => cached
        private string ieVersion = string.Empty;
        public string GetCurrentValue()
        {
            // @ Review : Should this move in SystemInformation ?
            if (ieVersion == string.Empty)
            {
                System.Security.Permissions.RegistryPermission registryPermission = null;
                registryPermission = new System.Security.Permissions.RegistryPermission(System.Security.Permissions.RegistryPermissionAccess.Read, "Version");
                try
                {
                    registryPermission.Assert();
                    using (Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer"))
                    {
                        string fullversion = (string)rk.GetValue("Version");
                        ieVersion = System.Text.RegularExpressions.Regex.Replace(fullversion, @"(^[^\.]+\.[^\.]+)\..+?$", "$1");
                    }
                }
                finally
                {
                    System.Security.Permissions.RegistryPermission.RevertAssert();
                }
            }
            return ieVersion;
        }
    }

    [MasterDimensionIndexable(true)]
    internal class OsCultureDimension : IMasterDimension
    {
        public string GetCurrentValue()
        {
            return System.Globalization.CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
        }
    }

    [MasterDimensionIndexable(true)]
    internal class OsVersionDimension : IMasterDimension
    {
        public string GetCurrentValue()
        {
            return Microsoft.Test.Diagnostics.SystemInformation.Current.Product.ToString();
        }
    }

    [MasterDimensionIndexable(true)]
    internal struct ThemeDimension : IMasterDimension
    {
        public string GetCurrentValue()
        {
            return Microsoft.Test.Display.DisplayConfiguration.GetTheme().ToLower();
        }
    }

    [MasterDimensionIndexable(false)]
    internal class VideoCardDimension : IMasterDimension
    {
        public string GetCurrentValue()
        {
            return Microsoft.Test.Display.Monitor.PrimaryAdaptorDescription;
        }
    }

    // MasterDimension that tells us what locale we will expect in WPF UI   
    [MasterDimensionIndexable(true)]
    internal class CurrentWPFUICultureDimension : IMasterDimension
    {
        public string GetCurrentValue()
        {
            return Microsoft.Test.Globalization.LanguagePackHelper.CurrentWPFUICulture().ToString();              
        }
    }
}
