// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Private.Windows.Ole;
using System.Reflection.Metadata;
using System.Windows.Media.Imaging;

namespace System.Windows;

/// <summary>
///  Provides methods to place data on and retrieve data from the system clipboard.
/// </summary>
public static class Clipboard
{
    /// <summary>
    ///  Clear the system clipboard.
    /// </summary>
    public static void Clear() => ClipboardCore.Clear().ThrowOnFailure();

    /// <summary>
    ///  Return <see langword="true"/> if Clipboard contains the audio data. Otherwise, return <see langword="false"/>.
    /// </summary>
    public static bool ContainsAudio() => ContainsDataInternal(DataFormats.WaveAudio);

    /// <summary>
    ///  Return <see langword="true"/> if Clipboard contains the specified data format. Otherwise, return <see langword="false"/>.
    /// </summary>
    public static bool ContainsData(string format)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        return ContainsDataInternal(format);
    }

    /// <summary>
    ///  Return <see langword="true"/> if Clipboard contains the file drop list format. Otherwise, return <see langword="false"/>.
    /// </summary>
    public static bool ContainsFileDropList() => ContainsDataInternal(DataFormats.FileDrop);

    /// <summary>
    ///  Return <see langword="true"/> if Clipboard contains the image format. Otherwise, return <see langword="false"/>.
    /// </summary>
    public static bool ContainsImage() => ContainsDataInternal(DataFormats.Bitmap);

    /// <summary>
    ///  Return <see langword="true"/> if Clipboard contains the text data format which is unicode.
    ///  Otherwise, return <see langword="false"/>.
    /// </summary>
    public static bool ContainsText() => ContainsDataInternal(DataFormats.UnicodeText);

    /// <summary>
    ///  Return <see langword="true"/> if Clipboard contains the specified text data format which is unicode. 
    ///  Otherwise, return <see langword="false"/>.
    /// </summary>
    public static bool ContainsText(TextDataFormat format) => !DataFormats.IsValidTextDataFormat(format)
        ? throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat))
        : ContainsDataInternal(DataFormats.ConvertToDataFormats(format));

    /// <summary>
    ///  Permanently renders the contents of the last IDataObject that was set onto the clipboard.
    /// </summary>
    public static void Flush() => ClipboardCore.Flush().ThrowOnFailure();

    /// <summary>
    ///  Get audio data as Stream from Clipboard.
    /// </summary>
    public static Stream? GetAudioStream() => GetTypedDataIfAvailable<Stream>(DataFormatNames.WaveAudio);

    /// <summary>
    ///  Get data for the specified data format from Clipboard.
    /// </summary>
    public static object? GetData(string format)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        return GetDataInternal(format);
    }

    private static T? GetTypedDataIfAvailable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string format)
    {
        IDataObject? data = GetDataObject();
        if (data is ITypedDataObject typed)
        {
            return typed.TryGetData(format, autoConvert: true, out T? value) ? value : default;
        }

        if (data is IDataObject dataObject)
        {
            return dataObject.GetData(format, autoConvert: true) is T value ? value : default;
        }

        return default;
    }

    /// <summary>
    ///  Get the file drop list as StringCollection from Clipboard.
    /// </summary>
    public static StringCollection GetFileDropList()
    {
        StringCollection result = [];

        if (GetTypedDataIfAvailable<string[]?>(DataFormatNames.FileDrop) is string[] strings)
        {
            result.AddRange(strings);
        }

        return result;
    }

    /// <summary>
    ///  Get the image from Clipboard.
    /// </summary>
    public static BitmapSource? GetImage() => GetTypedDataIfAvailable<BitmapSource>(DataFormats.Bitmap);

    /// <summary>
    ///  Get text from Clipboard.
    /// </summary>
    public static string GetText() => GetText(TextDataFormat.UnicodeText);

    /// <summary>
    ///  Get text from Clipboard.
    /// </summary>
    public static string GetText(TextDataFormat format)
    {
        if (!DataFormats.IsValidTextDataFormat(format))
        {
            throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
        }

        return GetTypedDataIfAvailable<string>(DataFormats.ConvertToDataFormats(format)) is string text
            ? text
            : string.Empty;
    }

    /// <summary>
    ///  Set the audio data to Clipboard.
    /// </summary>
    public static void SetAudio(byte[] audioBytes)
    {
        ArgumentNullException.ThrowIfNull(audioBytes);
        SetAudio(new MemoryStream(audioBytes));
    }

    /// <summary>
    ///  Set the audio data to Clipboard.
    /// </summary>
    public static void SetAudio(Stream audioStream)
    {
        ArgumentNullException.ThrowIfNull(audioStream);
        SetDataInternal(DataFormats.WaveAudio, audioStream);
    }

    /// <summary>
    ///  Set the specified data to Clipboard.
    /// </summary>
    public static void SetData(string format, object data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentNullException.ThrowIfNull(data);
        SetDataInternal(format, data);
    }

    /// <summary>
    ///  Set the file drop list to Clipboard.
    /// </summary>
    public static void SetFileDropList(StringCollection fileDropList) => ClipboardCore.SetFileDropList(fileDropList);

    /// <summary>
    ///  Set the image data to Clipboard.
    /// </summary>
    public static void SetImage(BitmapSource image)
    {
        ArgumentNullException.ThrowIfNull(image);
        SetDataInternal(DataFormats.Bitmap, image);
    }

    /// <summary>
    ///  Set the text data to Clipboard.
    /// </summary>
    public static void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        SetText(text, TextDataFormat.UnicodeText);
    }

    /// <summary>
    ///  Set the text data to Clipboard.
    /// </summary>
    public static void SetText(string text, TextDataFormat format)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!DataFormats.IsValidTextDataFormat(format))
        {
            throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextDataFormat));
        }

        SetDataInternal(DataFormats.ConvertToDataFormats(format), text);
    }

    /// <summary>
    ///  Retrieves the data object that is currently on the system clipboard.
    /// </summary>
    public static IDataObject? GetDataObject()
    {
        ClipboardCore.GetDataObject<DataObject, IDataObject>(out IDataObject? dataObject).ThrowOnFailure();
        return dataObject;
    }

    /// <summary>
    ///  Determines whether the data object previously placed on the clipboard
    ///  by the SetDataObject is still on the clipboard.
    /// </summary>
    /// <param name="data">
    ///  Data object from the current containing clipboard which the caller
    ///  previously placed on the clipboard.
    /// </param>
    public static bool IsCurrent(IDataObject data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return ClipboardCore.IsObjectOnClipboard(data);
    }

    /// <summary>
    ///  Places nonpersistent data on the system clipboard.
    /// </summary>
    /// <param name="data">
    ///  The specific data to be on clipboard.
    /// </param>
    public static void SetDataObject(object data)
    {
        ArgumentNullException.ThrowIfNull(data);
        SetDataObject(data, copy: false);
    }

    /// <summary>
    ///  Places data on the system Clipboard and uses copy to specify whether the data 
    ///  should remain on the Clipboard after the application exits.
    /// </summary>
    /// <param name="data">
    ///  The specific data to be on clipboard.
    /// </param>
    /// <param name="copy">
    ///  Specify whether the data should remain on the clipboard after the application exits.
    /// </param>
    public static void SetDataObject(object data, bool copy)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Wrap if we're not already a DataObject
        DataObject dataObject = data as DataObject ?? DataObject.CreateFromClipboard(data);
        ClipboardCore.SetData(dataObject, copy).ThrowOnFailure();
    }

    /// <summary>
    ///  Query the specified data format from Clipboard.
    /// </summary>
    private static bool ContainsDataInternal(string format) =>
        GetDataObject() is { } dataObject && dataObject.GetDataPresent(format, IsDataFormatAutoConvert(format));

    /// <summary>
    ///  Get the specified format from Clipboard.
    /// </summary>
    private static object? GetDataInternal(string format) => GetDataObject() is { } dataObject
        ? dataObject.GetData(format, IsDataFormatAutoConvert(format))
        : null;

    /// <summary>
    ///  Set the specified data into Clipboard.
    /// </summary>
    private static void SetDataInternal(string format, object data)
    {
        DataObject dataObject = new();
        dataObject.SetData(format, data, IsDataFormatAutoConvert(format));
        SetDataObject(dataObject, copy: true);
    }

    /// <summary>
    ///  Check the auto convert for the specified data format.
    /// </summary>
    private static bool IsDataFormatAutoConvert(string format) =>
        format == DataFormats.FileDrop || format == DataFormats.Bitmap;

    /// <summary>
    ///  Retrieves data in the specified format if that data is of type <typeparamref name="T"/>. This is the only
    ///  overload of TryGetData that has the possibility of falling back to the <see cref="BinaryFormatter"/> and
    ///  should only be used if you need <see cref="BinaryFormatter"/> support.
    /// </summary>
    /// <param name="format">
    ///  <para>
    ///   The format of the data to retrieve. See the <see cref="DataFormats"/> class for a set of predefined data formats.
    ///  </para>
    /// </param>
    /// <param name="resolver">
    ///  <para>
    ///   A <see cref="Func{Type, TypeName}"/> that is used only when deserializing non-OLE formats. It returns the type if
    ///   <see cref="TypeName"/> is allowed or throws a <see cref="NotSupportedException"/> if <see cref="TypeName"/> is not
    ///   expected. If the resolver returns <see langword="null"/>, the following types will be resolved automatically:
    ///  </para>
    ///  <list type="bullet">
    ///   <item>
    ///    <description>
    ///     <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/4e77849f-89e3-49db-8fb9-e77ee4bc7214">
    ///      NRBF primitive types
    ///     </see>
    ///     (bool, byte, char, decimal, double, short, int, long, sbyte, ushort, uint, ulong, float, string, TimeSpan, DateTime).
    ///    </description>
    ///   </item>
    ///   <item>
    ///    <description>
    ///     Arrays and List{} of NRBF primitive types.
    ///    </description>
    ///   </item>
    ///   <item>
    ///    <description>
    ///     Core System.Drawing types (Bitmap, PointF, RectangleF, Point, Rectangle, SizeF, Size, Color).
    ///    </description>
    ///   </item>
    ///  </list>
    ///  <para>
    ///   <see cref="TypeName"/> parameter can be matched according to the user requirements, for example, only namespace-qualified
    ///   type names, or full type and assembly names, or full type names and short assembly names.
    ///  </para>
    /// </param>
    /// <param name="data">
    ///  <para>
    ///   Out parameter that contains the retrieved data in the specified format, or <see langword="null"/> if the data is
    ///   unavailable in the specified format, or is of a wrong <see cref="Type"/>.
    ///  </para>
    /// </param>
    /// <typeparam name="T">
    ///  <para>
    ///   The expected type. A resolver must be provided to handle derived types.
    ///  </para>
    /// </typeparam>
    /// <returns>
    ///  <see langword="true"/> if the data of this format is present on the clipboard and the value is of a matching
    ///  type and that value can be successfully retrieved, or <see langword="false"/> if the format is not present or
    ///  the value is of a wrong <see cref="Type"/>.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This API will fall back to the <see cref="BinaryFormatter"/> if the application has enabled it and taken
    ///   the <see href="https://learn.microsoft.com/dotnet/standard/serialization/binaryformatter-migration-guide/">
    ///   unsupported System.Runtime.Serialization.Formatters package</see>. You also must have enabled the OLE specific
    ///   switch "Windows.ClipboardDragDrop.EnableUnsafeBinaryFormatterSerialization" to allow fallback to the
    ///   <see cref="BinaryFormatter"/>.
    ///  </para>
    ///  <para>
    ///   Pre-defined <see cref="DataFormats"/> or other data that was serialized via <see cref="SetDataAsJson{T}(string, T)"/>
    ///   or <see cref="DataObject.SetDataAsJson{T}(string, T)"/> will always be able to be deserialized without enabling
    ///   the <see cref="BinaryFormatter"/>. <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/4e77849f-89e3-49db-8fb9-e77ee4bc7214">
    ///   NRBF primitive types</see> are also handled, as well as <see cref="List{T}"/> or arrays of these type. Basic
    ///   System.Drawing exchange types and Bitmap are also handled.
    ///  </para>
    ///  <para>
    ///   If the data is serialized in the NRBF format, passing <see cref="SerializationRecord"/> for
    ///   <typeparamref name="T"/> will return the decoded data. This can be used for full deserialization customization.
    ///  </para>
    ///  <para>
    ///   Avoid loading assemblies named in the <see cref="TypeName"/> argument of your <paramref name="resolver"/>.
    ///   Calling the <see cref="Type.GetType(string)"/> method can cause assembly loads and is not safe to trim.
    ///   Use <see langword="typeof"/> where possible.
    ///  </para>
    ///  <para>
    ///   For compatibility, .NET types are usually serialized using their .NET Framework assembly names. The resolver
    ///   should be aware of <see cref="TypeName"/>s coming in with either .NET Framework assembly names or .NET ones.
    ///  </para>
    ///  <para>
    ///   Make sure to consider other assembly information when matching, such as version, if you expect to be able to
    ///   deserialize from multiple assembly versions.
    ///  </para>
    ///  <para>
    ///   Also consider that Arrays, generic types, and nullable value types will have assembly names nested, in the
    ///   <see cref="TypeName.FullName"/> property.
    ///  </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">
    ///  If application does not support <see cref="BinaryFormatter"/> and the object can't be deserialized otherwise, or
    ///  application supports <see cref="BinaryFormatter"/> but <typeparamref name="T"/> is an <see cref="object"/>,
    ///  or not a concrete type, or if <paramref name="resolver"/> does not resolve the actual payload type. Or
    ///  the <see cref="IDataObject"/> on the <see cref="Clipboard"/> does not implement <see cref="ITypedDataObject"/>
    ///  interface.
    /// </exception>
    /// <exception cref="ThreadStateException">
    ///  The current thread is not in single-threaded apartment (STA) mode.
    /// </exception>
    /// <example>
    ///  <![CDATA[
    ///   using System.Reflection.Metadata;
    ///
    ///   internal static Type MyExactMatchResolver(TypeName typeName)
    ///   {
    ///        // The preferred approach is to resolve types at build time to avoid assembly loading at runtime.
    ///        (Type type, TypeName typeName)[] allowedTypes =
    ///        [
    ///            (typeof(MyClass1), TypeName.Parse(typeof(MyClass1).AssemblyQualifiedName)),
    ///            (typeof(MyClass2), TypeName.Parse(typeof(MyClass2).AssemblyQualifiedName))
    ///        ];
    ///
    ///        foreach (var (type, name) in allowedTypes)
    ///        {
    ///            // Namespace-qualified type name, using case-sensitive comparison for C#.
    ///            if (name.FullName != typeName.FullName)
    ///            {
    ///                continue;
    ///            }
    ///
    ///            AssemblyNameInfo? info1 = typeName.AssemblyName;
    ///            AssemblyNameInfo? info2 = name.AssemblyName;
    ///
    ///            if (info1 is null && info2 is null)
    ///            {
    ///                return type;
    ///            }
    ///
    ///            if (info1 is null || info2 is null)
    ///            {
    ///                continue;
    ///            }
    ///
    ///            // Full assembly name comparison, case sensitive.
    ///            if (info1.Name == info2.Name
    ///                 && info1.Version == info2.Version
    ///                 && ((info1.CultureName ?? string.Empty) == info2.CultureName)
    ///                 && info1.PublicKeyOrToken.AsSpan().SequenceEqual(info2.PublicKeyOrToken.AsSpan()))
    ///            {
    ///                return type;
    ///            }
    ///        }
    ///
    ///        throw new NotSupportedException($"Can't resolve {typeName.AssemblyQualifiedName}");
    ///    }
    ///  ]]>
    /// </example>
    [CLSCompliant(false)]
    public static bool TryGetData<T>(
        string format,
        Func<TypeName, Type?> resolver,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data)
    {
        data = default;
        resolver.OrThrowIfNull();
        if (!ClipboardCore.IsValidTypeForFormat(typeof(T), format)
            || GetDataObject() is not { } dataObject)
        {
            // Invalid format or no object on the clipboard at all.
            return false;
        }

        return dataObject.TryGetData(format, resolver, autoConvert: false, out data);
    }

    /// <remarks>
    ///  <para>
    ///   This method will never allow falling back to the <see cref="BinaryFormatter"/>, even if it is fully enabled.
    ///   You must use the <see cref="TryGetData{T}(string, Func{TypeName, Type?}, out T)"/> with an explicit resolver.
    ///  </para>
    ///  <para>
    ///   Pre-defined <see cref="DataFormats"/> or other data that was serialized via <see cref="SetDataAsJson{T}(string, T)"/>
    ///   or <see cref="DataObject.SetDataAsJson{T}(string, T)"/> will always be able to be deserialized without enabling
    ///   the <see cref="BinaryFormatter"/>. <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/4e77849f-89e3-49db-8fb9-e77ee4bc7214">
    ///   NRBF primitive types</see> are also handled, as well as <see cref="List{T}"/> or arrays of these type. Basic
    ///   System.Drawing exchange types and Bitmap are also handled.
    ///  </para>
    ///  <para>
    ///   If the data is serialized in the NRBF format, passing <see cref="SerializationRecord"/> for
    ///   <typeparamref name="T"/> will return the decoded data. This can be used for full deserialization customization.
    ///  </para>
    /// </remarks>
    /// <inheritdoc cref="TryGetData{T}(string, Func{TypeName, Type}, out T)"/>
    public static bool TryGetData<T>(
        string format,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data)
    {
        data = default;
        if (!ClipboardCore.IsValidTypeForFormat(typeof(T), format)
            || GetDataObject() is not { } dataObject)
        {
            // Invalid format or no object on the clipboard at all.
            return false;
        }

        return dataObject.TryGetData(format, out data);
    }

    /// <inheritdoc cref="DataObject.SetDataAsJson{T}(string, T)"/>
    public static void SetDataAsJson<T>(string format, T data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        DataObject dataObject = new();
        dataObject.SetDataAsJson(format, data);
        SetDataObject(dataObject, copy: true);
    }
}
