// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Text pattern provider wrapper for WCP
//
//

using System;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using System.Windows.Automation.Peers;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Automation
{
    // see comment on InvokeProviderWrapper class for explanation of purpose and organization of these wrapper classes.
    internal class TextProviderWrapper : MarshalByRefObject, ITextProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private TextProviderWrapper( AutomationPeer peer, ITextProvider iface )
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface ITextProvider
        //
        //------------------------------------------------------
 
        #region Interface ITextProvider

        public ITextRangeProvider [] GetSelection()
        {
            return (ITextRangeProvider [])ElementUtil.Invoke(_peer, new DispatcherOperationCallback(GetSelection), null);
        }

        public ITextRangeProvider [] GetVisibleRanges()
        {
            return (ITextRangeProvider[])ElementUtil.Invoke(_peer, new DispatcherOperationCallback(GetVisibleRanges), null);
        }

        public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement)
        {
            if (!(childElement is ElementProxy))
            {
                throw new ArgumentException(SR.Get(SRID.TextProvider_InvalidChild, "childElement"));
            }

            return (ITextRangeProvider)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(RangeFromChild), childElement);
        }

        public ITextRangeProvider RangeFromPoint(Point screenLocation)
        {
            return (ITextRangeProvider)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(RangeFromPoint), screenLocation);
        }

        public ITextRangeProvider DocumentRange 
        {
            get
            {
                return (ITextRangeProvider)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(GetDocumentRange), null);
            }
        }

        public SupportedTextSelection SupportedTextSelection
        {
            get
            {
                return (SupportedTextSelection)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(GetSupportedTextSelection), null);
            }
        }

        #endregion Interface ITextProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface )
        {
            return new TextProviderWrapper( peer, (ITextProvider) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private object GetSelection(object unused)
        {
            return TextRangeProviderWrapper.WrapArgument( _iface.GetSelection(), _peer );
        }

        private object GetVisibleRanges(object unused)
        {
            return TextRangeProviderWrapper.WrapArgument( _iface.GetVisibleRanges(), _peer );
        }

        private object RangeFromChild(object arg)
        {
            IRawElementProviderSimple childElement = (IRawElementProviderSimple)arg;
            return TextRangeProviderWrapper.WrapArgument( _iface.RangeFromChild(childElement), _peer );
        }

        private object RangeFromPoint(object arg)
        {
            Point screenLocation = (Point)arg;
            return TextRangeProviderWrapper.WrapArgument( _iface.RangeFromPoint(screenLocation), _peer );
        }

        private object GetDocumentRange(object unused)
        {
            return TextRangeProviderWrapper.WrapArgument( _iface.DocumentRange, _peer );
        }

        private object GetSupportedTextSelection(object unused)
        {
            return _iface.SupportedTextSelection;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private ITextProvider _iface;

        #endregion Private Fields
    }
}

