// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
// Needed for Theme Park (Avalon Security) testing
using System.Security.Permissions;
using System.Threading;
using System.Xml;

namespace Microsoft.Test.Globalization
{
   /// <summary>
   /// Contains Method and Properties for Extracting info from system or from XML files.
   /// </summary>
   public class Extract
   {

      #region Properties
         static private Random m_rnd = new Random();
         static private Calendar m_calendar = Thread.CurrentThread.CurrentCulture.Calendar;
         static private string m_xmlFileLocation = null;


         /// <summary>
         /// Get / Set the current location where the xml files lives (DataString.xml / DataDate.xml)
         /// </summary>
         static public string XmlFileLocation
         {
            get
            {
               return m_xmlFileLocation;
            }
            set
            {
               m_xmlFileLocation = value;
            }
         }

         /// <summary>
         /// Get / Set the current calendar in use; Expect / return a CalendarEnum
         /// </summary>
         public static CalendarEnum CurrentCalendar   
         {
            get
            {
               m_calendar = Thread.CurrentThread.CurrentCulture.Calendar;
               switch(m_calendar.ToString())
               {
                  case "System.Globalization.GregorianCalendar" :
                  switch(((System.Globalization.GregorianCalendar)m_calendar).CalendarType)
                  {
                     case System.Globalization.GregorianCalendarTypes.Arabic:
                        return CalendarEnum.GregorianArabic;
                     case System.Globalization.GregorianCalendarTypes.Localized:
                        return CalendarEnum.GregorianLocalized;
                     case System.Globalization.GregorianCalendarTypes.MiddleEastFrench:
                        return CalendarEnum.GregorianMiddleEastFrench;
                     case System.Globalization.GregorianCalendarTypes.TransliteratedEnglish:
                        return CalendarEnum.GregorianTransliteralEnglish;
                     case System.Globalization.GregorianCalendarTypes.TransliteratedFrench:
                        return CalendarEnum.GregorianTransliteralFrench;
                     case System.Globalization.GregorianCalendarTypes.USEnglish:
                        return CalendarEnum.GregorianUSEnglish;
                     default:
                        throw new Exception("Invalid calendar type");
                  }
                  case "System.Globalization.HebrewCalendar" :
                     return CalendarEnum.Hebrew ;
                  case "System.Globalization.HijriCalendar" :
                     return CalendarEnum.Hirji ;
                  case "System.Globalization.JapaneseCalendar" :
                     return CalendarEnum.Japanese ;
                  case "System.Globalization.JulianCalendar" :
                     return CalendarEnum.Julian ;
                  case "System.Globalization.KoreanCalendar" :
                     return CalendarEnum.Korean ;
                  case "System.Globalization.TaiwanCalendar" :
                     return CalendarEnum.Taiwan ;
                  case "System.Globalization.ThaiBuddhistCalendar" :
                     return CalendarEnum.ThaiBuddhist ;
                  default :
                     throw( new IndexOutOfRangeException("Unknow enum type !"));
               }
            }
            set
            {
               switch(value)
               {
                  case CalendarEnum.GregorianUSEnglish :
                     m_calendar = new GregorianCalendar(System.Globalization.GregorianCalendarTypes.USEnglish);
                     break;
                  // BUGBUG : Cannot switch to another calendar, why not ?
                  // To be implemented.
                  default:break;
               }
            }
         }

         /// <summary>
         /// Get the ScriptType the OS is currently using
         /// </summary>
         public static ScriptTypeEnum CurrentScriptType
         {

            get
            {
               //          System.Globalization.CultureInfo culture =   System.Globalization.CultureInfo.CurrentCulture;
               CultureInfo culture  = Thread.CurrentThread.CurrentCulture;
               ScriptTypeEnum scriptType = ScriptTypeEnum.Latin;
               //          Cannot use "culture.TextInfo.ANSICodePage" cause Code page list is not complete 

                switch(culture.LCID & 0xff)
               {
                  case 0x00 : throw new Exception("OS set to Language Neutral; cannot determine the ScriptType");
                  case 0x01 : scriptType =  ScriptTypeEnum.Arabic;   // Arabic
                     break;
                  case 0x02 : scriptType = ScriptTypeEnum.Cyrillic;  // Bulgarian
                     break;
                  case 0x03 : scriptType = ScriptTypeEnum.Latin;  // Catalan
                     break;
                  case 0x04 : // Needs to find out if it's Traditional or Simplified chinese
                  switch(culture.LCID & 0xff00)
                  {
                     case 0x0800:   // Chinese - China
                        scriptType = ScriptTypeEnum.ChineseSimplified;
                        break;
                     case 0x0C00:   // Chinese - Hong-Kong S.A.R.
                        scriptType = ScriptTypeEnum.ChineseTraditional;
                        break;
                     case 0x1400:   // Chinese - Macau S.A.R.
                        scriptType = ScriptTypeEnum.ChineseTraditional;
                        break;
                     case 0x1000:   // Chinese - Singapour
                        scriptType = ScriptTypeEnum.ChineseSimplified;
                        break;
                     case 0x0400:   // Chinese - Taiwan
                        scriptType = ScriptTypeEnum.ChineseTraditional;
                        break;
                  }
                     break;
                  case 0x05 : scriptType = ScriptTypeEnum.Latin;  // Czech
                     break;
                  case 0x06 : scriptType = ScriptTypeEnum.Latin;  // Danish
                     break;
                  case 0x07 : scriptType = ScriptTypeEnum.Latin;  // German
                     break;
                  case 0x08 : scriptType = ScriptTypeEnum.Greek;  //Greek
                     break;
                  case 0x09 : scriptType = ScriptTypeEnum.Latin;  // English
                     break;
                  case 0x0a: scriptType = ScriptTypeEnum.Latin;   // Spanish
                     break;
                  case 0x0b: scriptType = ScriptTypeEnum.Latin;   // Finish
                     break;
                  case 0x0c: scriptType = ScriptTypeEnum.Latin;   // French
                     break;
                  case 0x0d: scriptType = ScriptTypeEnum.Hebrew;  // Hebrew
                     break;
                  case 0x0e: scriptType = ScriptTypeEnum.Latin;   // Hungarian
                     break;
                  case 0x0f: scriptType = ScriptTypeEnum.Latin;   // Iceland
                     break;
                  case 0x10: scriptType = ScriptTypeEnum.Latin;   // Italian
                     break;
                  case 0x11: scriptType = ScriptTypeEnum.Japanese;   // Japanese
                     break;
                  case 0x12: scriptType = ScriptTypeEnum.Korean;  // Korean
                     break;
                  case 0x13: scriptType = ScriptTypeEnum.Latin;   // Dutch
                     break;
                  case 0x14: scriptType = ScriptTypeEnum.Latin;   // Norwegian
                     break;
                  case 0x15: scriptType = ScriptTypeEnum.Latin; //Polish
                     break;
                  case 0x16: scriptType = ScriptTypeEnum.Latin;   // Poruguese
                     break;
                  case 0x17: scriptType = ScriptTypeEnum.Latin;   // Raeto-Romance
                     break;                
                  case 0x18: scriptType = ScriptTypeEnum.Latin;   // Romanian
                     break;
                  case 0x19: scriptType = ScriptTypeEnum.Cyrillic;   // Russian
                     break;
                  case 0x1a: // Croatian - Serbian
                     // Need to find out if it's Latin or cyrillic
                     if ( (culture.LCID & 0xFF00) == 0x0800 )
                     {
                        scriptType = ScriptTypeEnum.Latin;
                     }
                     else
                     {
                        scriptType = ScriptTypeEnum.Cyrillic;  // 0x1C00
                     }
                     break;
                  case 0x1b: scriptType = ScriptTypeEnum.Latin;   // Slovak
                     break;
                  case 0x1c: scriptType = ScriptTypeEnum.Latin;   // Albanian
                     break;
                  case 0x1d: scriptType = ScriptTypeEnum.Latin;   // Swedish
                     break;
                  case 0x1e: scriptType = ScriptTypeEnum.Thai; // Thai
                     break;
                  case 0x1f: scriptType = ScriptTypeEnum.Latin;   // Turkish
                     break;
                  case 0x20: scriptType = ScriptTypeEnum.Arabic;   // Urdu
                     break;
                  case 0x21: scriptType = ScriptTypeEnum.Latin;   // Indonedian
                     break;
                  case 0x22: scriptType = ScriptTypeEnum.Cyrillic;   // Ukrenian
                     break;
                  case 0x23: scriptType = ScriptTypeEnum.Cyrillic; // Belarusian
                     break;
                  case 0x24: scriptType = ScriptTypeEnum.Cyrillic;   //slovenian
                     break;
                  case 0x25: scriptType = ScriptTypeEnum.Cyrillic;   // Estonian
                     break;
                  case 0x26: scriptType = ScriptTypeEnum.Latin;   //Latvian
                     break;
                  case 0x27: scriptType = ScriptTypeEnum.Cyrillic;   // Lithuanian
                     break;
                  case 0x29: scriptType = ScriptTypeEnum.Arabic;  // Farsi
                     break;
                  case 0x2a: scriptType = ScriptTypeEnum.Khmer;   // Vietnamese
                     break;
                  case 0x2b: scriptType = ScriptTypeEnum.Armenian;   // Armenian
                     break;
                  case 0x2c: // Azeri
                     // Need to check if it's Latin or cyrillic
                     if ( (culture.LCID & 0xFF00) == 0x0400 )
                     {
                        scriptType = ScriptTypeEnum.Latin;
                     }
                     else
                     {
                        scriptType = ScriptTypeEnum.Cyrillic;
                     }
                     break;
                  case 0x2d: scriptType = ScriptTypeEnum.Latin;   // Basque
                     break;
                  case 0x2e: scriptType = ScriptTypeEnum.Latin;   // Sorbian
                     break;
                  case 0x2f: scriptType = ScriptTypeEnum.Cyrillic;   // Macedonian (FYROM)
                     break;
                  case 0x30: scriptType = ScriptTypeEnum.Latin;   // Sutu (Sesotho)
                     break;
                  case 0x31: scriptType = ScriptTypeEnum.Latin;   // Tsonga
                     break;
                  case 0x32: scriptType = ScriptTypeEnum.Latin;   // Setsuana
                     break;
                  case 0x34: scriptType = ScriptTypeEnum.Latin;   // Xhosa
                     break;
                  case 0x35: scriptType = ScriptTypeEnum.Latin;   // Zulu
                     break;
                  case 0x36: scriptType = ScriptTypeEnum.Latin; // Afrikaans
                     break;
                  case 0x37: scriptType = ScriptTypeEnum.Georgian;   // Georgian
                     break;
                  case 0x38: scriptType = ScriptTypeEnum.Latin;   // Faeroese
                     break;
                  case 0x39: scriptType = ScriptTypeEnum.Devanagari; // Hindi
                     break;
                  case 0x3a: scriptType = ScriptTypeEnum.Latin;   // Maltese
                     break;
                  case 0x3c: scriptType = ScriptTypeEnum.Latin;    // Gaelic
                     break;
                  case 0x3d: scriptType = ScriptTypeEnum.Latin;   // Yiddish
                     break;

                  case 0x3e: scriptType = ScriptTypeEnum.Latin;   // Malay
                     break;
                  case 0x3f: scriptType = ScriptTypeEnum.Cyrillic;   // Kazak
                     break;
                  case 0x40: scriptType = ScriptTypeEnum.Cyrillic;   // Kyrgyz
                     break;
                  case 0x41: scriptType = ScriptTypeEnum.Latin;   // swahili
                     break;
                  case 0x42: scriptType = ScriptTypeEnum.Latin; // Phsudo-Loc
                     break;
                  case 0x43: // Uzbek
                     // Need to check if it's Latin or Cyrillic
                     if ( (culture.LCID & 0xFF00) == 0x0400 )
                     {
                        scriptType = ScriptTypeEnum.Latin;
                     }
                     else
                     {
                        scriptType = ScriptTypeEnum.Cyrillic;  // 0x0800
                     }
                     break;
                  case 0x44: scriptType = ScriptTypeEnum.Cyrillic;   // tatar
                     break;
                  case 0x45: scriptType = ScriptTypeEnum.Bengali; // Bengali
                     break;
                  case 0x46: scriptType = ScriptTypeEnum.Gurmukhi;   // Punjabi
                     break;
                  case 0x47: scriptType = ScriptTypeEnum.Gujarati;   // Gurjarati
                     break;
                  case 0x48: scriptType = ScriptTypeEnum.Oriya;   //Oriya
                     break;
                  case 0x49: scriptType = ScriptTypeEnum.Tamil;   // Tamil
                     break;
                  case 0x4a: scriptType = ScriptTypeEnum.Telugu;  // Telugu
                     break;
                  case 0x4b: scriptType = ScriptTypeEnum.Kannada; // Kannada
                     break;
                  case 0x4c: scriptType = ScriptTypeEnum.Malayalam;  // malayalam
                     break;
                  case 0x4d: scriptType = ScriptTypeEnum.Bengali; // assamese
                     break;
                  case 0x4e: scriptType = ScriptTypeEnum.Devanagari; // Marathi
                     break;
                  case 0x4f: scriptType = ScriptTypeEnum.Sinhala; // Sanskrit
                     break;
                  case 0x50: scriptType = ScriptTypeEnum.Mongolian;  // Mongolian
                     break;
                  case 0x51: scriptType = ScriptTypeEnum.Tibetan;  // Tibetan (PRC)
                     break;
                     //             case 0x56: scriptType = ScriptTypeEnum // Galician
                     //                break;
                     //             case 0x57: scriptType = ScriptTypeEnum // Konkani
                     //                break;
                  case 0x58: scriptType = ScriptTypeEnum.Bengali; // Manipuri
                     break;
                  case 0x59: scriptType = ScriptTypeEnum.Sinhala; // Sindhi
                     break;
                  case 0x5a: scriptType = ScriptTypeEnum.Syriac;  // Syriac
                     break;
                     //             case 0x60: scriptType = ScriptTypeEnum // Kashmiri
                     //                break;
                  case 0x61: scriptType = ScriptTypeEnum.Tibetan; // Nepali
                     break;
                     //             case 0x65: scriptType = ScriptTypeEnum // Divehi
                     //                break;
                  default: throw new Exception("(AutoData::Extract::CurrentScriptType) Unknown ScriptType -- LCID = 0x" + culture.LCID.ToString("x"));
               }
               return scriptType;
            }
         }

         
         /*
                  // Array of invalid character for a file name
                  private static char[] invalid = { (char)0,(char)1,(char)2,(char)3,(char)4,(char)5,(char)6,
                                 (char)7,(char)8,(char)9,(char)10,(char)11,(char)12,
                                 (char)13,(char)14,(char)15,(char)16,(char)17,(char)18,
                                 (char)19,(char)20,(char)21,(char)22,(char)23,(char)24,
                                 (char)25,(char)26,(char)27,(char)28,(char)29,(char)30,
                                 (char)31,(char)34,(char)42,(char)47,(char)58,(char)60,
                                 (char)62,(char)63,(char)92,(char)124,(char)160,
                                 (char)8192,(char)8193,(char)8194,(char)8195,(char)8196,
                                 (char)8197,(char)8198,(char)8199,(char)8200,(char)8201,
                                 (char)8202,(char)8203,(char)12288,(char)65279,
                                 (char)65534,(char)65535 };
            */

      #endregion

      #region Methods

         #region Constructor
            /// <summary>
            /// This Class cannot be instantiated !
            /// </summary>
            private Extract() {} // private to block instanciation
         #endregion

         #region GetTestString (7 overloads)

            /// <summary>
            /// Return a string from the Array of "ProblemString"
            /// </summary>
            /// <param name="index">(integer) the index of the String requested based on the current selected culture</param>
            /// <returns>(string) the string at the specified index</returns>
            public static string GetTestString(int index)
            {

               string retVal = null;
               ScriptTypeEnum scriptType = CurrentScriptType;
               retVal = GetTestString(index, scriptType);
/*
               try
               {
                  // forward call with current scriptType.
                  retVal= GetTestString(index, scriptType);
                  if(retVal == null || retVal == "")
                     throw new Exception("("+ scriptType + ") Entry is empty, defaulting to Mixed type");
               }
               catch(Exception)
               {
                  try
                  {
                     // Index does not exist on this scriptType, use Mixed
                     retVal = GetTestString(index, ScriptTypeEnum.Mixed);
                     if(retVal == null || retVal == "")
                        throw new Exception("(Mixed) Entry is empty, defaulting to Latin");
                  }
                  catch(Exception)
                  {
                     // Index does not exist on Mixed, use Latin
                     if(scriptType != ScriptTypeEnum.Latin)
                     {
                        retVal = GetTestString(index,ScriptTypeEnum.Latin);
                        // if this is failing as well, just let the exception be passed to the app
                     }
                     else
                     {
                        // throw Exception
                        throw new ArgumentOutOfRangeException("No string found with the specified index in the current script type, Mixed nor Latin");
                     }
                  }
               }
*/
               return retVal;
            }

            /// <summary>
            /// Return a string from the Array of "ProblemString" without unwanted char
            /// </summary>
            /// <param name="index">integer) the index of the String requested based on the current selected culture</param>
            /// <param name="excludeChars">(char[]) an array of char the user don't want to be in the returned string</param>
            /// <returns>(string) the string at the specified index without the unwanted char</returns>
            public static string GetTestString(int index, char[] excludeChars)
            {
                if (excludeChars == null)
                {
                    throw new ArgumentNullException("excludeChars", "second argument must be a valid array of char (null was passed in)");
                }
                return System.Text.RegularExpressions.Regex.Replace(GetTestString(index), new string(excludeChars), "");
            }

            /// <summary>
            /// Return a string from the Array of "ProblemString" based on a specific language
            /// </summary>
            /// <param name="index">integer, the index of the String requested</param>
            /// <param name="scriptType">scriptTypeEnum (see scriptTypeEnum), the language array where to find the string</param>
            /// <returns>string, the string at the specified index for the requested language</returns>
            public static string GetTestString(int index, ScriptTypeEnum scriptType)
            {
               string[] scriptTypeEntries = GetAllTestStrings(scriptType);
               if(index > scriptTypeEntries.Length || scriptTypeEntries[index] == "" || scriptTypeEntries[index] == null)
               {
                  if(scriptType != ScriptTypeEnum.Mixed)
                  {
                     scriptTypeEntries = GetAllTestStrings(ScriptTypeEnum.Mixed);
                  }
                  if(index > scriptTypeEntries.Length || scriptTypeEntries[index] == "" || scriptTypeEntries[index] == null)
                  {
                     if(scriptType != ScriptTypeEnum.Latin)
                     {
                        scriptTypeEntries = GetAllTestStrings(ScriptTypeEnum.Latin);
                        if(index > scriptTypeEntries.Length || scriptTypeEntries[index] == "" || scriptTypeEntries[index] == null)
                        {
                           throw new ArgumentOutOfRangeException("index", index, "(AutoData::GetTestString) index out of bound for this scriptType");
                        }
                     }
                  }
               }        

               return scriptTypeEntries[index];
            }


            /// <summary>
            /// Returns the first characters of the string at the index position
            /// </summary>
            /// <param name="Index">(int) Index : the index of the string you wnat to be returned</param>
            /// <param name="StringLength">(int) StringLength : The number of character to return</param>
            /// <returns></returns>
            public static string GetTestString(int Index, int StringLength)
            {
               return GetTestString(Index, CurrentScriptType, StringLength, false);
            }


            /// <summary>
            /// Returns some characters from the string contained at the index position
            /// </summary>
            /// <param name="Index">(int) Index : the index of the string you wnat to be returned</param>
            /// <param name="StringLength">(int) StringLength : The number of character to return</param>
            /// <param name="RandomPick">(bool) RandomPick : specify if the charcter return should be picked ar random or sequentially</param>
            /// <returns></returns>
            public static string GetTestString(int Index, int StringLength, bool RandomPick)
            {
               return GetTestString(Index, CurrentScriptType, StringLength, RandomPick);
            }

            /// <summary>
            /// Returns the first characters of the string at the index position of the given a scriptType
            /// </summary>
            /// <param name="Index">Index of the string you wnat to be returned</param>
            /// <param name="scriptType">Culture format for script to be returned</param>
            /// <param name="StringLength">The number of characters to return</param>
            /// <returns></returns>
            public static string GetTestString(int Index, ScriptTypeEnum scriptType, int StringLength)
            {
               return GetTestString(Index, scriptType, StringLength, false);
            }

            /// <summary>
            /// Returns some characters of the string at the index position of the given a scriptType
            /// </summary>
            /// <param name="Index">Index of the string you want to be returned</param>
            /// <param name="scriptType">Culture format of string to use.</param>
            /// <param name="StringLength">Number of characters to return</param>
            /// <param name="RandomPick">Whether character returns should be picked at random or sequentially</param>
            /// <returns></returns>
            public static string GetTestString(int Index, ScriptTypeEnum scriptType, int StringLength, bool RandomPick)
            {
               string retVal = null;
               string originalString = GetTestString(Index, scriptType);
               if(RandomPick)
               {
                  // pick character at random within the string
                  System.Text.StringBuilder temp = new System.Text.StringBuilder(StringLength);
                  for(int t = 0; t < StringLength; t++)
                  {
                     temp.Append(originalString[m_rnd.Next(originalString.Length - 1)]);
                  }
                  retVal = temp.ToString();
               }
               else
               {
                  if(StringLength > originalString.Length)
                  {
                     // Paste from the beginning until the expected size is reached
                     int repeat = StringLength / originalString.Length;
                     int remain = StringLength - (repeat * originalString.Length);
                     System.Text.StringBuilder temp = new System.Text.StringBuilder(StringLength);
                     for(int t = 0; t < repeat; t++)
                     {
                        temp.Append(originalString);
                     }
                     temp.Append(originalString, 0, remain);
                     retVal = temp.ToString();
                  }
                  else
                  {
                     // Truncate it
                     retVal = originalString.Substring(0, StringLength);
                  }
               }

               return retVal;
            }

      #endregion GetTeststring

         #region GetAllTestStrings (2 overloads)            
            /// <summary>
            /// Retrieve all strings for the current active culture
            /// </summary>
            /// <returns>(System.Collection Hashtable) An HashTable, Hash with key = scriptType Name (string) and value = collection of string (string[]) from the XML file.</returns>
            public static Hashtable GetAllTestStrings()
            {
               Hashtable retVal = null;
               ScriptTypeEnum [] scriptTypes = null;
               XmlDocument xmlDoc = null;
               Stream stream = null;

               FileIOPermission fp = new FileIOPermission(PermissionState.Unrestricted);
               fp.Assert();

               try
               {
                  stream = GetStreamFromAssembly("ProblemString.xml");

                  // First we need to know how many script exist
                  scriptTypes = (ScriptTypeEnum [])Enum.GetValues(typeof(ScriptTypeEnum));
                  retVal = new Hashtable(scriptTypes.Length);

                  // loop thru scriptType and get the values.
                  xmlDoc = new XmlDocument();
                  xmlDoc.Load(stream);
               }
               catch(Exception exp)
               {
                  throw new Exception("(rethrowing exception, see inner Exception)", exp);
               }
               finally
               {
                  if(stream != null)
                     stream.Close();
                  System.Security.CodeAccessPermission.RevertAssert();
               }
               for(int t = 0; t < scriptTypes.Length; t++)
               {
//                  string[] scriptTypeCollection = GetStringsFromXML(xmlDoc.DocumentElement, "string", false, scriptTypes[t].ToString());
                  string[] scriptTypeCollection = GetStringsUsingXPath(xmlDoc, "descendant::ScriptType[@type='" + scriptTypes[t].ToString() + "']/Data/string");
                  if(scriptTypeCollection.Length != 0)
                  {
                     retVal.Add(scriptTypes[t].ToString(),scriptTypeCollection);
                  }
               }

               return retVal;

            }


            /// <summary>
            /// Retrieve all strings from a specific culture
            /// </summary>
            /// <param name="scriptType">scriptTypeEnum (see scriptTypeEnum), the language Array to return\nTip : Use "CurrentScript" Property to get all localized string.</param>
            /// <returns>string[], an array of string containing the strings for this language</returns>
            public static string[] GetAllTestStrings(ScriptTypeEnum scriptType)
            {
                Stream stream = null;
                FileIOPermission fp = new FileIOPermission(PermissionState.Unrestricted);
                fp.Assert();
                try
                {
                    stream = GetStreamFromAssembly("ProblemString.xml");
                    string[] stringsFromFile = GetStringsUsingXPath(stream, "descendant::ScriptType[@type='" + scriptType.ToString() + "']/Data/string");   // scriptType);
                    if(stringsFromFile != null)
                    {
                        return stringsFromFile;
                    }
                }
                catch(Exception e)
                {
                    // rethrow
                   throw new Exception("(rethrowing exception, see inner Exception)", e);
                }
                finally
                {
                    if (stream != null)
                        stream.Close();
                  System.Security.CodeAccessPermission.RevertAssert();
                }

               // ScriptType not found !
               throw new Exception("(AutoData::GetAllTestStrings) ScriptType not found in the XML file nor in the embedded XML file !");
            }

         #endregion GetAllTestStrings

         #region GetDate (3 overloads) & GetAllDates (4 overloads)

            /// <summary>
            /// 
            /// </summary>
            /// <param name="calendar"></param>
            /// <param name="valid"></param>
            /// <returns></returns>
            public static string[] GetAllDates(CalendarEnum calendar,bool valid)
            {

               string[] stringFromFile = null;
               Stream stream = null;

               FileIOPermission fp = new FileIOPermission(PermissionState.Unrestricted);
               fp.Assert();

               try
               {
                  stream = GetStreamFromAssembly("ProblemDate.xml");
                  // Get all the string from the file
                  stringFromFile = GetStringsFromStream(stream, calendar, ((valid) ? "Valid" : "Wrong"));
                  if(stringFromFile != null)
                  {
                     return stringFromFile;
                  }
               }
               catch(Exception exp)
               {
                  // rethrow the exception (catch needed here so the finally will call stream.close)
                  throw new Exception("(rethrowing exception, see inner Exception)", exp);
               }
               finally
               {
                  if (stream != null)
                     stream.Close();
                  System.Security.CodeAccessPermission.RevertAssert();
               }

               // ScriptType not found !
               throw new Exception("(AutoData::GetAllDates) Calendar type not found !");
               
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="valid"></param>
            /// <returns></returns>
            public static string[] GetAllDates(bool valid)
            {
               Stream stream = null;
               string[] retVal = null;

               FileIOPermission fp = new FileIOPermission(PermissionState.Unrestricted);
               fp.Assert();

               try
               {
                  stream = GetStreamFromAssembly("ProblemDate.xml");
                  

                  XmlElement[] xElemt = GetElementsFromXML(stream,"DateTime",false,((valid == true)?"ValidDateTime":"WrongDateTime"));
                  retVal = new string[xElemt.Length];
                  for(int t = 0; t < xElemt.Length; t++)
                  {
                     retVal[t] = xElemt[t].InnerText;
                  }
               }
               catch(Exception e)
               {
                  throw new Exception("(rethrowing exception, see inner Exception)", e);
               }
               finally
               {
                  if(stream != null)
                     stream.Close();
                  System.Security.CodeAccessPermission.RevertAssert();
               }
               return retVal;
            }

            /// <summary>
            /// Returns a string array of all dates for a particular calendar format
            /// </summary>
            /// <param name="calendar">Culture of calendar to use</param>
            /// <returns>Array of all dates in this calendar</returns>
            public static string[] GetAllDates(CalendarEnum calendar)
            {
               Stream stream = null;
               string[]retVal = null;

               FileIOPermission fp = new FileIOPermission(PermissionState.Unrestricted);
               fp.Assert();

               try
               {               
                  stream = GetStreamFromAssembly("ProblemDate.xml");
                  XmlElement[] xElemt = GetElementsFromXML(stream,"DateTime",false,calendar.ToString());
                  retVal = new string[xElemt.Length];
                  for(int t = 0; t < xElemt.Length; t++)
                  {
                     retVal[t] = xElemt[t].InnerText;
                  }
               }
               catch(Exception e)
               { 
                  throw new Exception("(rethrowing exception, see inner Exception)", e);
               }
               finally
               {
                  if (stream != null)
                     stream.Close();
                  System.Security.CodeAccessPermission.RevertAssert();
               }

               return retVal;
            }


            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public static string[] GetAllDates()
            {
               Stream stream = null;
               string[] retVal = null;

               FileIOPermission fp = new FileIOPermission(PermissionState.Unrestricted);
               fp.Assert();

               try
               {
                  stream = GetStreamFromAssembly("ProblemDate.xml");
                  XmlElement[] xElemt = GetElementsFromXML(stream,"DateTime",false,"Calendars");
                  retVal = new string[xElemt.Length];
                  for(int t = 0; t < xElemt.Length; t++)
                  {
                     retVal[t] = xElemt[t].InnerText;
                  }
               }
               catch(Exception e)
               {
                  throw new Exception("(rethrowing exception, see inner Exception)", e);
               }
               finally
               {
                  if (stream!=null)
                     stream.Close();
                  System.Security.CodeAccessPermission.RevertAssert();
               }

               return retVal;
            }


            /// <summary>
            /// Get a date (identified as problematic or not) based on an Calendar and an index.
            /// </summary>
            /// <param name="calendar">(CalendarEnum) Get a string from the requested Calendar type</param>
            /// <param name="index">(int) The index of the date to return</param>
            /// <param name="valid">(bool) Get a string from the Valid collection</param>
            /// <returns>(string) A string containing the requested date</returns>
            public static string GetDate(CalendarEnum calendar, int index, bool valid)
            {
               // check the argument
               if(index<0)
                  throw(new ArgumentOutOfRangeException("(AutoData::GetDate)The index cannot be negative !"));

               string[] dates = GetAllDates(calendar, valid);
               if (index > dates.Length)
                  throw(new ArgumentOutOfRangeException("index",index,"(AutoData::GetDate) 'index' must be less than " + dates.Length));

               return dates[index];
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="index"></param>
            /// <param name="valid"></param>
            /// <returns></returns>
            public static string GetDate(int index, bool valid)
            {
               // retrieve the active calendar and forward call
               return GetDate(CurrentCalendar, index, valid);
            }
       
            /// <summary>
            /// 
            /// </summary>
            /// <param name="valid"></param>
            /// <returns></returns>
            public static string GetDate(bool valid)
            {
               string[] dates = GetAllDates(CurrentCalendar,valid);
               int index = m_rnd.Next(dates.Length);

               return dates[index];
            }

         #endregion

         #region GetElementsFromXML (9 overloads)

            /// <summary>
            /// Extracts elements from XML given a tagname, can allow partial matches or specific parent-tag requirements
            /// </summary>
            /// <param name="XmlElem">Root element for performing search</param>
            /// <param name="TagName">Name of tag to match</param>
            /// <param name="PartialMatch">Whether to allow partial matches</param>
            /// <param name="UnderTag">Specific parent tag requirement</param>
            /// <returns>Array of 0..N matching elements</returns>
            public static XmlElement[] GetElementsFromXML(XmlElement XmlElem, string TagName, bool PartialMatch, string UnderTag)
            {
               XmlElement[]retVal = null;
               if (UnderTag == null)
               {
                  XmlNode[] nodes = GetElementsByTagName((XmlNode)XmlElem, TagName, PartialMatch);
                  retVal = new XmlElement[nodes.Length];
                  for(int t = 0 ; t < nodes.Length; t++)
                  {
                     retVal[t] = (XmlElement)nodes[t];//.InnerText;
                  }
                  return retVal;
               }
               else
               {
                  // Find All Tags
                  XmlNode[] NodeArray = new XmlNode[0];
                  XmlNode[] nodes = GetElementsByTagName((XmlNode)XmlElem, UnderTag, false);
                  foreach(XmlNode node in nodes)
                  {
                     // Try to find the specified tag under that node
                     XmlNode[] tempResult = GetElementsByTagName(node, TagName,PartialMatch);
                     NodeArray = concatXmlNodeArray(NodeArray, tempResult);
                  }
                  
                  retVal = new XmlElement[NodeArray.Length];
                  for(int t = 0; t < NodeArray.Length; t++)
                  {
                     retVal[t] = (XmlElement)NodeArray[t];
                  }

                  return retVal;
               }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="TagName"></param>
            /// <param name="PartialMatch"></param>
            /// <param name="UnderTag"></param>
            /// <returns></returns>
            public static XmlElement[] GetElementsFromXML(Stream stream, string TagName, bool PartialMatch, string UnderTag)
            {
               XmlDocument xmlDoc = new XmlDocument();
               xmlDoc.Load(stream);
               return GetElementsFromXML(xmlDoc.DocumentElement,TagName, PartialMatch, UnderTag);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="FileName"></param>
            /// <param name="TagName"></param>
            /// <param name="PartialMatch"></param>
            /// <param name="UnderTag"></param>
            /// <returns></returns>
            public static XmlElement[] GetElementsFromXML(string FileName, string TagName, bool PartialMatch, string UnderTag)
            {
               XmlDocument xmlDoc = new XmlDocument();
               xmlDoc.Load(FileName);
               return GetElementsFromXML(xmlDoc.DocumentElement,TagName, PartialMatch, UnderTag);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="XmlElem"></param>
            /// <param name="TagName"></param>
            /// <param name="PartialMatch"></param>
            /// <returns></returns>
            public static XmlElement[] GetElementsFromXML(XmlElement XmlElem, string TagName, bool PartialMatch)
            {
               return GetElementsFromXML(XmlElem,TagName,PartialMatch, null);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="TagName"></param>
            /// <param name="PartialMatch"></param>
            /// <returns></returns>
            public static XmlElement[] GetElementsFromXML(Stream stream, string TagName, bool PartialMatch)
            {
               return GetElementsFromXML(stream, TagName, PartialMatch, null);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="FileName"></param>
            /// <param name="TagName"></param>
            /// <param name="PartialMatch"></param>
            /// <returns></returns>
            public static XmlElement[] GetElementsFromXML(string FileName, string TagName, bool PartialMatch)
            {
               return GetElementsFromXML(FileName, TagName, PartialMatch, null);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="XmlElem"></param>
            /// <param name="TagName"></param>
            /// <returns></returns>
            public static XmlElement[] GetElementsFromXML(XmlElement XmlElem, string TagName)
            {
               return GetElementsFromXML(XmlElem, TagName, false, null);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="TagName"></param>
            /// <returns></returns>
            public static XmlElement[] GetElementsFromXML(Stream stream, string TagName)
            {
               return GetElementsFromXML(stream, TagName, false, null);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="FileName"></param>
            /// <param name="TagName"></param>
            /// <returns></returns>
            public static XmlElement[] GetElementsFromXML(string FileName, string TagName)
            {
               return GetElementsFromXML(FileName, TagName, false, null);
            }

         #endregion

         #region GetStringsFromXML (9 overloads)

            /// <summary>
            /// 
            /// </summary>
            /// <param name="xmlElem"></param>
            /// <param name="TagName"></param>
            /// <param name="PartialMatch"></param>
            /// <param name="UnderTag"></param>
            /// <returns></returns>
            public static string[] GetStringsFromXML(XmlElement xmlElem, string TagName, bool PartialMatch, string UnderTag)
            {
               string[] retVal = null;
               XmlElement[] elements = GetElementsFromXML(xmlElem, TagName, PartialMatch, UnderTag);
               retVal = new String[elements.Length];
               for(int t = 0; t < elements.Length; t++)
               {
                  retVal[t] += elements[t].InnerText;
               }
               return retVal;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="TagName"></param>
            /// <param name="PartialMatch"></param>
            /// <param name="UnderTag"></param>
            /// <returns></returns>
            public static string[] GetStringsFromXML(Stream stream, string TagName, bool PartialMatch, string UnderTag)
            {
               string[] retVal = null;
               XmlElement[] elements = GetElementsFromXML(stream, TagName, PartialMatch, UnderTag);
               retVal = new String[elements.Length];
               for(int t = 0; t < elements.Length; t++)
               {
                  retVal[t] += elements[t].InnerText;
               }
               return retVal;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="FileName"></param>
            /// <param name="TagName"></param>
            /// <param name="PartialMatch"></param>
            /// <param name="UnderTag"></param>
            /// <returns></returns>
            public static string[] GetStringsFromXML(string FileName, string TagName, bool PartialMatch, string UnderTag)
            {
               string[] retVal = null;
               XmlElement[] elements = GetElementsFromXML(FileName, TagName, PartialMatch, UnderTag);
               retVal = new String[elements.Length];
               for(int t = 0; t < elements.Length; t++)
               {
                  retVal[t] += elements[t].InnerText;
               }
               return retVal;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="xmlElem"></param>
            /// <param name="TagName"></param>
            /// <param name="PartialMatch"></param>
            /// <returns></returns>
            public static string[] GetStringsFromXML(XmlElement xmlElem, string TagName, bool PartialMatch)
            {
               return GetStringsFromXML(xmlElem, TagName, PartialMatch, null);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="TagName"></param>
            /// <param name="PartialMatch"></param>
            /// <returns></returns>
            public static string[] GetStringsFromXML(Stream stream, string TagName, bool PartialMatch)
            {
               return GetStringsFromXML(stream, TagName, PartialMatch, null);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="FileName"></param>
            /// <param name="TagName"></param>
            /// <param name="PartialMatch"></param>
            /// <returns></returns>
            public static string[] GetStringsFromXML(string FileName, string TagName, bool PartialMatch)
            {
               return GetStringsFromXML(FileName, TagName, PartialMatch, null);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="xmlElem"></param>
            /// <param name="TagName"></param>
            /// <returns></returns>
            public static string[] GetStringsFromXML(XmlElement xmlElem, string TagName)
            {
               return GetStringsFromXML(xmlElem, TagName, false, null);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="TagName"></param>
            /// <returns></returns>
            public static string[] GetStringsFromXML(Stream stream, string TagName)
            {
               return GetStringsFromXML(stream, TagName, false, null);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="FileName"></param>
            /// <param name="TagName"></param>
            /// <returns></returns>
            public static string[] GetStringsFromXML(string FileName, string TagName)
            {
               return GetStringsFromXML(FileName, TagName, false, null);
            }

         #endregion

         #region GetValidFontName

            /// <summary>
            /// Retrieve a Valid font for the current language (Script Type)
            /// </summary>
            /// <returns>(string) A Valid font for the current language (Script Type)</returns>
            public static string GetValidFontName()
            {
                return GetValidFontName(CurrentScriptType);
            }

            /// <summary>
            /// Retrieve a Valid font for the specified language (Script Type)
            /// </summary>
            /// <param name="scriptType">(ScriptTypeEnum) The script type to retrieve valid font for</param>
            /// <returns>(string) A Valid font for the specified language (Script Type)</returns>
            public static string GetValidFontName(ScriptTypeEnum scriptType)
            {
                Stream stream = null;
                try
                {
                    stream = GetStreamFromAssembly("ProblemString.xml");
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(stream);
                    XmlNode node = xmlDoc.SelectSingleNode("descendant::ScriptType[@type='" + scriptType.ToString() + "']/FontInfo/Implemented/Font/@Name");
                    return (node == null) ? null : node.InnerText;
                }
                catch(Exception e)
                {
                    //rethrow exception
                    throw new Exception("Exception occured, see inner Exception", e);
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                }
            }

         #endregion GetValidFontName

         #region GetInvalidFontName
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public static string GetInvalidFontName()
            {
                return GetInvalidFontName(CurrentScriptType);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="scriptType"></param>
            /// <returns></returns>
            public static string GetInvalidFontName(ScriptTypeEnum scriptType)
            {
                Stream stream = null;
                try
                {
                    stream = GetStreamFromAssembly("ProblemString.xml");
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(stream);
                    XmlNode node = xmlDoc.SelectSingleNode("descendant::ScriptType[@type='" + scriptType.ToString() + "']/FontInfo/NotImplemented/Font/@Name");
                    return (node == null) ? null : node.InnerText;
                }
                catch(Exception e)
                {
                    //rethrow exception
                    throw new Exception("Exception occured, see inner Exception", e);
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                }
            }
         #endregion GetInvalidFontName

         #region GetStringUsingXPath (2 overloads)

            /// <summary>
            /// Retrieve Node InnerText using an xPath
            /// </summary>
            /// <param name="stream">(Stream) the stream containing the xml</param>
            /// <param name="xPath">(String) The xPath to be applied on the xml root</param>
            /// <returns>(string[]) All strings satisfying the xPath</returns>
            static public string[] GetStringsUsingXPath(Stream stream, string xPath)
            {
                if(stream == null)
                {
                    throw new ArgumentNullException("stream", "A valid Stream must be passed in (null was provided)");
                }
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(stream);
                return GetStringsUsingXPath(xmlDoc, xPath);
            }

            /// <summary>
            /// Retrieve Node InnerText using an xPath
            /// </summary>
            /// <param name="node">(System.Xml.XmlNode) The XML node where to start the search</param>
            /// <param name="xPath">(string) The xPath to be applied on the xml node</param>
            /// <returns>(string[]) All strings satisfying the xPath</returns>
            static public string[] GetStringsUsingXPath(XmlNode node, string xPath)
            {
                string []retVal = null;

                // check Params
                if(node == null)
                {
                    throw new ArgumentNullException("node", "A valid XmlNode must be passed in (null was provided)");
                }
                if(xPath == null)
                {
                    throw new ArgumentNullException("xPath", "A valid String must be passed in (null was provided)");
                }

                XmlNodeList children = node.SelectNodes(xPath);
                if(children.Count != 0)
                {
                    retVal = new string[children.Count];
                    for(int t = 0; t < children.Count; t++)
                    {
                        retVal[t] = children[t].InnerText;
                    }
                }

                return retVal;
            }
         #endregion GetStringUsingXPath (2 overloads)
         #region GetExceptionString

       /// <summary>
       /// Extracts exception string from managed resource given a particular culture.
       /// </summary>
       /// <param name="key">index of resource string in assembly</param>
       /// <param name="assembly">Assembly to extract string from</param>
       /// <returns>Expanded and localized exception string</returns>
       static public string GetExceptionString(string key, Assembly assembly)
       {
           System.Resources.ResourceManager rm = new System.Resources.ResourceManager(String.Format("FxResources.{0}.SR", assembly.GetName().Name), assembly);
           CultureInfo ci = Thread.CurrentThread.CurrentUICulture;
           string message = rm.GetString(key,ci);
           if (message != null)
               return message;
           else
               throw new Exception("Can't get Exception Message.");
       }

       /// <summary>
       /// Extracts resource string from any table in managed assembly, possibly even the exception stringtable.  Badly titled.
       /// </summary>
       /// <param name="key">Index of resource in table</param>
       /// <param name="table">String table identity</param>
       /// <param name="assembly">Managed assembly to load resources from</param>
       /// <returns>Expanded and culture correct string value</returns>
       static public string GetExceptionString(string key, string table, Assembly assembly)
       {
           System.Resources.ResourceManager rm = new System.Resources.ResourceManager(table, assembly);
           CultureInfo ci = Thread.CurrentThread.CurrentUICulture;
           string message = rm.GetString(key, ci);

           if (message != null)
               return message;
           else
               throw new Exception("Can't get Exception Message.");
       }
#endregion
      #endregion
       #region Helper Function (private)

         /// <summary>
         /// 
         /// </summary>
         /// <returns></returns>
         static private Stream GetStreamFromAssembly(string FileNameAndExtension)
         {
            Stream stream = null;
            Assembly assembly =null;

            FileIOPermission fp = new FileIOPermission(PermissionState.Unrestricted);
            fp.Assert();

            if(XmlFileLocation != null)
            {
               try
               {
                  // Load an external XML file (Path & name specified by XmlFileLocation)
                  stream = new FileStream(XmlFileLocation + FileNameAndExtension,FileMode.Open);
                  System.Security.CodeAccessPermission.RevertAssert();
               }
               catch(FileNotFoundException exp)
               {
                  // File not found in the provided path, rethrow
                  System.Security.CodeAccessPermission.RevertAssert();
                  throw new FileNotFoundException("(AutoData::GetStreamFromAssembly) The file '" + FileNameAndExtension + "' located at the specified Location (" + XmlFileLocation + ") does not exist.", exp);
               }
               finally
               {
                  System.Security.CodeAccessPermission.RevertAssert();
               }
            }
            else
            {
                try
                {
                    assembly = Assembly.GetEntryAssembly();
                    if(assembly != null)
                    {
                        // Check if file exist
                        if(File.Exists(assembly.Location + @"\" + FileNameAndExtension))
                        {
                            // Extract the stream
                            stream = GetXmlFileSteam(assembly,FileNameAndExtension);
                        }
                    }
                    if (stream == null)
                    {
                        assembly = Assembly.GetCallingAssembly();
                        if(assembly != null)
                        {
                            // Check if file exist
                            if(File.Exists(assembly.Location + @"\" + FileNameAndExtension))
                            {
                                // Extract the stream
                                stream = GetXmlFileSteam(assembly,FileNameAndExtension);
                            }
                        }
                    }
                    if (stream == null)
                    {
                        assembly = Assembly.GetExecutingAssembly();
                        if(assembly != null)
                        {
                            // Check if file exist
                            if(File.Exists(assembly.Location + @"\" + FileNameAndExtension))
                            {
                                // Extract the stream
                                stream = GetXmlFileSteam(assembly,FileNameAndExtension);
                            }
                            else
                            {
                                // Get the embedded stream
                                stream = assembly.GetManifestResourceStream("" + FileNameAndExtension);
                            }
                        }
                    }
                    if (stream == null)
                    {
                        throw new Exception("Resource '" + FileNameAndExtension + "' not found or empty; Probably wrong compilation, use /resource switch");
                    }
                }
                catch(Exception e)
                {
                    // Rethrowing exception
                    throw new Exception("Rethrowing Exception, see inner exception for detail", e);

                }
                finally
                {
                    System.Security.CodeAccessPermission.RevertAssert();
                }
            }

            return stream;
         }
         /// <summary>
         /// 
         /// </summary>
         /// <param name="assembly"></param>
         /// <param name="FileName"></param>
         /// <returns>(Caller responsible for closing the stream)</returns>
         static private Stream GetXmlFileSteam(Assembly assembly, string FileName)
         {
//            StreamReader retVal = null;
            Stream stream = null;
            string path = assembly.Location;

            // truncate the string to get only the path.
            char[] anyOf = {'/','\\'};
            int pos = path.LastIndexOfAny(anyOf);
            path = path.Substring(0,pos+1);
            // add the specified file name to the path and get the stream.
            path += FileName;

            FileIOPermission fp = new FileIOPermission(PermissionState.Unrestricted);
            fp.Assert();

            try
            {
               stream = System.IO.File.OpenRead(path);
               //                 stream = new FileStream(path,FileMode.Open);
               //                 retVal = new StreamReader(stream);
            }
            catch (FileNotFoundException exp)
            {
               throw new FileNotFoundException("GetXmlFileSteam failed to open the requested stream.",exp);
            }
            finally
            {
               System.Security.CodeAccessPermission.RevertAssert();
            }
/*
             finally
             {
                 if (stream != null)
                 {
                     stream.Close();
                 }
             }
*/             

  //           return retVal.BaseStream;
             return stream;
         }

         /// <summary>
         /// 
         /// </summary>
         /// <param name="stream"></param>
         /// <param name="paramNames"></param>
         /// <returns></returns>
         static private string[] GetStringsFromStream(Stream stream, params object[] paramNames)
         {

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(stream);
            XmlElement xmlRoot = xmlDoc.DocumentElement;
            XmlNodeList nodeList = xmlRoot.ChildNodes;            
            return GetStringsFromXMLNode(nodeList, paramNames);
         }


         /// <summary>
         /// Returns the elements located at a the specified position under the node
         /// </summary>
         /// <param name="nodeList"></param>
         /// <param name="paramNames"></param>
         /// <returns></returns>
         static private string[] GetStringsFromXMLNode(XmlNodeList nodeList, params object[] paramNames)
         {
            foreach(XmlNode node in nodeList)
            {
 
               // Check if this is the Language/Calendar we want
               if (node.Name.ToUpper().IndexOf(paramNames[0].ToString().ToUpper()) != -1)
               { 
                  if (paramNames.Length == 1)
                  {
                     // create the array of string
                     XmlNodeList childList = node.ChildNodes;
                     string[] childArray = new string[childList.Count];
                     for( int t = 0, count = 0; t < childList.Count; t++)
                     {
                         // Fix Bug #4167 : Filter out the comments
                         if(childList[t].NodeType != System.Xml.XmlNodeType.Comment)
                         {
                             childArray[count] = childList[t].InnerText;
                             count++;
                         }
                     }
                     return childArray;
                  }
                  // pop the first param and call recursively
                  object[] newParam = new object[paramNames.Length - 1];
                  for(int t=0; t < newParam.Length; t++)
                  {
                     newParam[t] = paramNames[t+1];
                  }
                  return GetStringsFromXMLNode(node.ChildNodes, newParam);
               }
            }
            // I will not throw because it would make the code full of try/catch and rethrow ...
            return null;
         }


         #region Used by GetElementsFromXML

            /// <summary>
            /// 
            /// </summary>
            /// <param name="XmlNode"></param>
            /// <param name="TagName"></param>
            /// <param name="PartialMatch"></param>
            /// <returns></returns>
            private static XmlNode[] GetElementsByTagName(XmlNode XmlNode, string TagName, bool PartialMatch)
            {
               XmlNode[] retVal = null;

               if ( String.Compare(XmlNode.Name, TagName,true) == 0 || 
                  ( PartialMatch == true && 
                  XmlNode.Name.ToUpper().IndexOf(TagName.ToUpper()) != -1 )
                  )
               {
                  retVal = new XmlNode[1];
                  retVal[0] = XmlNode;
               }
               else
               {
                  retVal = new XmlNode[0];
               }

               if(XmlNode.HasChildNodes == true)
               {
                  foreach(XmlNode Node in XmlNode.ChildNodes)
                  {
                     XmlNode[] temp = GetElementsByTagName(Node,TagName,PartialMatch);
                     retVal = concatXmlNodeArray(retVal, temp);
                  }
               }

               return retVal;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="firstArray"></param>
            /// <param name="secondArray"></param>
            /// <returns></returns>
            private static  XmlNode[] concatXmlNodeArray(XmlNode[] firstArray, XmlNode[] secondArray)
            {
               XmlNode[] retVal = new XmlNode[firstArray.Length + secondArray.Length];
               int t = 0;
               for(t = 0; t < firstArray.Length; t++)
               {
                  retVal[t] = firstArray[t];
               }
               for(int i = 0; i < secondArray.Length; i++)
               {
                  retVal[i+t] = secondArray[i];
               }
               return retVal;
            }

         #endregion

      #endregion
       }

       /// <summary>
       /// Handles logging duties for AutoData class.
       /// </summary>
       public class Logger
       {
        /// <summary>
        /// 
        /// </summary>
        public Logger() { }
        #region ConvertToHex
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static public string ConvertToHex(string input)
        {
            if (input == null)
                throw new ArgumentNullException(input);
            string output = "";
            char[] temp = input.ToCharArray();
            for (int i = 0; i < temp.Length; i++)
            {
                int tempInt = Convert.ToInt32(temp[i], CultureInfo.InvariantCulture);
                string hex = tempInt.ToString("x", CultureInfo.InvariantCulture);
                output = output + "&#x" + hex.ToUpper(CultureInfo.InvariantCulture) + ";";
            }
            return output;
        }
        #endregion
       }
}
