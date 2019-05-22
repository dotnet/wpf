// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      FixedElement represents a flow element/object in the Fixed Document.
//

namespace System.Windows.Documents
{
    using MS.Internal.Documents;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Controls;
    using System.Windows.Shapes;
    using System.Windows.Markup;    // for XmlLanguage
    using System.Windows.Media;
    using System.Windows.Navigation;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Security;

    /// <summary>
    /// FixedElement represents a flow element/object in the Fixed Document.
    /// Its children collection is used for DependencyProperty evaluation.
    /// No Z-order is implied!
    /// </summary>
    // suggestion: derive from TextElement to get text properties like FontSize
    // will we also need TableCell.ColumnSpan?  Would we want to make this derive from TableCell?  Probably not.
    internal sealed class FixedElement : DependencyObject
    {
        internal enum ElementType
        {
            Paragraph,
            Inline,
            Run,
            Span,
            Bold,
            Italic,
            Underline,
            Object,
            Container, 
            Section,
            Figure,
            Table,
            TableRowGroup,
            TableRow,
            TableCell,
            List,
            ListItem,
            Header,
            Footer,
            Hyperlink,
            InlineUIContainer,
        }

        // apply to everything
        public static readonly DependencyProperty LanguageProperty =
                    FrameworkElement.LanguageProperty.AddOwner(
                                typeof(FixedElement));

        // only apply to ElementType.Run
        public static readonly DependencyProperty FontFamilyProperty = 
                    TextElement.FontFamilyProperty.AddOwner(
                                typeof(FixedElement));

        public static readonly DependencyProperty FontStyleProperty = 
                    TextElement.FontStyleProperty.AddOwner(
                                typeof(FixedElement));

        public static readonly DependencyProperty FontWeightProperty = 
                    TextElement.FontWeightProperty.AddOwner(
                                typeof(FixedElement));
        
        public static readonly DependencyProperty FontStretchProperty = 
                    TextElement.FontStretchProperty.AddOwner(
                                typeof(FixedElement));
        
        public static readonly DependencyProperty FontSizeProperty = 
                    TextElement.FontSizeProperty.AddOwner(
                                typeof(FixedElement));
        
        public static readonly DependencyProperty ForegroundProperty = 
                    TextElement.ForegroundProperty.AddOwner(
                                typeof(FixedElement));

        public static readonly DependencyProperty FlowDirectionProperty = 
                    FrameworkElement.FlowDirectionProperty.AddOwner(
                                typeof(FixedElement));



        //Applies only to Table
        public static readonly DependencyProperty CellSpacingProperty = 
                    Table.CellSpacingProperty.AddOwner(
                                typeof(FixedElement));
       

        //Applies only to TableCell
        public static readonly DependencyProperty BorderThicknessProperty = 
                    Block.BorderThicknessProperty.AddOwner(typeof(FixedElement));

        public static readonly DependencyProperty BorderBrushProperty = 
                    Block.BorderBrushProperty.AddOwner(typeof(FixedElement));

        public static readonly DependencyProperty ColumnSpanProperty =
                    TableCell.ColumnSpanProperty.AddOwner(typeof(FixedElement));

        //Applies only to Hyperlink
        public static readonly DependencyProperty NavigateUriProperty =
            Hyperlink.NavigateUriProperty.AddOwner(
                        typeof(FixedElement));

        //Applies only to images
        public static readonly DependencyProperty NameProperty =
            AutomationProperties.NameProperty.AddOwner(
                        typeof(FixedElement));

        public static readonly DependencyProperty HelpTextProperty =
            AutomationProperties.HelpTextProperty.AddOwner(
                        typeof(FixedElement));

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region Constructors
        // Ctor always set mutable flag to false
        internal FixedElement(ElementType type, FixedTextPointer start, FixedTextPointer end, int pageIndex)
        {
            _type = type;
#if DEBUG
            if (_type == ElementType.Object) // is EmbeddedObject
            {
                Debug.Assert((object)_start == (object)_end);
            }
            _children = new List<FixedElement>();
#endif
            _start = start;
            _end = end;
            _pageIndex = pageIndex;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

#if DEBUG
        /// <summary>
        /// Create a string representation of this object
        /// </summary>
        /// <returns>string - A string representation of this object</returns>
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{{S@{0}---E@{1}}} {2}", _start, _end, System.Enum.GetName(typeof(ElementType), _type));
        }
#endif

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Method to allow append of element
        // Note element order doesn't imply Z-order in this implementation!
        internal void Append(FixedElement e)
        {
#if DEBUG
            _children.Add(e);
#endif
            if (_type == ElementType.InlineUIContainer)
            {
                Debug.Assert(_object == null && e._type == ElementType.Object);
                _object = e._object; // To generate InlineUIContainer with child with object;
            }
        }

        internal object GetObject()
        {
            if (_type == ElementType.Hyperlink || _type == ElementType.Paragraph ||
                (_type >= ElementType.Table && _type <= ElementType.TableCell))
            {
                if (_object == null)
                {
                    _object = BuildObjectTree();
                }
                return _object;
            }

            if (!(_type == ElementType.Object || _type == ElementType.InlineUIContainer))
            {
                // This is currently not implemented for any other type
                return null;
            }

            Image im = GetImage();
            object o = im;
            if (_type == ElementType.InlineUIContainer)
            {
                InlineUIContainer c = new InlineUIContainer();
                c.Child = im;
                o = c;
            }
            return o;
        }

        internal object BuildObjectTree()
        {
            IAddChild root;
            switch (_type)
            {
                case ElementType.Table:
                    root = new Table();
                    break;
                case ElementType.TableRowGroup:
                    root = new TableRowGroup();
                    break;
                case ElementType.TableRow:
                    root = new TableRow();
                    break;
                case ElementType.TableCell:
                    root = new TableCell();
                    break;
                case ElementType.Paragraph:
                    root = new Paragraph();
                    break;
                case ElementType.Hyperlink:
                    Hyperlink link = new Hyperlink();
                    link.NavigateUri = GetValue(NavigateUriProperty) as Uri;
                    link.RequestNavigate += new RequestNavigateEventHandler(ClickHyperlink);
                    AutomationProperties.SetHelpText(link, (String)this.GetValue(HelpTextProperty));
                    AutomationProperties.SetName(link, (String)this.GetValue(NameProperty));
                    root = link;
                    break;
                default:
                    Debug.Assert(false);
                    root = null;
                    break;
            }

            ITextPointer pos = ((ITextPointer)_start).CreatePointer();

            while (pos.CompareTo((ITextPointer)_end) < 0)
            {
                TextPointerContext tpc = pos.GetPointerContext(LogicalDirection.Forward);
                if (tpc == TextPointerContext.Text)
                {
                    root.AddText(pos.GetTextInRun(LogicalDirection.Forward));
                }
                else if (tpc == TextPointerContext.EmbeddedElement)
                {
                    root.AddChild(pos.GetAdjacentElement(LogicalDirection.Forward));
                }
                else if (tpc == TextPointerContext.ElementStart)
                {
                    object obj = pos.GetAdjacentElement(LogicalDirection.Forward);
                    if (obj != null)
                    {
                        root.AddChild(obj);
                        pos.MoveToNextContextPosition(LogicalDirection.Forward);
                        pos.MoveToElementEdge(ElementEdge.BeforeEnd);
                    }
                }

                pos.MoveToNextContextPosition(LogicalDirection.Forward);
            }
            return root;
        }

        private Image GetImage()
        {
            Image image = null; // return value
            Uri source = _object as Uri;
            if (source != null)
            {
                image = new Image();
                image.Source = new System.Windows.Media.Imaging.BitmapImage(source);
                image.Width = image.Source.Width;
                image.Height = image.Source.Height;

                AutomationProperties.SetName(image, (String)this.GetValue(NameProperty));
                AutomationProperties.SetHelpText(image, (String)this.GetValue(HelpTextProperty));
            }
            return image;
        }

        private void ClickHyperlink(Object sender, RequestNavigateEventArgs args)
        {
            FixedDocument doc = _start.FixedTextContainer.FixedDocument;
            int pageno = doc.GetPageNumber(_start);
            FixedPage page = doc.SyncGetPage(pageno, false);

            //have page raise click event
            Hyperlink.RaiseNavigate(page, args.Uri, null);
        }

        #endregion Internal Methods

#if DEBUG
        internal void Dump()
        {
            DumpTree(0);
        }

        internal void DumpTree(int indent)
        {
            if (DocumentsTrace.FixedTextOM.Map.IsEnabled)
            {
                StringBuilder dp = new StringBuilder();

                dp.Append("\r\n");
                for (int kk = 0; kk < indent; kk++)
                {
                    dp.Append("    ");
                }
                dp.Append(this.ToString());
                for(int i = 0, n = _children.Count; i < n; i++)
                {
                    ((FixedElement)_children[i]).DumpTree(indent+1);
                }

                DocumentsTrace.FixedTextOM.Map.Trace(dp.ToString());
            }
        }
#endif
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties
        internal bool IsTextElement
        {
            get
            {
                return ! (_type == ElementType.Object
                    || _type == ElementType.Container);
            }
        }

        internal Type Type
        {
            get
            {
                switch (_type)
                {
                    case ElementType.Paragraph:
                        return typeof(Paragraph);

                    case ElementType.Inline:
                        return typeof(Inline);

                    case ElementType.Run:
                        return typeof(Run);

                    case ElementType.Span:
                        return typeof(Span);

                    case ElementType.Bold:
                        return typeof(Bold);

                    case ElementType.Italic:
                        return typeof(Italic);

                    case ElementType.Underline:
                        return typeof(Underline);

                    case ElementType.Object:
                        return typeof(Object);

                    case ElementType.Table:
                        return typeof(Table);

                    case ElementType.TableRowGroup:
                        return typeof(TableRowGroup);

                    case ElementType.TableRow:
                        return typeof(TableRow);

                    case ElementType.TableCell:
                        return typeof(TableCell);

                    case ElementType.List:
                        return typeof(List);

                    case ElementType.ListItem:
                        return typeof(ListItem);

                    case ElementType.Section:
                        return typeof(Section);

                    case ElementType.Figure:
                        return typeof(Figure);

                    case ElementType.Hyperlink:
                        return typeof(Hyperlink);

                    case ElementType.InlineUIContainer:
                        return typeof(InlineUIContainer);
                    default:
                        return typeof(Object);
                }
            }
        }

        internal FixedTextPointer Start
        {
            get { return _start; }
        }


        internal FixedTextPointer End
        {
            get { return _end; }
        }

        internal int PageIndex
        {
            get { return _pageIndex; }
        }

        internal object Object
        {
            set { _object = value; }
        }
        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields
#if DEBUG
        private List<FixedElement>      _children;
#endif
        private ElementType  _type;             // logical type that this element represents
        private FixedTextPointer  _start;      // start position for this element
        private FixedTextPointer  _end;        // end position for this element
        private object _object;                 // embedded object
        private int _pageIndex;
        #endregion Private Fields
    }
}

