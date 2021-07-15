// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

using MS.Internal;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
    /// 
    public class ListViewAutomationPeer : ListBoxAutomationPeer
    {
        ///
        public ListViewAutomationPeer(ListView owner)
            : base(owner)
        {
            Invariant.Assert(owner != null);
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            if (_viewAutomationPeer != null)
            {
                return _viewAutomationPeer.GetAutomationControlType();
            }
            else
            {
                return base.GetAutomationControlTypeCore();
            }
        }

        ///
        override protected string GetClassNameCore()
        {
            return "ListView";
        }

        /// 
        override public object GetPattern(PatternInterface patternInterface)
        {
            object ret = null;
            if (_viewAutomationPeer != null)
            {
                ret = _viewAutomationPeer.GetPattern(patternInterface);
                if (ret != null)
                {
                    return ret;
                }
            }
            
            return base.GetPattern(patternInterface);
        }

        /// 
        protected override List<AutomationPeer> GetChildrenCore()
        {
            if (_refreshItemPeers)
            {
                _refreshItemPeers = false;
                ItemPeers.Clear();
            }

            List<AutomationPeer> ret = base.GetChildrenCore();

            if (_viewAutomationPeer != null)
            {
                //If a custom view doesn't want to implement GetChildren details
                //just return null, we'll use the base.GetChildren as the return value
                ret = _viewAutomationPeer.GetChildren(ret);
            }

            return ret;
        }

        ///
        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return _viewAutomationPeer == null ? base.CreateItemAutomationPeer(item) : _viewAutomationPeer.CreateItemAutomationPeer(item);
        }

        /// <summary>
        /// 
        /// </summary>
        protected internal IViewAutomationPeer ViewAutomationPeer
        {
            // Note: see bug 1555137 for details.
            // Never inline, as we don't want to unnecessarily link the 
            // automation DLL via the ISelectionProvider interface type initialization.
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            get { return _viewAutomationPeer; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            set
            {
                if (_viewAutomationPeer != value)
                {
                    _refreshItemPeers = true;
                }
                _viewAutomationPeer = value;
            }
        }

        #region Private Fields

        private bool _refreshItemPeers = false;
        private IViewAutomationPeer _viewAutomationPeer;

        #endregion
    }
}


