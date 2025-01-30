// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// Description: Resource key for a DataTemplate
//

namespace System.Windows
{
    /// <summary> Resource key for a DataTemplate</summary>
    public class DataTemplateKey : TemplateKey
    {
        /// <summary> Constructor</summary>
        /// <remarks>
        /// When constructed without dataType (e.g. in XAML),
        /// the DataType must be specified as a property.
        /// </remarks>
        public DataTemplateKey()
            : base(TemplateType.DataTemplate)
        {
        }

        /// <summary> Constructor</summary>
        public DataTemplateKey(object dataType)
            : base(TemplateType.DataTemplate, dataType)
        {
        }
    }
}

