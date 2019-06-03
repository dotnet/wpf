// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Editing functionality for collection views.
//
// See spec at http://sharepoint/sites/wpftsv/Documents/DataGrid/DataGrid_CollectionView.mht
//

using System;
using System.Collections.ObjectModel;   // ReadOnlyCollection<T>

namespace System.ComponentModel
{
/// <summary>
/// IItemProperties is an interface that a collection view
/// can implement to expose information about the properties available on
/// items in the underlying collection.
/// </summary>
public interface IItemProperties
{
    /// <summary>
    /// Returns information about the properties available on items in the
    /// underlying collection.  This information may come from a schema, from
    /// a type descriptor, from a representative item, or from some other source
    /// known to the view.
    /// </summary>
    ReadOnlyCollection<ItemPropertyInfo>    ItemProperties { get; }
}

/// <summary>
/// Information about a property.  Returned by <seealso cref="IItemProperties.ItemProperties"/>
/// </summary>
public class ItemPropertyInfo
{
    /// <summary> Creates a new instance of ItemPropertyInfo. </summary>
    public ItemPropertyInfo(string name, Type type, object descriptor)
    {
        _name = name;
        _type = type;
        _descriptor = descriptor;
    }

    /// <summary> The property's name. </summary>
    public string  Name { get { return _name; } }

    /// <summary> The property's type. </summary>
    public Type    PropertyType { get { return _type; } }

    /// <summary> More information about the property.  This may be null,
    /// the view is unable to provide any more information.  Or it may be
    /// an object that describes the property, such as a PropertyDescriptor,
    /// a PropertyInfo, or the like.
    /// </summary>
    public object  Descriptor { get { return _descriptor; } }

    string _name;
    Type _type;
    object _descriptor;
}
}
