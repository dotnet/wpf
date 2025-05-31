// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Text pattern provider wrapper for WCP

using System.Windows.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

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

        public ITextRangeProvider[] GetSelection()
        {
            return ElementUtil.Invoke(_peer, static (state, peer) => TextRangeProviderWrapper.WrapArgument(state.GetSelection(), peer), _iface, _peer);
        }

        public ITextRangeProvider[] GetVisibleRanges()
        {
            return ElementUtil.Invoke(_peer, static (state, peer) => TextRangeProviderWrapper.WrapArgument(state.GetVisibleRanges(), peer), _iface, _peer);
        }

        public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement)
        {
            if (!(childElement is ElementProxy))
            {
                throw new ArgumentException(SR.Format(SR.TextProvider_InvalidChild, "childElement"));
            }

            return (ITextRangeProvider)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(RangeFromChild), childElement);
        }

        public ITextRangeProvider RangeFromPoint(Point screenLocation)
        {
            return (ITextRangeProvider)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(RangeFromPoint), screenLocation);
        }

        public ITextRangeProvider DocumentRange
        {
            get => ElementUtil.Invoke(_peer, static (state, peer) => TextRangeProviderWrapper.WrapArgument(state.DocumentRange, peer), _iface, _peer);
        }

        public SupportedTextSelection SupportedTextSelection
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.SupportedTextSelection, _iface);
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

