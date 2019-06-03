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
    public class StatusBarAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public StatusBarAutomationPeer(StatusBar owner): base(owner)
        {}

        ///
        protected override string GetClassNameCore()
        {
            return "StatusBar";
        }
    
        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.StatusBar;
        }

        /// 
        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> list = new List<AutomationPeer>();
            ItemsControl itemscontrol = Owner as ItemsControl;
            if (itemscontrol != null)
            {
                foreach (object obj in itemscontrol.Items)
                {
                    if (obj is Separator)
                    {
                        Separator separator = obj as Separator;
                        list.Add(UIElementAutomationPeer.CreatePeerForElement(separator));
                    }
                    else
                    {
                        StatusBarItem item = itemscontrol.ItemContainerGenerator.ContainerFromItem(obj) as StatusBarItem;

                        if (item != null)
                        {
                            //If the item is a string or TextBlock or StatusBarItem
                            //StatusBarItemAutomationPeer will be created to show the text
                            //Or we'll use the control's automation peer
                            if (obj is string || obj is TextBlock
                                || (obj is StatusBarItem && ((StatusBarItem)obj).Content is string))
                            {
                                list.Add(UIElementAutomationPeer.CreatePeerForElement(item));
                            }
                            else
                            {
                                List<AutomationPeer> childList = GetChildrenAutomationPeer(item);
                                if (childList != null)
                                {
                                    foreach (AutomationPeer ap in childList)
                                    {
                                        list.Add(ap);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return list;
        }


        /// <summary>
        /// Get the children of the parent which has automation peer
        /// </summary>
        private List<AutomationPeer> GetChildrenAutomationPeer(Visual parent)
        {
            Invariant.Assert(parent != null);

            List<AutomationPeer> children = null;

            iterate(parent,
                    (IteratorCallback)delegate(AutomationPeer peer)
                    {
                        if (children == null)
                            children = new List<AutomationPeer>();

                        children.Add(peer);
                        return (false);
                    });

            return children;
        }

        private delegate bool IteratorCallback(AutomationPeer peer);

        //
        private static bool iterate(Visual parent, IteratorCallback callback)
        {
            bool done = false;

            AutomationPeer peer = null;

            int count = parent.InternalVisualChildrenCount;
            for (int i = 0; i < count && !done; i++)
            {
                Visual child = parent.InternalGetVisualChild(i);
                if (child != null
                    && child.CheckFlagsAnd(VisualFlags.IsUIElement)
                    && (peer = CreatePeerForElement((UIElement)child)) != null)
                {
                    done = callback(peer);
                }
                else
                {
                    done = iterate(child, callback);
                }
            }

            return (done);
        }
    }
}



