// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Test.Input;
using Microsoft.Test.ObjectComparison;

namespace Microsoft.Test.Stability.Extensions
{
    //Anyone who thinks they need to modify this kind of code should sanity check with Peter Antal.
    internal static class HomelessTestHelpers
    {
        #region Merge Methods

        /// <summary>
        /// Merges Lists together
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        internal static void Merge(IList destination, IList source)
        {
            if (source != null && destination != null)
            {
                foreach (object o in source)
                {
                    destination.Add(o);
                }
            }
        }

        //The merge methods below are hard typed to compensate for design flaw in WPF type tree, and are exceptional cases.
        internal static void Merge(VisualCollection destination, List<Visual> source)
        {
            if (source != null && destination != null)
            {
                foreach (Visual o in source)
                {
                    destination.Add(o);
                }
            }
        }

        internal static void Merge(PageContentCollection destination, List<PageContent> source)
        {
            if (source != null && destination != null)
            {
                foreach (PageContent o in source)
                {
                    destination.Add(o);
                }
            }
        }
        #endregion

        /// <summary>
        /// Removes all objects of specified type from the List
        /// </summary>
        /// <typeparam name="T">Type of the List</typeparam>
        /// <param name="items">List to be processed</param>
        /// <param name="type">Type to be filtered</param>
        /// <returns>Filtered List</returns>
        internal static List<T> FilterListOfType<T>(List<T> items, Type type)
        {
            if (items != null && type != null)
            {
                List<T> result = new List<T>(items);
                result.RemoveAll(delegate(T element) { return element.GetType() == type; });
                return result;
            }
            else
            {
                return items;
            }
        }

        #region Tree Walkers
        internal static List<DependencyObject> LogicalTreeWalk(DependencyObject parent)
        {
            List<DependencyObject> elements = new List<DependencyObject>();

            IEnumerable children = LogicalTreeHelper.GetChildren(parent);
            foreach (object child in children)
            {
                DependencyObject childTree = child as DependencyObject;
                if (childTree != null)
                {
                    elements.Add(childTree);
                    elements.AddRange(LogicalTreeWalk(childTree));
                }
            }
            return elements;
        }

        internal static List<DependencyObject> VisualTreeWalk(DependencyObject parent)
        {
            List<DependencyObject> elements = new List<DependencyObject>();

            int numChildren = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numChildren; i++)
            {
                DependencyObject childTree = VisualTreeHelper.GetChild(parent, i);
                elements.Add(childTree);
                elements.AddRange(VisualTreeWalk(childTree));
            }
            return elements;
        }

        internal static IEnumerable<object> GetAllObjects(DependencyObject element, Type desiredType)
        {
            PublicPropertyObjectGraphFactory objectGraphFactory = new PublicPropertyObjectGraphFactory();
            GraphNode root = objectGraphFactory.CreateObjectGraph(element);
            List<GraphNode> nodes = new List<GraphNode>(root.GetNodesInDepthFirstOrder());
            foreach (GraphNode node in nodes)
            {
                if (node == null || node.ObjectType == null)
                {
                    yield break;
                }
                if (node.ObjectType.IsSubclassOf(desiredType))
                {
                    yield return node.ObjectValue;
                }
            }
        }

        #endregion

        internal static void KeyPress(System.Windows.Input.Key vKey)
        {
            KeyPress(new List<System.Windows.Input.Key> { vKey });
        }

        internal static void KeyPress(System.Windows.Input.Key vKey1, System.Windows.Input.Key vKey2)
        {
            KeyPress(new List<System.Windows.Input.Key> { vKey1, vKey2 });
        }

        internal static void KeyPress(System.Windows.Input.Key vKey1, System.Windows.Input.Key vKey2, System.Windows.Input.Key vKey3)
        {
            KeyPress(new List<System.Windows.Input.Key> { vKey1, vKey2, vKey3 });
        }

        internal static void KeyPress(List<System.Windows.Input.Key> vKeyList)
        {
            foreach (System.Windows.Input.Key key in vKeyList)
            {
                UserInput.KeyPress(key, true);
            }

            vKeyList.Reverse();

            foreach (System.Windows.Input.Key key in vKeyList)
            {
                UserInput.KeyPress(key, false);
            }
        }

        /// <summary>
        /// Check if the controlTemplate is usable for the targetType.
        /// If ControlTemplate.TargetType be null, or the targetType, or a subclass of targetType, it will return true.
        /// </summary>
        internal static bool IsControlTemplateUsable(ControlTemplate controlTemplate, Type targetType)
        {
            if (controlTemplate != null && (controlTemplate.TargetType == null || controlTemplate.TargetType == targetType || targetType.IsSubclassOf(controlTemplate.TargetType)))
            {
                return true;
            }

            return false;
        }
    }
}
