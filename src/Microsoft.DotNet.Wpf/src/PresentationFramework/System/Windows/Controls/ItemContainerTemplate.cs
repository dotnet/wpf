// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Windows.Markup;

namespace System.Windows.Controls
{
    /// <summary>
    ///   The template that produces a container for an ItemsControl
    /// </summary>
    [DictionaryKeyProperty("ItemContainerTemplateKey")]
    public class ItemContainerTemplate : DataTemplate
    {
        /// <summary>
        ///     The key that will be used if the ItemContainerTemplate is added to a
        ///     ResourceDictionary in Xaml without a specified Key (x:Key).
        /// </summary>
        public object ItemContainerTemplateKey
        {
            get
            {
                return (DataType != null) ? new ItemContainerTemplateKey(DataType) : null;
            }
        }
    }
}
