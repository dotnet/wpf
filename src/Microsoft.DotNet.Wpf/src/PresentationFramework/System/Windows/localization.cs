// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Localization.Comments & Localization.Attributes attached properties
//
//

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;  // ConditionalWeakTable
using MS.Internal.Globalization;

namespace System.Windows
{
    //
    // Note: the class name and property name must be kept in sync'ed with
    // Framework\MS\Internal\Globalization\LocalizationComments.cs file.
    // Compiler checks for them by literal string comparisons.
    //
    /// <summary>
    /// Class defines attached properties for Comments and Localizability
    /// </summary>
    public static class Localization
    {
        /// <summary>
        /// DependencyProperty for Comments property.
        /// </summary>
        public static readonly DependencyProperty CommentsProperty =
            DependencyProperty.RegisterAttached(
                "Comments",
                typeof(string),
                typeof(Localization)
                );

        /// <summary>
        /// DependencyProperty for Localizability property.
        /// </summary>
        public static readonly DependencyProperty AttributesProperty =
            DependencyProperty.RegisterAttached(
                "Attributes",
                typeof(string),
                typeof(Localization)
                );

        /// <summary>
        /// Reads the attached property Comments from given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        [AttachedPropertyBrowsableForType(typeof(object))]
        public static string GetComments(object element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return GetValue(element, CommentsProperty);
        }

        /// <summary>
        /// Writes the attached property Comments to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="comments">The property value to set</param>
        public static void SetComments(object element, string comments)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            LocComments.ParsePropertyComments(comments);
            SetValue(element, CommentsProperty, comments);
        }

        /// <summary>
        /// Reads the attached property Localizability from given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        [AttachedPropertyBrowsableForType(typeof(object))]
        public static string GetAttributes(object element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return GetValue(element, AttributesProperty);
        }

        /// <summary>
        /// Writes the attached property Localizability to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="attributes">The property value to set</param>
        public static void SetAttributes(object element, string attributes)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            LocComments.ParsePropertyLocalizabilityAttributes(attributes);
            SetValue(element, AttributesProperty, attributes);
        }

        private static string GetValue(object element, DependencyProperty property)
        {
            DependencyObject dependencyObject = element as DependencyObject;
            if (dependencyObject != null)
            {
                // For DO, get the value from the property system
                return (string) dependencyObject.GetValue(property);
            }

            // For objects, get the value from our own hashtable
            string result;
            if (property == CommentsProperty)
            {
                _commentsOnObjects.TryGetValue(element, out result);
            }
            else
            {
                Debug.Assert(property == AttributesProperty);
                _attributesOnObjects.TryGetValue(element, out result);
            }

            return result;
        }

        private static void SetValue(object element, DependencyProperty property, string value)
        {
            DependencyObject dependencyObject = element as DependencyObject;
            if (dependencyObject != null)
            {
                // For DO, store the value in the property system
                dependencyObject.SetValue(property, value);
                return;
            }

            // For other objects, store the value in our own hashtable
            if (property == CommentsProperty)
            {
                _commentsOnObjects.Remove(element);
                _commentsOnObjects.Add(element, value);
            }
            else
            {
                _attributesOnObjects.Remove(element);
                _attributesOnObjects.Add(element, value);
            }
        }


        ///
        /// private storage for values set on objects
        ///
        private static ConditionalWeakTable<object, string> _commentsOnObjects = new ConditionalWeakTable<object, string>();
        private static ConditionalWeakTable<object, string> _attributesOnObjects = new ConditionalWeakTable<object, string>();
    }
}
