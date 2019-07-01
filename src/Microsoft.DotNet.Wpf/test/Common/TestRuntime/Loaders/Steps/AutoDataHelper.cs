// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections;
using System.Globalization;
using System.Text;
using Microsoft.Test.Security.Wrappers;
using Microsoft.Test.Globalization;

namespace Microsoft.Test.Utilities.VariationEngine
{
    /// <summary>
    /// Helper for getting Localization/Globalization strings.
    /// Variation Engine helper to read data from AutoData and return relevant string information.    
    /// </summary>
    public class AutoDataHelper
    {
        static internal string currentculture = null;
        static internal string language = null;
        static CultureInfo[] allCultures;

        internal bool Isdirty = false;

        /// <summary>
        /// When {AutoData[&lt;language&gt;]:#} format is specified load autodata and retrieve the string 
        /// defined by autodata.
        /// </summary>
        /// <param name="autodatastring"></param>
        /// <param name="varnode"></param>
        /// <returns></returns>
        internal string DeriveAutoDataInformation(string autodatastring, XmlNode varnode)
        {
            // AutoData reading.            

            int index = -1;
            if (autodatastring.Contains("[") || autodatastring.Contains("]"))
            {
                index = autodatastring.IndexOf("[");
                if (index < 0)
                {
                    index = autodatastring.IndexOf("]");
                    language = autodatastring.Substring(0, index);
                }
                else
                {
                    if (autodatastring.Contains("]"))
                    {
                        language = autodatastring.Substring(index + 1, autodatastring.IndexOf("]") - index - 1);
                    }
                }
            }

            index = autodatastring.IndexOf(':');
            if (index < 0)
            {
                return null;
            }

            autodatastring = autodatastring.Substring(index + 1, autodatastring.Length - index - 1);

            int stringlength = 75;
            if (autodatastring.Contains("-"))
            {
                index = autodatastring.IndexOf('-');
                stringlength = Convert.ToInt16(autodatastring.Substring(index + 1));
                autodatastring = autodatastring.Substring(0, index - 1);
            }

            if (String.IsNullOrEmpty(language) || language == "current")
            {
                currentculture = GetCurrentCulture();
            }
            else
            {
                currentculture = language;
            }

            ScriptTypeEnum currentscript;
            if (String.IsNullOrEmpty(currentculture))
            {
                currentscript = ScriptTypeEnum.Latin;
            }
            else
            {
                object obj = Enum.Parse(typeof(ScriptTypeEnum), currentculture);
                currentscript = (ScriptTypeEnum)obj;
            }

            // Script index
            index = Convert.ToInt16(autodatastring);

            if (index >= Microsoft.Test.Globalization.Extract.GetAllTestStrings(currentscript).Length)
            {
                index = index % Microsoft.Test.Globalization.Extract.GetAllTestStrings(currentscript).Length;
            }

            autodatastring = Microsoft.Test.Globalization.Extract.GetTestString(index, currentscript);
            if (autodatastring.Length > Convert.ToInt16(stringlength))
            {
                autodatastring = Microsoft.Test.Globalization.Extract.GetTestString(index, currentscript, stringlength);
            }

            Isdirty = true;

            return autodatastring;
        }

        /// <summary>
        /// Get Culture name from Language. Example Arabic = ar
        /// </summary>
        /// <param name="culturename"></param>
        /// <param name="varnode"></param>
        /// <returns></returns>
        public static string GetCultureName(string culturename, XmlNode varnode)
        {
            AutoDataHelper adh = new AutoDataHelper();
            return adh.GetCurrentCultureName(culturename, varnode);
        }


        /// <summary>
        /// Internal method.
        /// </summary>
        /// <returns></returns>
        internal string GetCurrentCultureName(string culturename, XmlNode varnode)
        {
            if (String.IsNullOrEmpty(culturename))
            {
                return CultureInfo.CurrentUICulture.Name;
            }

            int index = culturename.IndexOf(':');
            if (index < 0)
            {
                return null;
            }

            culturename = culturename.Substring(index + 1);

            if (culturename.ToLowerInvariant() == "current")
            {
                return CultureInfo.CurrentUICulture.Name;
            }

            // If a culture is specified which is not supported we should get a Arguement exception.
            // This way we ensure that only the AutoData supported cultures are used.
            object obj = Enum.Parse(typeof(ScriptTypeEnum), culturename);

            if (allCultures == null)
            {
                CultureTypes[] mostCultureTypes = new CultureTypes[] {
                        CultureTypes.NeutralCultures, 
                        CultureTypes.SpecificCultures, 
                        CultureTypes.InstalledWin32Cultures, 
                        CultureTypes.UserCustomCulture, 
                        CultureTypes.ReplacementCultures, 
                        CultureTypes.FrameworkCultures,
                        CultureTypes.WindowsOnlyCultures
                        };

                // Get and enumerate all cultures.
                allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            }

            foreach (CultureInfo ci in allCultures)
            {
                // Display the name of each culture.
                Console.WriteLine("Culture: {0}", ci.Name);
                string englishname = ci.DisplayName;
                index = englishname.IndexOf('(');
                if (index >= 0)
                {
                    //throw new ApplicationException("This cannot happen. XmlVariationEngine:GetCultureName");
                    englishname = englishname.Substring(0, index).Trim();
                }

                if (englishname == culturename)
                {
                    culturename = ci.Name;
                    break;
                }
            }

            return culturename;
        }

        /// <summary>
        /// Get Culture name which is different than current culture.
        /// </summary>
        /// <returns>string</returns>
        internal string GetDifferentCultureName()
        {
            int diffCultureLCID = CultureInfo.CurrentCulture.LCID + 2;
            try
            {
                return CultureInfo.GetCultureInfo(diffCultureLCID).Name;
            }
            catch
            {
                return "en-US";
            }
        }

        /// <summary>
        /// Get Invalid Culture name.
        /// </summary>
        /// <returns>string</returns>
        internal string GetInvalidCultureName()
        {
                return "en-USS";
        }

        /// <summary>
        /// Helper function to get culture
        /// </summary>
        /// <returns></returns>
        private static string GetCurrentCulture()
        {
            return Microsoft.Test.Globalization.Extract.CurrentScriptType.ToString();
        }

        /// <summary>
        /// Helper function to return the language portion of UICulture englistname property.
        /// </summary>
        /// <param name="cultureenglishname"></param>
        /// <returns></returns>
        private static string GetCultureName(string cultureenglishname)
        {
            int index = cultureenglishname.IndexOf("(");
            if (index < 0)
            {
                return null;
            }

            cultureenglishname = cultureenglishname.Substring(0, index).Trim();
            if (cultureenglishname.ToLower() == "english")
            {
                return null;
            }

            return cultureenglishname;
        }

    }

}
