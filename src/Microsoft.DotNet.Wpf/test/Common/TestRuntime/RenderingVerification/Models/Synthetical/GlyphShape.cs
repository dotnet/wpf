// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Drawing;
        using System.Drawing.Imaging;
        using System.Runtime.Serialization;
        //using System.Runtime.Serialization.Formatters.Soap;
    #endregion using

    /// <summary>
    /// Summary description for GlyphShape.
    /// </summary>
    [SerializableAttribute]
    public abstract class GlyphShape : GlyphBase, ISerializable
    {
        #region Constructors
            /// <summary>
            /// The default constructor - XML serialixation only
            /// </summary>
            protected GlyphShape() : base() 
            {
            }
            /// <summary>
            /// The constructor
            /// </summary>
            protected GlyphShape(GlyphContainer container) : base(container)
            {
            }
            /// <summary>
            /// Serialization constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected GlyphShape(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
            #endregion Private Methods
            #region Public/Protected Methods
            #endregion Public/Protected Methods
        #endregion Methods

        #region ISerializable Members
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
            }
        #endregion
    }

    /// <summary>
    /// Summary description for GlyphRectangle.
    /// </summary>
    [SerializableAttribute]
    public class GlyphRectangle: GlyphBase, ISerializable
    {
        #region Constructors
            /// <summary>
            /// The default constructor - XML serialixation only
            /// </summary>
            public GlyphRectangle() : base()
            {
            }
            /// <summary>
            /// the constructor.
            /// </summary>
            public GlyphRectangle(GlyphContainer container) : base(container)
            {
            }
            /// <summary>
            /// Serialization Constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected GlyphRectangle(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        #endregion Constructors

        #region Methods
            #region Internal Methods
                /// <summary>
                /// internal render 
                /// </summary>
                internal override IImageAdapter _Render()
                {
                    throw new NotImplementedException();
                }
            #endregion Internal Methods
            #region Public Methods
            #endregion Public Methods
        #endregion Methods

        #region ISerializable Members
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                // TODO:  Add GlyphRectangle.ISerializable.GetObjectData implementation
            }
        #endregion
    }
}
