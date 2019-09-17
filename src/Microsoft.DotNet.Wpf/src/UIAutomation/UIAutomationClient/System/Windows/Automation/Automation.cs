// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Facade class that groups together the main functionality that clients need to get started.
//


// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using MS.Internal.Automation;
using MS.Win32;

namespace System.Windows.Automation
{
    /// <summary>
    /// Class containing client Automation methods that are not specific to a particular element
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class Automation
#else
    public static class Automation
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Condition that describes the Raw view of the UIAutomation tree</summary>
        public static readonly Condition RawViewCondition = Condition.TrueCondition;

        /// <summary>Condition that describes the Control view of the UIAutomation tree</summary>
        public static readonly Condition ControlViewCondition = new NotCondition(
                                            new PropertyCondition( AutomationElement.IsControlElementProperty, false) );
        // 


        /// <summary>Condition that describes the Content view of the UIAutomation tree</summary>
        public static readonly Condition ContentViewCondition = new NotCondition( new OrCondition(
                                            new PropertyCondition( AutomationElement.IsControlElementProperty, false),
                                            new PropertyCondition( AutomationElement.IsContentElementProperty, false)));

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

 
        #region Public Methods

        #region Element Comparisons
        /// <summary>
        /// Compares two elements, returning true if both refer to the same piece of UI.
        /// </summary>
        /// <param name="el1">element to compare</param>
        /// <param name="el2">element to compare</param>
        /// <returns>true if el1 and el2 refer to the same underlying UI</returns>
        /// <remarks>Both el1 and el1 must be non-null</remarks>
        public static bool Compare(AutomationElement el1, AutomationElement el2)
        {
            return Misc.Compare(el1, el2);
        }

        /// <summary>
        /// Compares two integer arrays, returning true if they have the same contents
        /// </summary>
        /// <param name="runtimeId1">integer array to compare</param>
        /// <param name="runtimeId2">integer array to compare</param>
        /// <returns>true if runtimeId1 and runtimeId2 refer to the same underlying UI</returns>
        /// <remarks>Both runtimeId1 and runtimeId2 must be non-null. Can be
        /// used to compare RuntimeIds from elements.</remarks>
        public static bool Compare(int[] runtimeId1, int[] runtimeId2)
        {
            return Misc.Compare(runtimeId1, runtimeId2);
        }
        #endregion Element Comparisons

        #region Misc: Find, Property Names
        /// <summary>
        /// Get string describing specified property idenfier
        /// </summary>
        /// <param name="property">property to get string for</param>
        /// <returns>Sting containing human-readable name of specified property</returns>
        public static string PropertyName( AutomationProperty property )
        {
            Misc.ValidateArgumentNonNull(property, "property");
            // Suppress PRESHARP Parameter to this public method must be validated; element is checked above.
#pragma warning suppress 56506
            string full = property.ProgrammaticName.Split('.')[1]; // remove portion before the ".", leaving just "NameProperty" or similar
            return full.Substring(0, full.Length - 8); // Slice away "Property" suffix
        }

        /// <summary>
        /// Get string describing specified pattern idenfier
        /// </summary>
        /// <param name="pattern">pattern to get string for</param>
        /// <returns>Sting containing human-readable name of specified pattern</returns>
        public static string PatternName( AutomationPattern pattern )
        {
            Misc.ValidateArgumentNonNull(pattern, "pattern");
            // Suppress PRESHARP Parameter to this public method must be validated; element is checked above.
#pragma warning suppress 56506
            string full = pattern.ProgrammaticName;
            return full.Substring(0, full.Length - 26); // Slice away "InvokePatternIdentifiers.Pattern" to get just "Invoke"
        }
        #endregion Misc: Find, Property Names

        #region Events
        /// <summary>
        /// Called by a client to add a listener for pattern or custom events.
        /// </summary>
        /// <param name="eventId">A control pattern or custom event identifier.</param>
        /// <param name="element">Element on which to listen for control pattern or custom events.</param>
        /// <param name="scope">Specifies whether to listen to property changes events on the specified element, and/or its ancestors and children.</param>
        /// <param name="eventHandler">Delegate to call when the specified event occurs.</param>
        public static void AddAutomationEventHandler(
            AutomationEvent eventId,
            AutomationElement element,
            TreeScope scope,
            AutomationEventHandler eventHandler
            )
        {
            Misc.ValidateArgumentNonNull(element, "element" );
            Misc.ValidateArgumentNonNull(eventHandler, "eventHandler" );
            Misc.ValidateArgument( eventId != AutomationElement.AutomationFocusChangedEvent, SRID.EventIdMustNotBeAutomationFocusChanged );
            Misc.ValidateArgument( eventId != AutomationElement.StructureChangedEvent,SRID.EventIdMustNotBeStructureChanged );
            Misc.ValidateArgument( eventId != AutomationElement.AutomationPropertyChangedEvent, SRID.EventIdMustNotBeAutomationPropertyChanged );

            if (eventId == WindowPattern.WindowClosedEvent)
            {
                // Once a window closes and the hwnd is destroyed we won't be able to determine where it was in the 
                // Automation tree; therefore only support WindowClosed events for all windows (eg. src==root and scope 
                // is descendants) or a specific WindowPattern element (src==root of a Window and scope is the element).
                // Also handle odd combinations (eg. src==specific element and scope is subtree|ancestors).

                bool paramsValidated = false;

                if ( Misc.Compare( element, AutomationElement.RootElement ) )
                {
                    // For root element need to have Descendants scope set (Note: Subtree includes Descendants)
                    if ( ( scope & TreeScope.Descendants ) == TreeScope.Descendants )
                    {
                        paramsValidated = true;
                    }
                }
                else
                {
                    // otherwise non-root elements must have the entire tree (Anscestors, Element and Descendants)...
                    if ( ( scope & ( TreeScope.Ancestors | TreeScope.Element | TreeScope.Descendants ) ) == ( TreeScope.Ancestors | TreeScope.Element | TreeScope.Descendants ) )
                    {
                        paramsValidated = true;
                    }
                    else if ( ( scope & TreeScope.Element ) == TreeScope.Element )
                    {
                        // ...OR Element where the element implements WindowPattern
                        // PRESHARP will flag this as warning 56506/6506:Parameter 'element' to this public method must be validated: A null-dereference can occur here.
                        // False positive, element is checked, see above
#pragma warning suppress 6506
                        object val = element.GetCurrentPropertyValue(AutomationElement.NativeWindowHandleProperty);
                        if ( val != null && val is int && (int)val != 0 )
                        {
                            if ( HwndProxyElementProvider.IsWindowPatternWindow( NativeMethods.HWND.Cast( new IntPtr( (int)val ) ) ) )
                            {
                                paramsValidated = true;
                            }
                        }
                    }
                }

                if ( !paramsValidated )
                {
                    throw new ArgumentException( SR.Get( SRID.ParamsNotApplicableToWindowClosedEvent ) );
                }
            }

            // Add a client-side Handler for for this event request
            EventListener l = new EventListener(eventId, scope, null, CacheRequest.CurrentUiaCacheRequest);
            ClientEventManager.AddListener(element, eventHandler, l);
        }


        /// <summary>
        /// Called by a client to remove a listener for pattern or custom events.
        /// </summary>
        /// <param name="eventId">a UIAccess or custom event identifier.</param>
        /// <param name="element">Element to remove listener for</param>
        /// <param name="eventHandler">The handler object that was passed to AddEventListener</param>
        public static void RemoveAutomationEventHandler(
            AutomationEvent eventId,
            AutomationElement element,
            AutomationEventHandler eventHandler
            )
        {
            Misc.ValidateArgumentNonNull(element, "element" );
            Misc.ValidateArgumentNonNull(eventHandler, "eventHandler" );
            Misc.ValidateArgument( eventId != AutomationElement.AutomationFocusChangedEvent, SRID.EventIdMustNotBeAutomationFocusChanged );
            Misc.ValidateArgument( eventId != AutomationElement.StructureChangedEvent, SRID.EventIdMustNotBeStructureChanged );
            Misc.ValidateArgument( eventId != AutomationElement.AutomationPropertyChangedEvent, SRID.EventIdMustNotBeAutomationPropertyChanged );

            // Remove the client-side listener for for this event
            ClientEventManager.RemoveListener( eventId, element, eventHandler );
        }

        /// <summary>
        /// Called by a client to add a listener for property changed events.
        /// </summary>
        /// <param name="element">Element on which to listen for property changed events.</param>
        /// <param name="scope">Specifies whether to listen to property changes events on the specified element, and/or its ancestors and children.</param>
        /// <param name="eventHandler">Callback object to call when a specified property change occurs.</param>
        /// <param name="properties">Params array of properties to listen for changes in.</param>
        public static void AddAutomationPropertyChangedEventHandler(
            AutomationElement element,            // reference element for listening to the event
            TreeScope scope,                   // scope to listen to
            AutomationPropertyChangedEventHandler eventHandler,    // callback object
            params AutomationProperty [] properties           // listen for changes to these properties
            )
        {
            Misc.ValidateArgumentNonNull(element, "element" );
            Misc.ValidateArgumentNonNull(eventHandler, "eventHandler" );
            Misc.ValidateArgumentNonNull(properties, "properties" );
            if (properties.Length == 0)
            {
                throw new ArgumentException( SR.Get(SRID.AtLeastOnePropertyMustBeSpecified) );
            }

            // Check that no properties are interpreted properties
            // If more interpreted properties are identified add a mapping of
            // on interpreted properties to the real property that raises events.
            foreach (AutomationProperty property in properties)
            {
                Misc.ValidateArgumentNonNull(property, "properties" );
            }

            // Add a client-side listener for for this event request
            EventListener l = new EventListener(AutomationElement.AutomationPropertyChangedEvent, scope, properties, CacheRequest.CurrentUiaCacheRequest);
            ClientEventManager.AddListener(element, eventHandler, l);
        }

        /// <summary>
        /// Called by a client to remove a listener for property changed events.
        /// </summary>
        /// <param name="element">Element to remove listener for</param>
        /// <param name="eventHandler">The handler object that was passed to AutomationPropertyChangedEventHandler</param>
        public static void RemoveAutomationPropertyChangedEventHandler(
            AutomationElement element,            // reference element being listened to
            AutomationPropertyChangedEventHandler eventHandler     // callback object (used as cookie here)
            )
        {
            Misc.ValidateArgumentNonNull(element, "element" );
            Misc.ValidateArgumentNonNull(eventHandler, "eventHandler" );

            // Remove the client-side listener for for this event
            ClientEventManager.RemoveListener(AutomationElement.AutomationPropertyChangedEvent, element, eventHandler);
        }

        /// <summary>
        /// Called by a client to add a listener for structure change events.
        /// </summary>
        /// <param name="element">Element on which to listen for structure change events.</param>
        /// <param name="scope">Specifies whether to listen to property changes events on the specified element, and/or its ancestors and children.</param>
        /// <param name="eventHandler">Delegate to call when a structure change event occurs.</param>
        public static void AddStructureChangedEventHandler(AutomationElement element, TreeScope scope, StructureChangedEventHandler eventHandler)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            Misc.ValidateArgumentNonNull(eventHandler, "eventHandler");

            // Add a client-side listener for for this event request
            EventListener l = new EventListener(AutomationElement.StructureChangedEvent, scope, null, CacheRequest.CurrentUiaCacheRequest);

            ClientEventManager.AddListener(element, eventHandler, l);
        }

        /// <summary>
        /// Called by a client to remove a listener for structure change events.
        /// </summary>
        /// <param name="element">Element to remove listener for</param>
        /// <param name="eventHandler">The handler object that was passed to AddStructureChangedListener</param>
        public static void RemoveStructureChangedEventHandler(AutomationElement element, StructureChangedEventHandler eventHandler)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            Misc.ValidateArgumentNonNull(eventHandler, "eventHandler");

            // Remove the client-side listener for for this event
            ClientEventManager.RemoveListener(AutomationElement.StructureChangedEvent, element, eventHandler);
        }

        /// <summary>
        /// Called by a client to add a listener for focus changed events.
        /// </summary>
        /// <param name="eventHandler">Delegate to call when a focus change event occurs.</param>
        public static void AddAutomationFocusChangedEventHandler(
            AutomationFocusChangedEventHandler eventHandler
            )
        {
            Misc.ValidateArgumentNonNull(eventHandler, "eventHandler" );

            // Add a client-side listener for for this event request
            EventListener l = new EventListener(AutomationElement.AutomationFocusChangedEvent, 
                                                TreeScope.Subtree | TreeScope.Ancestors, 
                                                null,
                                                CacheRequest.CurrentUiaCacheRequest);
            ClientEventManager.AddFocusListener(eventHandler, l);
        }

        /// <summary>
        /// Called by a client to remove a listener for focus changed events.
        /// </summary>
        /// <param name="eventHandler">The handler object that was passed to AddAutomationFocusChangedListener</param>
        public static void RemoveAutomationFocusChangedEventHandler(
            AutomationFocusChangedEventHandler eventHandler
            )
        {
            Misc.ValidateArgumentNonNull(eventHandler, "eventHandler" );

            // Remove the client-side listener for for this event
            ClientEventManager.RemoveFocusListener(eventHandler);
        }

        /// <summary>
        /// Called by a client to remove all listeners that the client has added.
        /// </summary>
        public static void RemoveAllEventHandlers()
        {
            // Remove the client-side listener for for this event
            ClientEventManager.RemoveAllListeners();
        }
        #endregion Events

        #endregion Public Methods
    }
}
