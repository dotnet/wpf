// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This program uses code hyperlinks available as part of the HyperAddin Visual Studio plug-in.
// It is available from http://www.codeplex.com/hyperAddin 
// 
using System;
using System.IO;

namespace Microsoft.Test.EventTracing.Utilities
{
    [CLSCompliant(false)] 
    public static class StreamUtilities {
        public static void CopyFromFile(string fromFilePath, Stream toStream)
        {
            using (Stream fromStream = File.OpenRead(fromFilePath))
                CopyStream(fromStream, toStream);
        }

        public static void CopyToFile(Stream fromStream, string toFilePath)
        {
            using (Stream toStream = File.Create(toFilePath))
                CopyStream(fromStream, toStream);
        }

        /// <summary>
        /// CopyStream simply copies 'fromStream' to 'toStream'
        /// </summary>
        public static int CopyStream(Stream fromStream, Stream toStream)
        {
            byte[] buffer = new byte[8192];
            int totalBytes = 0;
            for (; ; )
            {
                int count = fromStream.Read(buffer, 0, buffer.Length);
                if (count == 0)
                    break;
                toStream.Write(buffer, 0, count);
                totalBytes += count;
            }
            return totalBytes;
        }
    };
}