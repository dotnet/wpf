// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.IO;
        using System.Xml;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Design;
        using System.ComponentModel;
        using System.Drawing.Drawing2D;
        using System.Runtime.Serialization;
        //TODO-Miguep: changed to use binary, check it works the same
        using System.Runtime.Serialization.Formatters.Binary;
    #endregion using

    /// <summary>
    /// EventArgs definition for the NewGlyphMatch event
    /// </summary>
    public class GlyphMatchEventArgs : EventArgs
    {
        /// <summary>
        /// The GlyphModel
        /// </summary>
        public GlyphModel ModelGlyph = null;
        /// <summary>
        /// The matching GlyphBase
        /// </summary>
        public GlyphBase BaseGlyph = null;
        /// <summary>
        /// The MatchingInfo class containing info about this matchy
        /// </summary>
        public MatchingInfo MatchInfo = null;
        /// <summary>
        /// 'Ctor
        /// </summary>
        /// <param name="glyphModel"></param>
        /// <param name="glyphBase"></param>
        /// <param name="matchInfo"></param>
        public GlyphMatchEventArgs (GlyphModel glyphModel, GlyphBase glyphBase, MatchingInfo matchInfo)
        {
            ModelGlyph = glyphModel;
            BaseGlyph = glyphBase;
            MatchInfo = matchInfo;
        }
    }

    /// <summary>
    /// Summary description for GlyphModel.
    /// </summary>
    [SerializableAttribute]
    public class GlyphModel : GlyphContainer, ISerializable
    {
        #region Events
            /// <summary>
            /// event handler for target changed 
            /// </summary>
            public event EventHandler TargetChanged;
            /// <summary>
            /// glyph match event  
            /// </summary>
            public event GlyphMatchEventHandler NewGlyphMatch;
        #endregion Events

        #region Delegates
            /// <summary>
            /// Delegate for matching event handling
            /// </summary>
            public delegate void GlyphMatchEventHandler(object sender, GlyphMatchEventArgs e);
        #endregion Delegates

        #region Properties
            #region Private Properties
                private string _targetpath = string.Empty;
                private IImageAdapter _target = new ImageAdapter(1, 1);
                private string _path = string.Empty;
                private IImageAdapter _matchMap = null;
                private XenoSymbolBroker _symbolBroker = new XenoSymbolBroker();
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// The Target (ImageAdapter)
                /// </summary>
//                [XmlIgnore]
                [Browsable(false)]
                public IImageAdapter Target
                {
                    get { return _target; }
                    set
                    {
                        if (_target == value) { return; }
                        _target = value;
                        base.Size = new Size(_target.Width, _target.Height);
                        Panel.Size = new SizeF(_target.Width, _target.Height);
                        TriggerTargetChangeEvent();
                        _targetpath = string.Empty;
                    }
                }
                /// <summary>
                /// The path of the target 
                /// </summary>
                [Editor(typeof(GlyphFileNameChooser), typeof(UITypeEditor))]
                public string TargetPath
                {
                    get { return _targetpath; }
                    set
                    {
                        if (value == _targetpath) { return; }
                        CheckValidPath(value);
                        _targetpath = value;
                        _target = new ImageAdapter(_targetpath);
                        base.Size = new Size(_target.Width, _target.Height);
                        Panel.Size = new SizeF(_target.Width, _target.Height);
                        TriggerTargetChangeEvent();
                    }
                }
                /// <summary>
                /// The Bitmap result - positive are drawn red bounding boxes
                /// </summary>
//                [XmlIgnore]
                public IImageAdapter MatchMap
                {
                    get 
                    {
                        if (_matchMap == null) { throw new RenderingVerificationException("MatchMap will be generated after a call to MatchAll()"); }
                        return _matchMap; 
                    }
                }
                /// <summary>
                /// The Typographer of the model
                /// </summary>
//                [XmlIgnore]
                [System.Runtime.InteropServices.ComVisibleAttribute(false)]
                public ITypographer Typographer
                {
                    get
                    {
                        return TypographerBroker.Instance;
                    }
                }
/*
                /// <summary>
                /// The external Sumbol borker (theme proxy)
                /// </summary>
                [XmlIgnore]
                public XenoSymbolBroker SymbolBroker
                {
                    get
                    {
                        return _symbolBroker;
                    }
                }
*/
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// The default constructor 
            /// </summary>
            public GlyphModel() : base()
            {
                Size = new Size(320, 200);
                Panel.Size = new SizeF(Size.Width, Size.Height);
            }
            /// <summary>
            /// Deserialization Constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected GlyphModel(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                TargetPath =(string) info.GetValue("TargetPath", typeof(string));
            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private static void CheckValidPath(string fileName)
                {
                    Uri uri = null;
                    if (fileName == null || fileName.Trim() == string.Empty)
                    {
                        throw new ArgumentException("File name passed in is invalid (null or empty)");
                    }
                    if (Uri.TryCreate(fileName, UriKind.RelativeOrAbsolute, out uri) == false)
                    {
                        throw new ArgumentException("File name passed in ('" + fileName + "') is invalid");
                    }
                }
                /// <summary>
                /// Triggers the Target changed event 
                /// </summary>
                private void TriggerTargetChangeEvent()
                {
                    if (TargetChanged != null) { TargetChanged(this, new EventArgs()); }
                }
                /// <summary>
                /// Generates the Bitmap result 
                /// </summary>
                private void GenerateMatchMap()
                {
                    _matchMap = (IImageAdapter)Target.Clone();

                    foreach (GlyphBase glyph in Glyphs)
                    {
                        int nbMatch = glyph.CompareInfo.Matches.Count;
                        for (int i = 0; i < nbMatch; i++)
                        {
                            MatchingInfo matchInfo = (MatchingInfo)(glyph.CompareInfo.Matches[i]);
                            Rectangle rect = matchInfo.BoundingBox;
                            ImageUtility.FillRect(_matchMap, rect, new ColorByte(80, 255, 0, 0));
                            ImageUtility.DrawRect(_matchMap, rect, (ColorByte)Color.Yellow);
                        }
                    }
                }
            #endregion Private Methods
            #region Internal Methods
                /// <summary>
                /// match handler 
                /// </summary>
                internal void MatchHandler(GlyphBase o, MatchingInfo i)
                {
                    if (NewGlyphMatch != null)
                    {
                        NewGlyphMatch(this, new GlyphMatchEventArgs(this, o, i));
                    }
                }
            #endregion Internal Methods
            #region Public Methods
                /// <summary>
                /// Perfoms the Model matching by matching all the model glyphs
                /// </summary>
                public bool MatchAll()
                {
                    GeneratedImage = Render();

                    GlyphComparator glyphCompare = new GlyphComparator();
                    int count = glyphCompare.FindGlyphs((GlyphBase[])Glyphs.ToArray(typeof(GlyphBase)), this);

                    GenerateMatchMap();

                    return (count == Glyphs.Count);
                }
                /// <summary>
                /// Measure the model geometry
                /// </summary>
                public override SizeF Measure() { return SizeF.Empty; }
                /// <summary>
                /// Model serialization
                /// </summary>
                public void Serialize(string fileName)
                {
                    CheckValidPath(fileName);

                    FileStream fileStream = null;
                    try
                    {
                        fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                        BinaryFormatter formatter = new BinaryFormatter();
                        formatter.AssemblyFormat = 0;// System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple; TODO-Miguep: conflicts with newtonsoft.
                        formatter.Serialize(fileStream, this);
                    }
                    finally 
                    {
                        if (fileStream != null) { fileStream.Close(); fileStream = null; }
                    }
                }
                /// <summary>
                /// Model Deserialization
                /// </summary>
                public static GlyphModel Deserialize(string fileName)
                {
                    CheckValidPath(fileName);
                    if (File.Exists(fileName) == false)
                    {
                        throw new FileNotFoundException("The specified file cannot be found (File deleted ? Wrong location ? Local file ? Network issue ? Localization problem ?)", fileName);
                    }

                    GlyphModel retVal = null;
                    FileStream fileStream = null;
                    try
                    {
                        fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                        BinaryFormatter formatter = new BinaryFormatter();
                        formatter.AssemblyFormat = 0;// System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple; TODO-Miguep: conflicts with newtonsoft
                        // formatter.Binder = syntheticalBinder
                        retVal = (GlyphModel)formatter.Deserialize(fileStream);
                    }
                    finally 
                    {
                        if (fileStream != null) { fileStream.Close(); fileStream = null; }
                    }
                    return retVal;
                }
            #endregion Public Methods
        #endregion Methods

        #region ISerializable Members
            /// <summary>
            /// Serialization Method
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                info.AddValue("TargetPath", TargetPath);
            }
        #endregion
    }
}
