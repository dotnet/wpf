// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Private.Windows.Ole;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

namespace System.Windows.Ole;

/// <summary>
///  Adapts an <see cref="IDataObject"/> to <see cref="IDataObjectInternal"/>.
/// </summary>
internal sealed class DataObjectAdapter : IDataObjectInternal
{
    public IDataObject DataObject { get; }

    public DataObjectAdapter(IDataObject dataObject) => DataObject = dataObject;

    public object? GetData(string format, bool autoConvert) => DataObject.GetData(format, autoConvert);
    public object? GetData(string format) => DataObject.GetData(format);
    public object? GetData(Type format) => DataObject.GetData(format);
    public bool GetDataPresent(string format, bool autoConvert) => DataObject.GetDataPresent(format, autoConvert);
    public bool GetDataPresent(string format) => DataObject.GetDataPresent(format);
    public bool GetDataPresent(Type format) => DataObject.GetDataPresent(format);
    public string[] GetFormats(bool autoConvert) => DataObject.GetFormats(autoConvert);
    public string[] GetFormats() => DataObject.GetFormats();
    public void SetData(string format, bool autoConvert, object? data) => DataObject.SetData(format, data, autoConvert);
    public void SetData(string format, object? data) => DataObject.SetData(format, data);
    public void SetData(Type format, object? data) => DataObject.SetData(format, data);
    public void SetData(object? data) => DataObject.SetData(data);
    public bool TryGetData<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        [MaybeNullWhen(false), NotNullWhen(true)] out T data) => DataObject.TryGetData(out data);
    public bool TryGetData<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        string format,
        [MaybeNullWhen(false), NotNullWhen(true)] out T data) => DataObject.TryGetData(format, out data);
    public bool TryGetData<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        string format,
        bool autoConvert,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data) => DataObject.TryGetData(format, autoConvert, out data);
    public bool TryGetData<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        string format,
        Func<TypeName, Type?> resolver,
        bool autoConvert,
        [MaybeNullWhen(false), NotNullWhen(true)] out T data) => DataObject.TryGetData(format, resolver, autoConvert, out data);
}
