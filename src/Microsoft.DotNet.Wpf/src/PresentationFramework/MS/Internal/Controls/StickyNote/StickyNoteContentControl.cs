// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implementation of StickyNoteControl's internal TextBox/RichTextBox and InkCanvas helper classes.
//
//              See spec at StickyNoteControlSpec.mht
//

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Ink;
using System.Windows.Markup;
using System.Xml;

namespace MS.Internal.Controls.StickyNote
{
    /// <summary>
    /// An abstract class which defines the basic operation for StickyNote content 
    /// </summary>
    internal abstract class StickyNoteContentControl
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        protected StickyNoteContentControl(FrameworkElement innerControl)
        {
            SetInnerControl(innerControl);
        }


        private StickyNoteContentControl() { }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Saves the content to an Xml node
        /// </summary>
        /// <param name="node"></param>
        public abstract void Save(XmlNode node);

        /// <summary>
        /// Load the content from an Xml node
        /// </summary>
        /// <param name="node"></param>
        public abstract void Load(XmlNode node);

        /// <summary>
        /// Clears the current content.
        /// </summary>
        public abstract void Clear();

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Checks if the content is empty
        /// </summary>
        abstract public bool IsEmpty
        {
            get;
        }

        /// <summary>
        /// Returns the content type
        /// </summary>
        abstract public StickyNoteType Type
        {
            get;
        }

        /// <summary>
        /// Returns the inner control associated to this content.
        /// </summary>
        public FrameworkElement InnerControl
        {
            get
            {
                return _innerControl;
            }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Sets the internal control. The method also loads the custom style for the control if it's avaliable.
        /// </summary>
        /// <param name="innerControl">The inner control</param>
        protected void SetInnerControl(FrameworkElement innerControl)
        {
            _innerControl = innerControl;
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Protected Fields
        //
        //-------------------------------------------------------------------

        #region Protected Fields

        protected FrameworkElement _innerControl;

        // The maximum size of a byte buffer before its converted to a base64 string.
        protected const long MaxBufferSize = (Int32.MaxValue / 4) * 3;

        #endregion Protected Fields
    }

    /// <summary>
    /// A factory class which creates SticktNote content controls
    /// </summary>
    internal static class StickyNoteContentControlFactory
    {
        //-------------------------------------------------------------------
        //
        //  Private classes
        //
        //-------------------------------------------------------------------

        #region Private classes

        /// <summary>
        /// RichTextBox content implementation
        /// </summary>
        private class StickyNoteRichTextBox : StickyNoteContentControl
        {
            //-------------------------------------------------------------------
            //
            //  Constructors
            //
            //-------------------------------------------------------------------

            #region Constructors

            public StickyNoteRichTextBox(RichTextBox rtb)
                : base(rtb)
            {
                // Used to restrict enforce certain data format during pasting
                DataObject.AddPastingHandler(rtb, new DataObjectPastingEventHandler(OnPastingDataObject));
            }

            #endregion Constructors

            //-------------------------------------------------------------------
            //
            //  Public Methods
            //
            //-------------------------------------------------------------------

            #region Public Methods

            /// <summary>
            /// Clears the inner RichTextBox 
            /// </summary>
            public override void Clear()
            {
                ((RichTextBox)InnerControl).Document = new FlowDocument(new Paragraph(new Run()));
            }

            /// <summary>
            /// Save the RichTextBox data to an Xml node
            /// </summary>
            /// <param name="node"></param>
            public override void Save(XmlNode node)
            {
                // make constant
                Debug.Assert(node != null && !IsEmpty);
                RichTextBox richTextBox = (RichTextBox)InnerControl;

                TextRange rtbRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                if (!rtbRange.IsEmpty)
                {
                    using (MemoryStream buffer = new MemoryStream())
                    {
                        rtbRange.Save(buffer, DataFormats.Xaml);

                        if (buffer.Length.CompareTo(MaxBufferSize) > 0)
                            throw new InvalidOperationException(SR.Get(SRID.MaximumNoteSizeExceeded));

                        // Using GetBuffer avoids making a copy of the buffer which isn't necessary
                        // Safe cast because the array's length can never be greater than Int.MaxValue
                        node.InnerText = Convert.ToBase64String(buffer.GetBuffer(), 0, (int)buffer.Length);
                    }
                }
            }


            /// <summary>
            /// Load the RichTextBox data from an Xml node
            /// </summary>
            /// <param name="node"></param>
            public override void Load(XmlNode node)
            {
                Debug.Assert(node != null);
                RichTextBox richTextBox = (RichTextBox)InnerControl;

                FlowDocument document = new FlowDocument();
                TextRange rtbRange = new TextRange(document.ContentStart, document.ContentEnd, useRestrictiveXamlXmlReader: true);
                using (MemoryStream buffer = new MemoryStream(Convert.FromBase64String(node.InnerText)))
                {
                    rtbRange.Load(buffer, DataFormats.Xaml);
                }

                richTextBox.Document = document;
            }

            #endregion Public Methods

            //-------------------------------------------------------------------
            //
            //  Public Properties
            //
            //-------------------------------------------------------------------

            #region Public Properties

            /// <summary>
            /// A flag whidh indicates if RichTextBox is empty
            /// </summary>
            public override bool IsEmpty
            {
                get
                {
                    RichTextBox richTextBox = (RichTextBox)InnerControl;

                    TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                    return textRange.IsEmpty;
                }
            }

            /// <summary>
            /// Returns Text Type
            /// </summary>
            public override StickyNoteType Type
            {
                get
                {
                    return StickyNoteType.Text;
                }
            }

            #endregion Public Properties


            //-------------------------------------------------------------------
            //
            //  Private Methods
            //
            //-------------------------------------------------------------------

            #region Private Methods

            /// <summary>
            /// Serialization of Images isn't working so we restrict the pasting of images (and as a side effect
            /// all UIElements) into a text StickyNote until the serialization problem is corrected.
            /// </summary>
            private void OnPastingDataObject(Object sender, DataObjectPastingEventArgs e)
            {
                if (e.FormatToApply == DataFormats.Rtf)
                {
                    UTF8Encoding encoding = new UTF8Encoding();

                    // Convert the RTF to Avalon content
                    String rtfString = e.DataObject.GetData(DataFormats.Rtf) as String;
                    MemoryStream data = new MemoryStream(encoding.GetBytes(rtfString));
                    FlowDocument document = new FlowDocument();
                    TextRange range = new TextRange(document.ContentStart, document.ContentEnd);
                    range.Load(data, DataFormats.Rtf);

                    // Serialize the content without UIElements and make it the preferred content for the paste
                    MemoryStream buffer = new MemoryStream();
                    range.Save(buffer, DataFormats.Xaml);
                    DataObject dataObject = new DataObject();
                    dataObject.SetData(DataFormats.Xaml, encoding.GetString(buffer.GetBuffer()));
                    e.DataObject = dataObject;
                    e.FormatToApply = DataFormats.Xaml;
                }
                else if (e.FormatToApply == DataFormats.Bitmap ||
                    e.FormatToApply == DataFormats.EnhancedMetafile ||
                    e.FormatToApply == DataFormats.MetafilePicture ||
                    e.FormatToApply == DataFormats.Tiff)
                {
                    // Cancel all paste operations of stand-alone images
                    e.CancelCommand();
                }
                else if (e.FormatToApply == DataFormats.XamlPackage)
                {
                    // Choose Xaml without UIElements over a XamlPackage
                    e.FormatToApply = DataFormats.Xaml;
                }
            }

            #endregion Private Methods
        }

        /// <summary>
        /// InkCanvas content implementation
        /// </summary>
        private class StickyNoteInkCanvas : StickyNoteContentControl
        {
            //-------------------------------------------------------------------
            //
            //  Constructors
            //
            //-------------------------------------------------------------------

            #region Constructors

            public StickyNoteInkCanvas(InkCanvas canvas)
                : base(canvas)
            {
            }

            #endregion Constructors


            //-------------------------------------------------------------------
            //
            //  Public Methods
            //
            //-------------------------------------------------------------------

            #region Public Methods

            /// <summary>
            /// Clears the inner InkCanvas
            /// </summary>
            public override void Clear()
            {
                ((InkCanvas)InnerControl).Strokes.Clear();
            }

            /// <summary>
            /// Save the stroks data to an Xml node
            /// </summary>
            /// <param name="node"></param>
            public override void Save(XmlNode node)
            {
                Debug.Assert(node != null && !IsEmpty);

                StrokeCollection strokes = ((InkCanvas)InnerControl).Strokes;
                using (MemoryStream buffer = new MemoryStream())
                {
                    strokes.Save(buffer);

                    if (buffer.Length.CompareTo(MaxBufferSize) > 0)
                        throw new InvalidOperationException(SR.Get(SRID.MaximumNoteSizeExceeded));

                    // Using GetBuffer avoids making a copy of the buffer which isn't necessary
                    // Safe cast because the array's length can never be greater than Int.MaxValue                    
                    node.InnerText = Convert.ToBase64String(buffer.GetBuffer(), 0, (int)buffer.Length);
                }
            }

            /// <summary>
            /// Load the stroks data from an Xml node
            /// </summary>
            /// <param name="node"></param>
            public override void Load(XmlNode node)
            {
                Debug.Assert(node != null, "Try to load data from an invalid node");

                StrokeCollection strokes = null;

                if (string.IsNullOrEmpty(node.InnerText))
                {
                    // Create an empty StrokeCollection
                    strokes = new StrokeCollection();
                }
                else
                {
                    using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(node.InnerText)))
                    {
                        strokes = new StrokeCollection(stream);
                    }
                }

                ((InkCanvas)InnerControl).Strokes = strokes;
            }

            #endregion Public Methods

            //-------------------------------------------------------------------
            //
            //  Public Properties
            //
            //-------------------------------------------------------------------

            #region Public Properties

            /// <summary>
            /// A flag which indicates whether the InkCanvas is empty
            /// </summary>
            public override bool IsEmpty
            {
                get
                {
                    return ((InkCanvas)InnerControl).Strokes.Count == 0;
                }
            }

            /// <summary>
            /// Returns the Ink type
            /// </summary>
            public override StickyNoteType Type
            {
                get
                {
                    return StickyNoteType.Ink;
                }
            }

            #endregion Public Properties
        }


        #endregion Private classes

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// A method which creates a specified type content control.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static StickyNoteContentControl CreateContentControl(StickyNoteType type, UIElement content)
        {
            StickyNoteContentControl contentControl = null;

            switch (type)
            {
                case StickyNoteType.Text:
                    {
                        RichTextBox rtb = content as RichTextBox;
                        if (rtb == null)
                            throw new InvalidOperationException(SR.Get(SRID.InvalidStickyNoteTemplate, type, typeof(RichTextBox), SNBConstants.c_ContentControlId));

                        contentControl = new StickyNoteRichTextBox(rtb);
                        break;
                    }
                case StickyNoteType.Ink:
                    {
                        InkCanvas canvas = content as InkCanvas;
                        if (canvas == null)
                            throw new InvalidOperationException(SR.Get(SRID.InvalidStickyNoteTemplate, type, typeof(InkCanvas), SNBConstants.c_ContentControlId));

                        contentControl = new StickyNoteInkCanvas(canvas);
                        break;
                    }
            }

            return contentControl;
        }

        #endregion Public Methods
    }
}
