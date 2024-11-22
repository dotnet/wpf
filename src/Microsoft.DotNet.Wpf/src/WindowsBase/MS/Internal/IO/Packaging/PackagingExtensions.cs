// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

}
