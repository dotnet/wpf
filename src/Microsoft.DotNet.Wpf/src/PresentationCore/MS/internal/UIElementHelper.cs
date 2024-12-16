// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Debug.Assert(o != null, "UIElementHelper.IsHitTestVisible called with null argument");

            if (o is UIElement oAsUIElement)
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
            Debug.Assert(o != null, "UIElementHelper.IsVisible called with null argument");

            if (o is UIElement oAsUIElement)
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
            Debug.Assert(o != null, "UIElementHelper.PredictFocus called with null argument");

            if (o is UIElement uie)
            {
                return uie.PredictFocus(direction);
            }
            else if (o is ContentElement ce)
            {
                return ce.PredictFocus(direction);
            }
            else if (o is UIElement3D uie3d)
            {
                return uie3d.PredictFocus(direction);
            }

            return null;
        }

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
            if (child is Visual vis)
            {
                myParent = vis.InternalVisualParent;
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
                if (child is UIElement childAsUIElement)
                {
                    parent = InputElement.GetContainingInputElement(childAsUIElement.GetUIParentCore()) as DependencyObject;
                }
                else
                {
                    if (child is UIElement3D childAsUIElement3D)
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

            while (o != null && continueInvalidation)
            {
                continueInvalidation &= InvalidateAutomationPeer(o, out e, out ce, out e3d);

                //
                // Invoke InvalidateAutomationAncestorsCore
                //
                bool continuePastVisualTree = false;
                if (e != null)
                {
                    continueInvalidation &= e.InvalidateAutomationAncestorsCore(branchNodeStack, out continuePastVisualTree);

                    // Get element's visual parent
                    o = e.GetUIParent(continuePastVisualTree);
                }
                else if (ce != null)
                {
                    continueInvalidation &= ce.InvalidateAutomationAncestorsCore(branchNodeStack, out continuePastVisualTree);

                    // Get element's visual parent
                    o = (DependencyObject)ce.GetUIParent(continuePastVisualTree);
                }
                else if (e3d != null)
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
            if (e != null)
            {
                if (e.HasAutomationPeer == true)
                    ap = e.GetAutomationPeer();
            }
            else
            {
                ce = o as ContentElement;
                if (ce != null)
                {
                    if (ce.HasAutomationPeer == true)
                        ap = ce.GetAutomationPeer();
                }
                else
                {
                    e3d = o as UIElement3D;
                    if (e3d != null)
                    {
                        if (e3d.HasAutomationPeer == true)
                            ap = e3d.GetAutomationPeer();
                    }
                }
            }

            if (ap != null)
            {
                ap.InvalidateAncestorsRecursive();

                // Check for parent being non-null while stopping as we don't want to stop in between due to peers not connected to AT
                // those peers sometimes gets created to serve for various patterns.
                // e.g: ScrollViewAutomationPeer for Scroll Pattern in case of ListBox.
                if (ap.GetParent() != null)
                    return false;
            }

            return true;
        }
    }
}
