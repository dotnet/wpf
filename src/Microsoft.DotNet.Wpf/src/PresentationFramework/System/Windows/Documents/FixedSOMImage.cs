// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++                                                              
    Description:
        SOM object that wraps an image on the page. The corresponding markup element can be either an image or 
        a Path with an ImageBrush              
--*/

namespace System.Windows.Documents
{
    using System.Windows.Automation;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using System.Globalization;
    using System.Diagnostics;

    internal sealed class FixedSOMImage : FixedSOMElement
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------
        
        #region Constructors
        private FixedSOMImage(Rect imageRect, GeneralTransform trans, Uri sourceUri, FixedNode node, DependencyObject o) : base(node, trans)
        {
            _boundingRect = trans.TransformBounds(imageRect);
            _source = sourceUri;
            _startIndex = 0;
            _endIndex = 1;
            _name = AutomationProperties.GetName(o);
            _helpText = AutomationProperties.GetHelpText(o);
        }
        #endregion Constructors

        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------

        #region Public Methods

        public static FixedSOMImage Create(FixedPage page, Image image, FixedNode fixedNode)
        {
            Uri imageUri = null;
            if (image.Source is BitmapImage)
            {
                BitmapImage imageSource = image.Source as BitmapImage;
                imageUri = imageSource.UriSource;
            }
            else if (image.Source is BitmapFrame)
            {
                BitmapFrame imageSource = image.Source as BitmapFrame;
                imageUri = new Uri(imageSource.ToString(), UriKind.RelativeOrAbsolute);
            }
            Rect sourceRect = new Rect(image.RenderSize);

            GeneralTransform transform = image.TransformToAncestor(page);            
            return new FixedSOMImage(sourceRect, transform, imageUri, fixedNode, image);
        }

        public static FixedSOMImage Create(FixedPage page, Path path, FixedNode fixedNode)
        {
            Debug.Assert(path.Fill is ImageBrush);
            ImageSource source = ((ImageBrush)(path.Fill)).ImageSource;
            Uri imageUri = null;
            if (source is BitmapImage)
            {
                BitmapImage imageSource = source as BitmapImage;
                imageUri = imageSource.UriSource;
            }
            else if (source is BitmapFrame)
            {
                BitmapFrame imageSource = source as BitmapFrame;
                imageUri = new Uri(imageSource.ToString(), UriKind.RelativeOrAbsolute);
            }

            Rect sourceRect = path.Data.Bounds;
            GeneralTransform trans = path.TransformToAncestor(page);
            return new FixedSOMImage(sourceRect, trans, imageUri, fixedNode, path);
        }

#if DEBUG
       
        public override void Render(DrawingContext dc, string label, DrawDebugVisual debugVisual)
        {
            Pen pen = new Pen(Brushes.Yellow, 1);
            Rect rect = _boundingRect;
            rect.Inflate(5,5);
            dc.DrawRectangle(null, pen , rect);

            if (label != null && debugVisual == DrawDebugVisual.Paragraphs)
            {
                base.RenderLabel(dc, label);
            }
        }
#endif

        #endregion Public Methods

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties
        internal Uri Source
        {
            get { return _source; }
        }

        internal String Name
        {
            get { return _name; }
        }

        internal String HelpText
        {
            get { return _helpText; }
        }
        #endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private Uri _source;
        private String _name;
        private String _helpText;
        #endregion Interanl Fields
        
    }
}

