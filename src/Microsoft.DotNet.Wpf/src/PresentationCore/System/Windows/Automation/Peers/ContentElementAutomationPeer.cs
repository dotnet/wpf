// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Automation element for ContentElements
//

using System;                           // Object
using System.Collections.Generic;       // List<T>
using System.Windows.Input;             // AccessKeyManager
using MS.Internal.PresentationCore;     // SR
using System.Windows.Automation.Provider;
using System.Windows.Automation;
using MS.Internal.Automation;

namespace System.Windows.Automation.Peers
{
    /// 
    public class ContentElementAutomationPeer : AutomationPeer
    {
        ///
        public ContentElementAutomationPeer(ContentElement owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }
            _owner = owner;
        }

        /// 
        public ContentElement Owner
        {
            get
            {
                return _owner;
            }
        }

        ///<summary>
        /// This static helper creates an AutomationPeer for the specified element and 
        /// caches it - that means the created peer is going to live long and shadow the
        /// element for its lifetime. The peer will be used by Automation to proxy the element, and
        /// to fire events to the Automation when something happens with the element.
        /// The created peer is returned from this method and also from subsequent calls to this method
        /// and <seealso cref="FromElement"/>. The type of the peer is determined by the 
        /// <seealso cref="UIElement.OnCreateAutomationPeer"/> virtual callback. If FrameworkContentElement does not
        /// implement the callback, there will be no peer and this method will return 'null' (in other
        /// words, there is no such thing as a 'default peer').
        ///</summary>
        public static AutomationPeer CreatePeerForElement(ContentElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return element.CreateAutomationPeer();
        }

        ///
        public static AutomationPeer FromElement(ContentElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return element.GetAutomationPeer();
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetChildrenCore"/>
        /// </summary>
        override protected List<AutomationPeer> GetChildrenCore()
        {
            return null;
        }

        /// 
        override public object GetPattern(PatternInterface patternInterface)
        {
            //Support synchronized input
            if (patternInterface == PatternInterface.SynchronizedInput)
            {
                // Adaptor object is used here to avoid loading UIA assemblies in non-UIA scenarios.
                if (_synchronizedInputPattern == null)
                    _synchronizedInputPattern = new SynchronizedInputAdaptor(_owner);
                return _synchronizedInputPattern;
            }
            return null;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetAutomationControlTypeCore"/>
        /// </summary>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetAutomationIdCore"/>
        /// </summary>
        protected override string GetAutomationIdCore()
        {
            return AutomationProperties.GetAutomationId(_owner);
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetNameCore"/>
        /// </summary>
        protected override string GetNameCore()
        {
            return AutomationProperties.GetName(_owner);
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetHelpTextCore"/>
        /// </summary>
        protected override string GetHelpTextCore()
        {
            return AutomationProperties.GetHelpText(_owner);
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetBoundingRectangleCore"/>
        /// </summary>
        override protected Rect GetBoundingRectangleCore()
        {
            return Rect.Empty;
        }

        /// <summary>
        /// <see cref="AutomationPeer.IsOffscreenCore"/>
        /// </summary>
        override protected bool IsOffscreenCore()
        {
            IsOffscreenBehavior behavior = AutomationProperties.GetIsOffscreenBehavior(_owner);

            switch (behavior)
            {
                case IsOffscreenBehavior.Onscreen :
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetOrientationCore"/>
        /// </summary>
        override protected AutomationOrientation GetOrientationCore()
        {
            return AutomationOrientation.None;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetItemTypeCore"/>
        /// </summary>
        override protected string GetItemTypeCore()
        {
            return string.Empty;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetClassNameCore"/>
        /// </summary>
        override protected string GetClassNameCore()
        {
            return string.Empty;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetItemStatusCore"/>
        /// </summary>
        override protected string GetItemStatusCore()
        {
            return string.Empty;
        }

        /// <summary>
        /// <see cref="AutomationPeer.IsRequiredForFormCore"/>
        /// </summary>
        override protected bool IsRequiredForFormCore()
        {
            return false;
        }

        /// <summary>
        /// <see cref="AutomationPeer.IsKeyboardFocusableCore"/>
        /// </summary>
        override protected bool IsKeyboardFocusableCore()
        {
            return Keyboard.IsFocusable(_owner);
        }

        /// <summary>
        /// <see cref="AutomationPeer.HasKeyboardFocusCore"/>
        /// </summary>
        override protected bool HasKeyboardFocusCore()
        {
            return _owner.IsKeyboardFocused;
        }

        /// <summary>
        /// <see cref="AutomationPeer.IsEnabledCore"/>
        /// </summary>
        override protected bool IsEnabledCore()
        {
            return _owner.IsEnabled;
        }

        /// <summary>
        /// <see cref="AutomationPeer.IsPasswordCore"/>
        /// </summary>
        override protected bool IsPasswordCore()
        {
            return false;
        }

        /// <summary>
        /// <see cref="AutomationPeer.IsContentElementCore"/>
        /// </summary>
        override protected bool IsContentElementCore()
        {
            return true;
        }

        /// <summary>
        /// <see cref="AutomationPeer.IsControlElementCore"/>
        /// </summary>
        override protected bool IsControlElementCore()
        {
            return false;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetLabeledByCore"/>
        /// </summary>
        override protected AutomationPeer GetLabeledByCore()
        {
            return null;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetAcceleratorKeyCore"/>
        /// </summary>
        override protected string GetAcceleratorKeyCore()
        {
            return string.Empty;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetAccessKeyCore"/>
        /// </summary>
        override protected string GetAccessKeyCore()
        {
            return AccessKeyManager.InternalGetAccessKeyCharacter(_owner);
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetLiveSettingCore"/>
        /// </summary>
        override protected AutomationLiveSetting GetLiveSettingCore()
        {
            return AutomationProperties.GetLiveSetting(_owner);
        }

        /// <summary>
        /// Provides a value for UIAutomation's PositionInSet property
        /// Reads <see cref="AutomationProperties.PositionInSetProperty"/> and returns the value
        /// </summary>
        override protected int GetPositionInSetCore()
        {
            return AutomationProperties.GetPositionInSet(_owner);
        }

        /// <summary>
        /// Provides a value for UIAutomation's SizeOfSet property
        /// Reads <see cref="AutomationProperties.SizeOfSetProperty"/> and returns the value
        /// </summary>
        override protected int GetSizeOfSetCore()
        {
            return AutomationProperties.GetSizeOfSet(_owner);
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetClickablePointCore"/>
        /// </summary>
        override protected Point GetClickablePointCore()
        {
            return new Point(double.NaN, double.NaN);
        }

        /// <summary>
        /// <see cref="AutomationPeer.SetFocusCore"/>
        /// </summary>
        override protected void SetFocusCore()
        {
            if (!_owner.Focus())
                throw new InvalidOperationException(SR.Get(SRID.SetFocusFailed));
        }

        ///
        internal override Rect GetVisibleBoundingRectCore()
        {
            return GetBoundingRectangle();
        }
        

        private ContentElement _owner;
        private SynchronizedInputAdaptor _synchronizedInputPattern;
    }
}
