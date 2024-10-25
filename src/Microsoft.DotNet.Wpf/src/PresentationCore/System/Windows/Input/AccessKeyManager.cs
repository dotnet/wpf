// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Media;
using System.Windows.Interop;
using MS.Internal;
using System.Diagnostics;
using System.Windows;
using System.Security;

using SR=MS.Internal.PresentationCore.SR;

namespace System.Windows.Input
{
    /// <summary>
    ///   AccessKeyManager object is created on demand and it is one per thread.
    /// It attached an event handler for PostProcessInput on InputManager and expose registration and 
    /// unregistration of access keys. When the access key is pressed in calls OnAccessKey method on the target element
    /// </summary>
    public sealed class AccessKeyManager
    {
        #region Public API
        /// <summary>
        ///   Register the access key binding to the element that is the accesskey.
        /// </summary>
        /// <param name="key">When the key is pressed the element OnAccessKey method is called</param>
        /// <param name="element">The registration element</param>
        public static void Register(string key, IInputElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            key = NormalizeKey(key);

            AccessKeyManager instance = AccessKeyManager.Current;

            if (!instance._keyToElements.TryGetValue(key, out List<WeakReference<IInputElement>> elements))
            {
                elements = new List<WeakReference<IInputElement>>(1);
                instance._keyToElements[key] = elements;
            }
            else
            {
                // There were some elements there, remove dead ones
                PurgeDead(elements, null);
            }

            elements.Add(new WeakReference<IInputElement>(element));
        }

        /// <summary>
        /// Unregister one key bound to a particular element
        /// </summary>
        /// <param name="key"></param>
        /// <param name="element"></param>
        public static void Unregister(string key, IInputElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            key = NormalizeKey(key);

            AccessKeyManager instance = AccessKeyManager.Current;

            // Get all elements bound to this key and remove this element
            if (instance._keyToElements.TryGetValue(key, out List<WeakReference<IInputElement>> elements))
            {
                PurgeDead(elements, element);
                if (elements.Count == 0)
                {
                    instance._keyToElements.Remove(key);
                }
            }
        }

        /// <summary>
        /// Tells if there is a particular key registered at the global scope in this Context.
        /// </summary>
        /// <param name="scope">Scope to query (for example the PresentationSource of the visual)</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsKeyRegistered(object scope, string key)
        {
            key = NormalizeKey(key);

            List<IInputElement> targets = GetTargetsForScope(scope, key, null, AccessKeyInformation.Empty);
            return targets != null && targets.Count > 0;
        }

        /// <summary>
        /// Process the given key as if the key were pressed at the global scope in this context.
        /// </summary>
        /// <param name="scope">scope in which to invoke the access key</param>
        /// <param name="key">character being pressed</param>
        /// <param name="isMultiple">True if this key has multiple matches</param>
        /// <returns>false if there are no more keys that match, true otherwise</returns>
        /// <summary>
        ///     Executes the command on the given command source.
        /// </summary>
        public static bool ProcessKey(object scope, string key, bool isMultiple)
        {
            key = NormalizeKey(key);

            return ProcessKeyForScope(scope, key, isMultiple, false) == ProcessKeyResult.MoreMatches;
        }

        /// <summary>
        /// Returns StringInfo.GetNextTextElement(key).ToUpperInvariant() throwing exceptions for null
        /// and multi-char strings.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string NormalizeKey(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            string firstCharacter = StringInfo.GetNextTextElement(key);

            if (key != firstCharacter)
            {
                throw new ArgumentException(SR.Format(SR.AccessKeyManager_NotAUnicodeCharacter, nameof(key)));
            }

            return firstCharacter.ToUpperInvariant();
        }

        /// <summary>
        /// This event is used by elements that want to define a scope for accesskeys, such as Menu and Popup.
        /// This event will never be raised, it is used to identify classes that define new scopes.
        /// </summary>
        public static readonly RoutedEvent AccessKeyPressedEvent = EventManager.RegisterRoutedEvent(
            "AccessKeyPressed", RoutingStrategy.Bubble, typeof(AccessKeyPressedEventHandler), typeof(AccessKeyManager));

        /// <summary>
        ///     Adds a handler for the AccessKeyPressed attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddAccessKeyPressedHandler(DependencyObject element, AccessKeyPressedEventHandler handler)
        {
            UIElement.AddHandler(element, AccessKeyPressedEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the AccessKeyPressed attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveAccessKeyPressedHandler(DependencyObject element, AccessKeyPressedEventHandler handler)
        {
            UIElement.RemoveHandler(element, AccessKeyPressedEvent, handler);
        }

        #endregion
        
        #region Constructor
        private AccessKeyManager()
        {
            InputManager.Current.PostProcessInput += new ProcessInputEventHandler(PostProcessInput);
        }

        #endregion

        #region Properties
        
        /// <summary>
        /// Access to the current context's AccessKeyManager class
        /// </summary>
        private static AccessKeyManager Current
        {
            get 
            {
                s_accessKeyManager ??= new AccessKeyManager();

                return s_accessKeyManager;
            }
        }

        #endregion

        #region PostProcessInput Event Handlers

        private enum ProcessKeyResult
        {
            NoMatch,
            MoreMatches,
            LastMatch
        }

        private void PostProcessInput(object sender, ProcessInputEventArgs e)
        {
            if (e.StagingItem.Input.Handled)
                return;

            if (e.StagingItem.Input.RoutedEvent == Keyboard.KeyDownEvent)
            {
                OnKeyDown((KeyEventArgs)e.StagingItem.Input);
            }
            else if (e.StagingItem.Input.RoutedEvent == TextCompositionManager.TextInputEvent)
            {
                OnText((TextCompositionEventArgs)e.StagingItem.Input);
            }
}

        // Assumes key is already a single unicode character
        private static ProcessKeyResult ProcessKeyForSender(object sender, string key, bool existsElsewhere, bool userInitiated)
        {
            // This comes from OnKeyDown or OnText and though it is a single character it might not be uppercased.
            key = key.ToUpperInvariant();

            IInputElement inputElementSender = sender as IInputElement;
            List<IInputElement> targets = GetTargetsForSender(inputElementSender, key);

            return ProcessKey(targets, key, existsElsewhere, userInitiated);
        }

        // Assumes key is already a single unicode character AND is uppercased
        private static ProcessKeyResult ProcessKeyForScope(object scope, string key, bool existsElsewhere, bool userInitiated)
        {
            List<IInputElement> targets = GetTargetsForScope(scope, key, null, AccessKeyInformation.Empty);

            return ProcessKey(targets, key, existsElsewhere, userInitiated);
        }

        private static ProcessKeyResult ProcessKey(List<IInputElement> targets, string key, bool existsElsewhere, bool userInitiated)
        {
            if (targets != null)
            {
                bool oneUIElement = true;
                UIElement invokeUIElement = null;
                bool lastWasAccessed = false;

                int chosenIndex = 0;
                for (int i = 0; i < targets.Count; i++)
                {
                    UIElement target = targets[i] as UIElement;
                    Debug.Assert(target != null, "Targets should only be UIElements");
                    if (!target.IsEnabled)
                        continue;

                    if (invokeUIElement == null)
                    {
                        invokeUIElement = target;
                        chosenIndex = i;
                    }
                    else
                    {
                        if (lastWasAccessed)
                        {
                            invokeUIElement = target;
                            chosenIndex = i;
                        }

                        oneUIElement = false;
                    }

                    // 
                    lastWasAccessed = target.HasEffectiveKeyboardFocus;
                }

                if (invokeUIElement != null)
                {
                    AccessKeyEventArgs args = new AccessKeyEventArgs(key, !oneUIElement || existsElsewhere /* == isMultiple */,userInitiated);
                    try
                    {
                        invokeUIElement.InvokeAccessKey(args);
                    }
                    finally
                    {
                        args.ClearUserInitiated();
                    }

                    return (chosenIndex == targets.Count - 1) ? ProcessKeyResult.LastMatch : ProcessKeyResult.MoreMatches;
                }
            }

            return ProcessKeyResult.NoMatch;
        }

        private static void OnText(TextCompositionEventArgs e)
        {
            // AccessKeyManager handles both text and system text.
            string text = e.Text;
            if (string.IsNullOrEmpty(text))
            {
                text = e.SystemText;
            }

            if (!string.IsNullOrEmpty(text))
            {
                if (ProcessKeyForSender(e.OriginalSource, text, existsElsewhere: false, e.UserInitiated) != ProcessKeyResult.NoMatch)
                {
                    e.Handled = true;
                }
            }
        }

        private static void OnKeyDown(KeyEventArgs e)
        {
            string text = e.RealKey switch
            {
                Key.Enter => "\x000D",
                Key.Escape => "\x001B",
                _ => null
            };

            if (text is not null)
            {
                if (ProcessKeyForSender(e.OriginalSource, text, existsElsewhere: false, e.UserInitiated) != ProcessKeyResult.NoMatch)
                {
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Get the list of access key targets for the sender of the keyboard event.  If sender is null, 
        /// pretend key was pressed in the active window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static List<IInputElement> GetTargetsForSender(IInputElement sender, string key)
        {
            // Find the scope for the sender -- will be matched against the possible targets' scopes
            AccessKeyInformation senderInfo = GetInfoForElement(sender, key);

            return GetTargetsForScope(senderInfo.Scope, key, sender, senderInfo);
        }
        
        private static List<IInputElement> GetTargetsForScope(object scope, string key, IInputElement sender, AccessKeyInformation senderInfo)
        {
            // null scope defaults to the active window
            if (scope == null)
            {
                scope = GetActiveSource();

                // if there is no active scope then give up
                if (scope == null)
                {
                    return null;
                }
            }

            if (CoreCompatibilityPreferences.GetIsAltKeyRequiredInAccessKeyDefaultScope() && 
                (scope is PresentationSource) && (Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt)
            {
                // If AltKey is required and it isnt pressed then dont match against any targets
                return null;
            }

            //Scoping:
            //    1) When key is pressed, find matching AKs -> S
            //    3) find scope for keyevent.Source
            //    4) find scope for everything in S. throw away those that don't match.
            //    5) Final selection uses S.  yay!
            //

            AccessKeyManager instance = AccessKeyManager.Current;

            if (!instance._keyToElements.TryGetValue(key, out List<WeakReference<IInputElement>> elements))
                return null;

            List<IInputElement> possibleElements = CopyAndPurgeDead(elements);
            if (possibleElements is null)
                return null;

            List<IInputElement> finalTargets = new List<IInputElement>(1);

            // Go through all the possible elements, find the interesting candidates
            for (int i = 0; i < possibleElements.Count;  i++)
            {
                IInputElement element = possibleElements[i];
                if (element != sender)
                {
                    if (IsTargetable(element))
                    {
                        AccessKeyInformation elementInfo = GetInfoForElement(element, key);

                        if (elementInfo.Target is null)
                            continue;

                        if (scope == elementInfo.Scope)
                        {
                            finalTargets.Add(elementInfo.Target);
                        }
                    }
                }
                else
                {
                    // This is the same element that sent the event so it must be in the same scope.  
                    // Just add it to the final targets
                    if (senderInfo.Target is not null)
                    {
                        finalTargets.Add(senderInfo.Target);
                    }
                }
            }

            return finalTargets;
        }
        
        /// <summary>
        /// Returns scope for the given element.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="key"></param>
        /// <returns>Scope for the given element, null means the context global scope</returns>
        private static AccessKeyInformation GetInfoForElement(IInputElement element, string key)
        {
            if (element is null)
                return new AccessKeyInformation(GetActiveSource(), null);
            
            AccessKeyPressedEventArgs args = new(key);
            element.RaiseEvent(args);

            if (args.Scope is not null)
                return new AccessKeyInformation(args.Scope, args.Target);

            return new AccessKeyInformation(GetSourceForElement(element), args.Target);  
        }

        private static PresentationSource GetSourceForElement(IInputElement element)
        {
            PresentationSource source = null;
            DependencyObject elementDO = element as DependencyObject;

            // Use internal helpers to try to find the source of the element.
            // Because IInputElements can move around without notification we need to
            // look up the source every time.
            if (elementDO != null)
            {
                DependencyObject containingVisual = InputElement.GetContainingVisual(elementDO);

                if (containingVisual != null)
                {
                    source = PresentationSource.CriticalFromVisual(containingVisual);
                }
            }
            
            // NOTE: source can be null but IsTargetable(element) == true if the
            // element is in an orphaned tree but the tree has not yet been garbage collected.  
            return source;
        }

        private static PresentationSource GetActiveSource()
        {
            IntPtr hwnd = MS.Win32.UnsafeNativeMethods.GetActiveWindow();
            if (hwnd != IntPtr.Zero)
                return HwndSource.FromHwnd(hwnd);

            return null;
        }
        
        private static bool IsTargetable(IInputElement element)
        {
            DependencyObject uielement = InputElement.GetContainingUIElement((DependencyObject)element);

            // For an element to be a valid target it must be visible and enabled
            if (uielement != null 
                && IsVisible(uielement)
                && IsEnabled(uielement))
            {
                return true;
            }

            return false;
        }

        private static bool IsVisible(DependencyObject element)
        {
            while (element != null)
            {
                Visibility visibility;
                UIElement3D uiElem3D = element as UIElement3D;

                if (element is UIElement uiElem)
                {
                    visibility = uiElem.Visibility;
                }
                else
                {
                    visibility = uiElem3D.Visibility;                    
                }
                
                if (visibility != Visibility.Visible)
                {
                    return false;
                }
                
                element = UIElementHelper.GetUIParent(element);
            }
            
            return true;
        }

        // returns whether the given DO is enabled or not
        private static bool IsEnabled(DependencyObject element)
        {
            return (bool)element.GetValue(UIElement.IsEnabledProperty);                               
        }

        private readonly struct AccessKeyInformation
        {
            public readonly object Scope { get; }
            public readonly UIElement Target { get; }

            /// <summary>
            /// Represents an empty value where <see cref="Scope"/> and <see cref="Target"/> are <see langword="null"/>.
            /// </summary>
            public static AccessKeyInformation Empty => s_empty;

            public AccessKeyInformation(object scope, UIElement target)
            {
                Scope = scope;
                Target = target;
            }

            /// <summary>
            /// Holds the singleton for <see cref="AccessKeyInformation.Empty"/>.
            /// </summary>
            private static readonly AccessKeyInformation s_empty = new();

        }

        private static void PurgeDead(List<WeakReference<IInputElement>> elements, object elementToRemove)
        {
            for (int i = 0; i < elements.Count; )
            {
                WeakReference<IInputElement> weakReference = elements[i];
                if (!weakReference.TryGetTarget(out IInputElement element) || element == elementToRemove)
                {
                    elements.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        /// <summary>
        ///     Takes a List of WeakReferences, removes the dead references and returns
        ///     a generic List of IInputElements (strong references)
        /// </summary>
        private static List<IInputElement> CopyAndPurgeDead(List<WeakReference<IInputElement>> elements)
        {
            List<IInputElement> copy = new List<IInputElement>(elements.Count);

            for (int i = 0; i < elements.Count; )
            {
                WeakReference<IInputElement> weakReference = elements[i];

                if (!weakReference.TryGetTarget(out IInputElement element))
                {
                    elements.RemoveAt(i);
                }
                else
                {
                    copy.Add(element);
                    i++;
                }
            }

            return copy;
        }

        #endregion

        #region Private Properties
        
        /////////////////////////////////////////////////////////////////////////////////
        // Overview: Algorithm to look up access key from the element for which it is a target.
        //           
        //     When the AccessKeyCharacter for an element is requested we see if there
        //     is a corresponding AccessKeyElement stashed on the element.  If there is,
        //     raise the AccessKeyPressed event on it to see if that element is  still the 
        //     target for it.  If not, go through all registered accesskeys and get their 
        //     targets until we find the desired element.  The "primary" access key character
        //     is the first one we find.
        //     
        //     Note: The algorithm ends up being O(n) for each request for AccessKeyCharacter 
        //     because there is no mapping from AccessKeyElement to its "primary" character.
        //     Maintaining this would require storing a hash from AccessKeyElement to character
        //     or requiring that each element registered implement an interface or some other
        //     kind of contract.  Because we don't keep track of this or enforce this, to find 
        //     the "primary" character we must go through all registered pairs of 
        //     (character, element) to find the character -- O(n).
        //
        //     This ends up being just fine, because in any given context there shouldn't be
        //     so many elements registered that this cost is at all noticable.
        //
        /////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        ///     The primary access key element for an element.  This is stored as a WeakReference.
        /// </summary>
        private static readonly DependencyProperty AccessKeyElementProperty =
            DependencyProperty.RegisterAttached("AccessKeyElement", typeof(WeakReference<IInputElement>), typeof(AccessKeyManager));

        #endregion

        #region Private Methods for UIAutomation

        internal static string InternalGetAccessKeyCharacter(DependencyObject d)
        {
            return GetAccessKeyCharacter(d);
        }

        private static string GetAccessKeyCharacter(DependencyObject d)
        {
            AccessKeyManager instance = AccessKeyManager.Current;

            // See what the local value for AccessKeyElement is first and start with that.
            WeakReference<IInputElement> cachedElementWeakRef = (WeakReference<IInputElement>)d.GetValue(AccessKeyElementProperty);
            if (cachedElementWeakRef.TryGetTarget(out IInputElement accessKeyElement))
            {
                // First figure out if the target of accessKeyElement is still "d", then go find
                // the "primary" character for the accessKeyElement.  

                AccessKeyPressedEventArgs accessKeyPressedEventArgs = new AccessKeyPressedEventArgs();
                accessKeyElement.RaiseEvent(accessKeyPressedEventArgs);
                if (accessKeyPressedEventArgs.Target == d)
                {
                    // Because there is no way to get at the access key element's character from the
                    // element (there is no interface or anything) we have to go through all registered 
                    // access keys and see if this access key element is still registered and what its
                    // "primary" character is.
                
                    foreach (KeyValuePair<string, List<WeakReference<IInputElement>>> entry in instance._keyToElements)
                    {
                        List<WeakReference<IInputElement>> elements = entry.Value;
                        for (int i = 0; i < elements.Count; i++)
                        {
                            // If this element matches accessKeyElement, then return the current character
                            WeakReference<IInputElement> currentElementWeakRef = elements[i];

                            if (currentElementWeakRef.TryGetTarget(out IInputElement element) && element == accessKeyElement)
                            {
                                return entry.Key;
                            }
                        }
                    }
                }
            }


            // There was no access key stored or it no longer matched.  Clear out the cache and figure it out again.
            d.ClearValue(AccessKeyElementProperty);

            foreach (KeyValuePair<string, List<WeakReference<IInputElement>>> entry in instance._keyToElements)
            {
                List<WeakReference<IInputElement>> elements = entry.Value;
                for (int i = 0; i < elements.Count; i++)
                {
                    // Determine the target for this element.  Cache the weak reference for the element on the target.
                    WeakReference<IInputElement> currentElementWeakRef = elements[i];
                    if (currentElementWeakRef.TryGetTarget(out IInputElement currentElement))
                    {
                        AccessKeyPressedEventArgs accessKeyPressedEventArgs = new AccessKeyPressedEventArgs();
                        currentElement.RaiseEvent(accessKeyPressedEventArgs);

                        // If the target was non-null, cache the access key element on the target.
                        // if the target matches "d", return the current character.
                        if (accessKeyPressedEventArgs.Target != null)
                        {
                            accessKeyPressedEventArgs.Target.SetValue(AccessKeyElementProperty, currentElementWeakRef);

                            if (accessKeyPressedEventArgs.Target == d)
                            {
                                return entry.Key;
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }

        #endregion

        #region Data
        // Map: string -> List<WeakReference> to IInputElements
        private readonly Dictionary<string, List<WeakReference<IInputElement>>> _keyToElements = new(10);

        /// <summary>
        /// Holds a thread-specific instance of <see cref="AccessKeyManager"/>.
        /// </summary>
        [ThreadStatic]
        private static AccessKeyManager s_accessKeyManager;

        #endregion
    }

    /// <summary>
    /// The delegate type for handling a FindScope event
    /// </summary>
    public delegate void AccessKeyPressedEventHandler(object sender, AccessKeyPressedEventArgs e);

    /// <summary>
    /// The inputs to an AccessKeyPressedEventHandler
    /// </summary>
    public class AccessKeyPressedEventArgs : RoutedEventArgs
    {
        #region Constructors

        /// <summary>
        /// The constructor for AccessKeyPressed event args
        /// </summary>
        public AccessKeyPressedEventArgs()
        {
            RoutedEvent = AccessKeyManager.AccessKeyPressedEvent;
            _key = null;
        }

        /// <summary>
        /// Constructor for AccessKeyPressed event args
        /// </summary>
        /// <param name="key"></param>
        public AccessKeyPressedEventArgs(string key) : this()
        {
            _key = key;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The scope for the element that raised this event.
        /// </summary>
        public object Scope
        {
            get { return _scope; }
            set { _scope = value; }
        }

        /// <summary>
        /// Target element for the element that raised this event.
        /// </summary>
        /// <value></value>
        public UIElement Target
        {
            get { return _target; }
            set { _target = value; }
        }

        /// <summary>
        /// Key that was pressed
        /// </summary>
        /// <value></value>
        public string Key
        {
            get { return _key; }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// </summary>
        /// <param name="genericHandler">The handler to invoke.</param>
        /// <param name="genericTarget">The current object along the event's route.</param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            AccessKeyPressedEventHandler handler = (AccessKeyPressedEventHandler)genericHandler;

            handler(genericTarget, this);
        }

        #endregion

        #region Data

        private object _scope;
        private UIElement _target;
        private string _key;

        #endregion
    }

    /// <summary>
    /// Information pertaining to when the access key associated with an element is pressed
    /// </summary>
    public class AccessKeyEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        internal AccessKeyEventArgs(string key, bool isMultiple, bool userInitiated)
        {
            _key = key;
            _isMultiple = isMultiple;
            _userInitiated = userInitiated;
        }

        internal void ClearUserInitiated()
        {
            _userInitiated = false;
        }
        /// <summary>
        /// The key that was pressed which invoked this access key
        /// </summary>
        /// <value></value>
        public string Key
        {
            get { return _key; }
        }

        /// <summary>
        /// Were there other elements which are also invoked by this key
        /// </summary>
        /// <value></value>
        public bool IsMultiple
        {
            get { return _isMultiple; }
        }

        internal bool UserInitiated
        {
            get { return _userInitiated; }
        }
        

        private string _key;
        private bool _isMultiple;
        private bool _userInitiated;
}
}
