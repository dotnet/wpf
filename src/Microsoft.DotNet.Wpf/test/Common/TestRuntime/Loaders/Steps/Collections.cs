// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;


namespace Microsoft.Test.Utilities
{
    internal class HashtableUtils
    {
        /// <summary>
        /// Creates a Hashtable from a string array, suitable for analyzing command line parameters
        /// </summary>
        /// <param name="Pairs">string array where which string is of the form "parameter=value"</param>
        /// <param name="comparisonType">to make the hashtable case-(in)sensitive and/or culture invariant</param>
        /// <returns>a Hashtable object. It simply ignores pairs not of the form "parameter=value"</returns>
        static internal Hashtable HashtableFromStringArray(string[] Pairs, StringComparer comparisonType)
        {
            Hashtable table = new Hashtable();

            foreach (string pair in Pairs)
            {
                char[] seps = { '=' };
                string[] tokens = pair.Split(seps);

                if (tokens.Length != 2)
                {
                    continue;
                }

                table.Add(tokens[0], tokens[1]);
            }

            return (table);
        }
    }

    /// <summary>
    /// Hashtable that simulates duplicates keys
    /// </summary>
    internal class DuplicatesHashtable: Hashtable
    {
        /// <summary>
        /// Adds a key/value pair to the hashtable
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public override void Add(object key, object val)
        {
            // check if the key exists
            if (!ContainsKey(key))
            {
                // create a list for the key
                // performance: we can create the list, add the element and then add it to the hashtable
                // to avoid the lookup below
                base.Add(key, new ArrayList());
            }

            // add the value to the array list for that key
            ((ArrayList)this[key]).Add(val);
        }


        /// <summary>
        /// [,] operator returns the index-th object of the 'key' list
        /// </summary>
        /// <value>an object</value>
        internal object this[object key, int index]
        {
            get
            {
                if (base[key] == null)
                {
                    return (null);
                }
                return (((ArrayList)this[key])[index]);
            }
        }


        /// <summary>
        /// [] operator returns the 'key' list
        /// </summary>
        /// <value>an ArrayList</value>
        internal new ArrayList this[object key]
        {
            get
            {
                if (base[key] == null)
                {
                    return (null);
                }
                return ((ArrayList)base[key]);
            }
        }
    }

    /// <summary>
    /// Set contains useful helper methods for sets of things.
    /// </summary>
    internal class Set
    {
        /// <summary>
        /// ContainSameElements
        /// </summary>
        /// <param name="set1"></param>
        /// <param name="set2"></param>
        /// <returns>true if the sets have the quantity of each element, regardless of the order</returns>
        internal static bool ContainSameElements(object[] set1, object[] set2)
        {
            // find ocurrences of the 1st set in the 2nd
            foreach (object o in set1)
            {
                if (Ocurrences(set1, o) != Ocurrences(set2, o))
                {
                    return (false);
                }
            }

            // find ocurrences of the 2nd set in the 1st
            foreach (object o in set2)
            {
                if (Ocurrences(set1, o) != Ocurrences(set2, o))
                {
                    return (false);
                }
            }

            // sets are equal
            return (true);
        }

        /// <summary>
        /// Ocurrences
        /// </summary>
        /// <param name="set"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static int Ocurrences(object[] set, object obj)
        {
            int count = 0;
            foreach (object o in set)
            {
                if (o.Equals(obj))
                {
                    count++;
                }
            }
            return (count);
        }
    }

    /// <summary>
    /// KeyValuePairs: easy way to set and get variable/value pairs to a file
    /// </summary>
    internal class KeyValuePairs
    {
        /// <summary>
        /// _file
        /// </summary>
        static string _file = EnvironmentVariable.Get("HOMEDRIVE") + EnvironmentVariable.Get("HOMEPATH") + @"\KeyValuePairs.txt";

        /// <summary>
        /// _keyValueSeparator
        /// </summary>
        const string _keyValueSeparator = "=";

        /// <summary>
        /// Set
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        internal static void Set(string variable, string value)
        {
            // delete the file if older than a day
            const int hour = 3600;
            if (!FileHelper.Exists(_file))
            {
                FileHelper.DeleteIfOlderThan(_file, hour);
            }

            // position at the end of the file
            FileHandler fh = new FileHandler(_file, FileMode.Append, FileAccess.Write, FileShare.None);

            // write the pair
            fh.WriteLine(variable + _keyValueSeparator + value);

            // close file
            fh.Close();
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        internal static string Get(string variable)
        {
            // target value holder
            string value = null;

            // if file does not exist, return
            if (!FileHelper.Exists(_file))
            {
                return (null);
            }

            // open the file
            FileHandler r = new FileHandler(_file, FileMode.Open, FileAccess.Read, FileShare.Read);

            // read last line with the target variable, ignore the previous ones
            while (true)
            {
                string line = r.ReadLine();
                if (line == null)
                {
                    break;
                }

                // get the variable
                int firstSeparator = line.IndexOf(_keyValueSeparator, 0, line.Length);
                string var = line.Substring(0, firstSeparator);

                // is this the target variable?
                if (var != variable)
                {
                    continue;
                }

                // variable matches; get this value; discard the previous one
                value = line.Substring(firstSeparator + 1);
            }

            // close the file
            r.Close();

            // return the value
            return (value);
        }

        /// <summary>
        /// Clean
        /// </summary>
        internal static void Clean()
        {
            if (FileHelper.Exists(_file))
            {
                // clean the file
                File.Delete(_file);
            }
        }
    }
}
