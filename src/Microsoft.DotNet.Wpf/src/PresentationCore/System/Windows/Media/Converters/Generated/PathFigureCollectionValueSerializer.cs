// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using System.Windows.Media;
using System.Windows.Markup;

using ConverterHelper = System.Windows.Markup.TypeConverterHelper;

namespace System.Windows.Media.Converters
{
    /// <summary>
    /// PathFigureCollectionValueSerializer - ValueSerializer class for converting instances of strings to and from PathFigureCollection instances
    /// This is used by the MarkupWriter class.
    /// </summary>
    public class PathFigureCollectionValueSerializer : ValueSerializer 
    {
        /// <summary>
        /// Returns true.
        /// </summary>
        public override bool CanConvertFromString(string value, IValueSerializerContext context)
        {
            return true;
        }

        /// <summary>
        /// Returns true if the given value can be converted into a string
        /// </summary>
        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            // When invoked by the serialization engine we can convert to string only for some instances
            return value is PathFigureCollection pathFigureCollection && pathFigureCollection.CanSerializeToString();
        }

        /// <summary>
        /// Converts a string into a PathFigureCollection.
        /// </summary>
        public override object ConvertFromString(string value, IValueSerializerContext context)
        {
            return value is not null ? PathFigureCollection.Parse(value) : base.ConvertFromString(value, context);
        }

        /// <summary>
        /// Converts the value into a string.
        /// </summary>
        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            // When invoked by the serialization engine we can convert to string only for some instances
            if (value is not PathFigureCollection pathFigureCollection || !pathFigureCollection.CanSerializeToString())
            {
                // Let base throw an exception.
                return base.ConvertToString(value, context);
            }

            return pathFigureCollection.ConvertToString(null, ConverterHelper.InvariantEnglishUS);
        }
    }
}
