// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Implementation of an empty proxy provider

using System.Windows.Automation;
using System.Windows.Automation.Provider;

namespace MS.Internal.AutomationProxies
{
    // Empty proxy provider
    internal class EmptyElement : IRawElementProviderSimple
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        protected EmptyElement()        
        {
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  IRawElementProviderSimple
        //
        //------------------------------------------------------

        #region Interface IRawElementProviderSimple
        ProviderOptions IRawElementProviderSimple.ProviderOptions
        {
            get
            {
                return ProviderOptions.ClientSideProvider;
            }
        }

        object IRawElementProviderSimple.GetPatternProvider(int patternId)
        {
            return null;
        }

        object IRawElementProviderSimple.GetPropertyValue(int propertyId)
        {
            return null;
        }

        IRawElementProviderSimple IRawElementProviderSimple.HostRawElementProvider
        {
            get
            {
                return null;
            }
        }

        #endregion Interface IRawElementProviderSimple
    }


    // Empty GridItem cell implementation
    internal sealed class EmptyGridItem : EmptyElement,
        IRawElementProviderSimple,
        IGridItemProvider
    {
        #region Data
        private readonly int _row;
        private readonly int _column;
        private readonly int _rowSpan;
        private readonly int _columnSpan;
        private IRawElementProviderSimple _containingGrid;
        #endregion Data

        #region Constructor

        internal EmptyGridItem(int row, int column, IRawElementProviderSimple containingGrid)
        {
            _row = row;
            _column = column;
            _rowSpan = 1;
            _columnSpan = 1;
            _containingGrid = containingGrid;
        }

        #endregion Constructor

        #region IRawElementProviderSimple
        
        object IRawElementProviderSimple.GetPatternProvider(int patternId)
        {
            if (patternId == GridItemPattern.Pattern.Id)
            {
                return this;
            }
            return null;
        }
       
        #endregion IRawElementProviderSimple

        #region IGridItemProvider

        int IGridItemProvider.Column
        {
            get
            {
                return _column;
            }
        }
        int IGridItemProvider.ColumnSpan
        {
            get
            {
                return _columnSpan;
            }
        }
        IRawElementProviderSimple IGridItemProvider.ContainingGrid
        {
            get
            {
                return _containingGrid;
            }
        }
        int IGridItemProvider.Row
        {
            get
            {
                return _row;
            }
        }
        int IGridItemProvider.RowSpan
        {
            get
            {
                return _rowSpan;
            }
        }

        #endregion IGridItemProvider
    }
}
