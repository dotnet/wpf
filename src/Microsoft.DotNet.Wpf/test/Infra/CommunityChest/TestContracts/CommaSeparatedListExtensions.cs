// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Microsoft.Test
{
    /// <summary>
    /// Provides a set of extensions for converting between collections and
    /// comma separated lists. Useful for communicating collections across
    /// restricted type boundaries.
    /// </summary>
    public static class CommaSeparatedListExtensions
    {
        /// <summary>
        /// Convert a collection to a comma separated list.
        /// int[] { 1, 2, 3 } => "1,2,3"
        /// Commas in the textual representation are escaped with
        /// a backslash, as is backslash itself.
        /// </summary>
        /// <param name="collection">Collection to convert.</param>
        /// <returns>Comma separated list representation.</returns>
        public static string ToCommaSeparatedList<T>(this IEnumerable<T> collection)
        {
            string commaSeparatedList = String.Empty;

            if (collection != null)
            {
                foreach (T item in collection)
                {
                    if (commaSeparatedList != String.Empty)
                    {
                        commaSeparatedList += ",";
                    }

                    string valueToWrite;
                    // If the item is not null, we'll find the appropriate
                    // typeconverter and convert to a string. If the item
                    // is null, we'll use String.Empty.
                    if (item != null)
                    {
                        TypeConverter typeConverter = TypeDescriptor.GetConverter(item);
                        if (typeConverter != null && typeConverter.CanConvertTo(typeof(string)))
                        {
                            valueToWrite = typeConverter.ConvertToInvariantString(item);
                        }
                        else
                        {
                            valueToWrite = item.ToString();
                        }

                        if (item is Type)
                        {
                            valueToWrite = (item as Type).AssemblyQualifiedName;
                        }
                    }
                    else
                    {
                        valueToWrite = String.Empty;
                    }

                    valueToWrite = valueToWrite.Replace(@"\", @"\\");
                    valueToWrite = valueToWrite.Replace(",", @"\,");

                    commaSeparatedList += valueToWrite;
                }
            }

            return commaSeparatedList;
        }

        /// <summary>
        /// Convert a comma separated list to a collection. There must be a
        /// TypeConverter for the collection type that can convert from a string.
        /// "1,2,3" => IEnumerable(int) containing 1, 2, and 3.
        /// Commas in the textual representation itself should be escaped with
        /// a blackslash, as should backslash itself.
        /// </summary>
        /// <typeparam name="T">Type of objects in the collection.</typeparam>
        /// <param name="commaSeparatedList">Comma separated list representation.</param>
        /// <returns>Collection of objects.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static IEnumerable<T> FromCommaSeparatedList<T>(this string commaSeparatedList)
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
    }
}