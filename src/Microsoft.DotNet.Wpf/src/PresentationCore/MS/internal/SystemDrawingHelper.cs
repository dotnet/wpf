// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

// Description: Helper methods for code that uses types from System.Drawing.

using System.IO;
using Windows.Win32.Graphics.Gdi;

namespace MS.Internal
{
    internal static class SystemDrawingHelper
    {
        /// <inheritdoc cref="SystemDrawingExtensionMethods.IsBitmap(object?)"/>
        internal static bool IsBitmap(object? data) =>
            AssemblyHelper.ExtensionsForSystemDrawing()?.IsBitmap(data) ?? false;

        /// <inheritdoc cref="SystemDrawingExtensionMethods.IsImage(object?)"/>
        internal static bool IsImage(object? data) =>
            AssemblyHelper.ExtensionsForSystemDrawing()?.IsImage(data) ?? false;

        /// <inheritdoc cref="SystemDrawingExtensionMethods.IsMetafile(object?)"/>
        internal static bool IsMetafile(object? data) =>
            AssemblyHelper.ExtensionsForSystemDrawing()?.IsMetafile(data) ?? false;

        /// <inheritdoc cref="SystemDrawingExtensionMethods.GetHandleFromMetafile(object?)"/>
        internal static HENHMETAFILE GetHandleFromMetafile(object? data) =>
            (HENHMETAFILE)(AssemblyHelper.ExtensionsForSystemDrawing()?.GetHandleFromMetafile(data) ?? 0);

        /// <inheritdoc cref="SystemDrawingExtensionMethods.GetMetafileFromHemf(nint)"/>
        internal static object? GetMetafileFromHemf(HENHMETAFILE hMetafile) =>
            AssemblyHelper.ExtensionsForSystemDrawing(force: true)?.GetMetafileFromHemf(hMetafile);

        /// <inheritdoc cref="SystemDrawingExtensionMethods.GetBitmap(object?)"/>
        internal static object? GetBitmap(object? data) =>
            AssemblyHelper.ExtensionsForSystemDrawing(force: true)?.GetBitmap(data);

        /// <inheritdoc cref="SystemDrawingExtensionMethods.GetHBitmap(object?, out int, out int)"/>
        internal static HBITMAP GetHBitmap(object? data, out int width, out int height)
        {
            var extensions = AssemblyHelper.ExtensionsForSystemDrawing(force: true);
            if (extensions is not null)
            {
                return (HBITMAP)extensions.GetHBitmap(data, out width, out height);
            }

            width = height = 0;
            return HBITMAP.Null;
        }

        /// <inheritdoc cref="SystemDrawingExtensionMethods.GetHBitmapFromBitmap(object?)"/>
        internal static HBITMAP GetHBitmapFromBitmap(object? data) =>
            (HBITMAP)(AssemblyHelper.ExtensionsForSystemDrawing()?.GetHBitmapFromBitmap(data) ?? HBITMAP.Null);

        /// <inheritdoc cref="SystemDrawingExtensionMethods.ConvertMetafileToHBitmap(nint)"/>
        internal static HBITMAP ConvertMetafileToHBitmap(HENHMETAFILE handle) =>
            (HBITMAP)(AssemblyHelper.ExtensionsForSystemDrawing(force: true)?.ConvertMetafileToHBitmap(handle) ?? HBITMAP.Null);

        /// <inheritdoc cref="SystemDrawingExtensionMethods.GetCommentFromGifStream(Stream)"/>
        internal static Stream? GetCommentFromGifStream(Stream stream) =>
            AssemblyHelper.ExtensionsForSystemDrawing(force: true)?.GetCommentFromGifStream(stream);

        /// <inheritdoc cref="SystemDrawingExtensionMethods.SaveMetafileToImageStream(MemoryStream, Stream)"/>
        internal static void SaveMetafileToImageStream(MemoryStream metafileStream, Stream imageStream) =>
            AssemblyHelper.ExtensionsForSystemDrawing(force: true)?.SaveMetafileToImageStream(metafileStream, imageStream);

        /// <inheritdoc cref="SystemDrawingExtensionMethods.GetBitmapFromBitmapSource(object)"/>
        internal static object? GetBitmapFromBitmapSource(object source) =>
            AssemblyHelper.ExtensionsForSystemDrawing(force: true)?.GetBitmapFromBitmapSource(source);
    }
}
