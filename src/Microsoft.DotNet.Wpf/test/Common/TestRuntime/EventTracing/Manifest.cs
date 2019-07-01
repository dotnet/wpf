// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using Microsoft.Test.EventTracing.FastSerialization;


/// <summary>
/// Used to send the rawManifest into the event stream as a series of events.  
/// </summary>
internal struct ManifestEnvelope
{
    public const int MaxChunkSize = 0xFF00;
    public enum ManifestFormats : byte
    {
        SimpleXmlFormat = 1,          // Simply dump what is under the <proivider> tag in an XML manifest
    }

    // public ManifestFormats Format;
    public byte MajorVersion;
    public byte MinorVersion;
    public byte Magic;
    public ushort TotalChunks;
    public ushort ChunkNumber;
};
