// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Formats.Nrbf;
using System.IO;
using System.Private.Windows.Nrbf;
using System.Reflection.Metadata;

namespace System.Windows.Nrbf;

internal sealed class WpfNrbfSerializer : INrbfSerializer
{
    // This class is currently just a pass-through to the core NrbfSerializer. It could be extended to handle
    // binary formatted bitmaps and other WPF specific binary formatted types.

    // private static Dictionary<TypeName, Type>? s_knownTypes;

    // Do not allow construction of this type.
    private WpfNrbfSerializer() { }

    public static bool TryBindToType(TypeName typeName, [NotNullWhen(true)] out Type? type)
    {
        return CoreNrbfSerializer.TryBindToType(typeName, out type);

        // Should be able to fall back to handle binary formatted bitmaps. Could add whatever is needed in
        // SystemDrawingExtension, or better yet, factor the logic into System.Private.Windows.Core.GdiPlus and
        // take a dependency on it (as there is already a hard dependency on System.Drawing.Common).

        // s_knownTypes ??= new(2, TypeNameComparer.FullNameAndAssemblyNameMatch)
        // {
        //     { Types.ToTypeName($"{Types.BitmapType}, System.Drawing"), typeof(Bitmap) },
        //     { Types.ToTypeName($"{Types.BitmapType}, System.Drawing.Common"), typeof(Bitmap) }
        // };

        // return s_knownTypes.TryGetValue(typeName, out type);
    }

    public static bool TryGetObject(SerializationRecord record, [NotNullWhen(true)] out object? value) =>
        CoreNrbfSerializer.TryGetObject(record, out value);
    // This should be relatively easy to implement, see full comments above.
    // || TryGetBitmap(record, out value);

    public static bool TryWriteObject(Stream stream, object value) =>
        CoreNrbfSerializer.TryWriteObject(stream, value);
    // || BinaryFormatWriter.TryWriteObject(stream, value);

    public static bool IsFullySupportedType(Type type) => CoreNrbfSerializer.IsFullySupportedType(type);
    // || type == typeof(Bitmap);
}
