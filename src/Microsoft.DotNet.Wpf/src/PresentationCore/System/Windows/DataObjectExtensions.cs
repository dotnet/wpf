// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace System.Windows;

/// <summary>
///  Extension methods for data objects.
/// </summary>
public static class DataObjectExtensions
{
    private static ITypedDataObject GetTypedDataObjectOrThrow(IDataObject dataObject)
    {
        ArgumentNullException.ThrowIfNull(dataObject);

        if (dataObject is not ITypedDataObject typed)
        {
            throw new NotSupportedException(string.Format(
                SR.ITypeDataObject_Not_Implemented,
                dataObject.GetType().FullName));
        }

        return typed;
    }

    /// <inheritdoc cref="ITypedDataObject.TryGetData{T}(out T)"/>
    /// <exception cref="NotSupportedException">if the <paramref name="dataObject"/> does not implement <see cref="ITypedDataObject" />.</exception>
    /// <exception cref="ArgumentNullException">if the <paramref name="dataObject"/> is <see langword="null"/></exception>
    public static bool TryGetData<T>(
        this IDataObject dataObject,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data) =>
            GetTypedDataObjectOrThrow(dataObject).TryGetData(out data);

    /// <inheritdoc cref="ITypedDataObject.TryGetData{T}(string, out T)"/>
    /// <exception cref="NotSupportedException">if the <paramref name="dataObject"/> does not implement <see cref="ITypedDataObject" />.</exception>
    /// <exception cref="ArgumentNullException">if the <paramref name="dataObject"/> is <see langword="null"/></exception>
    public static bool TryGetData<T>(
        this IDataObject dataObject,
        string format,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data) =>
            GetTypedDataObjectOrThrow(dataObject).TryGetData(format, out data);

    /// <inheritdoc cref="ITypedDataObject.TryGetData{T}(string, bool, out T)"/>
    /// <exception cref="NotSupportedException">if the <paramref name="dataObject"/> does not implement <see cref="ITypedDataObject" />.</exception>
    /// <exception cref="ArgumentNullException">if the <paramref name="dataObject"/> is <see langword="null"/></exception>
    public static bool TryGetData<T>(
        this IDataObject dataObject,
        string format,
        bool autoConvert,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data) =>
            GetTypedDataObjectOrThrow(dataObject).TryGetData(format, autoConvert, out data);

    /// <inheritdoc cref="ITypedDataObject.TryGetData{T}(string, Func{Reflection.Metadata.TypeName, Type}, bool, out T)"/>
    /// <exception cref="NotSupportedException">if the <paramref name="dataObject"/> does not implement <see cref="ITypedDataObject" />.</exception>
    /// <exception cref="ArgumentNullException">if the <paramref name="dataObject"/> is <see langword="null"/></exception>
    [CLSCompliant(false)]
    public static bool TryGetData<T>(
        this IDataObject dataObject,
        string format,
        Func<Reflection.Metadata.TypeName, Type?> resolver,
        bool autoConvert,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data) =>
            GetTypedDataObjectOrThrow(dataObject).TryGetData(format, resolver, autoConvert, out data);
}
