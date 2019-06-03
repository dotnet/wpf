// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Text;
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Packaging.Extensions
{
    // These extensions are the APIs from the System.IO.Packaging package that builds out
    // of dotnet/corefx.
    internal static class PackagePartExtensions
    {
        // These extension methods are really properties
        public static ContentType ValidatedContentType(this PackagePart packagePart)
        {
            return new ContentType(packagePart.ContentType);
        }
    }
    
    internal class PackageRelationship
    {
        public static Uri ContainerRelationshipPartName => System.IO.Packaging.PackUriHelper.CreatePartUri(new Uri("/_rels/.rels", UriKind.Relative));
    }

    internal static class ZipPackage
    {
        private const string ForwardSlashString = "/"; //Required for creating a part name from a zip item name

        public static string GetZipItemNameFromOpcName(string opcName)
        {
            System.Diagnostics.Debug.Assert(opcName != null && opcName.Length > 0);
            return opcName.Substring(1);
        }

        public static string GetOpcNameFromZipItemName(string zipItemName)
        {
            return String.Concat(ForwardSlashString, zipItemName);
        }
    }
}
