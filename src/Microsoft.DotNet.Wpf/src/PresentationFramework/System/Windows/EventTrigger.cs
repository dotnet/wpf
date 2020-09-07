// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* An event trigger is a set of actions that will be activated in response to
*  the specified event fired elsewhere.
*
*
\***************************************************************************/
using System.Collections;
using System.Diagnostics;
using System.Windows.Markup;
using System.Collections.Specialized;
using System.ComponentModel;

namespace System.Windows
{
    /// <summary>
    ///   A class that controls a set of actions to activate in response to an event
    /// </summary>
    [ContentProperty("Actions")]
    public class EventTrigger : TriggerBase, IAddChild
    {
        ///////////////////////////////////////////////////////////////////////
        // Public members

        /// <summary>
        ///     Build an empty instance of the EventTrigger object
        /// </summary>
        public EventTrigger()
        {
        }

        /// <summary>
        ///     Build an instance of EventTrigger associated with the given event
        /// </summary>
        public EventTrigger( RoutedEvent routedEvent )
        {
            RoutedEvent = routedEvent;
        }

        /// <summary>
        ///  Add an object child to this trigger's Actions
        /// </summary>
        void IAddChild.AddChild(object value)
        {
            AddChild(value);
        }

        /// <summary>
        ///  Add an object child to this trigger's Actions
        /// </summary>
        protected virtual void AddChild(object value)
        {
            TriggerAction action = value as TriggerAction;
            if (action == null)
            {
                throw new ArgumentException(SR.Get(SRID.EventTriggerBadAction, value.GetType().Name));
            }
            Actions.Add(action);
        }

        /// <summary>
        ///  Add a text string to this trigger's Actions.  Note that this
        ///  is not supported and will result in an exception.
        /// </summary>
        void IAddChild.AddText(string text)
        {
            AddText(text);
        }

        /// <summary>
        ///  Add a text string to this trigger's Actions.  Note that this
        ///  is not supported and will result in an exception.
        /// </summary>
        protected virtual void AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }
        
        /// <summary>
        ///     The Event that will activate this trigger - one must be specified
        /// before an event trigger is meaningful.
        /// </summary>
        public RoutedEvent RoutedEvent
        {
            get
            {
                return _routedEvent;
            }
            set
            {
                if ( value == null )
                {
                    throw new ArgumentNullException("value");
                }

                if( IsSealed )
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "EventTrigger"));
                }

                // When used as an element trigger, we don't actually need to seal
                //  to ensure cross-thread usability.  However, if we *are* fixed on
                //  listening to an event already, don't allow this change.
                if( _routedEventHandler != null )
                {
                    // Recycle the Seal error message.
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "EventTrigger"));
                }

                _routedEvent = value;
            }
        }

        /// <summary>
        ///     The x:Name of the object whose event shall trigger this 
        /// EventTrigger.   If null, then this is the object being Styled
        /// and not anything under its Style.VisualTree.
        /// </summary>
        [DefaultValue(null)]
        public string SourceName
        {
            get
            {
                return _sourceName;
            }
            set
            {
                if( IsSealed )
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "EventTrigger"));
                }

                _sourceName = value;
            }
        }

        /// <summary>
        ///     Internal method to get the childId corresponding to the public
        /// sourceId.
        /// </summary>
        internal int TriggerChildIndex
        {
            get
            {
                return _childId;
            }
            set
            {
                _childId = value;
            }
        }

        /// <summary>
        ///     The collection of actions to activate when the Event occurs.
        /// At least one action is required for the trigger to be meaningful.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]        
        public TriggerActionCollection Actions
        {
            get
            {
                if( _actions == null )
                {
                    _actions = new TriggerActionCollection();

                    // Give the collection a back-link, this is used for the inheritance context
                    _actions.Owner = this;
                }
                return _actions;
            }
        }


        /// <summary>
        ///     If we get a new inheritance context (or it goes to null)
        ///     we need to tell actions about it.
        /// </summary>
        internal override void OnInheritanceContextChangedCore(EventArgs args)
        {
            base.OnInheritanceContextChangedCore(args);

            if (_actions == null)
            {
                return;
            }

            for (int i=0; i<_actions.Count; i++)
            {
                DependencyObject action = _actions[i] as DependencyObject;
                if (action != null && action.InheritanceContext == this)
                {
                    action.OnInheritanceContextChanged(args);
                }
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeActions()
        {
            return ( _actions != null && _actions.Count > 0 );
        }

        ///////////////////////////////////////////////////////////////////////
        // Internal members
        
        internal sealed override void Seal()
        {
            if( PropertyValues.Count > 0 )
            {
                throw new InvalidOperationException(SR.Get(SRID.EventTriggerDoNotSetProperties));
            }

            // EnterActions/ExitActions aren't meaningful on event triggers.
            if( HasEnterActions || HasExitActions )
            {
                throw new InvalidOperationException(SR.Get(SRID.EventTriggerDoesNotEnterExit));
            }

            if (_routedEvent != null && _actions != null && _actions.Count > 0)
            {
                _actions.Seal(this);  // TriggerActions need a link back to me to fetch the childId corresponding the sourceId string.
            }

            base.Seal(); // Should be almost a no-op given lack of PropertyValues
        }

        ///////////////////////////////////////////////////////////////////////
        // Private members

        // Event that will fire this trigger
        private RoutedEvent _routedEvent = null;

        // Name of the Style.VisualTree node whose event to listen to.
        //  May remain the default value of null, which  means the object being 
        //  Styled is the target instead of something within the Style.VisualTree.
        private string _sourceName = null;

        // Style childId corresponding to the SourceName string
        private int _childId = 0;

        // Actions to invoke when this trigger is fired
        private TriggerActionCollection _actions = null;

        ///////////////////////////////////////////////////////////////////////
        // Storage attached to other objects to support triggers
        //  Some of these can be moved to base class as necessary.

        //  Exists on objects that have information in their [Class].Triggers collection property. (Currently root FrameworkElement only.)
        internal static readonly UncommonField<TriggerCollection> TriggerCollectionField = new UncommonField<TriggerCollection>(null);

        // This is the listener that we hook up to the SourceId element.
        RoutedEventHandler _routedEventHandler = null;

        // This is the SourceId-ed element.
        FrameworkElement _source;
        


        ///////////////////////////////////////////////////////////////////////
        // Internal static methods to process event trigger information stored
        //  in attached storage of other objects.
        //
        // Called when the FrameworkElement and the tree structure underneath it has been
        //  built up.  This is the earliest point we can resolve all the child 
        //  node identification that may exist in a Trigger object.
        // This should be moved to base class if PropertyTrigger support is added.
        internal static void ProcessTriggerCollection( FrameworkElement triggersHost )
        {
            TriggerCollection triggerCollection = TriggerCollectionField.GetValue(triggersHost);
            if( triggerCollection != null )
            {
                // Don't seal the collection, because we allow it to change.  We will,
                // however, seal each of the triggers.
                
                for( int i = 0; i < triggerCollection.Count; i++ )
                {
                    ProcessOneTrigger( triggersHost, triggerCollection[i] );
                }
            }
        }


        ////////////////////////////////////////////////////////////////////////
        // ProcessOneTrigger
        //
        // Find the target element for this trigger, and set a listener for 
        // the event into (pointing back to the trigger).
        
        internal static void ProcessOneTrigger( FrameworkElement triggersHost, TriggerBase triggerBase )
        {
            // This code path is used in the element trigger case.  We don't actually
            //  need these guys to be usable cross-thread, so we don't really need
            //  to freeze/seal these objects.  The only one expected to cause problems
            //  is a change to the RoutedEvent.  At the same time we remove this
            //  Seal(), the RoutedEvent setter will check to see if the handler has
            //  already been created and refuse an update if so.
            // triggerBase.Seal();
            
            EventTrigger eventTrigger = triggerBase as EventTrigger;
            if( eventTrigger != null )
            {
                Debug.Assert( eventTrigger._routedEventHandler == null && eventTrigger._source == null);
                
                // PERF: Cache this result if it turns out we're doing a lot of lookups on the same name.
                eventTrigger._source = FrameworkElement.FindNamedFrameworkElement( triggersHost, eventTrigger.SourceName );

                // Create a statefull event delegate (which keeps a ref to the FE).
                EventTriggerSourceListener listener = new EventTriggerSourceListener( eventTrigger, triggersHost );


                // Store the RoutedEventHandler & target for use in DisconnectOneTrigger
                eventTrigger._routedEventHandler = new RoutedEventHandler(listener.Handler);
                eventTrigger._source.AddHandler( eventTrigger.RoutedEvent, eventTrigger._routedEventHandler,
                                                 false /* HandledEventsToo */ );
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.TriggersSupportsEventTriggersOnly));
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        // DisconnectAllTriggers
        //
        // Call DisconnectOneTrigger for each trigger in the Triggers collection.

        internal static void DisconnectAllTriggers( FrameworkElement triggersHost )
        {
            TriggerCollection triggerCollection = TriggerCollectionField.GetValue(triggersHost);

            if( triggerCollection != null )
            {
                for( int i = 0; i < triggerCollection.Count; i++ )
                {
                    DisconnectOneTrigger( triggersHost, triggerCollection[i] );
                }

            }
        }
        
        ////////////////////////////////////////////////////////////////////////
        //
        // DisconnectOneTrigger
        //
        // In ProcessOneTrigger, we connect an event trigger to the element
        // which it targets.  Here, we remove the event listener to clean up.

        internal static void DisconnectOneTrigger( FrameworkElement triggersHost, TriggerBase triggerBase )
        {
            EventTrigger eventTrigger = triggerBase as EventTrigger;
            
            if( eventTrigger != null )
            {
                eventTrigger._source.RemoveHandler( eventTrigger.RoutedEvent, eventTrigger._routedEventHandler);
                eventTrigger._routedEventHandler = null;
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.TriggersSupportsEventTriggersOnly));
            }
        }



        internal class EventTriggerSourceListener
        {
            internal EventTriggerSourceListener( EventTrigger trigger, FrameworkElement host )
            {
                _owningTrigger = trigger;
                _owningTriggerHost = host;
            }

            internal void Handler(object sender, RoutedEventArgs e)
            {
                // Invoke all actions of the associated EventTrigger object.
                TriggerActionCollection actions = _owningTrigger.Actions;
                for( int j = 0; j < actions.Count; j++ )
                {
                    actions[j].Invoke(_owningTriggerHost);
                }
            }

            private EventTrigger     _owningTrigger;
            private FrameworkElement _owningTriggerHost;
        }
        
    }
}
