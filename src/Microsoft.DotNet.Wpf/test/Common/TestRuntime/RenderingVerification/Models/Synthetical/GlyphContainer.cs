// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.Drawing;
        using System.Reflection;
        using System.Collections;
        using System.Runtime.Serialization;
        //using System.Runtime.Serialization.Formatters.Soap;
        using Microsoft.Test.RenderingVerification.Model.Synthetical.LayoutEngine;
    #endregion using

    /// <summary>
    /// Summary description for GlyphContainer.
    /// </summary>
    [SerializableAttribute]
    public abstract class GlyphContainer: GlyphBase, ISerializable
    {
        #region Properties
            #region Private Properties
                /// <summary>
                /// The gllyph arraylist
                /// </summary>
                private ArrayList _glyphPanels = new ArrayList();
                /// <summary>
                /// The Layout engine to use
                /// </summary>
                private ILayoutEngine _layoutEngine = null;
                private Animator _animator = null;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The collection of GlyphPanel
                /// </summary>
                public ArrayList Glyphs
                {
                    get { return _glyphPanels; }
                }
                /// <summary>
                /// perfomrs the layout
                /// </summary>
                public ILayoutEngine LayoutEngine
                {
                    get
                    {
                        return _layoutEngine;
                    }
                    set
                    {
                        if (value == null) { throw new ArgumentNullException("LayoutEngine", "Must be set to a valid object implementing ILayoutEngine (null passed in)"); }
                        _layoutEngine = value;
                        _layoutEngine.Container = this;
                    }
                }
                /// <summary>
                /// The property Animator
                /// </summary>
                public Animator Animator
                {
                    get
                    {
                        if (_animator == null) { _animator = new Animator(); }
                        return _animator;
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// The default constructor - XML serialixation only
            /// </summary>
            protected GlyphContainer() : base() 
            {
                LayoutEngine = new CanvasLayoutEngine();
            }
            /// <summary>
            /// The constructor
            /// </summary>
            protected GlyphContainer(GlyphContainer container) : base(container) 
            {
//                Size = container.Size;
                Position = container.Position;
                LayoutEngine = new CanvasLayoutEngine();
            }
            /// <summary>
            /// Deserialization Constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected GlyphContainer(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                _glyphPanels = (ArrayList)info.GetValue("Glyphs", typeof(ArrayList));
                Type layoutEngineType =  (Type)info.GetValue("LayoutEngine", typeof(Type));
                LayoutEngine = (ILayoutEngine)layoutEngineType.InvokeMember(".ctor", BindingFlags.CreateInstance, null, null, null);
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
            #endregion Private Methods
            #region Internal Methods
                override internal IImageAdapter _Render()
                {
                    IImageAdapter retVal = null;

                    // Render ever glyph hosted by this container
                    foreach(GlyphBase glyph in Glyphs)
                    {
                        glyph.Render();
                    }

                    // Perform Layout
                    LayoutEngine.Container = this;
                    LayoutEngine.ArrangeGlyphs();
                    LayoutEngine.Container = null;

                    // Compute final Image Size
                    if (Panel.Size == SizeF.Empty)
                    {
                        float maxWidth = 0f;
                        float maxHeight = 0f;
                        foreach (GlyphBase glyph in Glyphs)
                        {
                            if (glyph.ComputedLayout.PositionF.X + glyph.ComputedLayout.SizeF.Width > maxWidth) { maxWidth = glyph.ComputedLayout.PositionF.X + glyph.ComputedLayout.SizeF.Width; }
                            if (glyph.ComputedLayout.PositionF.Y + glyph.ComputedLayout.SizeF.Height > maxHeight) { maxHeight = glyph.ComputedLayout.PositionF.Y + glyph.ComputedLayout.SizeF.Height; }
                        }
                        retVal = new ImageAdapter((int)(maxWidth + .5f), (int)(maxHeight + .5f), BackgroundColor);
                    }
                    else 
                    {
                        retVal = new ImageAdapter((int)(Panel.Size.Width + .5f),(int)(Panel.Size.Width + .5f), BackgroundColor);
                    }

                    //Render final Image
                    foreach (GlyphBase glyph in Glyphs)
                    {
                        retVal = ImageUtility.CopyImageAdapter(retVal, glyph.GeneratedImage, glyph.ComputedLayout.Position, glyph.ComputedLayout.Size, true);
                    }

                    return retVal;


/*
                    LayoutEngine.ArrangeGlyphs((GlyphBase[])Glyphs.ToArray(typeof(GlyphBase)));

                    IImageAdapter retVal = null;
                    int maxWidth = 0;
                    int maxHeight = 0;
                    // Determine the size of IImageAdapter
                    foreach (GlyphBase glyph in Glyphs)
                    {
                        if (glyph.Layout.PositionF.X + glyph.Layout.SizeF.Width > maxWidth) { maxWidth = (int)(glyph.Layout.PositionF.X + glyph.Layout.SizeF.Width); }
                        if (glyph.Layout.PositionF.Y + glyph.Layout.SizeF.Height > maxHeight) { maxHeight = (int)(glyph.Layout.PositionF.Y + glyph.Layout.SizeF.Height); }
                    }
                    retVal = new ImageAdapter(maxWidth, maxHeight, ColorByte.Empty);


                    foreach(GlyphBase glyph in Glyphs)
                    {
                        IImageAdapter image = glyph._Render();
                        retVal = ImageUtility.CopyImageAdapter(retVal, image, glyph.Layout.Position, glyph.Layout.Size, true);
                    }

                    // Update Image
                    Image = retVal;

                    return retVal;
*/
                }
            #endregion Internal Methods
            #region Public Methods
/*
                /// <summary>
                /// Return the number of matching children 
                /// </summary>
                /// <value></value>
                public override ArrayList Matches
                {
                    get
                    {
                        ArrayList retVal = new ArrayList();
                        foreach (GlyphBase glyph in Glyphs)
                        {
                            retVal.AddRange(glyph.Matches);
                        }
                        return retVal;
                    }
                }
*/
            #endregion Public Methods
        #endregion Methods

        #region ISerializable Members'
            /// <summary>
            /// Serialization Method
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                info.AddValue("Glyphs", Glyphs);
                info.AddValue("LayoutEngine", LayoutEngine.GetType());
            }
        #endregion
    }
}
