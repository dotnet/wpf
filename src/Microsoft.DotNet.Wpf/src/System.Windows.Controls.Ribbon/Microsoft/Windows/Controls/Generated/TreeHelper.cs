// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Windows.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Media;

    internal static class TreeHelper
    {
        /// <summary>
        ///     Returns Visual parent of the given element.
        ///     If includeContentElements is true then the
        ///     logic considers the logical parent of the content
        ///     element as visual parent.
        /// </summary>
        private static DependencyObject GetVisualParent(DependencyObject element, bool includeContentElements)
        {
            if (includeContentElements)
            {
                ContentElement ce = element as ContentElement;
                if (ce != null)
                {
                    return LogicalTreeHelper.GetParent(ce);
                }
            }
            return VisualTreeHelper.GetParent(element);
        }

        /// <summary>
        ///     Returns visual parent if possible else
        ///     logical parent of the element.
        /// </summary>
        public static DependencyObject GetParent(DependencyObject element)
        {
            DependencyObject parent = null;
            if (!(element is ContentElement))
            {
                parent = VisualTreeHelper.GetParent(element);
            }
            if (parent == null)
            {
                parent = LogicalTreeHelper.GetParent(element);
            }
            return parent;
        }

        /// <summary>
        ///     Walks up the templated parent tree looking for a parent type.
        /// </summary>
        public static T FindTemplatedAncestor<T>(FrameworkElement element) where T : FrameworkElement
        {
            while (element != null)
            {
                element = element.TemplatedParent as FrameworkElement;
                T correctlyTyped = element as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }
            }

            return null;
        }

        /// <summary>
        ///     Walks up the visual parent tree looking for a parent type.
        ///     element can be a ContentElement.
        /// </summary>
        public static T FindVisualAncestor<T>(DependencyObject element) where T : DependencyObject
        {
            // Allows element to be ContentElement
            bool includeContentElements = true;
            while (element != null)
            {
                element = GetVisualParent(element, includeContentElements);
                T correctlyTyped = element as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }
                includeContentElements = false;
            }

            return null;
        }

        /// <summary>
        ///     Walks up the visual parent tree looking for a parent which satisfies a predicate
        ///     element can be a ContentElement.
        /// </summary>
        public static DependencyObject FindVisualAncestor(DependencyObject element,
            Predicate<DependencyObject> predicate)
        {
            // Allows element to be ContentElement
            bool includeContentElements = true;
            while (element != null)
            {
                element = GetVisualParent(element, includeContentElements);
                if (element != null && predicate(element))
                {
                    return element;
                }
                includeContentElements = false;
            }
            return null;
        }

        /// <summary>
        ///     Walks up the logical tree looking for a parent type
        /// </summary>
        public static T FindLogicalAncestor<T>(DependencyObject element) where T : DependencyObject
        {
            while (element != null)
            {
                element = LogicalTreeHelper.GetParent(element);
                T correctlyTyped = element as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }
            }

            return null;
        }

        /// <summary>
        ///     Walks up the visual parent tree looking for a parent type. 
        ///     If we are out of visual parents it switches over to the logical parent.
        /// </summary>
        public static DependencyObject FindAncestor(DependencyObject element,
            Predicate<DependencyObject> predicate)
        {
            while (element != null)
            {
                element = GetParent(element);
                if (element != null && predicate(element))
                {
                    return element;
                }
            }
            return null;
        }

        /// <summary>
        ///     Walks up the visual / logical tree and finds the element
        ///     which has neither visual parent nor logical parent.
        /// </summary>
        public static DependencyObject FindRoot(DependencyObject element)
        {
            while (element != null)
            {
                DependencyObject parent = GetParent(element);
                if (parent == null)
                {
                    return element;
                }
                element = parent;
            }
            return null;
        }

        /// <summary>
        ///     Walks up the visual tree and finds the element
        ///     which has no visual parent.
        ///     element can be a ContentElement.
        /// </summary>
        public static DependencyObject FindVisualRoot(DependencyObject element)
        {
            // Allows element to be ContentElement
            bool includeContentElements = true;
            while (element != null)
            {
                DependencyObject parent = GetVisualParent(element, includeContentElements);
                if (parent == null)
                {
                    return element;
                }
                element = parent;
                includeContentElements = false;
            }
            return null;
        }

        /// <summary>
        /// Walks up the visual tree and invalidates Measure till a parent of type PathEndType is found.
        /// </summary>
        /// <typeparam name="PathEndType">Invalidation ends when this type is found in the visual tree</typeparam>
        /// <param name="pathStart">Invalidation starts from this child</param>
        public static void InvalidateMeasureForVisualAncestorPath<PathEndType>(DependencyObject pathStart) where PathEndType : DependencyObject
        {
            InvalidateMeasureForVisualAncestorPath<PathEndType>(pathStart, /*includePathEnd*/ true);
        }

        /// <summary>
        ///     Walks up the visual tree and invalidates Measure till a parent of type PathEndType is found.
        ///     pathStart can be a ContentElement.
        /// </summary>
        public static void InvalidateMeasureForVisualAncestorPath<PathEndType>(DependencyObject pathStart,
            bool includePathEnd) where PathEndType : DependencyObject
        {
            // Allows element to be ContentElement
            bool includeContentElements = true;
            while (pathStart != null)
            {
                bool isEndType = (pathStart is PathEndType);
                if (!includePathEnd && isEndType)
                {
                    return;
                }
                UIElement element = pathStart as UIElement;
                if (element != null)
                {
                    element.InvalidateMeasure();
                }
                if (isEndType)
                {
                    return;
                }
                pathStart = GetVisualParent(pathStart, includeContentElements);
                includeContentElements = false;
            }
        }

        /// <summary>
        ///     Walks up the visual tree and invalidates Measure till a parent satisfies the predicate.
        /// </summary>
        public static void InvalidateMeasureForVisualAncestorPath(DependencyObject pathStart,
            Predicate<DependencyObject> predicate)
        {
            // Allows element to be ContentElement
            bool includeContentElements = true;
            while (pathStart != null)
            {
                UIElement element = pathStart as UIElement;
                if (element != null)
                {
                    element.InvalidateMeasure();
                }

                if (predicate(pathStart))
                {
                    return;
                }
                pathStart = GetVisualParent(pathStart, includeContentElements);
                includeContentElements = false;
            }
        }

        /// <summary>
        ///     Returns true if ancestor is a visual ancestor of the descendant.
        ///     descendant can be a ContentElement.
        ///     Returns true if both ancestor and descendant are the same.
        /// </summary>
        public static bool IsVisualAncestorOf(DependencyObject ancestor, DependencyObject descendant)
        {
            if (ancestor == null ||
                descendant == null)
            {
                return false;
            }
            // Allows element to be ContentElement
            bool includeContentElements = true;
            while (descendant != null)
            {
                if (descendant == ancestor)
                {
                    return true;
                }
                descendant = GetVisualParent(descendant, includeContentElements);
                includeContentElements = false;
            }
            return false;
        }

    }
}
