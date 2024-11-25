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

namespace MS.Internal
{
    internal static class UIElementHelper
    {
        internal static bool IsHitTestVisible(DependencyObject o)
        {
            Debug.Assert(o is not null, "UIElementHelper.IsHitTestVisible called with null argument");

            UIElement oAsUIElement = o as UIElement;
            if (oAsUIElement is not null)
            {
                return oAsUIElement.IsHitTestVisible;
            }
            else
            {
                return ((UIElement3D)o).IsHitTestVisible;
            }
        }

        internal static bool IsVisible(DependencyObject o)
        {
            Debug.Assert(o is not null, "UIElementHelper.IsVisible called with null argument");

            UIElement oAsUIElement = o as UIElement;
            if (oAsUIElement is not null)
            {
                return oAsUIElement.IsVisible;
            }
            else
            {
                return ((UIElement3D)o).IsVisible;
            }
        }

        internal static DependencyObject PredictFocus(DependencyObject o, FocusNavigationDirection direction)
        {
            Debug.Assert(o is not null, "UIElementHelper.PredictFocus called with null argument");

            UIElement uie;
            ContentElement ce;
            UIElement3D uie3d;

            if ((uie = o as UIElement) is not null)
            {
                return uie.PredictFocus(direction);
            }
            else if ((ce = o as ContentElement) is not null)
            {
                return ce.PredictFocus(direction);
            }
            else if ((uie3d = o as UIElement3D) is not null)
            {
                return uie3d.PredictFocus(direction);
            }

            return null;
        }

        internal static UIElement GetContainingUIElement2D(DependencyObject reference)
        {
            UIElement element = null;

            while (reference is not null)
            {
                element = reference as UIElement;

                if (element is not null) break;

                reference = VisualTreeHelper.GetParent(reference);
            }

            return element;
        }

        internal static DependencyObject GetUIParent(DependencyObject child)
        {
            DependencyObject parent = GetUIParent(child, false);

            return parent;
        }

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
            if(parent is null && continuePastVisualTree)
            {
                UIElement childAsUIElement = child as UIElement;
                if (childAsUIElement is not null)
                {
                    parent = InputElement.GetContainingInputElement(childAsUIElement.GetUIParentCore()) as DependencyObject;
                }
                else
                {
                    UIElement3D childAsUIElement3D = child as UIElement3D;
                    if (childAsUIElement3D is not null)
                    {
                        parent = InputElement.GetContainingInputElement(childAsUIElement3D.GetUIParentCore()) as DependencyObject;
                    }
                }
            }

            return parent;
        }

        internal static bool IsUIElementOrUIElement3D(DependencyObject o)
        {
            return (o is UIElement or UIElement3D);
        }

        internal static void InvalidateAutomationAncestors(DependencyObject o)
        {
            UIElement e = null;
            UIElement3D e3d = null;
            ContentElement ce = null;

            Stack<DependencyObject> branchNodeStack = new Stack<DependencyObject>();
            bool continueInvalidation = true;

            while (o is not null && continueInvalidation)
            {
                continueInvalidation &= InvalidateAutomationPeer(o, out e, out ce, out e3d);

                //
                // Invoke InvalidateAutomationAncestorsCore
                //
                bool continuePastVisualTree = false;
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
                    o = (DependencyObject)ce.GetUIParent(continuePastVisualTree);
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
            e = null;
            ce = null;
            e3d = null;

            AutomationPeer ap = null;

            e = o as UIElement;
            if (e is not null)
            {
                if (e.HasAutomationPeer == true)
                    ap = e.GetAutomationPeer();
            }
            else
            {
                ce = o as ContentElement;
                if (ce is not null)
                {
                    if (ce.HasAutomationPeer == true)
                        ap = ce.GetAutomationPeer();
                }
                else
                {
                    e3d = o as UIElement3D;
                    if (e3d is not null)
                    {
                        if (e3d.HasAutomationPeer == true)
                            ap = e3d.GetAutomationPeer();
                    }
                }
            }

            if (ap is not null)
            {
                ap.InvalidateAncestorsRecursive();

                // Check for parent being non-null while stopping as we don't want to stop in between due to peers not connected to AT
                // those peers sometimes gets created to serve for various patterns.
                // e.g: ScrollViewAutomationPeer for Scroll Pattern in case of ListBox.
                if (ap.GetParent() is not null)
                    return false;
            }

            return true;
        }
    }
}
