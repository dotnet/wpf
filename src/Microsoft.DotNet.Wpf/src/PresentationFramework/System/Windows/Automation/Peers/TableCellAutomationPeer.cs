// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Automation peer for TableCell
//

using System.Windows.Automation.Provider;   // IRawElementProviderSimple
using System.Windows.Documents;

namespace System.Windows.Automation.Peers
{
    ///
    public class TableCellAutomationPeer : TextElementAutomationPeer, IGridItemProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner of the AutomationPeer.</param>
        public TableCellAutomationPeer(TableCell owner)
            : base(owner)
        { }

        /// <summary>
        /// <see cref="AutomationPeer.GetPattern"/>
        /// </summary>
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.GridItem)
            {
                return this;
            }
            else
            {
                return base.GetPattern(patternInterface);
            }
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetAutomationControlTypeCore"/>
        /// </summary>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetLocalizedControlTypeCore"/>
        /// </summary>
        protected override string GetLocalizedControlTypeCore()
        {
            return "cell";
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetClassNameCore"/>
        /// </summary>
        protected override string GetClassNameCore()
        {
            return "TableCell";
        }

        /// <summary>
        /// <see cref="AutomationPeer.IsControlElementCore"/>
        /// </summary>
        protected override bool IsControlElementCore()
        {
            // We only want this peer to show up in the Control view if it is visible
            // For compat we allow falling back to legacy behavior (returning true always)
            // based on AppContext flags, IncludeInvisibleElementsInControlView evaluates them.
            return IncludeInvisibleElementsInControlView || IsTextViewVisible == true;
        }

        /// <summary>
        /// Raises property changed events in response to column span change.
        /// </summary>
        internal void OnColumnSpanChanged(int oldValue, int newValue)
        {
            RaisePropertyChangedEvent(GridItemPatternIdentifiers.ColumnSpanProperty, oldValue, newValue);
        }

        /// <summary>
        /// Raises property changed events in response to row span change.
        /// </summary>
        internal void OnRowSpanChanged(int oldValue, int newValue)
        {
            RaisePropertyChangedEvent(GridItemPatternIdentifiers.RowSpanProperty, oldValue, newValue);
        }

        //-------------------------------------------------------------------
        //
        //  IGridProvider Members
        //
        //-------------------------------------------------------------------

        #region IGridItemProvider Members

        /// <summary>
        /// Returns the current row that the item is located at.
        /// </summary>
        int IGridItemProvider.Row
        {
            get
            {
                return ((TableCell)Owner).RowIndex;
            }
        }
        
        /// <summary>
        /// Returns the current column that the item is located at.
        /// </summary>
        int IGridItemProvider.Column
        {
            get
            {
                return ((TableCell)Owner).ColumnIndex;
            }
        }

        /// <summary>
        /// Return the current number of rows that the item spans.
        /// </summary>
        int IGridItemProvider.RowSpan
        {
            get
            {
                return ((TableCell)Owner).RowSpan;
            }
        }

        /// <summary>
        /// Return the current number of columns that the item spans.
        /// </summary>
        int IGridItemProvider.ColumnSpan
        {
            get
            {
                return ((TableCell)Owner).ColumnSpan;
            }
        }

        /// <summary>
        /// Returns the container that maintains the grid layout for the item.
        /// </summary>
        IRawElementProviderSimple IGridItemProvider.ContainingGrid
        {
            get
            {
                if ((TableCell)Owner != null)
                {
                    return ProviderFromPeer(CreatePeerForElement(((TableCell)Owner).Table));
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion IGridItemProvider Members
    }
}
