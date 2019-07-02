// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.IO;
        using System.Drawing;
        using System.Runtime.Serialization;
        using Microsoft.Test.RenderingVerification.ResourceFetcher;
    #endregion using

    /// <summary>
    /// Summary description for GlyphResource.
    /// </summary>
    [SerializableAttribute]
    public class GlyphResource: GlyphBase, ISerializable
    {
        #region Properties
            #region Private Properties
                private ResourceKey _resourceKey = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get /set the ResourceKey to use to fetch the resource
                /// </summary>
                /// <value></value>
                public ResourceKey ResourceKey
                {
                    get { return _resourceKey; }
                    set { _resourceKey = value; }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// The default constructor - XML serialixation only
            /// </summary>
            public GlyphResource(ResourceKey resourceKey) : base() 
            {
                _resourceKey = resourceKey;
            }
            /// <summary>
            /// The constructor
            /// </summary>
            public GlyphResource(ResourceKey resourceKey, GlyphContainer container) : base(container)
            {
                _resourceKey = resourceKey;
            }
            /// <summary>
            /// Deserialization Constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected GlyphResource(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                _resourceKey = (ResourceKey)info.GetValue("ResourceKey", typeof(ResourceKey));
            }
        #endregion Constructors

        #region Methods
            #region Internal Methods
                /// <summary>
                /// internal render 
                /// </summary>
                internal override IImageAdapter _Render()
                {
                    if(_resourceKey == null) { throw new RenderingVerificationException("ResourceKey not set (currently null)");}

                    Stream stream = null;
                    try
                    {
                        stream = EmbeddedResourceProvider.Current.GetResource(_resourceKey).GetStream();
                        // Dispose (called when block exit) will close the underlying stream
                        using (Bitmap bitmap = (Bitmap)Bitmap.FromStream(stream))
                        {
                            GeneratedImage = new ImageAdapter(bitmap);
                            Size = new Size(GeneratedImage.Width, GeneratedImage.Height);
                            return GeneratedImage;
                        }
                    }
                    finally 
                    {
                        // Should be useless (except if error occurs if Bitmap.FromStream) since
                        // disposing the bitmap (done automatically when using block exit) will close the underlying stream
                        if (stream != null) { stream.Close(); stream = null; }
                    }
                }
            #endregion Internal Methods
            #region Public Methods
                /// <summary>
                /// Return the size of the Resource
                /// </summary>
                /// <returns></returns>
                public override SizeF Measure()
                {
                    if (GeneratedImage == null) { return SizeF.Empty; }
                    return new SizeF(GeneratedImage.Width, GeneratedImage.Height);
                }
            #endregion Public Methods
        #endregion Methods

        #region ISerializable Members
            /// <summary>
            /// Serialization Method
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                info.AddValue("ResourceKey", _resourceKey);
            }
        #endregion
    }
}


