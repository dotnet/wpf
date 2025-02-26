// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Private.Windows.Ole;

namespace System.Windows;

/// <summary>
///  Represents a data format type.
/// </summary>
public sealed class DataFormat : IDataFormat<DataFormat>
{
    /// <summary>
    ///  Initializes a new instance of the DataFormat class and specifies format name and id.
    /// </summary>
    public DataFormat(string name, int id)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (name.Length == 0)
        {
            throw new ArgumentException(SR.DataObject_EmptyFormatNotAllowed); 
        }

        Name = name;
        Id = id;
    }

    /// <summary>
    ///  Specifies the name of this format.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///  Specifies the Id number for this format.
    /// </summary>
    public int Id { get; }

    static DataFormat IDataFormat<DataFormat>.Create(string name, int id) => new(name, id);
}
