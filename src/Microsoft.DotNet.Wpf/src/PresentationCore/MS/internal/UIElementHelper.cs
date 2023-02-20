// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Input;
using System.Windows.Automation.Peers;

using MS.Internal.PresentationCore;

namespace MS.Internal
{
    internal static class UIElementHelper
    {
        [FriendAccessAllowed]
        internal static bool IsHitTestVisible(DependencyObject o)
        {
            Debug.Assert(o != null, "UIElementHelper.IsHitTestVisible called with null argument");

            UIElement oAsUIElement = o as UIElement;
            if (oAsUIElement != null)
            {
                return oAsUIElement.IsHitTestVisible;
            }
            else
            {
                return ((UIElement3D)o).IsHitTestVisible;
            }
        }

        [FriendAccessAllowed]
        internal static bool IsVisible(DependencyObject o)
        {
            Debug.Assert(o != null, "UIElementHelper.IsVisible called with null argument");

            UIElement oAsUIElement = o as UIElement;
            if (oAsUIElement != null)
            {
                return oAsUIElement.IsVisible;
            }
            else
            {
                return ((UIElement3D)o).IsVisible;
            }
        }

        [FriendAccessAllowed]
        internal static DependencyObject PredictFocus(DependencyObject o, FocusNavigationDirection direction)
        {
            Debug.Assert(o != null, "UIElementHelper.PredictFocus called with null argument");

            UIElement uie;
            ContentElement ce;
            UIElement3D uie3d;

            if ((uie = o as UIElement) != null)
            {
                return uie.PredictFocus(direction);
            }
            else if ((ce = o as ContentElement) != null)
            {
                return ce.PredictFocus(direction);
            }
            else if ((uie3d = o as UIElement3D) != null)
            {
                return uie3d.PredictFocus(direction);
            }

            return null;
        }

        [FriendAccessAllowed]
        internal static UIElement GetContainingUIElement2D(DependencyObject reference)
        {
            UIElement element = null;

            while (reference != null)
            {
                element = reference as UIElement;

                if (element != null) break;

                reference = VisualTreeHelper.GetParent(reference);
            }

            return element;
        }

        [FriendAccessAllowed]
        internal static DependencyObject GetUIParent(DependencyObject child)
        {
            DependencyObject parent = GetUIParent(child, false);

            return parent;
        }

        [FriendAccessAllowed]
        internal static DependencyObject GetUIParent(DependencyObject child, bool continuePastVisualTree)
        {
            DependencyObject parent = null;
            DependencyObject myParent = null;

            // Try to find a UIElement parent in the visual ancestry.
            if (child is Visual)
            {
                myParent = ((Visual)child).InternalVisualParent;
            }
            else
            {
                myParent = ((Visual3D)child).InternalVisualParent;
            }

            parent = InputElement.GetContainingUIElement(myParent) as DependencyObject;

            // If there was no UIElement parent in the visual ancestry,
            // check along the logical branch.
            if(parent == null && continuePastVisualTree)
            {
                UIElement childAsUIElement = child as UIElement;
                if (childAsUIElement != null)
                {
                    parent = InputElement.GetContainingInputElement(childAsUIElement.GetUIParentCore()) as DependencyObject;
                }
                else
                {
                    UIElement3D childAsUIElement3D = child as UIElement3D;
                    if (childAsUIElement3D != null)
                    {
                        parent = InputElement.GetContainingInputElement(childAsUIElement3D.GetUIParentCore()) as DependencyObject;
                    }
                }
            }

            return parent;
        }

        [FriendAccessAllowed]
        internal static bool IsUIElementOrUIElement3D(DependencyObject o)
        {
            return (o is UIElement or UIElement3D);
        }

        [FriendAccessAllowed]
        internal static void InvalidateAutomationAncestors(DependencyObject o)
        {
            Stack<DependencyObject> branchNodeStack = new();
            bool continueInvalidation = true;

            while (o != null && continueInvalidation)
            {
                continueInvalidation &= InvalidateAutomationPeer(o, out UIElement e, out ContentElement ce, out UIElement3D e3d);

                //
                // Invoke InvalidateAutomationAncestorsCore
                //
                bool continuePastVisualTree;
                if (e is not null)
                {
                    continueInvalidation &= e.InvalidateAutomationAncestorsCore(branchNodeStack, out continuePastVisualTree);

                    // Get element's visual parent
                    o = e.GetUIParent(continuePastVisualTree);
                }
                else if (ce is not null)
                {
                    continueInvalidation &= ce.InvalidateAutomationAncestorsCore(branchNodeStack, out continuePastVisualTree);

                    // Get element's visual parent
                    o = ce.GetUIParent(continuePastVisualTree);
                }
                else if (e3d is not null)
                {
                    continueInvalidation &= e3d.InvalidateAutomationAncestorsCore(branchNodeStack, out continuePastVisualTree);

                    // Get element's visual parent
                    o = e3d.GetUIParent(continuePastVisualTree);
                }
            }
        }

        internal static bool InvalidateAutomationPeer(
            DependencyObject o,
            out UIElement e,
            out ContentElement ce,
            out UIElement3D e3d)
        {
            ce = null;
            e3d = null;

            AutomationPeer ap = null;

            e = o as UIElement;
            if (e is not null)
            {
                if (e.HasAutomationPeer)
                    ap = e.GetAutomationPeer();
            }
            else
            {
                ce = o as ContentElement;
                if (ce is not null)
                {
                    if (ce.HasAutomationPeer)
                        ap = ce.GetAutomationPeer();
                }
                else
                {
                    e3d = o as UIElement3D;
                    if (e3d is { HasAutomationPeer: true }) ap = e3d.GetAutomationPeer();
                }
            }

            if (ap is null) return true;
            ap.InvalidateAncestorsRecursive();

            // Check for parent being non-null while stopping as we don't want to stop in between due to peers not connected to AT
            // those peers sometimes gets created to serve for various patterns.
            // e.g: ScrollViewAutomationPeer for Scroll Pattern in case of ListBox.
            return ap.GetParent() == null;
        }
    }
}
