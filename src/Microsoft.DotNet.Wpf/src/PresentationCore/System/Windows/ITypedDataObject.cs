// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

namespace System.Windows;

/// <summary>
///  Provides a format-independent mechanism for reading data of a specified <see cref="Type"/>.
/// </summary>
/// <remarks>
///  <para>
///   Implement this interface to use your data object with <see cref="Clipboard.TryGetData{T}(string, out T)"/>
///   family of methods as well as in the drag and drop operations. This interface will ensure that only
///   data of the specified <see cref="Type"/> is exchanged. Otherwise the APIs that specify a <see cref="Type"/> parameter
///   will throw a <see cref="NotSupportedException"/>. This is replacement of <see cref="IDataObject"/>
///   interface, implement this interface as well. Otherwise the APIs that specify a <see cref="Type"/> parameter
///   will throw a <see cref="NotSupportedException"/>.
///  </para>
/// </remarks>
public interface ITypedDataObject : IDataObject
{
    /// <inheritdoc cref="IDataObjectInternal.TryGetData{T}(out T)"/>
    bool TryGetData<T>(
        [NotNullWhen(true), MaybeNullWhen(false)] out T data);

    /// <inheritdoc cref="IDataObjectInternal.TryGetData{T}(string, out T)" />
    bool TryGetData<T>(
        string format,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data);

    /// <inheritdoc cref="IDataObjectInternal.TryGetData{T}(string, bool, out T)" />
    bool TryGetData<T>(
        string format,
        bool autoConvert,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data);

    /// <inheritdoc cref="IDataObjectInternal.TryGetData{T}(string, Func{TypeName, Type}, bool, out T)" />
    bool TryGetData<T>(
        string format,
#pragma warning disable CS3001 // Argument type is not CLS-compliant
        Func<TypeName, Type?> resolver,
#pragma warning restore CS3001
        bool autoConvert,
        [NotNullWhen(true), MaybeNullWhen(false)] out T data);
}
