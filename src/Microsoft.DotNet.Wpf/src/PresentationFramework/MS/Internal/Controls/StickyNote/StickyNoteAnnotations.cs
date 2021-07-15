// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: This is the partial class of the StickyNoteControl.
//              This file contains all CAF annotation related implementations.
//
//              See spec at StickyNoteControlSpec.mht
//

using MS.Internal;
using MS.Internal.Annotations;
using MS.Internal.Annotations.Component;
using MS.Internal.Controls;
using MS.Internal.Controls.StickyNote;
using MS.Internal.KnownBoxes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;                           // Assert
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Windows.Documents;
using MS.Internal.Documents;
using MS.Internal.Annotations.Anchoring; //TextSelectionHelper
using System.Windows.Controls.Primitives;   // IScrollInfo
using MS.Utility;



namespace MS.Internal.Controls.StickyNote
{
    // NOTICE-2004/08/18,
    // Whenever we add a new type data to this enum type, make sure to update SNCAnnotation.AllValues and SNCAnnotation.AllContents.
    // This is a collection which contains all the Xml elements being used by StickyNoteControl CAF schema.
    [System.Flags]
    internal enum XmlToken
    {
        MetaData = 0x00000001,
        Left = 0x00000004,
        Top = 0x00000008,
        XOffset = 0x00000010,
        YOffset = 0x00000020,
        Width = 0x00000080,
        Height = 0x00000100,
        IsExpanded = 0x00000200,
        Author = 0x00000400,
        Text = 0x00002000,
        Ink = 0x00008000,
        ZOrder = 0x00020000
    }

    // This a wrapper class which encapsulates the operations for dealing with CAF's cargo, resources and XmlElements.
    // The schema can be found in http://tabletpc/longhorn/Specs/WinFX%20StickyNoteControl%20M8.1.mht#_Toc79371211
    internal class SNCAnnotation
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructor

        // This is a static constructor which initializes the internal xml name table and the name space manager.
        static SNCAnnotation()
        {
            // Create our xml name dictionary.
            s_xmlTokeFullNames = new Dictionary<XmlToken, string>();

            // Fill in the name dictionary.
            foreach (XmlToken val in Enum.GetValues(typeof(XmlToken)))
            {
                AddXmlTokenNames(val);
            }
        }

        public SNCAnnotation(Annotation annotation)
        {
            Debug.Assert(annotation != null);

            _annotation = annotation;
            _isNewAnnotation = _annotation.Cargos.Count == 0;

            // Initialize the data cache collection.
            _cachedXmlElements = new Dictionary<XmlToken, object>();
        }

        private SNCAnnotation() { }

        #endregion Constructor

        //-------------------------------------------------------------------------------
        //
        // Public Methods
        //
        //-------------------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// This static method will update an Annotation object with the specified data in a StickyNoteControl
        /// </summary>
        /// <param name="token">A flag which indicates the data needs to be updated. The flag can be combinated with the any valid bits.</param>
        /// <param name="snc">A StickyNoteControl instance</param>
        /// <param name="sncAnnotation">An SNCAnnotation object which contains a CAF annotation object</param>
        public static void UpdateAnnotation(XmlToken token, StickyNoteControl snc, SNCAnnotation sncAnnotation)
        {
            AnnotationService service = null;
            bool autoFlush = false;
            try
            {
                service = AnnotationService.GetService(((IAnnotationComponent)snc).AnnotatedElement);
                if (service != null && service.Store != null)
                {
                    autoFlush = service.Store.AutoFlush;
                    // Temporarily turn off autoflush until we are done
                    // updating all the necessary values
                    service.Store.AutoFlush = false;
                }

                Debug.Assert((token & AllValues) != 0);

                // Update Ink
                if ((token & XmlToken.Ink) != 0 && snc.Content.Type == StickyNoteType.Ink)
                {
                    sncAnnotation.UpdateContent(snc, true, XmlToken.Ink);
                }

                // Update Text
                if ((token & XmlToken.Text) != 0 && snc.Content.Type == StickyNoteType.Text)
                {
                    sncAnnotation.UpdateContent(snc, true, XmlToken.Text);
                }


                // Update MetaData
                if ((token & NegativeAllContents) != 0)
                {
                    UpdateMetaData(token, snc, sncAnnotation);
                }
            }
            finally
            {
                if (service != null && service.Store != null)
                {
                    // If auto flush was true before, setting it to true again should cause a flush.
                    service.Store.AutoFlush = autoFlush;
                }
            }
        }

        /// <summary>
        /// This static method will update a StickyNoteControl object with the specified data in an Annotation
        /// </summary>
        /// <param name="token">A flag which indicates the data needs to be updated. The flag can be combinated with the any valid bits.</param>
        /// <param name="snc">A StickyNoteControl instance</param>
        /// <param name="sncAnnotation">An SNCAnnotation object which contains a CAF annotation object</param>
        public static void UpdateStickyNoteControl(XmlToken token, StickyNoteControl snc, SNCAnnotation sncAnnotation)
        {
            Invariant.Assert((token & AllValues) != 0, "No token specified.");
            Invariant.Assert(snc != null, "Sticky Note Control is null.");
            Invariant.Assert(sncAnnotation != null, "Annotation is null.");

            // FUTURE-2004/08/18-WAYNEZEN,
            // Updating the xml data is synchronized below which could pontentially block the UIContext
            // for updating the huge amount data like Ink.
            // We could conside to post the callbacks to the UIContext for update data in the future.

            XmlAttribute node;

            // Update Ink
            if ((token & XmlToken.Ink) != 0 && sncAnnotation.HasInkData)
            {
                sncAnnotation.UpdateContent(snc, false, XmlToken.Ink);
            }

            // Update Text
            if ((token & XmlToken.Text) != 0 && sncAnnotation.HasTextData)
            {
                sncAnnotation.UpdateContent(snc, false, XmlToken.Text);
            }

            // Update Author
            if ((token & XmlToken.Author) != 0)
            {
                int nCount = sncAnnotation._annotation.Authors.Count;
                // Get the culture specific text separator.
                string listSeparator = snc.Language.GetSpecificCulture().TextInfo.ListSeparator;
                string authors = string.Empty;
                for (int i = 0; i < nCount; i++)
                {
                    if (i != 0)
                    {
                        authors += listSeparator + sncAnnotation._annotation.Authors[i];
                    }
                    else
                    {
                        authors += sncAnnotation._annotation.Authors[i];
                    }
                }

                // Setting the author property will cause the UI to update
                snc.SetValue(StickyNoteControl.AuthorPropertyKey, authors);
            }

            // Update Height
            if ((token & XmlToken.Height) != 0)
            {
                node = (XmlAttribute)sncAnnotation.FindData(XmlToken.Height);
                if (node != null)
                {
                    double height = Convert.ToDouble(node.Value, CultureInfo.InvariantCulture);
                    snc.SetValue(FrameworkElement.HeightProperty, height);
                }
                else
                {
                    snc.ClearValue(FrameworkElement.HeightProperty);
                }
            }

            // Update Width
            if ((token & XmlToken.Width) != 0)
            {
                node = (XmlAttribute)sncAnnotation.FindData(XmlToken.Width);
                if (node != null)
                {
                    double width = Convert.ToDouble(node.Value, CultureInfo.InvariantCulture);
                    snc.SetValue(FrameworkElement.WidthProperty, width);
                }
                else
                {
                    snc.ClearValue(FrameworkElement.WidthProperty);
                }
            }

            // Update IsExpanded
            if ((token & XmlToken.IsExpanded) != 0)
            {
                node = (XmlAttribute)sncAnnotation.FindData(XmlToken.IsExpanded);
                if (node != null)
                {
                    bool expanded = Convert.ToBoolean(node.Value, CultureInfo.InvariantCulture);
                    snc.IsExpanded = expanded;
                }
                else
                {
                    snc.ClearValue(StickyNoteControl.IsExpandedProperty);
                }
            }

            // Update ZOrder
            if ((token & XmlToken.ZOrder) != 0)
            {
                node = (XmlAttribute)sncAnnotation.FindData(XmlToken.ZOrder);
                if (node != null)
                {
                    ((IAnnotationComponent)snc).ZOrder = Convert.ToInt32(node.Value, CultureInfo.InvariantCulture);
                }
            }

            // Update Position
            if ((token & PositionValues) != 0)
            {
                TranslateTransform transform = new TranslateTransform();
                if ((token & XmlToken.Left) != 0)
                {
                    node = (XmlAttribute)sncAnnotation.FindData(XmlToken.Left);
                    if (node != null)
                    {
                        double left = Convert.ToDouble(node.Value, CultureInfo.InvariantCulture);
                        // All 'left' values are persisted assuming two things:
                        //  1) the top-left corner (visually) of the StickyNote is the origin of its coordinate space
                        //  2) the positive x-axis of its parent is to the right
                        // This flag signals that we have a positive x-axis to the left and our
                        // top-right corner (visually) is our origin.  So we need to flip the
                        // value before using it.
                        if (snc.FlipBothOrigins)
                        {
                            left = -(left + snc.Width);
                        }
                        transform.X = left;
                    }
                }

                if ((token & XmlToken.Top) != 0)
                {
                    node = (XmlAttribute)sncAnnotation.FindData(XmlToken.Top);
                    if (node != null)
                    {
                        double top = Convert.ToDouble(node.Value, CultureInfo.InvariantCulture);
                        transform.Y = top;
                    }
                }

                // Now, we update the StickyNote offset
                if ((token & XmlToken.XOffset) != 0)
                {
                    node = (XmlAttribute)sncAnnotation.FindData(XmlToken.XOffset);
                    if (node != null)
                    {
                        snc.XOffset = Convert.ToDouble(node.Value, CultureInfo.InvariantCulture);
                    }
                }

                if ((token & XmlToken.YOffset) != 0)
                {
                    node = (XmlAttribute)sncAnnotation.FindData(XmlToken.YOffset);
                    if (node != null)
                    {
                        snc.YOffset = Convert.ToDouble(node.Value, CultureInfo.InvariantCulture);
                    }
                }

                // Set the adorner layer transform.
                snc.PositionTransform = transform;
            }
        }


        #endregion Public Methods

        //-------------------------------------------------------------------------------
        //
        // Public Fields
        //
        //-------------------------------------------------------------------------------

        #region Public Fields

        // A const field which contains all the Xml Elements which are mapped to a StickyNoteControl's properties.
        public const XmlToken AllValues = XmlToken.Left | XmlToken.Top | XmlToken.XOffset | XmlToken.YOffset | XmlToken.Width | XmlToken.Height
                                                | XmlToken.IsExpanded | XmlToken.Author
                                                | XmlToken.Text | XmlToken.Ink | XmlToken.ZOrder;

        // A const field which contains all the Xml Elements which are related to the StickyNotes persisted position.
        public const XmlToken PositionValues = XmlToken.Left | XmlToken.Top | XmlToken.XOffset | XmlToken.YOffset;
        // A const field which contains all the Xml Elements for initial draw of the StickyNote (this includes the offsets but not the ZOrder).
        public const XmlToken Sizes = XmlToken.Left | XmlToken.Top | XmlToken.XOffset | XmlToken.YOffset | XmlToken.Width | XmlToken.Height;
        // A const field which contains all the Xml Elements which are related to the properties of the its contents.
        public const XmlToken AllContents = XmlToken.Text | XmlToken.Ink;
        public const XmlToken NegativeAllContents = AllValues ^ XmlToken.Text ^ XmlToken.Ink;

        #endregion Public Fields

        //-------------------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Returns whether this annotation is considered new by the StickyNoteControl.
        /// </summary>
        public bool IsNewAnnotation
        {
            get
            {
                return _isNewAnnotation;
            }
        }

        /// <summary>
        /// Returns whether this annotation has ink data.
        /// </summary>
        public bool HasInkData
        {
            get
            {
                return FindData(XmlToken.Ink) != null;
            }
        }

        /// <summary>
        /// Returns wether this annotation has text data.
        /// </summary>
        public bool HasTextData
        {
            get
            {
                return FindData(XmlToken.Text) != null;
            }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// This method will find a specified cargo.
        /// </summary>
        /// <param name="cargoName">A specified cargo name</param>
        /// <returns>The existing cargo or null</returns>
        private AnnotationResource FindCargo(string cargoName)
        {
            foreach (AnnotationResource cargo in _annotation.Cargos)
            {
                if (cargoName.Equals(cargo.Name))
                    return cargo;
            }

            return null;
        }

        /// <summary>
        /// Find the specified Annotation data
        /// </summary>
        /// <param name="token">A flag which is corresponding to the annotation data</param>
        /// <returns>The annotation data or null</returns>
        private object FindData(XmlToken token)
        {
            // Assume that we can't find any thing.
            object ret = null;

            // First, check if we have the data cached.
            if (_cachedXmlElements.ContainsKey(token))
            {
                ret = _cachedXmlElements[token];
            }
            else
            {
                // Then, we try to search the data in the current annotation.
                AnnotationResource cargo = FindCargo(GetCargoName(token));
                if (cargo != null)
                {
                    ret = SNCAnnotation.FindContent(token, cargo);

                    // If we found the data in annotation, we go ahead cache it.
                    if (ret != null)
                    {
                        _cachedXmlElements.Add(token, ret);
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Returns the AnnotationResource and the XML root for the given token from the passed in annotation.
        /// If the cargo or root do not exist they are created but not added to the annotation.  The newCargo
        /// and newRoot flags specify whether they were created.  The caller must use these to add the items
        /// to the annotation after they are done with their modifications.
        /// </summary>
        /// <param name="annotation">the current annotation</param>
        /// <param name="token">the token to be processed</param>
        /// <param name="cargo">the cargo for the token</param>
        /// <param name="root">the root XML element</param>
        /// <param name="newCargo">means a new root and new cargo was created.  the root was already added to the cargo but the cargo was not added to the annotation</param>
        /// <param name="newRoot">means a new root was created.  it has not been added to the cargo.</param>
        private static void GetCargoAndRoot(
            SNCAnnotation annotation, XmlToken token, out AnnotationResource cargo, out XmlElement root, out bool newCargo, out bool newRoot)
        {
            Invariant.Assert(annotation != null, "Annotation is null.");
            Invariant.Assert((token & (AllValues | XmlToken.MetaData)) != 0, "No token specified.");

            string cargoName = GetCargoName(token);

            newRoot = false;
            newCargo = false;
            cargo = annotation.FindCargo(cargoName);

            // Cargo exists
            if (cargo != null)
            {
                root = FindRootXmlElement(token, cargo);
                // Uncommon situation - cargo created without root XmlElement
                if (root == null)
                {
                    newRoot = true;
                    XmlDocument xmlDoc = new XmlDocument();
                    root = xmlDoc.CreateElement(GetXmlName(token), AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);
                    // Don't add it to the cargo yet - wait until all the
                    // values are set on it
                }
            }
            else
            {
                newCargo = true;
                cargo = new AnnotationResource(cargoName);
                XmlDocument xmlDoc = new XmlDocument();
                root = xmlDoc.CreateElement(GetXmlName(token), AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);

                // Since the cargo is new, its safe to add the root to it
                // No events will make it to the annotation yet
                cargo.Contents.Add(root);
            }
        }

        /// <summary>
        /// Updates the value of the specified token on the specified XmlElement.
        /// If the value is the same, no update is made.  If the new value is null
        /// the attribute is removed.
        /// </summary>
        private void UpdateAttribute(XmlElement root, XmlToken token, string value)
        {
            string name = GetXmlName(token);

            XmlNode oldValue = root.GetAttributeNode(name, AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);
            if (oldValue == null)
            {
                if (value == null)
                    return;
                else
                    root.SetAttribute(name, AnnotationXmlConstants.Namespaces.BaseSchemaNamespace, value);
            }
            else
            {
                if (value == null)
                    root.RemoveAttribute(name, AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);
                else if (oldValue.Value != value)
                    root.SetAttribute(name, AnnotationXmlConstants.Namespaces.BaseSchemaNamespace, value);
            }
        }

        /// <summary>
        /// This static method returns the Xml name for the specified token.
        /// </summary>
        /// <param name="token">A specified token</param>
        /// <returns>The name for the Xml node</returns>
        private static string GetXmlName(XmlToken token)
        {
            return s_xmlTokeFullNames[token];
        }

        /// <summary>
        /// This method is called by the static constructor to set up the Xml name dictionary.
        /// </summary>
        /// <param name="token">A specified token</param>
        private static void AddXmlTokenNames(XmlToken token)
        {
            // Conver the enum value to the string first.
            string xmlName = token.ToString();

            // Depending on the data, we add the proper prefix to the full name.
            switch (token)
            {
                // The element names should be qualified
                case XmlToken.MetaData:
                case XmlToken.Text:
                case XmlToken.Ink:
                    {
                        s_xmlTokeFullNames.Add(token, AnnotationXmlConstants.Prefixes.BaseSchemaPrefix + ":" + xmlName);
                    }
                    break;

                //the attribute names should be local
                case XmlToken.Left:
                case XmlToken.Top:
                case XmlToken.XOffset:
                case XmlToken.YOffset:
                case XmlToken.Width:
                case XmlToken.Height:
                case XmlToken.IsExpanded:
                case XmlToken.ZOrder:
                default:
                    {
                        s_xmlTokeFullNames.Add(token, xmlName);
                    }
                    break;
            }
        }

        /// <summary>
        /// Return the cargo name which hosts the specified data.
        /// </summary>
        /// <param name="token">The specified data</param>
        /// <returns>The host cargo name</returns>
        private static string GetCargoName(XmlToken token)
        {
            string cargoName;
            switch (token)
            {
                // Those tokens use *snc* prefix.
                case XmlToken.MetaData:
                case XmlToken.Left:
                case XmlToken.Top:
                case XmlToken.XOffset:
                case XmlToken.YOffset:
                case XmlToken.Width:
                case XmlToken.Height:
                case XmlToken.IsExpanded:
                case XmlToken.ZOrder:
                    {
                        cargoName = SNBConstants.MetaResourceName;
                    }
                    break;
                // Those tokens use *media* prefix.
                case XmlToken.Text:
                    {
                        cargoName = SNBConstants.TextResourceName;
                    }
                    break;
                case XmlToken.Ink:
                    {
                        cargoName = SNBConstants.InkResourceName;
                    }
                    break;
                default:
                    {
                        cargoName = string.Empty;
                        Debug.Assert(false);
                    }
                    break;
            }
            return cargoName;
        }

        /// <summary>
        /// The method returns the root node which contains the specified data in a cargo.
        /// </summary>
        /// <param name="token">The specified data</param>
        /// <param name="cargo">The specified cargo</param>
        /// <returns>The root node or null</returns>
        private static XmlElement FindRootXmlElement(XmlToken token, AnnotationResource cargo)
        {
            Debug.Assert(cargo != null);

            XmlElement element = null;
            string xmlName = string.Empty;

            // Get the xml name of the root node
            switch (token)
            {
                case XmlToken.Text:
                case XmlToken.Ink:
                    xmlName = GetXmlName(token);
                    break;
                case XmlToken.MetaData:
                case XmlToken.IsExpanded:
                case XmlToken.Width:
                case XmlToken.Height:
                case XmlToken.Top:
                case XmlToken.Left:
                case XmlToken.XOffset:
                case XmlToken.YOffset:
                case XmlToken.ZOrder:
                    xmlName = GetXmlName(XmlToken.MetaData);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            // Search the root in the cargo's contents.
            foreach (XmlElement node in cargo.Contents)
            {
                if (node.Name.Equals(xmlName))
                {
                    element = node;
                    break;
                }
            }

            return element;
        }

        /// <summary>
        /// Find the specified data in a cargo.
        /// </summary>
        /// <param name="token">The specified data</param>
        /// <param name="cargo">The cargo which we are searhing in</param>
        /// <returns>The data object or null</returns>
        private static object FindContent(XmlToken token, AnnotationResource cargo)
        {
            object content = null;
            XmlElement root = SNCAnnotation.FindRootXmlElement(token, cargo);

            // If we found the root node, we should use XPath to query the node which contains the corresponding data.
            // The StickyNoteControl's xml schema can be found
            // in http://tabletpc/longhorn/Specs/WinFX%20StickyNoteControl%20M8.1.mht#_Toc79371211
            if (root != null)
            {
                switch (token)
                {
                    case XmlToken.Text:
                    case XmlToken.Ink:
                        return root;
                    case XmlToken.IsExpanded:
                    case XmlToken.ZOrder:
                    case XmlToken.Top:
                    case XmlToken.Left:
                    case XmlToken.XOffset:
                    case XmlToken.YOffset:
                    case XmlToken.Width:
                    case XmlToken.Height:
                        return root.GetAttributeNode(GetXmlName(token), AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);
                    default:
                        Debug.Assert(false);
                        break;
                }
            }

            return content;
        }

        // Update ink data from/to SNC.
        private void UpdateContent(StickyNoteControl snc, bool updateAnnotation, XmlToken token)
        {
            Invariant.Assert(snc != null, "Sticky Note Control is null.");
            Invariant.Assert((token & AllContents) != 0, "No token specified.");

            StickyNoteContentControl contentControl = snc.Content;

            // Template hasn't been applied yet.  Once it has the content control will then be setup.
            if (contentControl == null)
            {
                return;
            }

            // Check whether the annotation data matches the content control.
            if ((token == XmlToken.Ink && contentControl.Type != StickyNoteType.Ink)
                || (token == XmlToken.Text && contentControl.Type != StickyNoteType.Text))
            {
                Debug.Assert(false, "The annotation data does match with the current content control in StickyNote");
                return;
            }

            XmlElement root = null;

            if (updateAnnotation)
            {
                // Update annotation from SNC

                AnnotationResource cargo = null;
                bool newRoot = false;
                bool newCargo = false;

                // Check if the text is empty.
                if (!contentControl.IsEmpty)
                {
                    GetCargoAndRoot(this, token, out cargo, out root, out newCargo, out newRoot);
                    contentControl.Save(root);
                }
                else
                {
                    string cargoName = GetCargoName(token);
                    cargo = FindCargo(cargoName);
                    if (cargo != null)
                    {
                        _annotation.Cargos.Remove(cargo);
                        _cachedXmlElements.Remove(token);
                    }
                }
                if (newRoot)
                {
                    Invariant.Assert(root != null, "XmlElement should have been created.");
                    Invariant.Assert(cargo != null, "Cargo should have been retrieved.");
                    cargo.Contents.Add(root);
                }
                if (newCargo)
                {
                    Invariant.Assert(cargo != null, "Cargo should have been created.");
                    _annotation.Cargos.Add(cargo);
                }
            }
            else
            {
                // Update SNC from annotation

                // Check if we have the text data in the xml store.
                XmlElement node = (XmlElement)FindData(token);
                if (node != null)
                {
                    contentControl.Load(node);
                }
                else
                {
                    if (!contentControl.IsEmpty)
                    {
                        contentControl.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Update the metadata tokens specified in token.
        /// </summary>
        private static void UpdateMetaData(XmlToken token, StickyNoteControl snc, SNCAnnotation sncAnnotation)
        {
            bool newCargo, newRoot;
            AnnotationResource cargo;
            XmlElement root;

            GetCargoAndRoot(sncAnnotation, XmlToken.MetaData, out cargo, out root, out newCargo, out newRoot);

            // Update Expanded
            if ((token & XmlToken.IsExpanded) != 0)
            {
                bool expanded = snc.IsExpanded;
                sncAnnotation.UpdateAttribute(root, XmlToken.IsExpanded, expanded.ToString(CultureInfo.InvariantCulture));
            }

            // Update Height
            if ((token & XmlToken.Height) != 0)
            {
                Debug.Assert(snc.IsExpanded);
                double height = (double)snc.GetValue(FrameworkElement.HeightProperty);
                sncAnnotation.UpdateAttribute(root, XmlToken.Height, height.ToString(CultureInfo.InvariantCulture));
            }

            // Update Width
            if ((token & XmlToken.Width) != 0)
            {
                Debug.Assert(snc.IsExpanded);
                double width = (double)snc.GetValue(FrameworkElement.WidthProperty);
                sncAnnotation.UpdateAttribute(root, XmlToken.Width, width.ToString(CultureInfo.InvariantCulture));
            }

            // Update Left
            if ((token & XmlToken.Left) != 0)
            {
                double left = snc.PositionTransform.X;
                // All 'left' values are persisted assuming two things:
                //  1) the top-left corner (visually) of the StickyNote is the origin of its coordinate space
                //  2) the positive x-axis of its parent is to the right
                // This flag signals that we have a positive x-axis to the left and our
                // top-right corner (visually) is our origin.  So we need to flip the
                // value before persisting it.
                if (snc.FlipBothOrigins)
                {
                    left = -(left + snc.Width);
                }
                sncAnnotation.UpdateAttribute(root, XmlToken.Left, left.ToString(CultureInfo.InvariantCulture));
            }

            // Update Top
            if ((token & XmlToken.Top) != 0)
            {
                sncAnnotation.UpdateAttribute(root, XmlToken.Top, snc.PositionTransform.Y.ToString(CultureInfo.InvariantCulture));
            }

            // Update XOffset
            if ((token & XmlToken.XOffset) != 0)
            {
                sncAnnotation.UpdateAttribute(root, XmlToken.XOffset, snc.XOffset.ToString(CultureInfo.InvariantCulture));
            }

            // Update YOffset
            if ((token & XmlToken.YOffset) != 0)
            {
                sncAnnotation.UpdateAttribute(root, XmlToken.YOffset, snc.YOffset.ToString(CultureInfo.InvariantCulture));
            }

            // Update ZOrder
            if ((token & XmlToken.ZOrder) != 0)
            {
                sncAnnotation.UpdateAttribute(root, XmlToken.ZOrder, ((IAnnotationComponent)snc).ZOrder.ToString(CultureInfo.InvariantCulture));
            }

            if (newRoot)
            {
                cargo.Contents.Add(root);
            }
            if (newCargo)
            {
                sncAnnotation._annotation.Cargos.Add(cargo);
            }
        }

        #endregion Private Methods

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        private static Dictionary<XmlToken, string> s_xmlTokeFullNames;     // A dictionary for the names of the xml elements

        private Dictionary<XmlToken, object> _cachedXmlElements;   // A dictionary for caching the data object
        private Annotation _annotation;
        private readonly bool _isNewAnnotation;

        #endregion Private Fields
    }
}

namespace System.Windows.Controls
{
    public partial class StickyNoteControl
    {
        //-------------------------------------------------------------------------------
        //
        // IAnnotationComponent Interface
        //
        //-------------------------------------------------------------------------------

        #region IAnnotationComponent Members
        /// <summary>
        /// Adds an attached annotations to this StickyNoteControl
        /// </summary>
        /// <param name="attachedAnnotation">An IAttachedAnnotation instance</param>
        void IAnnotationComponent.AddAttachedAnnotation(IAttachedAnnotation attachedAnnotation)
        {
            if (attachedAnnotation == null)
            {
                throw new ArgumentNullException("attachedAnnotation");
            }

            if (_attachedAnnotation == null)
            {
                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.AddAttachedSNBegin);

                SetAnnotation(attachedAnnotation);

                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.AddAttachedSNEnd);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.AddAnnotationsNotImplemented));
            }
        }

        /// <summary>
        /// Removes an attached annotations from this StickyNoteControl.
        /// </summary>
        /// <param name="attachedAnnotation">An IAttachedAnnotation instance</param>
        void IAnnotationComponent.RemoveAttachedAnnotation(IAttachedAnnotation attachedAnnotation)
        {
            if (attachedAnnotation == null)
            {
                throw new ArgumentNullException("attachedAnnotation");
            }

            if (attachedAnnotation == _attachedAnnotation)
            {
                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.RemoveAttachedSNBegin);

                GiveUpFocus();

                ClearAnnotation();

                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.RemoveAttachedSNEnd);
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.InvalidValueSpecified), "attachedAnnotation");
            }
        }

        /// <summary>
        /// Called when an AttachedAnnotation's attached anchor changes.
        /// </summary>
        /// <param name="attachedAnnotation">The attached annotation after modification</param>
        /// <param name="previousAttachedAnchor">The attached anchor previously associated with the attached annotation.</param>
        /// <param name="previousAttachmentLevel">The previous attachment level of the attached annotation.</param>
        void IAnnotationComponent.ModifyAttachedAnnotation(IAttachedAnnotation attachedAnnotation, object previousAttachedAnchor, AttachmentLevel previousAttachmentLevel)
        {
            throw new NotSupportedException(SR.Get(SRID.NotSupported));
        }

        /// <summary>
        /// Gets the attached annotations this component is representing.
        /// </summary>
        /// <returns>list of IAttachedAnnotation instances this component is representing</returns>
        IList IAnnotationComponent.AttachedAnnotations
        {
            get
            {
                ArrayList annotations = new ArrayList(1);

                if (_attachedAnnotation != null)
                {
                    annotations.Add(_attachedAnnotation);
                }

                return annotations;
            }
        }

        /// <summary>
        /// Compute the transform for the icon.
        /// also hide or show the expanded component.
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        GeneralTransform IAnnotationComponent.GetDesiredTransform(GeneralTransform transform)
        {
            if (_attachedAnnotation != null)
            {
                // If we are expanded and set to flow from RightToLeft and we are in a viewer using a DocumentPageHost
                // we need to mirror ourselves.  This is work around an issue with DocumentViewerBase which mirrors its
                // contents (because its generated always as LeftToRight).  We are anchored to that content so the mirror
                // gets applied to as as well.  This is our attempt to cancel out that mirror.
                if (this.IsExpanded
                    && this.FlowDirection == FlowDirection.RightToLeft
                    && _attachedAnnotation.Parent is DocumentPageHost)
                {
                    _selfMirroring = true;
                }
                else
                {
                    _selfMirroring = false;
                }

                Point anchor = _attachedAnnotation.AnchorPoint;

                if (double.IsInfinity(anchor.X) || double.IsInfinity(anchor.Y))
                {
                    throw new InvalidOperationException(SR.Get(SRID.InvalidAnchorPosition));
                }

                if ((double.IsNaN(anchor.X)) || (double.IsNaN(anchor.Y)))
                    return null;

                GeneralTransformGroup transformations = new GeneralTransformGroup();

                // We should be in normal Right-To-Left mode, but because of special cases
                // in DocumentPageView, we've been mirrored.  We detect this and re-mirror
                // ourselves.  We also need to do special handling of move/resize operations.
                if (_selfMirroring)
                {
                    // This is the mirroring transform that will get the StickyNote to lay out
                    // as if it were in Right-To-Left mode
                    transformations.Children.Add(new MatrixTransform(-1.0, 0.0, 0.0, 1.0, this.Width, 0.0));
                }

                transformations.Children.Add(new TranslateTransform(anchor.X, anchor.Y));

                TranslateTransform offsetTransform = new TranslateTransform(0, 0);
                if (IsExpanded == true)
                {
                    offsetTransform = PositionTransform.Clone();

                    // Reset delta values
                    _deltaX = _deltaY = 0;

                    //if we are in any kind of page viewer we might need to bring SN on the page
                    Rect rectPage = PageBounds;
                    Rect rectStickyNote = StickyNoteBounds;

                    // Get the current offsets
                    double offsetX, offsetY;
                    GetOffsets(rectPage, rectStickyNote, out offsetX, out offsetY);

                    // If the current offsets are greater than the cached the values,
                    // we will make sure that stickynote sticks on the cached values.
                    if (DoubleUtil.GreaterThan(Math.Abs(offsetX), Math.Abs(_offsetX)))
                    {
                        // Whatever the offset - don't move to the right more than
                        double offset = _offsetX - offsetX;
                        if (DoubleUtil.LessThan(offset, 0)) // if we are moving to the left, don't go beyond the edge
                        {
                            offset = Math.Max(offset, -(rectStickyNote.Left - rectPage.Left));
                        }
                        offsetTransform.X += offset;
                        _deltaX = offset;
                    }

                    if (DoubleUtil.GreaterThan(Math.Abs(offsetY), Math.Abs(_offsetY)))
                    {
                        double offset = _offsetY - offsetY;
                        if (DoubleUtil.LessThan(offset, 0)) // if we are moving to the top, don't go beyond the edge
                        {
                            offset = Math.Max(offset, -(rectStickyNote.Top - rectPage.Top));
                        }
                        offsetTransform.Y += offset;
                        _deltaY = offset;
                    }
                }

                if (offsetTransform != null)
                    transformations.Children.Add(offsetTransform);
                transformations.Children.Add(transform);
                return transformations;
            }

            return null;
        }

        /// <summary>
        /// Return the attached annotation parent as the annotated element
        /// </summary>
        /// <value></value>
        UIElement IAnnotationComponent.AnnotatedElement
        {
            get
            {
                return _attachedAnnotation != null ? _attachedAnnotation.Parent as UIElement : null;
            }
        }

        /// <summary>
        /// Get/Set the PresentationContext
        /// </summary>
        /// <value></value>
        PresentationContext IAnnotationComponent.PresentationContext
        {
            get
            {
                return _presentationContext;
            }
            set
            {
                _presentationContext = value;
            }
        }

        /// <summary>
        /// Sets and gets the Z-order of this component. NOP -
        /// Highlight does not have Z-order
        /// </summary>
        /// <value>Context this annotation component is hosted in</value>
        int IAnnotationComponent.ZOrder
        {
            get
            {
                return _zOrder;
            }

            set
            {
                _zOrder = value;
                UpdateAnnotationWithSNC(XmlToken.ZOrder);
            }
        }

        /// <summary>
        /// Notifies the component that the AnnotatedElement content has changed
        /// </summary>
        bool IAnnotationComponent.IsDirty
        {
            get
            {
                if (_anchor != null)
                    return _anchor.IsDirty;
                return false;
            }
            set
            {
                if (_anchor != null)
                    _anchor.IsDirty = value;
                if (value)
                    InvalidateVisual();
            }
        }

        #endregion // IAnnotationComponent Members

        #region Public Fields

        /// <summary>
        /// The Xml type name which is used by the Annotation to instantiate a Text StickyNoteControl
        /// </summary>
        public static readonly XmlQualifiedName TextSchemaName = new XmlQualifiedName("TextStickyNote", AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);

        /// <summary>
        /// The Xml type name which is used by the Annotation to instantiate an Ink StickyNoteControl
        /// </summary>
        public static readonly XmlQualifiedName InkSchemaName = new XmlQualifiedName("InkStickyNote", AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);

        #endregion Public Fields

        //-------------------------------------------------------------------------------
        //
        // Internal Properties
        //
        //-------------------------------------------------------------------------------

        #region Internal Properties

        // The property is the accessor of the variable of _positionTransform which is used by
        // IAnnotationComponent.GetDesiredTransform method. The annotation adorner layer will use the transform to
        // position the StickyNoteControl
        internal TranslateTransform PositionTransform
        {
            get
            {
                return _positionTransform;
            }
            set
            {
                Invariant.Assert(value != null, "PositionTransform cannot be null.");
                _positionTransform = value;

                InvalidateTransform();
            }
        }

        /// <summary>
        /// Gets/Sets the cached X offset when StikcyNote is cross the page boundary
        /// </summary>
        internal double XOffset
        {
            get
            {
                return _offsetX;
            }
            set
            {
                _offsetX = value;
            }
        }

        /// <summary>
        /// Gets/Sets the cached Y offset when StikcyNote is cross the page boundary
        /// </summary>
        internal double YOffset
        {
            get
            {
                return _offsetY;
            }
            set
            {
                _offsetY = value;
            }
        }

        /// <summary>
        /// This flag signals that we are truly in Right-To-Left mode.  This means the positive
        /// x-axis points to the left and our top-right corner (visually) is our origin.  We use
        /// this flag to determine whether we should flip the value for 'left' before storing it.
        /// Flipping it allows it to be persisted in a way that can be used by the StickyNote in
        /// non Right-To-Left mode or in the self-mirroring case.
        /// </summary>
        internal bool FlipBothOrigins
        {
            get
            {
                return (this.IsExpanded && this.FlowDirection == FlowDirection.RightToLeft &&
                    _attachedAnnotation != null && _attachedAnnotation.Parent is DocumentPageHost);
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        // A registered handler for a bubble listening to the annotation author update event.
        //     obj  -   The sender of the event
        //     args -   event arguement
        private void OnAuthorUpdated(object obj, AnnotationAuthorChangedEventArgs args)
        {
            Debug.Assert(_attachedAnnotation != null && _attachedAnnotation.Annotation == args.Annotation);

            if (!InternalLocker.IsLocked(LockHelper.LockFlag.AnnotationChanged))
            {
                UpdateSNCWithAnnotation(XmlToken.Author);
                IsDirty = true;
            }
        }

        // A registered handler for a bubble listening to the annotation store update event.
        //     obj  -   The sender of the event
        //     args -   event arguement
        private void OnAnnotationUpdated(object obj, AnnotationResourceChangedEventArgs args)
        {
            Debug.Assert(_attachedAnnotation != null && _attachedAnnotation.Annotation == args.Annotation);

            if (!InternalLocker.IsLocked(LockHelper.LockFlag.AnnotationChanged))
            {
                SNCAnnotation sncAnnotation = new SNCAnnotation(args.Annotation);
                _sncAnnotation = sncAnnotation;
                UpdateSNCWithAnnotation(SNCAnnotation.AllValues);
                IsDirty = true;
            }
        }

        /// <summary>
        /// The method sets an instance of the IAttachedAnnotation to the StickyNoteControl.
        /// It will be called by IAnnotationComponent.AddAttachedAnnotation.
        /// </summary>
        /// <param name="attachedAnnotation">The instance of the IAttachedAnnotation</param>
        private void SetAnnotation(IAttachedAnnotation attachedAnnotation)
        {
            SNCAnnotation sncAnnotation = new SNCAnnotation(attachedAnnotation.Annotation);

            // Retrieve the data type. Then set the StickyNote to correct type.
            // If we have empty data, we won't change the current StickyNote type.
            bool hasInkData = sncAnnotation.HasInkData;
            bool hasTextData = sncAnnotation.HasTextData;
            if (hasInkData && hasTextData)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidStickyNoteAnnotation), "attachedAnnotation");
            }
            else if (hasInkData)
            {
                _stickyNoteType = StickyNoteType.Ink;
            }
            else if (hasTextData)
            {
                _stickyNoteType = StickyNoteType.Text;
            }

            // If we already created a Content control, make sure it matches our new type or
            // gets recreated to match.
            if (Content != null)
            {
                EnsureStickyNoteType();
            }

            //create cargo if it is a new Annotation so it is not considered as new next time
            if (sncAnnotation.IsNewAnnotation)
            {
                AnnotationResource cargo = new AnnotationResource(SNBConstants.MetaResourceName);
                attachedAnnotation.Annotation.Cargos.Add(cargo);
            }

            // Set the internal variables
            _attachedAnnotation = attachedAnnotation;
            _attachedAnnotation.Annotation.CargoChanged += new AnnotationResourceChangedEventHandler(OnAnnotationUpdated);
            _attachedAnnotation.Annotation.AuthorChanged += new AnnotationAuthorChangedEventHandler(OnAuthorUpdated);
            _sncAnnotation = sncAnnotation;
            _anchor.AddAttachedAnnotation(attachedAnnotation);

            // Update all value
            UpdateSNCWithAnnotation(SNCAnnotation.AllValues);

            // The internal data is just sync'ed to the store. So, reset the dirty to false.
            IsDirty = false;

            //now check if the SN must be seen
            if ((_attachedAnnotation.AttachmentLevel & AttachmentLevel.StartPortion) == 0)
            {
                //we do not need to show the StickyNote
                SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            }
            else
            {
                //if it is seen we need to take care about bringing into view when needed
                RequestBringIntoView += new RequestBringIntoViewEventHandler(OnRequestBringIntoView);
            }
        }

        /// <summary>
        /// Clear the internal variables and events which are related to the annotation.
        /// The method will be called by IAnnotationComponent.RemoveAttachedAnnotation
        /// </summary>
        private void ClearAnnotation()
        {
            _attachedAnnotation.Annotation.CargoChanged -= new AnnotationResourceChangedEventHandler(OnAnnotationUpdated);
            _attachedAnnotation.Annotation.AuthorChanged -= new AnnotationAuthorChangedEventHandler(OnAuthorUpdated);
            _anchor.RemoveAttachedAnnotation(_attachedAnnotation);
            _sncAnnotation = null;

            _attachedAnnotation = null;
            RequestBringIntoView -= new RequestBringIntoViewEventHandler(OnRequestBringIntoView);
        }

        /// <summary>
        /// Brings the SN into view. When navigate through the keyboard navigation the focus
        /// can get to a SN that is not currently  into view.
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">arguments</param>
        /// <remarks> Since the AdornerLayer does not have scrolling abilities we need to scroll
        /// AnnotatedElement. In order to get out SN into view we must calculate the corresponding area of
        /// the AnnotatedElement.</remarks>
        private void OnRequestBringIntoView(Object sender, RequestBringIntoViewEventArgs e)
        {
            Debug.Assert(((IAnnotationComponent)this).AnnotatedElement != null, "undefined annotated element");
            FrameworkElement target = ((IAnnotationComponent)this).AnnotatedElement as FrameworkElement;

            DocumentPageHost host = target as DocumentPageHost;
            if (host != null)
            {
                target = host.PageVisual as FrameworkElement;
            }

            if (target == null)
            {
                //we have nothing to do here
                return;
            }

            //if target is IScrollInfo - check if we are within the viewport
            IScrollInfo scrollInfo = target as IScrollInfo;
            if (scrollInfo != null)
            {
                Rect bounds = StickyNoteBounds;
                Rect viewport = new Rect(0, 0, scrollInfo.ViewportWidth, scrollInfo.ViewportHeight);
                if (bounds.IntersectsWith(viewport))
                    return;
            }

            //get adorned element
            Transform adornerTransform = (Transform)TransformToVisual(target);
            Debug.Assert(adornerTransform != null, "transform to AnnotatedElement is null");

            //get SN sizes
            Rect rect = new Rect(0, 0, Width, Height);
            rect.Transform(adornerTransform.Value);

            // Schedule BringIntoView, rather than making direct call.
            // Otherwise in some cases this can cause nested RequestBringIntoView call to be issued.
            // In scenario when document is inside scroll viewer and annotation needs to be 
            // brought to view the call to BringIntoView won't work because of nested events.
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(DispatchBringIntoView), new object[] { target, rect });
        }

        /// <summary>
        /// Schedules BringIntoView call on <see cref="IAnnotationComponent.AnnotatedElement"/> from <see cref="OnRequestBringIntoView"/>
        /// </summary>
        /// <param name="arg">object array of size 2. param[0] is target to bring into view, param[1] is target Rect</param>
        /// <returns>null</returns>
        private object DispatchBringIntoView(object arg)
        {
            object[] args = (object[])arg;
            FrameworkElement target = (FrameworkElement)(args[0]);
            Rect rect = (Rect)(args[1]);
            target.BringIntoView(rect);
            return null;
        }

        /// <summary>
        /// This method will update this StickyNoteControl data based on the internal annotation variable.
        /// </summary>
        /// <param name="tokens">The data need to be updated</param>
        private void UpdateSNCWithAnnotation(XmlToken tokens)
        {
            if (_sncAnnotation != null)
            {
                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.UpdateSNCWithAnnotationBegin);

                // Now, we are going to update this StickyNoteControl. We will get notified for the data being changed.
                // we don't want to update our internal annotation because for the data changes which are coming
                // from the annotation itself. So, we lock our internal locker.
                using (LockHelper.AutoLocker locker = new LockHelper.AutoLocker(InternalLocker, LockHelper.LockFlag.DataChanged))
                {
                    SNCAnnotation.UpdateStickyNoteControl(tokens, this, _sncAnnotation);
                }

                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.UpdateSNCWithAnnotationEnd);
            }
        }

        /// <summary>
        /// This method will update this internal annotation based on this StickyNoteControl.
        /// </summary>
        /// <param name="tokens"></param>
        private void UpdateAnnotationWithSNC(XmlToken tokens)
        {
            // Check if we have an annotation attached.
            // Also we don't want to update the annotation when the data changes are actually caused by UpdateSNCWithAnnotation.
            if (_sncAnnotation != null &&
                !InternalLocker.IsLocked(LockHelper.LockFlag.DataChanged))
            {
                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.UpdateAnnotationWithSNCBegin);

                // Now, we are going to update the annotation. Since we will get notified from the annotation store,
                // we don't want to update our internal annotation if the change has been made by this instance.
                // Here we lock the internal locker.
                using (LockHelper.AutoLocker locker = new LockHelper.AutoLocker(InternalLocker, LockHelper.LockFlag.AnnotationChanged))
                {
                    // Now, update the attached annotation.
                    SNCAnnotation.UpdateAnnotation(tokens, this, _sncAnnotation);
                }
                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.UpdateAnnotationWithSNCEnd);
            }
        }

        /// <summary>
        /// The method will update the X and Y offsets when StickyNote is cross the host page boundary.
        /// This method should only be called from OnDragDelta.
        /// </summary>
        private void UpdateOffsets()
        {
            // If we don't have annotation, don't even borther.
            if (_attachedAnnotation != null)
            {
                Rect rectPage = PageBounds;
                Rect rectStickyNote = StickyNoteBounds;

                // Make sure that we have the valid bounds.
                if (!rectPage.IsEmpty && !rectStickyNote.IsEmpty)
                {
                    // StickyNote should never disappear from the host page.
                    Invariant.Assert(DoubleUtil.GreaterThan(rectStickyNote.Right, rectPage.Left), "Note's right is off left of page.");
                    Invariant.Assert(DoubleUtil.LessThan(rectStickyNote.Left, rectPage.Right), "Note's left is off right of page.");
                    Invariant.Assert(DoubleUtil.GreaterThan(rectStickyNote.Bottom, rectPage.Top), "Note's bottom is off top of page.");
                    Invariant.Assert(DoubleUtil.LessThan(rectStickyNote.Top, rectPage.Bottom), "Note's top is off bottom of page.");

                    double offsetX, offsetY;
                    // Get the current offsets.
                    GetOffsets(rectPage, rectStickyNote, out offsetX, out offsetY);

                    // If the offsets have been changed, we should update the cached values.
                    if (!DoubleUtil.AreClose(XOffset, offsetX))
                    {
                        XOffset = offsetX;
                    }

                    if (!DoubleUtil.AreClose(YOffset, offsetY))
                    {
                        YOffset = offsetY;
                    }
                }
            }
        }

        /// <summary>
        /// A method which calculates the X and Y offsets between StickyNote and Page bounds.
        /// </summary>
        /// <param name="rectPage">The page bounds</param>
        /// <param name="rectStickyNote">The StickyNote bounds</param>
        /// <param name="offsetX">
        /// X Offset.
        /// 0 means that StickyNote is completely inside page on X dimension.
        /// A negative value means that StickyNote is partially beyond page left.
        /// A positive value means that StickyNote is partially beyond page right.
        /// </param>
        /// <param name="offsetY">
        /// Y Offset.
        /// 0 means that StickyNote is completely inside page on Y dimension.
        /// A negative value means that StickyNote is partially beyond page top.
        /// A positive value means that StickyNote is partially beyond page bottom.
        /// </param>
        private static void GetOffsets(Rect rectPage, Rect rectStickyNote, out double offsetX, out double offsetY)
        {
            offsetX = 0;
            if (DoubleUtil.LessThan(rectStickyNote.Left, rectPage.Left))
            {
                // StickyNote is beyond the left boundary
                offsetX = rectStickyNote.Left - rectPage.Left;
            }
            else if (DoubleUtil.GreaterThan(rectStickyNote.Right, rectPage.Right))
            {
                // StickyNote is beyond the right boundary
                offsetX = rectStickyNote.Right - rectPage.Right;
            }


            offsetY = 0;
            if (DoubleUtil.LessThan(rectStickyNote.Top, rectPage.Top))
            {
                // StickyNote is beyond the top boundary
                offsetY = rectStickyNote.Top - rectPage.Top;
            }
            else if (DoubleUtil.GreaterThan(rectStickyNote.Bottom, rectPage.Bottom))
            {
                // StickyNote is beyond the bottom boundary
                offsetY = rectStickyNote.Bottom - rectPage.Bottom;
            }
        }

        private Rect StickyNoteBounds
        {
            get
            {
                Debug.Assert(_attachedAnnotation != null, "This property should never be acccessed from outside of CAF");

                Rect ret = Rect.Empty;
                Point anchor = _attachedAnnotation.AnchorPoint;

                if (!(double.IsNaN(anchor.X)) && !(double.IsNaN(anchor.Y)) && PositionTransform != null)
                {
                    ret = new Rect(anchor.X + PositionTransform.X + _deltaX, anchor.Y + PositionTransform.Y + _deltaY, Width, Height);
                }

                return ret;
            }
        }

        private Rect PageBounds
        {
            get
            {
                Rect pageBounds = Rect.Empty;

                IAnnotationComponent component = (IAnnotationComponent)this;

                // If the annotated element is a scroll info, we should use the
                // full size of the scrollable content - ExtendWidth/ExtentHeight.
                IScrollInfo scrollInfo = component.AnnotatedElement as IScrollInfo;
                if (scrollInfo != null)
                {
                    pageBounds = new Rect(-scrollInfo.HorizontalOffset, -scrollInfo.VerticalOffset, scrollInfo.ExtentWidth, scrollInfo.ExtentHeight);
                }
                else
                {
                    UIElement parent = component.AnnotatedElement;

                    if (parent != null)
                    {
                        Size pageSize = parent.RenderSize;
                        pageBounds = new Rect(0, 0, pageSize.Width, pageSize.Height);
                    }
                }

                return pageBounds;
            }
        }

        #endregion // Private Methods

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// the presentation context this sticky note is in
        /// </summary>
        PresentationContext _presentationContext;

        /// <summary>
        /// Offset from anchor point to sticky note icon
        /// </summary>
        TranslateTransform _positionTransform = new TranslateTransform(0, 0);

        // A reference of the current attached annotation instance.
        private IAttachedAnnotation _attachedAnnotation;
        private SNCAnnotation _sncAnnotation;

        // The cached horizontal and vertical portions of the StickyNote the user
        // put off the page boundary.  This is used to attempt to reproduce the same
        // portions when the page has been reflowed.
        // 0 means that StickyNote is completely inside page on X or Y dimension.
        // A negative value means that StickyNote is partially beyond page top/left.
        // A positive value means that StickyNote is partially beyond page bottom/right.
        // These are updated everytime the user moves the StickyNote and are persisted.
        private double _offsetX;
        private double _offsetY;

        // The distances the StickyNote has been moved at layout time in order to
        // reproduce the same portion off the page as the user previously created.
        // 0 means we had to do no adjusting, the note is where the use put it.
        // A negative value means we had to move the StickyNote towards the origin.
        // A positive value menas we had to move the StickyNote away from the origin.
        // These are updated on every layout pass and are not persisted.
        private double _deltaX;
        private double _deltaY;

        //Component Z-order
        private int _zOrder;

        // This flag signals that we should be in normal Right-To-Left mode, but because
        // of special cases in DocumentPageView, we've been mirrored (back to Left-To-Right mode).
        // Therefore we need to re-mirror ourselves and special handling of move/resize operations.
        private bool _selfMirroring = false;

        #endregion // Private Fields
    }
}

