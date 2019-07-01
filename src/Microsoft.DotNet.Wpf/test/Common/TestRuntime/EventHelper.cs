// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace Microsoft.Test
{
    public delegate void EventTriggerCallback();
    public delegate void EventValidationCallback<T>(object sender, T eventArgs) where T : EventArgs;
    public static class EventHelper
    {
        private static bool isEventFired = false;
        private static object sender;
        private static EventArgs actualEventArgs;
        private static string eventName;

        public static void ExpectEvent<T>(EventTriggerCallback eventTrigger,
            object sender, string eventName, EventValidationCallback<T> eventValidation) where T : EventArgs
        {
            EventInfo eventInfo = sender.GetType().GetEvent(eventName);
            if (eventInfo == null)
            {
                throw new TestValidationException("Could not find " + eventName + " event in object " + sender.GetType().Name);
            }

            EventHelper.eventName = eventName;
            Type eventDelegate = eventInfo.EventHandlerType;
            MethodInfo callbackMethodInfo = typeof(EventHelper).GetMethod("CallbackEventHandler",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Delegate eventHandler = Delegate.CreateDelegate(eventDelegate, callbackMethodInfo);
            RoutedEvent[] routedEvents = EventManager.GetRoutedEvents();
            RoutedEvent candidateRoutedEvent = null;

            foreach (RoutedEvent routedEvent in routedEvents)
            {
                if (routedEvent.Name.Equals(eventName) && (sender.GetType().IsSubclassOf(routedEvent.OwnerType) || sender.GetType() == routedEvent.OwnerType))
                {
                    candidateRoutedEvent = routedEvent;
                }
            }

            if (sender is UIElement && candidateRoutedEvent != null)
            {
                ((UIElement)sender).AddHandler(candidateRoutedEvent, eventHandler);
                eventTrigger();
                ((UIElement)sender).RemoveHandler(candidateRoutedEvent, eventHandler);
            }
            else
            {
                MethodInfo addHandler = eventInfo.GetAddMethod();
                MethodInfo removeHandler = eventInfo.GetRemoveMethod();
                Object[] handlerArgs = { eventHandler };
                addHandler.Invoke(sender, handlerArgs);
                eventTrigger();
                removeHandler.Invoke(sender, handlerArgs);
            }

            eventValidation(sender, (T)EventHelper.actualEventArgs);
            isEventFired = false;
        }

        public static void ExpectEvent<T>(EventTriggerCallback eventTrigger,
            object sender, string eventName, T expectedEventArgs, bool eventShouldFire) where T : EventArgs
        {
            ExpectEvent(eventTrigger, sender, eventName, delegate(object actualSender, T actualEventArgs)
            {
                if (eventShouldFire)
                {
                    if (!isEventFired)
                    {
                        throw new TestValidationException("The " + eventName + " event did not fire.");
                    }

                    if (actualEventArgs != null)
                    {
                        if (expectedEventArgs != null)
                        {
                            if (!EventHelper.sender.Equals(actualSender))
                            {
                                throw new TestValidationException("The actual sender " + actualSender.ToString() + " reference is not equal to the expected sender.");
                            }

                            if (expectedEventArgs.GetType() != actualEventArgs.GetType())
                            {
                                throw new TestValidationException("Type of actual EventArgs and expected EventArgs is not the same.");
                            }

                            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(actualEventArgs.GetType());

                            foreach (PropertyDescriptor property in properties)
                            {
                                if (property.GetValue(actualEventArgs) is IEnumerable)
                                {
                                    // Loop through both IEnumerables and compare value by value...
                                    IEnumerator actualEventArgsPropertyValues = ((IEnumerable)property.GetValue(actualEventArgs)).GetEnumerator();
                                    IEnumerator expectedEventArgsPropertyValues = ((IEnumerable)property.GetValue(expectedEventArgs)).GetEnumerator();
                                    bool firstEnumeratorMoveNext, secondEnumeratorMoveNext;
                                    while ((firstEnumeratorMoveNext = actualEventArgsPropertyValues.MoveNext()) & (secondEnumeratorMoveNext = expectedEventArgsPropertyValues.MoveNext()))
                                    {
                                        if (!actualEventArgsPropertyValues.Current.Equals(expectedEventArgsPropertyValues.Current))
                                        {
                                            throw new TestValidationException("Actual EventArgs IEumerable property value " + actualEventArgsPropertyValues.Current.ToString() + " is not the same as expected EventArgs IEumerable property value " + expectedEventArgsPropertyValues.Current.ToString() + ".");
                                        }
                                    }
                                    if (firstEnumeratorMoveNext != secondEnumeratorMoveNext)
                                    {
                                        throw new TestValidationException("Actual EventArgs IEnumerable property " + property.Name + " has different number of items than expected EventArgs.");
                                    }
                                }
                                else if (!property.GetValue(actualEventArgs).Equals(property.GetValue(expectedEventArgs)))
                                {
                                    throw new TestValidationException("Property Name: " + property.Name + ", Expected Value: " + property.GetValue(expectedEventArgs) + ", Actual Value: " + property.GetValue(actualEventArgs));
                                }
                            }
                        }
                        else
                        {
                            throw new TestValidationException("The expected EventArgs can not be null because the actual EventArgs is not null.");
                        }
                    }
                    else
                    {
                        if (expectedEventArgs != null)
                        {
                            throw new TestValidationException("The expected EventArgs should be null because the the actual EventArgs is null.");
                        }
                    }
                }
                else
                {
                    if (isEventFired)
                    {
                        throw new TestValidationException("The " + eventName + " event fired.");
                    }
                }
            }
                );
        }

        public static void ExpectEvent<T>(EventTriggerCallback eventTrigger,
            object sender, string eventName, T expectedEventArgs) where T : EventArgs
        {
            ExpectEvent<T>(eventTrigger, sender, eventName, expectedEventArgs, true);
        }

        private static void CallbackEventHandler(object sender, EventArgs eventArgs)
        {
            if (!isEventFired)
            {
                isEventFired = true;
            }
            else
            {
                throw new TestValidationException("The " + EventHelper.eventName + " event was raised more than one time.");
            }
            EventHelper.sender = sender;
            EventHelper.actualEventArgs = eventArgs;
        }
    }
}
