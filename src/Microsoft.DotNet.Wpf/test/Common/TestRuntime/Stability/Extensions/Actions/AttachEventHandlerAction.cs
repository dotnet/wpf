// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public abstract class AttachEventHandlerAction : SimpleDiscoverableAction
    {
        protected void AddOrRemoveHandlers(DependencyObject target, bool add)
        {
            //
            // Attach to CLR events.
            //
            EventInfo[] eventInfos = target.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public);

            for (int i = 0; i < eventInfos.Length; i++)
            {
                EventInfo eventInfo = eventInfos[i];
                Type handlerType = eventInfo.EventHandlerType;

                if (eventHandlers.ContainsKey(handlerType))
                {
                    Delegate eventHandler = eventHandlers[handlerType];

                    if (eventHandler != null)
                    {
                        if (add)
                        {
                            eventInfo.AddEventHandler(target, eventHandler);
                        }
                        else
                        {
                            eventInfo.RemoveEventHandler(target, eventHandler);
                        }
                    }
                }
            }
            //
            // Attach using Mouse, Keyboard, and CommandManager static methods.
            //
            Type objType = target.GetType();
            Type inputElementType = typeof(IInputElement);
            foreach (MethodInfo methodInfo in staticMethods)
            {
                if (add != methodInfo.Name.StartsWith("Add", StringComparison.InvariantCulture))
                    continue;

                ParameterInfo[] parameterInfo = methodInfo.GetParameters();
                Type elementType = parameterInfo[0].ParameterType;
                Type handlerType = parameterInfo[1].ParameterType;

                if (inputElementType.IsAssignableFrom(objType) &&
                    elementType.IsAssignableFrom(objType) &&
                    eventHandlers.ContainsKey(handlerType))
                {
                    Delegate eventHandler = eventHandlers[handlerType];
                    methodInfo.Invoke(target, new object[] {target, eventHandler });
                }
            }
        }

        static AttachEventHandlerAction()
        {
            //
            // Initialize event handlers.
            //
            eventHandlers[typeof(EventHandler)] = new EventHandler(OnGenericEvent);
            eventHandlers[typeof(RoutedEventHandler)] = new RoutedEventHandler(OnRoutedEvent);
            eventHandlers[typeof(KeyEventHandler)] = new KeyEventHandler(OnKeyEvent);
            eventHandlers[typeof(KeyboardFocusChangedEventHandler)] = new KeyboardFocusChangedEventHandler(OnFocusEvent);
            eventHandlers[typeof(TextCompositionEventHandler)] = new TextCompositionEventHandler(OnTextCompositionEvent);
            eventHandlers[typeof(MouseEventHandler)] = new MouseEventHandler(OnMouseEvent);
            eventHandlers[typeof(MouseButtonEventHandler)] = new MouseButtonEventHandler(OnMouseButtonEvent);
            eventHandlers[typeof(MouseButtonEventHandler)] = new MouseButtonEventHandler(OnMouseDoubleClickEvent);
            eventHandlers[typeof(MouseWheelEventHandler)] = new MouseWheelEventHandler(OnMouseWheelEvent);
            eventHandlers[typeof(DragEventHandler)] = new DragEventHandler(OnDragEvent);
            eventHandlers[typeof(GiveFeedbackEventHandler)] = new GiveFeedbackEventHandler(OnFeedbackEvent);
            eventHandlers[typeof(QueryCursorEventHandler)] = new QueryCursorEventHandler(OnQueryCursorEvent);
            eventHandlers[typeof(ExecutedRoutedEventHandler)] = new ExecutedRoutedEventHandler(OnExecutedEvent);
            eventHandlers[typeof(CanExecuteRoutedEventHandler)] = new CanExecuteRoutedEventHandler(OnCanExecuteEvent);
            eventHandlers[typeof(DependencyPropertyChangedEventHandler)] = new DependencyPropertyChangedEventHandler(OnPropertyChangedEvent);

            //
            // Initialize routed event static method list.
            //
            staticMethods = new List<MethodInfo>();

            Type[] types = new Type[] { typeof(Mouse), typeof(Keyboard), typeof(CommandManager) };

            foreach (Type type in types)
            {
                MethodInfo[] methodInfos = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
                foreach (MethodInfo methodInfo in methodInfos)
                {
                    if (methodInfo.Name.EndsWith("Handler", StringComparison.InvariantCulture) &&
                        (methodInfo.Name.StartsWith("Add", StringComparison.InvariantCulture) ||
                         methodInfo.Name.StartsWith("Remove", StringComparison.InvariantCulture)))
                    {
                        staticMethods.Add(methodInfo);
                    }
                }
            }
        }

        #region Event Handlers

        private static void OnPropertyChangedEvent(object sender, DependencyPropertyChangedEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnKeyEvent(object sender, KeyEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnFocusEvent(object sender, KeyboardFocusChangedEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnTextCompositionEvent(object sender, TextCompositionEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnMouseEvent(object sender, MouseEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnMouseButtonEvent(object sender, MouseButtonEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnMouseDoubleClickEvent(object sender, MouseButtonEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnMouseWheelEvent(object sender, MouseWheelEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnQueryCursorEvent(object sender, QueryCursorEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnExecutedEvent(object sender, ExecutedRoutedEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnCanExecuteEvent(object sender, CanExecuteRoutedEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnDragEvent(object sender, DragEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnFeedbackEvent(object sender, GiveFeedbackEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnRoutedEvent(object sender, RoutedEventArgs args)
        {
            OnEvent(sender, args);
        }
        private static void OnGenericEvent(object sender, EventArgs args)
        {
            OnEvent(sender, args);
        }
        /// <summary>
        /// Common handler for events.
        /// </summary>
        private static void OnEvent(object sender, DependencyPropertyChangedEventArgs args)
        {
            ReadProperties(sender);
            ReadProperties(args);
        }
        /// <summary>
        /// Common handler for events.
        /// </summary>
        private static void OnEvent(object sender, EventArgs args)
        {
            ReadProperties(sender);
            ReadProperties(args);
        }

        #endregion

        #region Private Data

        private static IDictionary<Type, Delegate> eventHandlers = new Dictionary<Type, Delegate>();

        private static List<MethodInfo> staticMethods = null;

        /// <summary>
        /// This reads properties of an object.
        /// </summary>
        public static void ReadProperties(object sender)
        {
            // Just return if sender is null.
            // sender is null in some cases.  That's okay.
            if (sender == null)
            {
                return;
            }

            object obj = null;
            //
            // Read all properties via TypeDescriptor.
            //
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(sender, true);
            foreach (PropertyDescriptor property in properties)
            {
                obj = property.GetValue(sender);
            }

            //
            // Read CLR properties via reflection.
            //
            PropertyInfo[] propInfos = sender.GetType().GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            for (int i = 0; i < propInfos.Length; i++)
            {
                PropertyInfo propInfo = propInfos[i];

                if (propInfo.CanRead)
                {
                    obj = propInfo.GetValue(sender, null);
                }
            }
        }

        #endregion
    }

    
}
