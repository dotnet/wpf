// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __DWRITEINTERFACES_H
#define __DWRITEINTERFACES_H

using namespace System;
using namespace System::Runtime::InteropServices;

///
/// COMPATIBILITY WARNING
/// These interfaces are copies of the interfaces defined in DWrite.h
/// You can not modify them unless the matching ones in DWrite.h have
/// changed
///
namespace MS { namespace Internal { namespace Text { namespace TextInterface { namespace Interfaces {
    /// <summary>
    /// The interface for loading font file data.
    /// </summary>
    [ComImport(), Guid("6d4865fe-0ab8-4d91-8f62-5dd6be34a3e0"), InterfaceType(ComInterfaceType::InterfaceIsIUnknown)]
    interface class IDWriteFontFileStreamMirror
    {
        /// <summary>
        /// Reads a fragment from a file.
        /// </summary>
        /// <param name="fragmentStart">Receives the pointer to the start of the font file fragment.</param>
        /// <param name="fileOffset">Offset of the fragment from the beginning of the font file.</param>
        /// <param name="fragmentSize">Size of the fragment in bytes.</param>
        /// <param name="fragmentContext">The client defined context to be passed to the ReleaseFileFragment.</param>
        /// <returns>
        /// Standard HRESULT error code.
        /// </returns>
        /// <remarks>
        /// IMPORTANT: ReadFileFragment() implementations must check whether the requested file fragment
        /// is within the file bounds. Otherwise, an error should be returned from ReadFileFragment.
        /// </remarks>
        [PreserveSig]
        HRESULT ReadFileFragment(
            [Out] const void **fragmentStart,
            [In, MarshalAs(UnmanagedType::U8)] UINT64 fileOffset,
            [In, MarshalAs(UnmanagedType::U8)] UINT64 fragmentSize,
            [Out] void **fragmentContext
            );

        /// <summary>
        /// Releases a fragment from a file.
        /// </summary>
        /// <param name="fragmentContext">The client defined context of a font fragment returned from ReadFileFragment.</param>
        [PreserveSig]
        void ReleaseFileFragment(
            [In] void *fragmentContext
            );

        /// <summary>
        /// Obtains the total size of a file.
        /// </summary>
        /// <param name="fileSize">Receives the total size of the file.</param>
        /// <returns>
        /// Standard HRESULT error code.
        /// </returns>
        /// <remarks>
        /// Implementing GetFileSize() for asynchronously loaded font files may require
        /// downloading the complete file contents, therefore this method should only be used for operations that
        /// either require complete font file to be loaded (e.g., copying a font file) or need to make
        /// decisions based on the value of the file size (e.g., validation against a persisted file size).
        /// </remarks>
        [PreserveSig]
        HRESULT GetFileSize(
            [Out/*, MarshalAs(UnmanagedType::U8)*/] UINT64 *fileSize
            );

        /// <summary>
        /// Obtains the last modified time of the file. The last modified time is used by DirectWrite font selection algorithms
        /// to determine whether one font resource is more up to date than another one.
        /// </summary>
        /// <param name="lastWriteTime">Receives the last modifed time of the file in the format that represents
        /// the number of 100-nanosecond intervals since January 1, 1601 (UTC).</param>
        /// <returns>
        /// Standard HRESULT error code. For resources that don't have a concept of the last modified time, the implementation of
        /// GetLastWriteTime should return E_NOTIMPL.
        /// </returns>
        [PreserveSig]
        HRESULT GetLastWriteTime(
            [Out/*, MarshalAs(UnmanagedType::U8)*/] UINT64 *lastWriteTime
            );
    };
    
    /// <summary>
    /// Font file loader interface handles loading font file resources of a particular type from a key.
    /// The font file loader interface is recommended to be implemented by a singleton object.
    /// IMPORTANT: font file loader implementations must not register themselves with DirectWrite factory
    /// inside their constructors and must not unregister themselves in their destructors, because
    /// registration and unregistraton operations increment and decrement the object reference count respectively.
    /// Instead, registration and unregistration of font file loaders with DirectWrite factory should be performed
    /// outside of the font file loader implementation as a separate step.
    /// </summary>    
    [ComImport(), Guid("727cad4e-d6af-4c9e-8a08-d695b11caa49"), InterfaceType(ComInterfaceType::InterfaceIsIUnknown)]
    interface class IDWriteFontFileLoaderMirror
    {        
        /// <summary>
        /// Creates a font file stream object that encapsulates an open file resource.
        /// The resource is closed when the last reference to fontFileStream is released.
        /// </summary>
        /// <param name="fontFileReferenceKey">Font file reference key that uniquely identifies the font file resource
        /// within the scope of the font loader being used.</param>
        /// <param name="fontFileReferenceKeySize">Size of font file reference key in bytes.</param>
        /// <param name="fontFileStream">Pointer to the newly created font file stream.</param>
        /// <returns>
        /// Standard HRESULT error code.
        /// </returns>
        [PreserveSig]
        HRESULT CreateStreamFromKey(
            [In] void const* fontFileReferenceKey,
            [In, MarshalAs(UnmanagedType::U4)] UINT32 fontFileReferenceKeySize,
            [Out/*, MarshalAs(UnmanagedType::Interface)*/] IntPtr* fontFileStream
            );
    };

    /// <summary>
    /// The font file enumerator interface encapsulates a collection of font files. The font system uses this interface
    /// to enumerate font files when building a font collection.
    /// </summary>
    [ComImport(), Guid("72755049-5ff7-435d-8348-4be97cfa6c7c"), InterfaceType(ComInterfaceType::InterfaceIsIUnknown)]
    interface class IDWriteFontFileEnumeratorMirror
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
        HRESULT MoveNext(
            [Out, MarshalAs(UnmanagedType::Bool)] bool% hasCurrentFile
            );

        /// <summary>
        /// Gets a reference to the current font file.
        /// </summary>
        /// <param name="fontFile">Pointer to the newly created font file object.</param>
        /// <returns>
        /// Standard HRESULT error code.
        /// </returns>
        [PreserveSig]
        HRESULT GetCurrentFontFile(
            /*[Out, MarshalAs(UnmanagedType::Interface)]*/ IDWriteFontFile** fontFile
            );
    };
    
    /// <summary>
    /// The font collection loader interface is used to construct a collection of fonts given a particular type of key.
    /// The font collection loader interface is recommended to be implemented by a singleton object.
    /// IMPORTANT: font collection loader implementations must not register themselves with DirectWrite factory
    /// inside their constructors and must not unregister themselves in their destructors, because
    /// registration and unregistraton operations increment and decrement the object reference count respectively.
    /// Instead, registration and unregistration of font file loaders with DirectWrite factory should be performed
    /// outside of the font file loader implementation as a separate step.
    /// </summary>
    [ComImport(), Guid("cca920e4-52f0-492b-bfa8-29c72ee0a468"), InterfaceType(ComInterfaceType::InterfaceIsIUnknown)]
    interface class IDWriteFontCollectionLoaderMirror
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
        HRESULT CreateEnumeratorFromKey(
            /*[In, MarshalAs(UnmanagedType::Interface)]*/ IntPtr factory,
            [In] void const* collectionKey,
            [In, MarshalAs(UnmanagedType::U4)] UINT32 collectionKeySize,
            /*[Out, MarshalAs(UnmanagedType::Interface)]*/ IntPtr* fontFileEnumerator
            );
    };

}}}}}

using namespace MS::Internal::Text::TextInterface::Interfaces;

#endif
