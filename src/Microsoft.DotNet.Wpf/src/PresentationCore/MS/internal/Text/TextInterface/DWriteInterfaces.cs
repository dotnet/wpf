// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using MS.Internal.Interop.DWrite;

namespace MS.Internal.Text.TextInterface
{
    /// <summary>
    /// The font collection loader interface is used to construct a collection of fonts given a particular type of key.
    /// The font collection loader interface is recommended to be implemented by a singleton object.
    /// IMPORTANT: font collection loader implementations must not register themselves with DirectWrite factory
    /// inside their constructors and must not unregister themselves in their destructors, because
    /// registration and unregistraton operations increment and decrement the object reference count respectively.
    /// Instead, registration and unregistration of font file loaders with DirectWrite factory should be performed
    /// outside of the font file loader implementation as a separate step.
    /// </summary>
    [ComImport]
    [Guid("cca920e4-52f0-492b-bfa8-29c72ee0a468")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe interface IDWriteFontCollectionLoaderMirror
    {
        /// <summary>
        /// Creates a font file enumerator object that encapsulates a collection of font files.
        /// The font system calls back to this interface to create a font collection.
        /// </summary>
        /// <param name="collectionKey">Font collection key that uniquely identifies the collection of font files within
        /// the scope of the font collection loader being used.</param>
        /// <param name="collectionKeySize">Size of the font collection key in bytes.</param>
        /// <param name="fontFileEnumerator">Pointer to the newly created font file enumerator.</param>
        /// <returns>
        /// Standard HRESULT error code.
        /// </returns>
        [PreserveSig]
        int CreateEnumeratorFromKey(
            /*[In, MarshalAs(UnmanagedType.Interface)]*/ IntPtr factory,
            [In] void* collectionKey,
            [In, MarshalAs(UnmanagedType.U4)] uint collectionKeySize,
            /*[Out, MarshalAs(UnmanagedType.Interface)]*/ IntPtr* fontFileEnumerator
            );
    }

    /// <summary>
    /// The font file enumerator interface encapsulates a collection of font files. The font system uses this interface
    /// to enumerate font files when building a font collection.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("72755049-5ff7-435d-8348-4be97cfa6c7c")]
    internal interface IDWriteFontFileEnumeratorMirror
    {
        /// <summary>
        /// Advances to the next font file in the collection. When it is first created, the enumerator is positioned
        /// before the first element of the collection and the first call to MoveNext advances to the first file.
        /// </summary>
        /// <param name="hasCurrentFile">Receives the value TRUE if the enumerator advances to a file, or FALSE if
        /// the enumerator advanced past the last file in the collection.</param>
        /// <returns>
        /// Standard HRESULT error code.
        /// </returns>
        [PreserveSig]
        int MoveNext([MarshalAs(UnmanagedType.Bool)] out bool hasCurrentFile);

        /// <summary>
        /// Gets a reference to the current font file.
        /// </summary>
        /// <param name="fontFile">Pointer to the newly created font file object.</param>
        /// <returns>
        /// Standard HRESULT error code.
        /// </returns>
        [PreserveSig]
        unsafe int GetCurrentFontFile(/*[Out, MarshalAs(UnmanagedType.Interface)]*/ IDWriteFontFile** fontFile);
    }
}
