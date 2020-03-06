// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: A static utilitly class for stream related functions.

using System;
using System.IO;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// A static utilitly class for stream related functions.
/// </summary>
internal static class StreamHelper
{
    #region Internal Methods
    //--------------------------------------------------------------------------
    // Internal Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// A simple stream copy from one to another.
    /// </summary>
    /// <remarks>
    /// If anyone knows of an existing mechanism please contact me and
    /// I will remove this code.
    /// 
    /// Performance:  This block of code is likely to be the most heavily used
    /// code with large packages.  We should explorer other designs if it
    /// becomes an issue.
    /// </remarks>
    /// <param name="source">The Stream to read from.</param>
    /// <param name="comparee">The Stream to write to.</param>
    internal static void CopyStream(Stream source, Stream target)
    {
        int bufferSize = 4096; // Arbitrary Value
        byte[] buffer = new byte[bufferSize];

        long originalSourcePosition = source.Position;
        long originalTargetPosition = target.Position;

        // move to the start
        source.Position = 0;
        target.Position = 0;

        // ensure we have enough space
        long size = source.Length;
        target.SetLength(size);

        // copy the stream syncronously
        int read = 0;
        long leftToCopy = size;
        while (leftToCopy > 0)
        {
            if (leftToCopy < bufferSize)
            {
                bufferSize = (int)leftToCopy;
                buffer = new byte[bufferSize];
            }
            read = source.Read(buffer, 0, bufferSize);
            target.Write(buffer, 0, read);
            leftToCopy -= read;
        }

        // return the streams to thier orignal locations
        source.Position = originalSourcePosition;
        target.Position = originalTargetPosition;

        Trace.SafeWrite(Trace.File, "Copied: {0} bytes.", target.Length);
    }

#if DRT
    /// <summary>
    /// Compares two streams byte by byte.
    /// </summary>
    /// <param name="original">The original stream</param>
    /// <param name="comparee">The stream to compare with.</param>
    internal static void CompareStream(Stream original, Stream comparee)
    {
        original.Position = 0;
        comparee.Position = 0;
        int data = 0;
        int pos = 0;
        while (data != -1)
        {
            data = original.ReadByte();
            pos++;
            Invariant.Assert(data == comparee.ReadByte(),
                "Data mismatch at postion " + pos);
        }
        if (pos - 1 == comparee.Length)
        {
            Trace.SafeWrite(Trace.File, "Validate: {0} bytes.", comparee.Length);
        }
    }
#endif
    #endregion Internal Methods
}
}
