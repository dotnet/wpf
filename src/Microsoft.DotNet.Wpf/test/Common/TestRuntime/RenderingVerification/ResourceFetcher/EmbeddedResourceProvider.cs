// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.ResourceFetcher
{
    #region usings
        using System;
        using System.IO;
        using System.Drawing;
    #endregion usings;

    /// <summary>
    /// Implementation of IResourceProvider for extracting embedded resources (exe / dll / ocx)
    /// </summary>
    internal class EmbeddedResourceProvider : IResourceProvider
    {
        #region Properties
            #region Private Properties
                private static object _lockKey = 0;
                private static EmbeddedResourceProvider _current = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get the Current provider
                /// </summary>
                /// <value></value>
                internal static EmbeddedResourceProvider Current
                {
                    get
                    {
                        lock (_lockKey)
                        {
                            if (_current == null) { _current = new EmbeddedResourceProvider(); }
                        }
                        return _current;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            private EmbeddedResourceProvider() { }
        #endregion Constructors

        #region Methods
            /// <summary>
            /// Get the resource associated with the resourceKey
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public virtual Resource GetResource(ResourceKey key)
            {
                EmbeddedResourceKey embeddedKey = key as EmbeddedResourceKey;
                if(key == null) { throw new NotSupportedException("This resourceKey is not supported"); }

                string description = "Mapped Resource from the specified exe/dll";
                string fileName = embeddedKey.FileName;
                ResourceType resourceType = embeddedKey.ResourceType;
                object resourceName = embeddedKey.ResourceName;

                Stream stream = null;
                ResourceStripper resourceStrip = null;
                try
                {
                    resourceStrip = new ResourceStripper(fileName);
                    stream = resourceStrip.GetResource(resourceType, resourceName, System.Globalization.CultureInfo.CurrentCulture);
                    string name = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName);
                    return new Resource(description, name, extension, stream);
                }
                finally
                {
                    if (resourceStrip != null) { resourceStrip.Dispose(); resourceStrip = null; }
                }
            }
        #endregion Methods
    }
}
