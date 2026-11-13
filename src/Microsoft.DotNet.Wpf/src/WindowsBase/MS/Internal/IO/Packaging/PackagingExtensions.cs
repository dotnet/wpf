// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Packaging;

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
    
    internal static class PackageRelationship
    {
        public static Uri ContainerRelationshipPartName => System.IO.Packaging.PackUriHelper.CreatePartUri(new Uri("/_rels/.rels", UriKind.Relative));
    }

}
