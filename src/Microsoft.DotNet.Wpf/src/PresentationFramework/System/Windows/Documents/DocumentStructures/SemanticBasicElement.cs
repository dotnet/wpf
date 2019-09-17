// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal.Documents;
using MS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Markup;

[assembly: XmlnsDefinition(
    "http://schemas.microsoft.com/xps/2005/06/documentstructure",
    "System.Windows.Documents.DocumentStructures")]

namespace System.Windows.Documents.DocumentStructures
{
    /// <summary>
    ///
    /// </summary>
    public class SemanticBasicElement : BlockElement
    {
        /// <summary>
        ///
        /// </summary>
        internal SemanticBasicElement()
        {
            _elementList = new List<BlockElement>();
        }

        internal List<BlockElement> BlockElementList
        {
            get
            {
                return _elementList;
            }
        }

        /// <summary>
        ///
        /// </summary>
        internal List<BlockElement> _elementList;
    }

    /// <summary>
    ///
    /// </summary>
    public class SectionStructure : SemanticBasicElement, IAddChild, IEnumerable<BlockElement>, IEnumerable
    {
        /// <summary>
        ///
        /// </summary>
        public SectionStructure()
        {
            _elementType = FixedElement.ElementType.Section;
        }

        public void Add(BlockElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            ((IAddChild) this).AddChild(element);
        }
        
        void IAddChild.AddChild(object value)
        {
            if (value is ParagraphStructure || value is FigureStructure
                || value is ListStructure || value is TableStructure )
            {
                _elementList.Add((BlockElement)value);
                return;
            }

            throw new ArgumentException(SR.Get(SRID.DocumentStructureUnexpectedParameterType4, value.GetType(),
                typeof(ParagraphStructure), typeof(FigureStructure), typeof(ListStructure), typeof(TableStructure)), 
                "value");
        }

        void IAddChild.AddText(string text) { }
        
        IEnumerator<BlockElement> IEnumerable<BlockElement>.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<BlockElement>)this).GetEnumerator();
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class ParagraphStructure : SemanticBasicElement, IAddChild, IEnumerable<NamedElement>, IEnumerable
    {
        /// <summary>
        ///
        /// </summary>
        public ParagraphStructure()
        {
            _elementType = FixedElement.ElementType.Paragraph;
        }

        public void Add(NamedElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            ((IAddChild) this).AddChild(element);
        }
        
        void IAddChild.AddChild(object value)
        {
            if (value is NamedElement)
            {
                _elementList.Add((BlockElement)value);
                return;
            }

            throw new ArgumentException(SR.Get(SRID.DocumentStructureUnexpectedParameterType1, value.GetType(),
                typeof(NamedElement)),
                "value");
        }
        
        void IAddChild.AddText(string text) { }

        IEnumerator<NamedElement> IEnumerable<NamedElement>.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<NamedElement>)this).GetEnumerator();
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class FigureStructure : SemanticBasicElement, IAddChild, IEnumerable<NamedElement>, IEnumerable
    {
        /// <summary>
        ///
        /// </summary>
        public FigureStructure()
        {
            _elementType = FixedElement.ElementType.Figure;
        }

        void IAddChild.AddChild(object value)
        {
            if (value is NamedElement)
            {
                _elementList.Add((BlockElement)value);
                return;
            }
            throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(NamedElement)), "value");
        }
        
        void IAddChild.AddText(string text) { }
        
        public void Add(NamedElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            ((IAddChild) this).AddChild(element);
        }
        
        IEnumerator<NamedElement> IEnumerable<NamedElement>.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<NamedElement>)this).GetEnumerator();
        }
    }
    
    /// <summary>
    ///
    /// </summary>
    public class ListStructure : SemanticBasicElement, IAddChild, IEnumerable<ListItemStructure>, IEnumerable
    {
        /// <summary>
        ///
        /// </summary>
        public ListStructure()
        {
            _elementType = FixedElement.ElementType.List;
        }

        public void Add(ListItemStructure listItem)
        {
            if (listItem == null)
            {
                throw new ArgumentNullException(nameof(listItem));
            }
            ((IAddChild) this).AddChild(listItem);
        }
        
        void IAddChild.AddChild(object value)
        {
            if (value is ListItemStructure)
            {
                _elementList.Add((ListItemStructure)value);
                return;
            }

            throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(ListItemStructure)), nameof(value));
        }
        
        void IAddChild.AddText(string text) { }

        IEnumerator<ListItemStructure> IEnumerable<ListItemStructure>.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ListItemStructure>)this).GetEnumerator();
        }
    }
    /// <summary>
    ///
    /// </summary>
    public class ListItemStructure : SemanticBasicElement, IAddChild, IEnumerable<BlockElement>, IEnumerable
    {
        /// <summary>
        ///
        /// </summary>
        public ListItemStructure()
        {
            _elementType = FixedElement.ElementType.ListItem;
        }

        public void Add(BlockElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            ((IAddChild) this).AddChild(element);
        }

        void IAddChild.AddChild(object value)
        {
            if (value is ParagraphStructure || value is TableStructure || value is ListStructure || value is FigureStructure)
            {
                _elementList.Add((BlockElement)value);
                return;
            }

            throw new ArgumentException(SR.Get(SRID.DocumentStructureUnexpectedParameterType4, value.GetType(),
                typeof(ParagraphStructure), typeof(TableStructure), typeof(ListStructure), typeof(FigureStructure)), "value");
        }
        void IAddChild.AddText(string text) { }

        IEnumerator<BlockElement> IEnumerable<BlockElement>.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<BlockElement>)this).GetEnumerator();
        }
        
        /// <summary>
        ///
        /// </summary>
        public String Marker
        {
            get { return _markerName; }
            set { _markerName = value; }
        }

        private String _markerName;

    }
    /// <summary>
    ///
    /// </summary>
    public class TableStructure : SemanticBasicElement, IAddChild, IEnumerable<TableRowGroupStructure>, IEnumerable
    {
        /// <summary>
        ///
        /// </summary>
        public TableStructure()
        {
            _elementType = FixedElement.ElementType.Table;
        }

        public void Add(TableRowGroupStructure tableRowGroup)
        {
            if (tableRowGroup == null)
            {
                throw new ArgumentNullException(nameof(tableRowGroup));
            }
            ((IAddChild) this).AddChild(tableRowGroup);
        }
        
        void IAddChild.AddChild(object value)
        {
            if (value is TableRowGroupStructure)
            {
                _elementList.Add((TableRowGroupStructure)value);
                return;
            }
            throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(TableRowGroupStructure)), "value");
        }
        
        void IAddChild.AddText(string text) { }
        
        IEnumerator<TableRowGroupStructure> IEnumerable<TableRowGroupStructure>.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TableRowGroupStructure>)this).GetEnumerator();
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class TableRowGroupStructure : SemanticBasicElement, IAddChild, IEnumerable<TableRowStructure>, IEnumerable
    {
        /// <summary>
        ///
        /// </summary>
        public TableRowGroupStructure()
        {
            _elementType = FixedElement.ElementType.TableRowGroup;
        }

        public void Add(TableRowStructure tableRow)
        {
            if (tableRow == null)
            {
                throw new ArgumentNullException(nameof(tableRow));
            }
            ((IAddChild) this).AddChild(tableRow);
        }
        
        void IAddChild.AddChild(object value)
        {
            if (value is TableRowStructure)
            {
                _elementList.Add((TableRowStructure)value);
                return;
            }
            throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(TableRowStructure)), nameof(value));
        }

        void IAddChild.AddText(string text) { }

        
        IEnumerator<TableRowStructure> IEnumerable<TableRowStructure>.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TableRowStructure>)this).GetEnumerator();
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class TableRowStructure : SemanticBasicElement, IAddChild, IEnumerable<TableCellStructure>, IEnumerable
    {
        /// <summary>
        ///
        /// </summary>
        public TableRowStructure()
        {
            _elementType = FixedElement.ElementType.TableRow;
        }

        public void Add(TableCellStructure tableCell)
        {
            if (tableCell == null)
            {
                throw new ArgumentNullException(nameof(tableCell));
            }
            ((IAddChild) this).AddChild(tableCell);
        }
    
        void IAddChild.AddChild(object value)
        {
            if (value is TableCellStructure)
            {
                _elementList.Add((TableCellStructure)value);
                return;
            }
            throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(TableCellStructure)), nameof(value));
        }
        
        void IAddChild.AddText(string text) { }
        
        IEnumerator<TableCellStructure> IEnumerable<TableCellStructure>.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TableCellStructure>)this).GetEnumerator();
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class TableCellStructure : SemanticBasicElement, IAddChild, IEnumerable<BlockElement>, IEnumerable
    {
        /// <summary>
        ///
        /// </summary>
        public TableCellStructure()
        {
            _elementType = FixedElement.ElementType.TableCell;
            _rowSpan = 1;
            _columnSpan = 1;
        }

        public void Add(BlockElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            ((IAddChild) this).AddChild(element);
        }
        
        void IAddChild.AddChild(object value)
        {
            if (value is ParagraphStructure || value is TableStructure || value is ListStructure || value is FigureStructure)
            {
                _elementList.Add((BlockElement)value);
                return;
            }
            throw new ArgumentException(SR.Get(SRID.DocumentStructureUnexpectedParameterType4, value.GetType(),
                typeof(ParagraphStructure), typeof(TableStructure), typeof(ListStructure), typeof(FigureStructure)), nameof(value));
        }
        
        void IAddChild.AddText(string text) { }

        IEnumerator<BlockElement> IEnumerable<BlockElement>.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<BlockElement>)this).GetEnumerator();
        }
        
        /// <summary>
        ///
        /// </summary>
        public int RowSpan
        {
            get { return _rowSpan; }
            set {_rowSpan = value; }
        }

        /// <summary>
        ///
        /// </summary>
        public int ColumnSpan
        {
            get { return _columnSpan; }
            set {_columnSpan = value; }
        }

        private int _rowSpan;
        private int _columnSpan;
    }
 }

