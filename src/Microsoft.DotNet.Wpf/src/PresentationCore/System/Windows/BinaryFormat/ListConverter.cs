// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;

namespace System.Windows
{
    internal static class ListConverter
    {
  
    public static ListConverter<object?> GetPrimitiveConverter(
        IList values,
        StringRecordsCollection strings) => new(
            values,
            (object? value) => value switch
            {
                null => ObjectNull.Instance,
                string stringValue => strings.GetStringRecord(stringValue),
                _ => new MemberPrimitiveTyped(value)
            });
    }
}
