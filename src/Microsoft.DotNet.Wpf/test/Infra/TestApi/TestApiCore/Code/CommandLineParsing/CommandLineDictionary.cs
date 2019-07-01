// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.Test.CommandLineParsing
{
    /// <summary>
    /// Represents a dictionary that is aware of command line input patterns. All lookups for keys ignore case.
    /// </summary>
    ///
    /// <example>
    /// The example below demonstrates parsing a command line such as "Test.exe /verbose /runId=10"
    /// <code>
    /// CommandLineDictionary d = CommandLineDictionary.FromArguments(args);
    ///
    /// bool verbose = d.ContainsKey("verbose");
    /// int runId = Int32.Parse(d["runId"]);
    /// </code>
    /// </example>
    ///
    /// <example>
    /// You can also explicitly provide key and value identifiers for the cases
    /// that use other characters (rather than '/' and '=') as key/value identifiers. 
    /// The example below demonstrates parsing a command line such as "Test.exe -verbose -runId:10"
    /// <code>
    /// CommandLineDictionary d = CommandLineDictionary.FromArguments(args, '-', ':');  
    ///
    /// bool verbose = d.ContainsKey("verbose");
    /// int runId = Int32.Parse(d["runId"]);
    /// </code>
    /// </example>
    [Serializable]
    public class CommandLineDictionary : Dictionary<string, string>
    {
        #region Constructors

        /// <summary>
        /// Create an empty CommandLineDictionary using the default key/value
        /// separators of '/' and '='.
        /// </summary>
        public CommandLineDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        {
            KeyCharacter = '/';
            ValueCharacter = '=';
        }

        /// <summary>
        /// Creates a dictionary using a serialization info and context. This
        /// is used for Xml deserialization and isn't normally called from user code.
        /// </summary>
        /// <param name="info">Data needed to deserialize the dictionary.</param>
        /// <param name="context">Describes source and destination of the stream.</param>
        protected CommandLineDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Initializes a new instance of the CommandLineDictionary class, populating a 
        /// dictionary with key/value pairs from a command line that supports syntax 
        /// where options are provided in the form "/key=value".
        /// </summary>
        /// <param name="arguments">Key/value pairs.</param>
        /// <returns></returns>
        public static CommandLineDictionary FromArguments(IEnumerable<string> arguments)
        {
            return FromArguments(arguments, '/', '=');
        }

        /// <summary>
        /// Creates a dictionary that is populated with key/value pairs from a command line 
        /// that supports syntax where options are provided in the form "/key=value". 
        /// This method supports the ability to specify delimiter characters for options in 
        /// the command line.
        /// </summary>
        /// <param name="arguments">Key/value pairs.</param>
        /// <param name="keyCharacter">A character that precedes a key.</param>
        /// <param name="valueCharacter">A character that separates a key from a value.</param>
        /// <returns></returns>
        public static CommandLineDictionary FromArguments(IEnumerable<string> arguments, char keyCharacter, char valueCharacter)
        {
            CommandLineDictionary cld = new CommandLineDictionary();
            cld.KeyCharacter = keyCharacter;
            cld.ValueCharacter = valueCharacter;
            foreach (string argument in arguments)
            {
                cld.AddArgument(argument);
            }

            return cld;
        }

        #endregion

        #region Override Members

        /// <summary>
        /// Converts dictionary contents to a command line string of key/value pairs.
        /// </summary>
        /// <returns>Command line string.</returns>
        public override string ToString()
        {
            string commandline = String.Empty;
            foreach (KeyValuePair<String, String> pair in this)
            {
                if (!string.IsNullOrEmpty(pair.Value))
                {
                    commandline += String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}{3} ", KeyCharacter, pair.Key, ValueCharacter, pair.Value);
                }
                else // There is no value, so we just serialize the key
                {
                    commandline += String.Format(CultureInfo.InvariantCulture, "{0}{1} ", KeyCharacter, pair.Key);
                }
            }
            return commandline.TrimEnd();
        }

        #endregion

        #region Protected Members

        /// <summary>
        /// Populates a SerializationInfo with data needed to serialize the dictionary.
        /// This is used by Xml serialization and isn't normally called from user code.
        /// </summary>
        /// <param name="info">SerializationInfo object to populate.</param>
        /// <param name="context">StreamingContext to populate data from.</param>
        [SuppressMessage("Microsoft.Security", "CA2123")]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Character to treat as the key character in the value line.
        /// If the arguments should be of the form /Foo=Bar, then the
        /// key character is /. (Which is the default)
        /// </summary>
        private char KeyCharacter { get; set; }

        /// <summary>
        /// Character to treat as the value character in the value line.
        /// If the arguments should be of the form /Foo=Bar, then the
        /// value character is =. (Which is the default)
        /// </summary>
        private char ValueCharacter { get; set; }

        /// <summary>
        /// Adds the specified argument to the dictionary
        /// </summary>
        /// <param name="argument">Key/Value pair argument.</param>
        private void AddArgument(string argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException("argument");
            }

            string key;
            string value;

            if (argument.StartsWith(KeyCharacter.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                string[] splitArg = argument.Substring(1).Split(ValueCharacter);

                //Key is extracted from first element
                key = splitArg[0];

                //Reconstruct the value. We could also do this using substrings.
                if (splitArg.Length > 1)
                {
                    value = string.Join("=", splitArg, 1, splitArg.Length - 1);
                }
                else
                {
                    value = string.Empty;
                }
            }
            else
            {
                throw new ArgumentException("Unsupported value line argument format.", argument);
            }

            Add(key, value);
        }

        #endregion
    }
}

