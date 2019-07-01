// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Collections;
using Microsoft.Test.Stability.Extensions.Factories;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40    
    [TargetTypeAttribute(typeof(RoutedEvent))]
    public abstract class RoutedEventActions : SimpleDiscoverableAction
    {
        public RoutedEvent CreateRoutedEvent(String eventName)
        {            
            RoutedEvent[] IDs = EventManager.GetRoutedEventsForOwner(typeof(CustomControl));
            if (IDs != null)
            {
                foreach (RoutedEvent thisEvent in IDs)
                {
                    if (thisEvent.Name == eventName)
                    {
                        return thisEvent;
                    }
                }
            }
            RoutedEvent routedEvent;
            routedEvent = EventManager.RegisterRoutedEvent(eventName, RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CustomControl));
            return routedEvent;
        }

        public void CustomRoutedEventHandler(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
    
    [TargetTypeAttribute(typeof(RoutedEvent))]
    public class RaiseRoutedEventAction : RoutedEventActions
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public StackPanel StackPanel1 { get; set; }                

        public override void Perform()
        {
            RoutedEvent routedEvent = CreateRoutedEvent("routedEvent");
            CustomControl customControl = new CustomControl();
            RoutedEventArgs args;
            args = new RoutedEventArgs(routedEvent);
            RoutedEventHandler handler = new RoutedEventHandler(CustomRoutedEventHandler);            
            StackPanel1.Children.Add(customControl);
            customControl.AddHandler(routedEvent,handler);
            customControl.RaiseEvent(args);
            customControl.RemoveHandler(routedEvent, handler);
        }        
    }

    [TargetTypeAttribute(typeof(RoutedEvent))]
    public class NestedRoutedEventAction : RoutedEventActions
    {
        CustomControl customControl1 = new CustomControl();
        CustomControl customControl2 = new CustomControl();
        CustomControl customControl3 = new CustomControl();
        CustomControl customControl4 = new CustomControl();
        
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public StackPanel StackPanel1 { get; set; }

        public override void Perform()
        {            
            //nest them
            customControl3.AppendChild(customControl4);
            customControl2.AppendChild(customControl3);
            customControl1.AppendChild(customControl2);
            StackPanel1.Children.Add(customControl1);

            RoutedEvent routedEvent = CreateRoutedEvent("routedEvent");
            RoutedEventArgs args;
            args = new RoutedEventArgs(routedEvent);
            RoutedEventHandler handler = new RoutedEventHandler(CustomRoutedEventHandler);
            customControl4.AddHandler(routedEvent, handler);
            customControl4.RaiseEvent(args);
            customControl4.RemoveHandler(routedEvent, handler);
        }        
    }

    /// <summary>
    ///     CustomControl class is a subclass of FrameworkElement
    /// </summary>
    public class CustomControl : FrameworkElement
    {
        #region Construction

        /// <summary>
        ///     Constructor for  CustomControl
        /// </summary>
        public CustomControl()
            : base()
        {
            _children = new VisualCollection(this);
        }

        #endregion Construction

        #region External API

        /// <summary>
        ///     Appends model child
        /// </summary>
        public void AppendModelChild(object modelChild)
        {
            AddLogicalChild(modelChild);
        }

        /// <summary>
        /// Appends a child.
        /// </summary>
        public void AppendChild(Visual child)
        {
            _children.Add(child);
        }
        /// <summary>
        /// Remove a child.
        /// </summary>
        public void RemoveChild(Visual child)
        {
            _children.Remove(child);
        }

        /// <summary>
        /// Returns the child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            // if you have a template
            if (base.VisualChildrenCount != 0 && index == 0)
            {
                return base.GetVisualChild(0);
            }
            // otherwise you can have your own children
            if (_children == null)
            {
                throw new ArgumentOutOfRangeException("index is out of range");
            }
            if (index < 0 || index >= _children.Count)
            {
                throw new ArgumentOutOfRangeException("index is out of range");
            }

            return _children[index];
        }

        /// <summary>
        /// Returns the Visual children count.
        /// </summary>        
        protected override int VisualChildrenCount
        {
            get
            {
                //you can either have a Template or your own children
                if (base.VisualChildrenCount > 0) return 1;
                else return _children.Count;
            }
        }
        #endregion External API

        private VisualCollection _children;
    }    
#endif
}
