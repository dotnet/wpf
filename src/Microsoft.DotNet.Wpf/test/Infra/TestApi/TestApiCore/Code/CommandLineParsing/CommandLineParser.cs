// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.Test.CommandLineParsing
{
    /// <summary>
    /// Provides utilities for parsing command-line values.
    /// </summary>
    /// 
    /// <example>
    /// The following example shows parsing of a command-line such as "Test.exe /verbose /runId=10"
    /// into a strongly-typed structure.
    /// <code>
    /// using System;
    /// using System.Linq;
    /// using Microsoft.Test.CommandLineParsing;
    /// 
    /// public class CommandLineArguments
    /// {
    ///     public bool? Verbose { get; set; }
    ///     public int? RunId { get; set; }
    /// }
    ///
    /// public class Program
    /// {
    ///     public static void Main(string[] args)
    ///     {
    ///         CommandLineArguments a = new CommandLineArguments();
    ///         a.ParseArguments(args);  // or CommandLineParser.ParseArguments(a, args);
    ///         
    ///         Console.WriteLine("Verbose: {0}, RunId: {1}", a.Verbose, a.RunId);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// The following example shows parsing of a command-line such as "Test.exe RUN /verbose /runId=10"
    /// into a strongly-typed Command, that can then be excuted.
    /// <code>
    /// using System;
    /// using System.Linq;
    /// using Microsoft.Test.CommandLineParsing;
    /// 
    /// public class RunCommand : Command
    /// {
    ///     public bool? Verbose { get; set; }
    ///     public int? RunId { get; set; }
    ///
    ///     public override void Execute()
    ///     {
    ///         Console.WriteLine("RunCommand: Verbose={0} RunId={1}", Verbose, RunId);  
    ///     }
    /// }
    ///
    /// public class Program
    /// {
    ///     public static void Main(string[] args)
    ///     {
    ///         if (String.Compare(args[0], "run", StringComparison.InvariantCultureIgnoreCase) == 0)
    ///         {
    ///             Command c = new RunCommand();
    ///             c.ParseArguments(args.Skip(1)); // or CommandLineParser.ParseArguments(c, args.Skip(1))
    ///             c.Execute();
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public static class CommandLineParser
    {
        #region Constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static CommandLineParser()
        {
            // The parser will want to convert from value line string arguments into various
            // data types on a value. Any type that doesn't have a default TypeConverter that
            // can convert from string to it's type needs to have a custom TypeConverter written
            // for it, and have it added here.
            TypeDescriptor.AddAttributes(typeof(DirectoryInfo), new TypeConverterAttribute(typeof(DirectoryInfoConverter)));
            TypeDescriptor.AddAttributes(typeof(FileInfo), new TypeConverterAttribute(typeof(FileInfoConverter)));
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Sets properties on an object from a series of key/value string
        /// arguments that are in the form "/PropertyName=Value", where the
        /// value is converted from a string into the property type.
        /// </summary>
        /// <param name="valueToPopulate">The object to set properties on.</param>
        /// <param name="args">The key/value arguments describing the property names and values to set.</param>
        /// <returns>
        /// Indicates whether the properties were successfully set.  Reasons for failure reasons include
        /// a property name that does not exist or a value that cannot be converted from a string.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown when one of the key/value strings cannot be parsed into a property.</exception>
        public static void ParseArguments(this object valueToPopulate, IEnumerable<string> args)
        {
            CommandLineDictionary commandLineDictionary = CommandLineDictionary.FromArguments(args);

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(valueToPopulate);

            // Ensure required properties are specified.
            foreach (PropertyDescriptor property in properties)
            {
                // See whether any of the attributes on the property is a RequiredAttribute.
                if (property.Attributes.Cast<Attribute>().Any(attribute => attribute is RequiredAttribute))
                {
                    // If so, and the command line dictionary doesn't contain a key matching
                    // the property's name, it means that a required property isn't specified.
                    if (!commandLineDictionary.ContainsKey(property.Name))
                    {
                        throw new ArgumentException("A value for the " + property.Name + " property is required.");
                    }
                }
            }

            foreach (KeyValuePair<string, string> keyValuePair in commandLineDictionary)
            {
                // Find a property whose name matches the kvp's key, ignoring case.
                // We can't just use the indexer because that is case-sensitive.                
                PropertyDescriptor property = MatchProperty(keyValuePair.Key, properties,valueToPopulate.GetType());

                // If the value is null/empty and the property is a bool, we
                // treat it as a flag, which means its presence means true.
                if (String.IsNullOrEmpty(keyValuePair.Value) &&
                    (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?)))
                {
                    property.SetValue(valueToPopulate, true);
                    continue;
                }

                object valueToSet;

                // We support a limited set of collection types. Setting a List<T>
                // is one of the most flexible types as it supports three different
                // interfaces, but the catch is that we don't support the concrete
                // Collection<T> type. We can expand it to support Collection<T>
                // in the future, but the code will get a bit uglier.
                switch (property.PropertyType.Name)
                {
                    case "IEnumerable`1":
                    case "ICollection`1":
                    case "IList`1":
                    case "List`1":
                        MethodInfo methodInfo = typeof(CommandLineParser).GetMethod("FromCommaSeparatedList", BindingFlags.Static | BindingFlags.NonPublic);
                        Type[] genericArguments = property.PropertyType.GetGenericArguments();
                        MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(genericArguments);
                        valueToSet = genericMethodInfo.Invoke(null, new object[] { keyValuePair.Value });
                        break;
                    default:
                        TypeConverter typeConverter = TypeDescriptor.GetConverter(property.PropertyType);
                        if (typeConverter == null || !typeConverter.CanConvertFrom(typeof(string)))
                        {
                            throw new ArgumentException("Unable to convert from a string to a property of type " + property.PropertyType + ".");
                        }
                        valueToSet = typeConverter.ConvertFromInvariantString(keyValuePair.Value);
                        break;
                }

                property.SetValue(valueToPopulate, valueToSet);
            }

            return;
        }

        /// <summary>
        /// Match the property to the specified keyName
        /// 
        /// If match cannot be found, throw an argument exception
        /// </summary>        
        private static PropertyDescriptor MatchProperty(string keyName, PropertyDescriptorCollection properties, Type targetType)
        {
            foreach(PropertyDescriptor prop in properties)
            {
                if(prop.Name.Equals(keyName, StringComparison.OrdinalIgnoreCase))
                {
                    return prop;
                }
            }
            throw new ArgumentException("A matching public property of name " + keyName + " on type " + targetType + " could not be found.");            
        }

        /// <summary>
        /// Prints names and descriptions for properties on the specified component.
        /// </summary>
        /// <param name="component">The component to print usage for.</param>
        public static void PrintUsage(object component)
        {
            IEnumerable<PropertyDescriptor> properties = TypeDescriptor.GetProperties(component).Cast<PropertyDescriptor>();
            IEnumerable<string> propertyNames = properties.Select(property => property.Name);
            IEnumerable<string> propertyDescriptions = properties.Select(property => property.Description);
            IEnumerable<string> lines = FormatNamesAndDescriptions(propertyNames, propertyDescriptions, Console.WindowWidth);

            Console.WriteLine("Possible arguments:");
            foreach (string line in lines)
            {
                Console.WriteLine(line);
            }
        }

        /// <summary>
        /// Prints a general summary of each command.
        /// </summary>
        /// <param name="commands">A collection of possible commands.</param>
        public static void PrintCommands(IEnumerable<Command> commands)
        {
            // Print out general descriptions for every command.
            IEnumerable<string> commandNames = commands.Select(command => command.Name);
            IEnumerable<string> commandDescriptions = commands.Select(command => command.GetAttribute<DescriptionAttribute>().Description);
            IEnumerable<string> lines = FormatNamesAndDescriptions(commandNames, commandDescriptions, Console.WindowWidth);

            Console.WriteLine("Possible commands:");
            foreach (string line in lines)
            {
                Console.WriteLine(line);
            }
        }

        /// <summary>
        /// Creates a string that represents key/value arguments for the properties of the 
        /// specified object. For example, an object with a name (string) of "example" and a 
        /// priority value (integer) of 1 translates to '/name=example  /priority=1'. This 
        /// can be used to send data structures through the command line.
        /// </summary>
        /// <param name="valueToConvert">Value to create key/value arguments from.</param>
        /// <returns>Space-delimited key/value arguments.</returns>
        public static string ToString(object valueToConvert)
        {
            IEnumerable<PropertyDescriptor> properties = TypeDescriptor.GetProperties(valueToConvert).Cast<PropertyDescriptor>();
            IEnumerable<PropertyDescriptor> propertiesOnParent = TypeDescriptor.GetProperties(valueToConvert.GetType().BaseType).Cast<PropertyDescriptor>();
            properties = properties.Except(propertiesOnParent);
            CommandLineDictionary commandLineDictionary = new CommandLineDictionary();

            foreach (PropertyDescriptor property in properties)
            {
                commandLineDictionary[property.Name] = property.GetValue(valueToConvert).ToString();
            }

            return commandLineDictionary.ToString();
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Given collections of names and descriptions, returns a set of lines
        /// where the description text is wrapped and left aligned. eg:
        /// First Name   this is a string that wraps around
        ///              and is left aligned.
        /// Second Name  this is another string.
        /// </summary>
        /// <param name="names">Collection of name strings.</param>
        /// <param name="descriptions">Collection of description strings.</param>
        /// <param name="maxLineLength">Maximum length of formatted lines</param>
        /// <returns>Formatted lines of text.</returns>
        private static IEnumerable<string> FormatNamesAndDescriptions(IEnumerable<string> names, IEnumerable<string> descriptions, int maxLineLength)
        {
            if (names.Count() != descriptions.Count())
            {
                throw new ArgumentException("Collection sizes are not equal", "names");
            }

            int namesMaxLength = names.Max(commandName => commandName.Length);

            List<string> lines = new List<string>();

            for (int i = 0; i < names.Count(); i++)
            {
                string line = names.ElementAt(i);
                line = line.PadRight(namesMaxLength + 2);

                foreach (string wrappedLine in WordWrap(descriptions.ElementAt(i), maxLineLength - namesMaxLength - 3))
                {
                    line += wrappedLine;
                    lines.Add(line);
                    line = new string(' ', namesMaxLength + 2);
                }
            }

            return lines;
        }

        /// <summary>
        /// Convert a comma separated list to a List of T. There must be a
        /// TypeConverter for the collection type that can convert from a string.
        /// "1,2,3" => List(int) containing 1, 2, and 3.
        /// Commas in the textual representation itself should be escaped with
        /// a blackslash, as should backslash itself.
        /// </summary>
        /// <typeparam name="T">Type of objects in the collection.</typeparam>
        /// <param name="commaSeparatedList">Comma separated list representation.</param>
        /// <returns>Collection of objects.</returns>
        private static List<T> FromCommaSeparatedList<T>(this string commaSeparatedList)
        {
            List<T> collection = new List<T>();

            TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(T));
            if (typeConverter.CanConvertFrom(typeof(string)))
            {
                StringBuilder builder = new StringBuilder();
                bool isEscaped = false;

                foreach (char character in commaSeparatedList)
                {
                    // If we are in escaped mode, add the character and exit escape mode
                    if (isEscaped)
                    {
                        builder.Append(character);
                        isEscaped = false;
                    }
                    // If we see the backslash and are not in escaped mode, go into escaped mode
                    else if (character == '\\' && !isEscaped)
                    {
                        isEscaped = true;
                    }
                    // A comma outside of escaped mode is an item separator, convert
                    // built string to T and add to collection, then zero out the builder
                    else if (character == ',' && !isEscaped)
                    {
                        collection.Add((T)typeConverter.ConvertFromInvariantString(builder.ToString()));
                        builder.Length = 0;
                    }
                    // Otherwise simply add the character
                    else
                    {
                        builder.Append(character);
                    }
                }

                // If builder.Length is non-zero, of course we want to add it.
                // If, however, it is zero, it can mean one of two things:
                // - There are no items at all, i.e. the commaSeparatedList string
                //   is null/empty, and we should return an empty collection.
                // - The builder just got flushed by a comma, and there is one last
                //   item in the collection to add that should be typeconverted
                //   from an empty string.
                // collection.Count is always 0 for the former and greater than 0
                // for the later, so we will also add if Count > 0.
                if (builder.Length > 0 || collection.Count > 0)
                {
                    collection.Add((T)typeConverter.ConvertFromInvariantString(builder.ToString()));
                }
            }

            return collection;
        }

        /// <summary>
        /// Gets an attribute on the specified object instance.
        /// </summary>
        /// <typeparam name="T">Type of attribute to get.</typeparam>
        /// <param name="value">Object instance to look for attribute on.</param>
        /// <returns>First instance of the specified attribute.</returns>
        private static T GetAttribute<T>(this object value) where T : Attribute
        {
            IEnumerable<Attribute> attributes = TypeDescriptor.GetAttributes(value).Cast<Attribute>();
            return (T)attributes.First(attribute => attribute is T);
        }

        /// <summary>
        /// Word wrap text for a specified maximum line length.
        /// </summary>
        /// <param name="text">Text to word wrap.</param>
        /// <param name="maxLineLength">Maximum length of a line.</param>
        /// <returns>Collection of lines for the word wrapped text.</returns>
        private static IEnumerable<string> WordWrap(string text, int maxLineLength)
        {
            List<string> lines = new List<string>();
            string currentLine = String.Empty;

            foreach (string word in text.Split(' '))
            {
                // Whenever adding the word would push us over the maximum
                // width, add the current line to the lines collection and
                // begin a new line. The new line starts with space padding
                // it to be left aligned with the previous line of text from
                // this column.
                if (currentLine.Length + word.Length > maxLineLength)
                {
                    lines.Add(currentLine);
                    currentLine = String.Empty;
                }

                currentLine += word;

                // Add spaces between words except for when we are at exactly the
                // maximum width.
                if (currentLine.Length != maxLineLength)
                {
                    currentLine += " ";
                }
            }

            // Add the remainder of the current line except for when it is
            // empty, which is true in the case when we had just started a
            // new line.
            if (currentLine.Trim() != String.Empty)
            {
                lines.Add(currentLine);
            }

            return lines;
        }

        #endregion
    }
}
