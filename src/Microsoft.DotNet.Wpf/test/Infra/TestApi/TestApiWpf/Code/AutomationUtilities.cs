// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Automation;

namespace Microsoft.Test
{
    /// <summary>
    /// The AutomationUtilities class provides a simple interface to common 
    /// <a href="http://msdn.microsoft.com/en-us/library/ms747327.aspx">UI Automation</a> (UIA) operations. 
    /// The most common class of UIA operations in testing involves discovery of UI elements. 
    /// </summary>
    /// <example>
    /// This sample discovers and clicks the "Close" button in an "About" dialog box, thus
    /// dismissing the "About" dialog box.
    /// <code>
    ///
    ///    string aboutDialogName = "About";
    ///    string closeButtonName = "Close";
    ///
    ///    AutomationElementCollection aboutDialogs = AutomationUtilities.FindElementsByName(
    ///        AutomationElement.RootElement,
    ///        aboutDialogName);
    ///        
    ///    AutomationElementCollection closeButtons = AutomationUtilities.FindElementsByName(
    ///        aboutDialogs[0],
    ///        closeButtonName);
    ///        
    ///    // You can either invoke the discovered control, through its invoke pattern ...
    ///    InvokePattern p = 
    ///        closeButtons[0].GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
    ///    p.Invoke();
    ///    
    ///    // ... or you can handle the mouse directly and click on the control.
    ///    System.Windows.Point winPoint = closeButtons[0].GetClickablePoint();
    ///    System.Drawing.Point drawingPoint = new System.Drawing.Point((int)winPoint.X, (int)winPoint.Y);
    ///    Mouse.MoveTo(drawingPoint);
    ///    Mouse.Click(System.Windows.Input.MouseButton.Left);
    ///    
    /// </code>
    /// </example>
    public static class AutomationUtilities
    {
        /// <summary>
        /// Retrieves the child element with the specified index.
        /// </summary>
        /// <param name="rootElement">The parent element (e.g., a ListBox control).</param>
        /// <param name="index"> The index of the child element to find.</param>
        /// <returns>An AutomationElement representing the discovered child element.</returns>
        public static AutomationElement FindElementByIndex(AutomationElement rootElement, int index)
        {
            Condition condition = new PropertyCondition(
                AutomationElement.IsControlElementProperty, 
                true);

            AutomationElementCollection found = rootElement.FindAll(TreeScope.Children, condition);
            return found[index];
        }

        
        /// <summary>
        /// Retrieves all UIA elements that meet the specified conditions.
        /// </summary>
        /// <param name="rootElement">Parent element, such as an application window, or 
        /// AutomationElement.RootElement when searching for the application window.</param>
        /// <param name="conditions">Conditions that the returned collection should meet.</param>
        /// <returns>A UIA element collection.</returns>
        public static AutomationElementCollection FindElements(AutomationElement rootElement, params Condition[] conditions)
        {
            Condition condition = new AndCondition(conditions);

            return rootElement.FindAll(TreeScope.Children, condition);
        }


        /// <summary>
        /// Retrieves a UIA collection of all elements with a given class name.
        /// </summary>
        /// <param name="rootElement">Parent element, such as an application window, or 
        /// AutomationElement.RootElement when searching for the application window.</param>
        /// <param name="className">The class name of the control type to find.</param>
        /// <returns>A UIA element collection.</returns>
        public static AutomationElementCollection FindElementsByClassName(AutomationElement rootElement, string className)
        {
            Condition condition = new PropertyCondition(
                AutomationElement.ClassNameProperty, 
                className);

            return rootElement.FindAll(TreeScope.Children, condition);
        }


        /// <summary>
        /// Retrieves a UIA collection of all elements of a given control type.
        /// </summary>
        /// <param name="rootElement">Parent element, such as an application window, or 
        /// AutomationElement.RootElement when searching for the application window.</param>
        /// <param name="controlType">Control type of the control, such as Button.</param>
        /// <returns>A UIA element collection.</returns>
        public static AutomationElementCollection FindElementsByControlType(AutomationElement rootElement, ControlType controlType)
        {
            Condition condition = new PropertyCondition(
                AutomationElement.ControlTypeProperty, 
                controlType);

            return rootElement.FindAll(TreeScope.Element | TreeScope.Children, condition);
        }


        /// <summary>
        /// Retrieves a UIA collection of all elements with a given UIA identifier.
        /// </summary>
        /// <param name="rootElement">Parent element, such as an application window, or 
        /// AutomationElement.RootElement when searching for the application window.</param>
        /// <param name="automationId">UIA identifier of the searched element, such as "button1".</param>
        /// <returns>A UIA element collection.</returns>
        public static AutomationElementCollection FindElementsById(AutomationElement rootElement, string automationId)
        {
            Condition condition = new PropertyCondition(
                AutomationElement.AutomationIdProperty, 
                automationId,
                PropertyConditionFlags.IgnoreCase);

            return rootElement.FindAll(TreeScope.Element | TreeScope.Children, condition);
        }

        
        /// <summary>
        /// Retrieves a UIA collection of all elements with a given name.
        /// </summary>
        /// <param name="rootElement">Parent element, such as an application window, or 
        /// AutomationElement.RootElement when searching for the application window.</param>
        /// <param name="name">Name of the searched element, such as "button1".</param>
        /// <returns>A UIA element collection.</returns>
        public static AutomationElementCollection FindElementsByName(AutomationElement rootElement, string name)
        {
            Condition condition = new PropertyCondition(
                AutomationElement.NameProperty, 
                name,
                PropertyConditionFlags.IgnoreCase);

            return rootElement.FindAll(TreeScope.Element | TreeScope.Children, condition);
        }
    }
}
