// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Synthetical
{
    #region using
        using System;
        using System.IO;
        using System.Drawing;
        using System.Windows.Forms;
        using System.Globalization;
        using System.Drawing.Design;
        using System.ComponentModel;
        using System.Runtime.Serialization;
        //using System.Runtime.Serialization.Formatters.Soap;
        using Microsoft.Test.RenderingVerification.Filters;
    #endregion using

    /// <summary>
    /// Summary description for GlyphImage.
    /// </summary>
    [SerializableAttribute]
    public class GlyphImage: GlyphBase, ISerializable
    {
        #region Properties
            #region Private Properties
//                private Bitmap _image = null;
                private IImageAdapter _imageToUse = null;
                private string _path = string.Empty;
            #endregion Private Properties
            #region Public Properties
                /// <summary>
                /// Get/set The path to the image to load
                /// </summary>
                [Editor(typeof(GlyphFileNameChooser), typeof(UITypeEditor))]
                public string Path
                {
                    get { return _path; }
                    set
                    {
                        if (_path.Trim().ToLower(CultureInfo.InvariantCulture) ==  value.Trim().ToLower(CultureInfo.InvariantCulture)) { return; }
                        if (value == null) { throw new ArgumentNullException("Path", "Must be set to a valid instance of an object (null passed in)"); }
                        if (File.Exists(value) == false) { throw new FileNotFoundException("The file specified by 'Path' was not found", value); }
                        _imageToUse = new ImageAdapter(value);
                        _path = value;
                        SetSizeLocation(_imageToUse);
                    }
                }
                /// <summary>
                /// Set a new image to use (will discard the 'Path' value)
                /// </summary>
                /// <value></value>
                public IImageAdapter ImageToUse
                {
                    get 
                    {
                        return _imageToUse;
                    }
                    set 
                    {
                        if (value == null) { throw new ArgumentNullException("ImageToUse", "Must be set to a valid instance of an object implementing IImageAdapter (null passed in)"); }
                        if (value == _imageToUse) { return; }
                        _path = string.Empty;
                        _imageToUse = value;
                        SetSizeLocation(_imageToUse);
                    }
                }
            #endregion Public Properties
        #endregion Properties

        #region Constructors
            /// <summary>
            /// The default constructor - XML serialixation only
            /// </summary>
            public GlyphImage() : base() 
            {
            }
            /// <summary>
            /// The constructor
            /// </summary>
            public GlyphImage(GlyphContainer container) : base(container)
            {
            }
            /// <summary>
            /// Deserialization Constructor
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected GlyphImage(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                string path = (string)info.GetValue("Path", typeof(string));
                if (path != null && path != string.Empty)
                {
                    Path = path;
                }
                else 
                {
                    using ( Bitmap imageToUse = (Bitmap)info.GetValue("ImageToUse", typeof(Bitmap)))
                    {
                        if (imageToUse != null) { ImageToUse = new ImageAdapter(imageToUse); }
                    }
                }

            }
        #endregion Constructors

        #region Methods
            #region Private Methods
                private void SetSizeLocation(IImageAdapter image)
                {
                    Size = new Size(image.Width, image.Height);
                    Panel.Size = new SizeF(image.Width, image.Height);
                }
            #endregion Private Methods
            #region Internal Methods
            /// <summary>
            /// internal render 
                /// </summary>
                internal override IImageAdapter _Render()
                {
                    // @review : return IImageAdapter with only empty colors instead of throwing ?
                    if (_imageToUse == null) { throw new ApplicationException("Neither ImageToUse nor Path as been set, this GlyphImage is empty"); }
                    GeneratedImage = new ImageAdapter(_imageToUse);
                    return GeneratedImage;
                }
            #endregion Internal Methods
            #region Public Methods
            #endregion Public Methods
        #endregion Methods

        #region ISerializable Members
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                if (_path != string.Empty)
                {
                    info.AddValue("Path", _path);
                }
                else 
                {
                    if (_imageToUse != null)
                    {
                        Bitmap bmp = ImageUtility.ToBitmap(_imageToUse);
                        info.AddValue("ImageToUse", bmp);   // Dispose will be called by Formatter ?
                    }
                }
            }
        #endregion
    }


    /// <summary>
    /// dedicated editor for files 
    /// </summary>
    public class GlyphFileNameChooser: UITypeEditor
    {
        /// <summary>
        /// the editing style - modal in this case
        /// </summary>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        /// <summary>
        /// returns the edited value
        /// </summary>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            string fileName = string.Empty;
            using ( OpenFileDialog opd = new OpenFileDialog() )
            {
                opd.Filter = string.Empty;
                if (context.Instance != null)
                {
#if TODO
                if (context.Instance is VXaml)
                {
                    opd.Filter = "Xaml files (*.xaml)|*.xaml";
                }
#endif
                }
                if (opd.ShowDialog() != DialogResult.OK)
                {
                    return string.Empty;
                }
            }

            return fileName;
        }
    }













}
