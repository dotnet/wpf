// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// Automation Peer for DataGridHeader
    /// </summary>
    public class DataGridColumnHeaderItemAutomationPeer : ItemAutomationPeer,
        IInvokeProvider, IScrollItemProvider, ITransformProvider, IVirtualizedItemProvider
    {
        public DataGridColumnHeaderItemAutomationPeer(object item, DataGridColumn column, DataGridColumnHeadersPresenterAutomationPeer peer)
            :base(item, peer)
        {
            _column = column;
        }
                
        #region AutomationPeer Overrides

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.HeaderItem;
        }

        /// <summary>
        /// Called by GetClassName that gets a human readable name that, in addition to AutomationControlType, 
        /// differentiates the control represented by this AutomationPeer.
        /// </summary>
        /// <returns>The string that contains the name.</returns>
        protected override string GetClassNameCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetClassName();
            }
            else
            {
                ThrowElementNotAvailableException();
            }
            return String.Empty;
        }

        /// <summary>
        /// Gets the control pattern that is associated with the specified System.Windows.Automation.Peers.PatternInterface.
        /// </summary>
        /// <param name="patternInterface">A value from the System.Windows.Automation.Peers.PatternInterface enumeration.</param>
        /// <returns>The object that supports the specified pattern, or null if unsupported.</returns>
        public override object GetPattern(PatternInterface patternInterface)
        {
            switch (patternInterface)
            {
                case PatternInterface.Invoke:
                    {
                        if (Column != null && Column.CanUserSort)
                        {
                            return this;
                        }

                        break;
                    }

                case PatternInterface.ScrollItem:
                    {
                        if (Column != null)
                        {
                            return this;
                        }
                        break;
                    }

                case PatternInterface.Transform:
                    {
                        if (Column != null && Column.CanUserResize)
                        {
                            return this;
                        }
                        
                        break;
                    }
                case PatternInterface.VirtualizedItem:
                    {
                        if (Column != null)
                        {
                            return this;
                        }

                        break;
                    }
            }

            return null;
        }

        // AutomationControlType.HeaderItem must return IsContentElement false.
        // See http://msdn.microsoft.com/en-us/library/ms742202.aspx
        protected override bool IsContentElementCore()
        {
            return false;
        }
        #endregion

        #region IInvokeProvider
        void IInvokeProvider.Invoke()
        {
            UIElementAutomationPeer wrapperPeer = GetWrapperPeer() as UIElementAutomationPeer;
            if (wrapperPeer != null)
            {
                ((DataGridColumnHeader)wrapperPeer.Owner).Invoke();
            }
            else
                ThrowElementNotAvailableException();
        }
        #endregion

        #region IScrollItemProvider

        void IScrollItemProvider.ScrollIntoView()
        {
            if (Column != null && this.OwningDataGrid != null)
            {
                this.OwningDataGrid.ScrollIntoView(null, Column);
            }
        }

        #endregion

        #region ITransformProvider

        bool ITransformProvider.CanMove 
        { 
            get 
            { 
                return false; 
            } 
        }

        bool ITransformProvider.CanResize 
        { 
            get 
            { 
                if (this.Column != null)
                    return Column.CanUserResize;
                return false;
            } 
        }

        bool ITransformProvider.CanRotate 
        { 
            get 
            { 
                return false; 
            } 
        }

        void ITransformProvider.Move(double x, double y)
        {
            throw new InvalidOperationException(SR.Get(SRID.DataGridColumnHeaderItemAutomationPeer_Unsupported));
        } 

        void ITransformProvider.Resize(double width, double height)
        {
            if (this.OwningDataGrid != null && Column.CanUserResize)
            {
                Column.Width = new DataGridLength(width);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.DataGridColumnHeaderItemAutomationPeer_Unresizable));
            }
        }

        void ITransformProvider.Rotate(double degrees)
        {
            throw new InvalidOperationException(SR.Get(SRID.DataGridColumnHeaderItemAutomationPeer_Unsupported));
        }

        #endregion

        #region IVirtualizedItemProvider
        void IVirtualizedItemProvider.Realize()
        {
            if (this.OwningDataGrid != null)
                OwningDataGrid.ScrollIntoView(null,Column);
        }

        #endregion

        #region Properties

        internal override bool AncestorsInvalid
        {
            get { return base.AncestorsInvalid; }
            set
            {
                base.AncestorsInvalid = value;
                if (value)
                    return;
                AutomationPeer wrapperPeer = OwningColumnHeaderPeer;
                if (wrapperPeer != null)
                {
                    wrapperPeer.AncestorsInvalid = false;
                }
            }
        }

        internal DataGridColumnHeader OwningHeader
        {
            get
            {
                return GetWrapper() as DataGridColumnHeader;
            }
        }

        internal DataGrid OwningDataGrid
        {
            get
            {
                return Column.DataGridOwner;
            }
        }

        internal DataGridColumn Column
        {
            get { return _column; }
        }

        internal DataGridColumnHeaderAutomationPeer OwningColumnHeaderPeer
        {
            get
            {
                return GetWrapperPeer() as DataGridColumnHeaderAutomationPeer;
            }
        }

        #endregion

        #region Private Variables
        DataGridColumn _column;
        #endregion
    }
}
