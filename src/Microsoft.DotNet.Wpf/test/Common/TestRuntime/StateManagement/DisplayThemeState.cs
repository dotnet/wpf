// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Test.Display;
using System.IO;
using Microsoft.Win32;
using System.Xml.Serialization;

namespace Microsoft.Test.StateManagement
{
    /// <summary>
    /// Reports and allows you to modify the current monitor settings.
    /// The settings include Width, Height, BitsPerPixel and Frequency
    /// </summary>
    public class DisplayThemeState : State<DisplayThemeStateValue, object>
    {
        #region Private Data

        private static readonly string BackupStr = " [CUSTOMIZED]";

        #endregion

        #region Constructor

        /// <summary/>
        public DisplayThemeState()
            : base()
        {
        }

        #endregion

        #region Override Members

        /// <summary/>
        public override DisplayThemeStateValue GetValue()
        {
            string themeFilename = DisplayConfiguration.GetThemeFilename();
            //If we query on a backed up theme, the BACKUP words will show up
            //API returns Windows Classic if no theme is found (Server 2003 for example)
            string themeName = DisplayConfiguration.GetTheme().Replace(BackupStr,string.Empty);
            
            //If the theme manager is disabled or there's no theme selected, return windows classic
            if (!DisplayConfiguration.IsThemeEnabled || themeFilename == null)
                return new DisplayThemeStateValue(themeName, null);
            
            //If the theme has been customized, make sure we back it up
            string modifiedThemeName = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\LastTheme", "DisplayName of Modified", null) as string;
            if (!String.IsNullOrEmpty(modifiedThemeName))
            {
                //Need to save theme
                themeFilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), String.Format("{0}{1}.theme", themeName, BackupStr));
                DisplayConfiguration.SaveCurrentTheme(themeFilename);
            }

            return new DisplayThemeStateValue(themeName, themeFilename);
        }

        /// <summary/>
        public override bool SetValue(DisplayThemeStateValue value, object action)
        {
            if (!String.IsNullOrEmpty(value.ThemeFilename))
            {
                if (!string.Equals(DisplayConfiguration.GetThemeFilename(), value.ThemeFilename, StringComparison.InvariantCultureIgnoreCase))                            
                    DisplayConfiguration.SetCustomTheme(value.ThemeFilename);

                return String.Equals(DisplayConfiguration.GetThemeFilename(), value.ThemeFilename, StringComparison.InvariantCultureIgnoreCase);
            }
            else if (!String.IsNullOrEmpty(value.Theme))
            {
                //If we have no theme file information, we need to ensure we are using the right theme by switching
                //TODO: We could optimize a bit more by checking the name of the theme, making sure it's from the system folder
                //and that there's no custom changes
                DisplayConfiguration.SetTheme(value.Theme);
                return DisplayConfiguration.GetTheme().Equals(value.Theme, StringComparison.InvariantCultureIgnoreCase);
            }
            return false;
        }

        /// <summary/>
        public override bool Equals(object obj)
        {
            DisplayThemeState other = obj as DisplayThemeState;
            if (other == null)
                return false;
            return true;
        }

        /// <summary/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }


    /// <summary>
    /// Theme Settings
    /// </summary>
    public class DisplayThemeStateValue
    {
        private string theme;
        private string themeFilename;

        #region Constructor

        /// <summary/>
        public DisplayThemeStateValue() : this(null, null)
        {
        }

        /// <summary/>
        public DisplayThemeStateValue(string theme) : this(theme, null)
        {            
        }

        /// <summary/>
        public DisplayThemeStateValue(string theme, string themeFilename)
        {
            this.theme = theme;
            this.themeFilename = themeFilename;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Theme
        /// </summary>
        [XmlAttribute()]
        public string Theme
        {
            get { return theme; }
            set { theme = value; }
        }

        /// <summary>
        /// Theme location
        /// </summary>
        [XmlAttribute()]
        public string ThemeFilename
        {
            get { return themeFilename; }
            set { themeFilename = value; }
        }

        #endregion

        #region Override Members

        /// <summary/>
        public override bool Equals(object obj)
        {
            DisplayThemeStateValue other = obj as DisplayThemeStateValue;
            if (other == null)
                return false;

            return (other.theme == theme && other.themeFilename == themeFilename);
        }

        /// <summary/>
        public override int GetHashCode()
        {
            int themeHashCode = String.IsNullOrEmpty(theme) ? 0 : theme.GetHashCode();
            int themeFilenameHashCode = String.IsNullOrEmpty(themeFilename) ? 0 : themeFilename.GetHashCode();
            return themeHashCode ^ themeFilenameHashCode;
        }

        /// <summary/>
        public static bool operator ==(DisplayThemeStateValue x, DisplayThemeStateValue y)
        {
            if (object.Equals(x, null))
                return object.Equals(y, null);
            else
                return x.Equals(y);
        }

        /// <summary/>
        public static bool operator !=(DisplayThemeStateValue x, DisplayThemeStateValue y)
        {
            if (object.Equals(x, null))
                return !object.Equals(y, null);
            else
                return !x.Equals(y);
        }

        #endregion
    }
}
