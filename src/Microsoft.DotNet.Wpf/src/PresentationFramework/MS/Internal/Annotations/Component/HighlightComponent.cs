// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: AnnotationComponent that visualizes highlights
//

using System;
using MS.Internal;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using MS.Internal.Text;
using System.Xml;
using System.IO;
using System.Windows.Annotations;
using System.Reflection;
using System.Windows.Annotations.Storage;
using System.Windows.Controls;
using MS.Internal.Annotations.Anchoring;
using MS.Utility;


namespace MS.Internal.Annotations.Component
{
    // Highlight rendering for the Annotation highlight and sticky note anchor.
    // TBD
    internal class HighlightComponent : Canvas, IAnnotationComponent, IHighlightRange
    {
        #region Constructor

        /// <summary>
        /// Creates a new instance of the HighlightComponent
        /// </summary>
        public HighlightComponent()
        {
        }

        /// <summary>
        /// Creates a new instance of the HighlightComponent with
        /// nondefault priority and type;
        /// </summary>
        /// <param name="priority">component priority</param>
        /// <param name="highlightContent">if true - highlight only content of tables, figures and floaters</param>
        /// <param name="type">component type</param>
        public HighlightComponent(int priority, bool highlightContent, XmlQualifiedName type)
            : base()
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            _priority = priority;
            _type = type;
            _highlightContent = highlightContent;
        }

        #endregion Constructor

        #region Public Properties

        /// <summary>
        /// Return a copy of the list of IAttachedAnnotations held by this component
        /// </summary>
        public IList AttachedAnnotations
        {
            get
            {
                ArrayList list = new ArrayList();
                if (_attachedAnnotation != null)
                {
                    list.Add(_attachedAnnotation);
                }
                return list;
            }
        }

        /// <summary>
        /// Sets and gets the context this annotation component is hosted in.
        /// </summary>
        /// <value>Context this annotation component is hosted in</value>
        public PresentationContext PresentationContext
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
        public int ZOrder
        {
            get
            {
                return -1;
            }

            set
            {
            }
        }

        /// <summary>
        /// The annotation type name
        /// </summary>
        public static XmlQualifiedName TypeName
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// gets and sets the default backgroud color for the highlight
        /// </summary>
        public Color DefaultBackground
        {
            get
            {
                return _defaultBackroundColor;
            }
            set
            {
                _defaultBackroundColor = value;
            }
        }

        /// <summary>
        /// gets and sets the default foregroud color for the highlight
        /// </summary>
        public Color DefaultActiveBackground
        {
            get
            {
                return _defaultActiveBackgroundColor;
            }
            set
            {
                _defaultActiveBackgroundColor = value;
            }
        }

        /// <summary>
        /// Highlight color
        /// </summary>
        public Brush HighlightBrush
        {
            set
            {
                SetValue(HighlightComponent.HighlightBrushProperty, value);
            }
        }

        //those properies are exposed to allow external components like StickyNote to synchronize their color
        // with Highlight colors
        public static DependencyProperty HighlightBrushProperty = DependencyProperty.Register("HighlightBrushProperty", typeof(Brush), typeof(HighlightComponent));

        /// <summary>
        /// Returns the one element the annotation component is attached to.
        /// </summary>
        /// <value></value>
        public UIElement AnnotatedElement
        {
            get
            {
                return _attachedAnnotation != null ? (_attachedAnnotation.Parent as UIElement) : null;
            }
        }

        /// <summary>
        /// When the value is set to true - the AnnotatedElement content has changed -
        /// save the value and invalidate the visual, so we can recalculate the geometry.
        /// The value is set to false when the geometry is synced with the content
        /// </summary>
        public bool IsDirty
        {
            get
            {
                return _isDirty;
            }

            set
            {
                _isDirty = value;
                if (value)
                    InvalidateChildren();
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// The HighlightComponent uses built-in highlight rendering, so no transformation is needed
        /// </summary>
        /// <param name="transform">Transform to the AnnotatedElement.</param>
        /// <returns>Transform to the annotation component</returns>
        public GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            return transform;
        }

        /// <summary>
        /// Add an attached annotation to the component. The attached anchor will be used to add
        /// a highlight to the appropriate TextContainer
        /// </summary>
        /// <param name="attachedAnnotation">The attached annotation to be added to the component</param>
        public void AddAttachedAnnotation(IAttachedAnnotation attachedAnnotation)
        {
            if (_attachedAnnotation != null)
            {
                throw new ArgumentException(SR.Get(SRID.MoreThanOneAttachedAnnotation));
            }

            //fire trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.AddAttachedHighlightBegin);

            //check input data and retrieve the TextContainer
            ITextContainer textContainer = CheckInputData(attachedAnnotation);

            TextAnchor textAnchor = attachedAnnotation.AttachedAnchor as TextAnchor;

            //Get highlight Colors from the cargo. For undefined Colors the default values are used
            GetColors(attachedAnnotation.Annotation, out _background, out _selectedBackground);
            _range = textAnchor;

            Invariant.Assert(textContainer.Highlights != null, "textContainer.Highlights is null");

            //get or create AnnotationHighlightLayer in the textContainer
            AnnotationHighlightLayer highlightLayer = textContainer.Highlights.GetLayer(typeof(HighlightComponent)) as AnnotationHighlightLayer;
            if (highlightLayer == null)
            {
                highlightLayer = new AnnotationHighlightLayer();
                textContainer.Highlights.AddLayer(highlightLayer);
            }

            //save the attached annotation
            _attachedAnnotation = attachedAnnotation;

            //register for cargo changes
            _attachedAnnotation.Annotation.CargoChanged += new AnnotationResourceChangedEventHandler(OnAnnotationUpdated);

            //add this highlight range
            highlightLayer.AddRange(this);
            HighlightBrush = new SolidColorBrush(_background);
            IsHitTestVisible = false;

            //fire trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.AddAttachedHighlightEnd);
        }

        /// <summary>
        /// Remove an attached annotation from the component
        /// </summary>
        /// <param name="attachedAnnotation">The attached annotation to be removed from the component</param>
        public void RemoveAttachedAnnotation(IAttachedAnnotation attachedAnnotation)
        {
            if (attachedAnnotation == null)
            {
                throw new ArgumentNullException("attachedAnnotation");
            }

            if (attachedAnnotation != _attachedAnnotation)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidAttachedAnnotation), "attachedAnnotation");
            }

            Invariant.Assert(_range != null, "null highlight range");

            //fire trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.RemoveAttachedHighlightBegin);

            //check input data and retrieve the TextContainer
            ITextContainer textContainer = CheckInputData(attachedAnnotation);

            Invariant.Assert(textContainer.Highlights != null, "textContainer.Highlights is null");

            //get AnnotationHighlightLayer in the textContainer
            AnnotationHighlightLayer highlightLayer = textContainer.Highlights.GetLayer(typeof(HighlightComponent)) as AnnotationHighlightLayer;
            Invariant.Assert(highlightLayer != null, "AnnotationHighlightLayer is not initialized");

            //unregister of cargo changes
            _attachedAnnotation.Annotation.CargoChanged -= new AnnotationResourceChangedEventHandler(OnAnnotationUpdated);

            highlightLayer.RemoveRange(this);

            //highlight is removed - remove the attached annotation and the data
            _attachedAnnotation = null;

            //fire trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.RemoveAttachedHighlightEnd);
        }

        /// <summary>
        /// Modify an attached annotation that is held by the component
        /// </summary>
        /// <param name="attachedAnnotation">The attached annotation after modification</param>
        /// <param name="previousAttachedAnchor">The attached anchor previously associated with the attached annotation.</param>
        /// <param name="previousAttachmentLevel">The previous attachment level of the attached annotation.</param>
        public void ModifyAttachedAnnotation(IAttachedAnnotation attachedAnnotation, object previousAttachedAnchor, AttachmentLevel previousAttachmentLevel)
        {
            throw new NotSupportedException(SR.Get(SRID.NotSupported));
        }

        /// <summary>
        /// Sets highlight color to active/inactive
        /// <param name="active">true - activate, false = deactivate</param>
        /// </summary>
        public void Activate(bool active)
        {
            //return if the state is unchanged
            if (_active == active)
                return;

            //get the highlight layer
            if (_attachedAnnotation == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.NoAttachedAnnotationToModify));
            }

            TextAnchor textAnchor = _attachedAnnotation.AttachedAnchor as TextAnchor;
            Invariant.Assert(textAnchor != null, "AttachedAnchor is not a text anchor");

            //this should be in a fixed or flow textcontainer
            ITextContainer textContainer = textAnchor.Start.TextContainer;
            Invariant.Assert(textContainer != null, "TextAnchor does not belong to a TextContainer");

            //get AnnotationHighlightLayer in the textContainer
            AnnotationHighlightLayer highlightLayer = textContainer.Highlights.GetLayer(typeof(HighlightComponent)) as AnnotationHighlightLayer;
            Invariant.Assert(highlightLayer != null, "AnnotationHighlightLayer is not initialized");

            highlightLayer.ActivateRange(this, active);
            _active = active;

            if (active)
                HighlightBrush = new SolidColorBrush(_selectedBackground);
            else
                HighlightBrush = new SolidColorBrush(_background);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  IHighlightRange implementation
        //
        //------------------------------------------------------
        #region IHighlightRange implementation

        #region Internal Methods

        /// <summary>
        /// Adds a new shape to the children of the corresponding visual
        /// </summary>
        /// <param name="child">the new child</param>
        void IHighlightRange.AddChild(Shape child)
        {
            Children.Add(child);
        }


        /// <summary>
        /// Removes a shape from the children of the corresponding visual
        /// </summary>
        /// <param name="child">the new child</param>
        void IHighlightRange.RemoveChild(Shape child)
        {
            Children.Remove(child);
        }

        #endregion Internal Methods
        //------------------------------------------------------
        //
        //  Internal properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Highlight Background color
        /// </summary>
        Color IHighlightRange.Background
        {
            get
            {
                return _background;
            }
        }

        /// <summary>
        /// Highlight color if highlight is active
        /// </summary>
        Color IHighlightRange.SelectedBackground
        {
            get
            {
                return _selectedBackground;
            }
        }

        /// <summary>
        /// Highlight TextSegment
        /// </summary>
        TextAnchor IHighlightRange.Range
        {
            get
            {
                return _range;
            }
        }

        /// <summary>
        /// Highlight priority
        /// </summary>
        int IHighlightRange.Priority
        {
            get
            {
                return _priority;
            }
        }

        /// <summary>
        /// if true - highlight only the content of tables, figures and floaters
        /// </summary>
        bool IHighlightRange.HighlightContent
        {
            get
            {
                return _highlightContent;
            }
        }

        #endregion Internal Properties

        #endregion IHighlightRange implementation


        #region Internal Methods
        internal bool IsSelected(ITextRange selection)
        {
            if (selection == null)
            {
                throw new ArgumentNullException("selection");
            }
            Invariant.Assert(_attachedAnnotation != null, "No _attachedAnnotation");

            // For activation based on the selection state, we need to use anchors that
            // span virtualized content (i.e., content that isn't visible).  For that we
            // grab the 'fullly resolved' anchors instead of the anchors used by the
            // framework for all other purposes.
            TextAnchor fullAnchor = _attachedAnnotation.FullyAttachedAnchor as TextAnchor;
            //Debug.Assert(fullAnchor != null, "null TextAnchor");
            if (fullAnchor == null)
                return false;

            return fullAnchor.IsOverlapping(selection.TextSegments);
        }

        /// <summary>
        /// Looks for Colors from the Annotation's cargo. If corresponding Color is present
        /// set it to the input parameter. Otherwise leave the parameter intact.
        /// </summary>
        /// <param name="annot">The Annotation</param>
        /// <param name="backgroundColor">background Color</param>
        /// <param name="activeBackgroundColor">background Color for active highlight</param>
        internal static void GetCargoColors(Annotation annot, ref Nullable<Color> backgroundColor, ref Nullable<Color> activeBackgroundColor)
        {
            Invariant.Assert(annot != null, "annotation is null");

            ICollection<AnnotationResource> cargos = annot.Cargos;

            if (cargos != null)
            {
                foreach (AnnotationResource cargo in cargos)
                {
                    if (cargo.Name == HighlightResourceName)
                    {
                        ICollection contents = cargo.Contents;
                        foreach (XmlElement content in contents)
                        {
                            if ((content.LocalName == ColorsContentName) &&
                                (content.NamespaceURI == AnnotationXmlConstants.Namespaces.BaseSchemaNamespace))
                            {
                                if (content.Attributes[BackgroundAttributeName] != null)
                                    backgroundColor = GetColor(content.Attributes[BackgroundAttributeName].Value);
                                if (content.Attributes[ActiveBackgroundAttributeName] != null)
                                    activeBackgroundColor = GetColor(content.Attributes[ActiveBackgroundAttributeName].Value);
                            }
                        }
                    }
                }
            }
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Checks if this attachedAnnotation data - AttachedAnchor and Annotation
        /// </summary>
        /// <param name="attachedAnnotation">The AttachedAnnotation</param>
        /// <returns>The AttachedAnchor TextContainer</returns>
        private ITextContainer CheckInputData(IAttachedAnnotation attachedAnnotation)
        {
            if (attachedAnnotation == null)
            {
                throw new ArgumentNullException("attachedAnnotation");
            }

            TextAnchor textAnchor = attachedAnnotation.AttachedAnchor as TextAnchor;
            if (textAnchor == null)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidAttachedAnchor), "attachedAnnotation");
            }

            //this should be in a fixed or flow textcontainer
            ITextContainer textContainer = textAnchor.Start.TextContainer;

            Invariant.Assert(textContainer != null, "TextAnchor does not belong to a TextContainer");

            if (attachedAnnotation.Annotation == null)
            {
                throw new ArgumentException(SR.Get(SRID.AnnotationIsNull), "attachedAnnotation");
            }

            //check annotation type
            if (!_type.Equals(attachedAnnotation.Annotation.AnnotationType))
            {
                throw new ArgumentException(SR.Get(SRID.NotHighlightAnnotationType, attachedAnnotation.Annotation.AnnotationType.ToString()), "attachedAnnotation");
            }

            return textContainer;
        }

        /// <summary>
        /// Converts a string to Color object
        /// </summary>
        /// <param name="color">Color string</param>
        /// <returns>Color object</returns>
        private static Color GetColor(string color)
        {
            return (Color)ColorConverter.ConvertFromString(color);
        }

        /// <summary>
        /// Gets the Colors from the Annotation's cargo. If corresponding Color is not present
        /// a default value is used.
        /// </summary>
        /// <param name="annot">The Annotation</param>
        /// <param name="backgroundColor">background Color</param>
        /// <param name="activeBackgroundColor">background Color for active highlight</param>
        private void GetColors(Annotation annot, out Color backgroundColor, out Color activeBackgroundColor)
        {
            Nullable<Color> tempBackgroundColor = _defaultBackroundColor;
            Nullable<Color> tempActiveBackgroundColor = _defaultActiveBackgroundColor;
            GetCargoColors(annot, ref tempBackgroundColor, ref tempActiveBackgroundColor);
            backgroundColor = (Color)tempBackgroundColor;
            activeBackgroundColor = (Color)tempActiveBackgroundColor;
        }

        /// <summary>
        /// Called when the Annotation cargo changes
        /// </summary>
        /// <param name="sender">the sender</param>
        /// <param name="args">event arguments</param>
        private void OnAnnotationUpdated(object sender, AnnotationResourceChangedEventArgs args)
        {
            Invariant.Assert(_attachedAnnotation != null && _attachedAnnotation.Annotation == args.Annotation, "_attachedAnnotation is different than the input one");
            Invariant.Assert(_range != null, "The highlight range is null");

            //get text container
            TextAnchor textAnchor = _attachedAnnotation.AttachedAnchor as TextAnchor;
            Invariant.Assert(textAnchor != null, "wrong anchor type of the saved attached annotation");

            //this should be in a fixed or flow textcontainer
            ITextContainer textContainer = textAnchor.Start.TextContainer;

            Invariant.Assert(textContainer != null, "TextAnchor does not belong to a TextContainer");


            //Get highlight Colors from the cargo and update the highlight layer
            Color background, activeBackground;
            GetColors(args.Annotation, out background, out activeBackground);

            if (!_background.Equals(background) ||
                !_selectedBackground.Equals(activeBackground))
            {
                //modify the highlight
                Invariant.Assert(textContainer.Highlights != null, "textContainer.Highlights is null");

                //get AnnotationHighlightLayer in the textContainer
                AnnotationHighlightLayer highlightLayer = textContainer.Highlights.GetLayer(typeof(HighlightComponent)) as AnnotationHighlightLayer;
                if (highlightLayer == null)
                {
                    throw new InvalidDataException(SR.Get(SRID.MissingAnnotationHighlightLayer));
                }

                //change the colors and invalidate
                _background = background;
                _selectedBackground = activeBackground;
                highlightLayer.ModifiedRange(this);
            }
        }

        /// <summary>
        /// Invalidating the measure on all the children (which are Shapes)
        /// causes them to recalculate their desired geometry
        /// </summary>
        private void InvalidateChildren()
        {
            // Invalidating the measure on all the children (which are Shapes)
            // causes them to recalculate their desired geometry
            foreach (Visual child in Children)
            {
                Shape uiChild = child as Shape;
                Invariant.Assert(uiChild != null, "HighlightComponent has non-Shape children.");
                uiChild.InvalidateMeasure();
            }

            //reset the Dirty flag
            IsDirty = false;
        }

        #endregion Private Methods

        #region Public Fields

        //resource and content names
        public const string HighlightResourceName = "Highlight";
        public const string ColorsContentName = "Colors";
        public const string BackgroundAttributeName = "Background";
        public const string ActiveBackgroundAttributeName = "ActiveBackground";


        #endregion Public Fields

        //------------------------------------------------------
        //
        //  Private fields
        //
        //------------------------------------------------------

        #region Private Fields

        private Color _background;
        private Color _selectedBackground;
        private TextAnchor _range; //the entire range for this highlight

        private IAttachedAnnotation _attachedAnnotation;
        private PresentationContext _presentationContext;
        private static readonly XmlQualifiedName _name = new XmlQualifiedName("Highlight", AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);
        private XmlQualifiedName _type = _name;
        private int _priority = 0;  //used for highlights Z-order. The segment owners are ordered with highest
        // priority first
        private bool _highlightContent = true; //highlight only the content of tables, figures and floaters
        private bool _active = false; //saves the highlight state - active or not
        private bool _isDirty = true; //shows if the annotation is in sync with the content of the AnnotatedElement

        //default color colors
        private Color _defaultBackroundColor = (Color)ColorConverter.ConvertFromString("#33FFFF00");
        private Color _defaultActiveBackgroundColor = (Color)ColorConverter.ConvertFromString("#339ACD32");

        #endregion Private Fields
    }
}
