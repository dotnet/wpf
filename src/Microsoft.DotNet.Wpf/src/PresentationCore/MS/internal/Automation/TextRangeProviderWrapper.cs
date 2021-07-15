// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: TextRange provider wrapper for WCP
//
//

using System;
using System.Collections;
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
    internal class TextRangeProviderWrapper: MarshalByRefObject, ITextRangeProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        internal TextRangeProviderWrapper( AutomationPeer peer, ITextRangeProvider iface )
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface ITextRangeProvider
        //
        //------------------------------------------------------
 
        #region Interface ITextRangeProvider

        public ITextRangeProvider Clone()
        {
            return (ITextRangeProvider)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(Clone), null);
        }

        public bool Compare(ITextRangeProvider range)
        {
            if (!(range is TextRangeProviderWrapper))
            {
                throw new ArgumentException(SR.Get(SRID.TextRangeProvider_InvalidRangeProvider, "range"));
            }

            return (bool)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(Compare), range);
        }

        public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            if (!(targetRange is TextRangeProviderWrapper))
            {
                throw new ArgumentException(SR.Get(SRID.TextRangeProvider_InvalidRangeProvider, "targetRange"));
            }

            object[] args = new object[] { endpoint, targetRange, targetEndpoint };
            return (int)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(CompareEndpoints), args);
        }

        public void ExpandToEnclosingUnit(TextUnit unit)
        {
            object[] args = new object[] { unit };
            ElementUtil.Invoke(_peer, new DispatcherOperationCallback(ExpandToEnclosingUnit), args);
        }

        public ITextRangeProvider FindAttribute(int attribute, object val, bool backward)
        {
            object[] args = new object[] { attribute, val, backward };
            return (ITextRangeProvider)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(FindAttribute), args);
        }

        public ITextRangeProvider FindText(string text, bool backward, bool ignoreCase)
        {
            object[] args = new object[] { text, backward, ignoreCase };
            return (ITextRangeProvider)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(FindText), args);
        }

        public object GetAttributeValue(int attribute)
        {
            object[] args = new object[] { attribute };
            return ElementUtil.Invoke(_peer, new DispatcherOperationCallback(GetAttributeValue), args);
        }

        public double [] GetBoundingRectangles()
        {
            return (double [])ElementUtil.Invoke(_peer, new DispatcherOperationCallback(GetBoundingRectangles), null);
        }

        public IRawElementProviderSimple GetEnclosingElement()
        {
            return (IRawElementProviderSimple)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(GetEnclosingElement), null);
        }

        public string GetText(int maxLength)
        {
            object[] args = new object[] {maxLength};
            return (string)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(GetText), args);
        }

        public int Move(TextUnit unit, int count)
        {
            object[] args = new object[] { unit, count };
            return (int)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(Move), args);
        }

        public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
        {
            object[] args = new object[] { endpoint, unit, count };
            return (int)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(MoveEndpointByUnit), args);
        }

        public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            if (!(targetRange is TextRangeProviderWrapper))
            {
                throw new ArgumentException(SR.Get(SRID.TextRangeProvider_InvalidRangeProvider, "targetRange"));
            }

            object[] args = new object[] { endpoint, targetRange, targetEndpoint };
            ElementUtil.Invoke(_peer, new DispatcherOperationCallback(MoveEndpointByRange), args);
        }

        public void Select()
        {
            ElementUtil.Invoke(_peer, new DispatcherOperationCallback(Select), null);
        }

        public void AddToSelection()
        {
            ElementUtil.Invoke(_peer, new DispatcherOperationCallback(AddToSelection), null);
        }

        public void RemoveFromSelection()
        {
            ElementUtil.Invoke(_peer, new DispatcherOperationCallback(RemoveFromSelection), null);
        }

        public void ScrollIntoView(bool alignToTop)
        {
            ElementUtil.Invoke(_peer, new DispatcherOperationCallback(ScrollIntoView), alignToTop);
        }

        public IRawElementProviderSimple[] GetChildren()
        {
                return (IRawElementProviderSimple[])ElementUtil.Invoke(_peer, new DispatcherOperationCallback(GetChildren), null);
        }


        #endregion Interface ITextRangeProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        // Wrap arguments that are being returned, assuming they're not null or already wrapped.
        static internal ITextRangeProvider WrapArgument(ITextRangeProvider argument, AutomationPeer peer)
        {
            if (argument == null)
                return null;

            if (argument is TextRangeProviderWrapper)
                return argument;

            return new TextRangeProviderWrapper(peer, argument);
        }

        static internal ITextRangeProvider [] WrapArgument(ITextRangeProvider [] argument, AutomationPeer peer)
        {
            if (argument == null)
                return null;

            if (argument is TextRangeProviderWrapper [])
                return argument;

            ITextRangeProvider[] outArray = new ITextRangeProvider[argument.Length];
            for (int i = 0; i < argument.Length; i++)
            {
                outArray[i] = WrapArgument(argument[i], peer);
            }
            return outArray;
        }

        // Remove the wrapper from the argument if a wrapper exists
        static internal ITextRangeProvider UnwrapArgument(ITextRangeProvider argument)
        {
            if (argument is TextRangeProviderWrapper)
            {
                 return ((TextRangeProviderWrapper)argument)._iface;
            }

            return argument;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private object Clone(object unused)
        {
            return TextRangeProviderWrapper.WrapArgument( _iface.Clone(), _peer );
        }

        private object Compare(object arg)
        {
            ITextRangeProvider range = (ITextRangeProvider)arg;
            return _iface.Compare( TextRangeProviderWrapper.UnwrapArgument( range ) );
        }

        private object CompareEndpoints(object arg)
        {
            object[] args = (object[])arg;
            TextPatternRangeEndpoint endpoint = (TextPatternRangeEndpoint)args[0];
            ITextRangeProvider targetRange = (ITextRangeProvider)args[1];
            TextPatternRangeEndpoint targetEndpoint = (TextPatternRangeEndpoint)args[2];
            return _iface.CompareEndpoints(endpoint, TextRangeProviderWrapper.UnwrapArgument( targetRange ), targetEndpoint);
        }

        private object ExpandToEnclosingUnit(object arg)
        {
            object[] args = (object[])arg;
            TextUnit unit = (TextUnit)args[0];
            _iface.ExpandToEnclosingUnit(unit);
            return null;
        }

        private object FindAttribute(object arg)
        {
            object[] args = (object[])arg;
            int attribute = (int)args[0];
            object val = args[1];
            bool backward = (bool)args[2];
            return TextRangeProviderWrapper.WrapArgument( _iface.FindAttribute(attribute, val, backward), _peer );
        }

        private object FindText(object arg)
        {
            object[] args = (object[])arg;
            string text = (string)args[0];
            bool backward = (bool)args[1];
            bool ignoreCase = (bool)args[2];
            return TextRangeProviderWrapper.WrapArgument( _iface.FindText(text, backward, ignoreCase), _peer );
        }

        private object GetAttributeValue(object arg)
        {
            object[] args = (object[])arg;
            int attribute = (int)args[0];
            return _iface.GetAttributeValue(attribute);
            // note: if an attribute value is ever a range then we'll need to wrap/unwrap it appropriately here.
        }

        private object GetBoundingRectangles(object unused)
        {
            return _iface.GetBoundingRectangles();
        }

        private object GetEnclosingElement(object unused)
        {
            return _iface.GetEnclosingElement();
        }

        private object GetText(object arg)
        {
            object[] args = (object[])arg;
            int maxLength = (int)args[0];
            return _iface.GetText(maxLength);
        }

        private object Move(object arg)
        {
            object[] args = (object[])arg;
            TextUnit unit = (TextUnit)args[0];
            int count = (int)args[1];
            return _iface.Move(unit, count);
        }

        private object MoveEndpointByUnit(object arg)
        {
            object[] args = (object[])arg;
            TextPatternRangeEndpoint endpoint = (TextPatternRangeEndpoint)args[0];
            TextUnit unit = (TextUnit)args[1];
            int count = (int)args[2];
            return _iface.MoveEndpointByUnit(endpoint, unit, count);
        }

        private object MoveEndpointByRange(object arg)
        {
            object[] args = (object[])arg;
            TextPatternRangeEndpoint endpoint = (TextPatternRangeEndpoint)args[0];
            ITextRangeProvider targetRange = (ITextRangeProvider)args[1];
            TextPatternRangeEndpoint targetEndpoint = (TextPatternRangeEndpoint)args[2];
            _iface.MoveEndpointByRange(endpoint, TextRangeProviderWrapper.UnwrapArgument( targetRange ), targetEndpoint);
            return null;
        }

        private object Select(object unused)
        {
            _iface.Select();
            return null;
        }

        private object AddToSelection(object unused)
        {
            _iface.AddToSelection();
            return null;
        }

        private object RemoveFromSelection(object unused)
        {
            _iface.RemoveFromSelection();
            return null;
        }

        private object ScrollIntoView(object arg)
        {
            bool alignTop = (bool)arg;
            _iface.ScrollIntoView(alignTop);
            return null;
        }

        private object GetChildren(object unused)
        {
            return _iface.GetChildren();
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private ITextRangeProvider _iface;

        #endregion Private Fields
    }
}

