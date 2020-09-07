// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls
#else
namespace Microsoft.Windows.Controls
#endif
{
    #region Using declarations

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Markup;
    using System.Windows.Media;
    using System.Windows.Threading;
#if RIBBON_IN_FRAMEWORK
    using MS.Internal;
    using Microsoft.Windows.Controls;
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    ///     The service class which enables and manages KeyTips.
    /// </summary>
    public class KeyTipService
    {
        #region Constructor

        private KeyTipService()
        {
            InputManager.Current.PostProcessInput += new ProcessInputEventHandler(PostProcessInput);
            InputManager.Current.PreProcessInput += new PreProcessInputEventHandler(PreProcessInput);
            State = KeyTipState.None;
        }

        #endregion

        #region KeyTip Properties

        /// <summary>
        ///     DependencyProperty for KeyTip string.
        /// </summary>
        public static readonly DependencyProperty KeyTipProperty =
            DependencyProperty.RegisterAttached("KeyTip", typeof(string), typeof(KeyTipService), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnKeyTipChanged)));

        /// <summary>
        ///     Gets the value of KeyTip on the specified object
        /// </summary>
        public static string GetKeyTip(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (string)element.GetValue(KeyTipProperty);
        }

        /// <summary>
        ///     Sets the value of KeyTip on the specified object
        /// </summary>
        public static void SetKeyTip(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(KeyTipProperty, value);
        }

        private static void OnKeyTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            string newKeyTip = (string)e.NewValue;
            string oldKeyTip = (string)e.OldValue;

            bool isNewEmpty = string.IsNullOrEmpty(newKeyTip);

            if (isNewEmpty != string.IsNullOrEmpty(oldKeyTip))
            {
                if (isNewEmpty)
                {
                    if (CanUnregisterKeyTipElement(d))
                    {
                        UnregisterKeyTipElement(d);
                    }
                }
                else
                {
                    RegisterKeyTipElement(d);
                }
            }
        }

        public static bool GetIsKeyTipScope(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(IsKeyTipScopeProperty);
        }

        public static void SetIsKeyTipScope(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(IsKeyTipScopeProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsKeyTipScope.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsKeyTipScopeProperty =
            DependencyProperty.RegisterAttached("IsKeyTipScope", typeof(bool), typeof(KeyTipService), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsKeyTipScopeChanged)));

        private static void OnIsKeyTipScopeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProcessKeyTipScopeChange(d, (bool)e.NewValue);
        }

        public static Style GetKeyTipStyle(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (Style)element.GetValue(KeyTipStyleProperty);
        }

        public static void SetKeyTipStyle(DependencyObject element, Style value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(KeyTipStyleProperty, value);
        }

        // Using a DependencyProperty as the backing store for KeyTipStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty KeyTipStyleProperty =
            DependencyProperty.RegisterAttached("KeyTipStyle", typeof(Style), typeof(KeyTipService), new FrameworkPropertyMetadata(null));

        #endregion

        #region Registration

        private static readonly DependencyPropertyKey KeyTipScopePropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("KeyTipScope", typeof(DependencyObject), typeof(KeyTipService),
                new UIPropertyMetadata(null, new PropertyChangedCallback(OnKeyTipScopeChanged)));

        private static void OnKeyTipScopeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DependencyObject oldScope = e.OldValue as DependencyObject;
            DependencyObject newScope = e.NewValue as DependencyObject;
            KeyTipService current = Current;

            if (oldScope != null)
            {
                // Remove the element from the old scope data sets.
                WeakHashSet<DependencyObject> oldElementSet = null;
                if (current._scopeToElementMap.TryGetValue(oldScope, out oldElementSet))
                {
                    if (oldElementSet != null)
                    {
                        oldElementSet.Remove(d);
                    }
                }
            }
            if (newScope != null)
            {
                // Add the element to the new scope data sets.
                if (!current._scopeToElementMap.ContainsKey(newScope) ||
                    current._scopeToElementMap[newScope] == null)
                {
                    current._scopeToElementMap[newScope] = new WeakHashSet<DependencyObject>();
                }
                current._scopeToElementMap[newScope].Add(d);
            }
        }

        private static void HookKeyTipElementEventHandlers(DependencyObject element)
        {
            FrameworkElement fe = element as FrameworkElement;
            if (fe == null)
            {
                FrameworkContentElement fce = element as FrameworkContentElement;
                if (fce != null)
                {
                    fce.Loaded += new RoutedEventHandler(OnKeyTipElementLoaded);
                    fce.Unloaded += new RoutedEventHandler(OnKeyTipElementUnLoaded);
                }
            }
            else
            {
                fe.Loaded += new RoutedEventHandler(OnKeyTipElementLoaded);
                fe.Unloaded += new RoutedEventHandler(OnKeyTipElementUnLoaded);
            }
        }

        private static void UnhookKeyTipElementEventHandlers(DependencyObject element)
        {
            FrameworkElement fe = element as FrameworkElement;
            if (fe == null)
            {
                FrameworkContentElement fce = element as FrameworkContentElement;
                if (fce != null)
                {
                    fce.Loaded -= new RoutedEventHandler(OnKeyTipElementLoaded);
                    fce.Unloaded -= new RoutedEventHandler(OnKeyTipElementUnLoaded);
                }
            }
            else
            {
                fe.Loaded -= new RoutedEventHandler(OnKeyTipElementLoaded);
                fe.Unloaded -= new RoutedEventHandler(OnKeyTipElementUnLoaded);
            }
        }

        private static void RegisterKeyTipElement(DependencyObject element)
        {
            AddElementForKeyTipScoping(element);
            HookKeyTipElementEventHandlers(element);
        }

        private static void UnregisterKeyTipElement(DependencyObject element)
        {
            KeyTipService current = Current;
            current._toBeScopedElements.Remove(element);
            element.ClearValue(KeyTipScopePropertyKey);
            UnhookKeyTipElementEventHandlers(element);
        }

        private static bool CanUnregisterKeyTipElement(DependencyObject element)
        {
            return (string.IsNullOrEmpty(GetKeyTip(element)) &&
                GetKeyTipAutoGenerationElements(element) == null &&
                GetCustomSiblingKeyTipElements(element) == null);
        }

        private static void OnKeyTipElementLoaded(object sender, RoutedEventArgs e)
        {
            AddElementForKeyTipScoping(sender as DependencyObject);
        }

        private static void OnKeyTipElementUnLoaded(object sender, RoutedEventArgs e)
        {
            AddElementForKeyTipScoping(sender as DependencyObject);
        }

        private static void AddElementForKeyTipScoping(DependencyObject element)
        {
            Debug.Assert(element != null);
            KeyTipService current = Current;
            if (!current._toBeScopedElements.Contains(element))
            {
                current._toBeScopedElements.Add(element);
            }
        }

        private static void ProcessKeyTipScopeChange(DependencyObject scopeElement, bool newIsScope)
        {
            KeyTipService current = Current;
            if (current._unprocessedScopes.ContainsKey(scopeElement))
            {
                if (current._unprocessedScopes[scopeElement] != newIsScope)
                {
                    // Remove the scope element from unprocess scopes list
                    // if its IsKeyTipScope value changed from T->F->T or
                    // F->T->F
                    current._unprocessedScopes.Remove(scopeElement);
                }
            }
            else
            {
                current._unprocessedScopes[scopeElement] = newIsScope;
            }
        }

        private void ProcessScoping()
        {
            Dictionary<DependencyObject, bool> processedElements = new Dictionary<DependencyObject, bool>();

            // Process the all the scopes elements
            foreach (DependencyObject scope in _unprocessedScopes.Keys)
            {
                if (_unprocessedScopes[scope])
                {
                    // If an element newly became scope then process
                    // all the keytip elements under its parent scope.
                    DependencyObject scopedParent = FindScope(scope);
                    if (scopedParent == null)
                    {
                        scopedParent = scope;
                    }
                    ReevaluateScopes(scopedParent, processedElements);
                }
                else
                {
                    // Else process all the elements currently under the scope.
                    ReevaluateScopes(scope, processedElements);
                }
            }
            _unprocessedScopes.Clear();

            // Process all the one of elements which are queued for
            // scope processing.
            foreach (DependencyObject element in _toBeScopedElements)
            {
                if (!processedElements.ContainsKey(element))
                {
                    DependencyObject newScope = FindScope(element);
                    AddElementToScope(newScope, element);
                    processedElements[element] = true;
                }
            }
            _toBeScopedElements.Clear();
        }

        private static void AddItemToArrayListRef(object item, ref ArrayList list)
        {
            if (list == null)
            {
                list = new ArrayList(2);
            }
            list.Add(item);
        }

        /// <summary>
        ///     Reevalutates the scoping for all elements under
        ///     the given scope.
        /// </summary>
        private void ReevaluateScopes(DependencyObject scopedParent, Dictionary<DependencyObject, bool> processedElements)
        {
            if (scopedParent == null)
            {
                return;
            }
            if (_scopeToElementMap.ContainsKey(scopedParent))
            {
                WeakHashSet<DependencyObject> elementSet = _scopeToElementMap[scopedParent];
                if (elementSet != null && elementSet.Count > 0)
                {
                    Dictionary<DependencyObject, DependencyObject> elementToScopeMap = null;
                    foreach (DependencyObject element in elementSet)
                    {
                        if (!processedElements.ContainsKey(element))
                        {
                            DependencyObject newScope = FindScope(element);
                            if (newScope != scopedParent)
                            {
                                if (elementToScopeMap == null)
                                {
                                    elementToScopeMap = new Dictionary<DependencyObject, DependencyObject>();
                                }
                                elementToScopeMap[element] = newScope;
                            }
                            processedElements[element] = true;
                        }
                    }
                    if (elementToScopeMap != null)
                    {
                        foreach (DependencyObject element in elementToScopeMap.Keys)
                        {
                            // Add the element to the new scope.
                            AddElementToScope(elementToScopeMap[element], element);
                        }
                    }
                }
            }
        }

        private static void AddElementToScope(DependencyObject scopedParent, DependencyObject element)
        {
            element.SetValue(KeyTipScopePropertyKey, scopedParent);
        }

        private static DependencyObject FindScope(DependencyObject element)
        {
            return FindScope(element, true);
        }

        private static DependencyObject FindScope(DependencyObject element, bool searchVisualTreeOnly)
        {
            FrameworkElement fe = element as FrameworkElement;
            if (fe == null)
            {
                FrameworkContentElement fce = element as FrameworkContentElement;
                if (fce == null || !fce.IsLoaded)
                {
                    return null;
                }
            }
            else if (!fe.IsLoaded)
            {
                return null;
            }

            // If an ancestor has IsKeyTipScope set to true or
            // if it does not have niether logical nor visual parent
            // then it is considered to be a scope.
            if (searchVisualTreeOnly)
            {
                return TreeHelper.FindVisualAncestor(element,
                    delegate(DependencyObject d)
                    {
                        return ((KeyTipService.GetIsKeyTipScope(d) == true) || (TreeHelper.GetParent(d) == null));
                    });
            }
            else
            {
                return TreeHelper.FindAncestor(element, delegate(DependencyObject d)
                {
                    return ((KeyTipService.GetIsKeyTipScope(d) == true) || (TreeHelper.GetParent(d) == null));
                });
            }
        }

        #endregion

        #region Custom KeyTip Siblings

        internal static IEnumerable<DependencyObject> GetCustomSiblingKeyTipElements(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (IEnumerable<DependencyObject>)element.GetValue(CustomSiblingKeyTipElementsProperty);
        }

        internal static void SetCustomSiblingKeyTipElements(DependencyObject element, IEnumerable<DependencyObject> value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(CustomSiblingKeyTipElementsProperty, value);
        }

        // Using a DependencyProperty as the backing store for CustomSiblingKeyTipElements.  This enables animation, styling, binding, etc...
        internal static readonly DependencyProperty CustomSiblingKeyTipElementsProperty =
            DependencyProperty.RegisterAttached("CustomSiblingKeyTipElements",
                typeof(IEnumerable<DependencyObject>),
                typeof(KeyTipService),
                new UIPropertyMetadata(null, new PropertyChangedCallback(OnCustomSiblingKeyTipElementsChanged)));

        private static void OnCustomSiblingKeyTipElementsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                RegisterKeyTipElement(d);
            }
            else
            {
                if (CanUnregisterKeyTipElement(d))
                {
                    UnregisterKeyTipElement(d);
                }
            }
        }

        #endregion

        #region KeyTip AutoGeneration

        internal static IEnumerable<DependencyObject> GetKeyTipAutoGenerationElements(DependencyObject obj)
        {
            return (IEnumerable<DependencyObject>)obj.GetValue(KeyTipAutoGenerationElementsProperty);
        }

        internal static void SetKeyTipAutoGenerationElements(DependencyObject obj, IEnumerable<DependencyObject> value)
        {
            obj.SetValue(KeyTipAutoGenerationElementsProperty, value);
        }

        // Using a DependencyProperty as the backing store for KeyTipAutoGenerationElements.  This enables animation, styling, binding, etc...
        private static readonly DependencyProperty KeyTipAutoGenerationElementsProperty =
            DependencyProperty.RegisterAttached("KeyTipAutoGenerationElements",
                typeof(IEnumerable<DependencyObject>),
                typeof(KeyTipService),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnKeyTipAutoGenerationElementsChanged)));

        private static void OnKeyTipAutoGenerationElementsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                // Register for scoping
                RegisterKeyTipElement(d);
            }
            else
            {
                if (CanUnregisterKeyTipElement(d))
                {
                    // Unregister from scope.
                    UnregisterKeyTipElement(d);
                }
            }
        }

        /// <summary>
        ///     Helper method which determines if the keytip
        ///     was auto generated or not.
        /// </summary>
        private static bool IsAutoGeneratedKeyTip(string keyTip)
        {
            int length = keyTip.Length;
            if (length == 0)
            {
                return false;
            }

            if (length == 1)
            {
                int keyTipValue = 0;
                if (Int32.TryParse(keyTip, out keyTipValue))
                {
                    if (keyTipValue > 0 && keyTipValue <= NonZeroDigitCount)
                    {
                        return true;
                    }
                }
            }
            else
            {
                string keyTipEnd = keyTip.TrimStart('0');
                if (keyTipEnd != null && keyTipEnd.Length == 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Helper method to return next keytip string
        ///     in the auto generation sequence.
        /// </summary>
        private string GetNextAutoGenerationKeyTip(DependencyObject targetAutoGenerationScope)
        {
            // The first nine are 1 to 9
            CultureInfo culture = GetCultureForElement(targetAutoGenerationScope);
            if (string.IsNullOrEmpty(_autoGenerationKeyTipPrefix))
            {
                if (_nextAutoGenerationIndex < NonZeroDigitCount)
                {
                    _nextAutoGenerationIndex++;
                    return _nextAutoGenerationIndex.ToString(culture);
                }
                else
                {
                    _nextAutoGenerationIndex = 0;
                    _autoGenerationKeyTipPrefix = _nextAutoGenerationIndex.ToString(culture);
                }
            }

            if (_nextAutoGenerationIndex < NonZeroDigitCount)
            {
                // 9 to 1 precedeeded by appropriate repetitions of '0'
                return _autoGenerationKeyTipPrefix + (NonZeroDigitCount - _nextAutoGenerationIndex++).ToString(culture);
            }
            else
            {
                // A to Z preceeded by appropriate repetitions of '0'
                int alphaIndex = _nextAutoGenerationIndex++ - 9;
                if (alphaIndex < QatKeyTipCharacters.Length)
                {
                    return _autoGenerationKeyTipPrefix + QatKeyTipCharacters[alphaIndex];
                }

                _nextAutoGenerationIndex = 1;

                int i = 0;
                _autoGenerationKeyTipPrefix += i.ToString(culture);
                i = 9;
                return _autoGenerationKeyTipPrefix + i.ToString(culture);
            }
        }

        /// <summary>
        ///     Method which generates keytips for elements provided by
        ///     KeyTipAutoGenerationElements enumerables in current scope.
        /// </summary>
        private void AutoGenerateKeyTips(WeakHashSet<DependencyObject> currentScopeElements)
        {
            if (currentScopeElements != null && currentScopeElements.Count > 0)
            {
                bool reprocessScoping = false;
                foreach (DependencyObject element in currentScopeElements)
                {
                    IEnumerable<DependencyObject> keyTipAutoGenerationElements = GetKeyTipAutoGenerationElements(element);
                    if (keyTipAutoGenerationElements != null)
                    {
                        foreach (DependencyObject autoGenerationElement in keyTipAutoGenerationElements)
                        {
                            if (!_keyTipAutoGeneratedElements.Contains(autoGenerationElement))
                            {
                                reprocessScoping = true;
                                SetKeyTip(autoGenerationElement, GetNextAutoGenerationKeyTip(element));
                                _keyTipAutoGeneratedElements.Add(autoGenerationElement);
                            }
                        }
                    }
                }
                if (reprocessScoping)
                {
                    ProcessScoping();
                }
            }
        }

        private void ResetAutoGeneratedKeyTips()
        {
            if (_keyTipAutoGeneratedElements != null && _keyTipAutoGeneratedElements.Count > 0)
            {
                foreach (DependencyObject d in _keyTipAutoGeneratedElements)
                {
                    string keyTip = GetKeyTip(d);
                    if (!string.IsNullOrEmpty(keyTip) &&
                        IsAutoGeneratedKeyTip(keyTip))
                    {
                        d.ClearValue(KeyTipProperty);
                    }
                }
            }
        }

        #endregion

        #region Input

        internal enum KeyTipState
        {
            None,
            Pending,
            Enabled
        }

        internal KeyTipState State
        {
            get;
            private set;
        }

        private static bool IsKeyTipKey(KeyEventArgs e)
        {
            return ((e.Key == Key.System) &&
                (e.SystemKey == Key.RightAlt ||
                    e.SystemKey == Key.LeftAlt ||
                    e.SystemKey == Key.F10) &&
                (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift &&
                (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control);
        }

        private static bool IsKeyTipClosingKey(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                case Key.Enter:
                case Key.Left:
                case Key.Right:
                case Key.Up:
                case Key.Down:
                case Key.PageDown:
                case Key.PageUp:
                case Key.Home:
                case Key.End:
                case Key.Tab:
                    return true;

                case Key.System:
                    {
                        switch (e.SystemKey)
                        {
                            case Key.NumLock:
                            case Key.NumPad0:
                            case Key.NumPad1:
                            case Key.NumPad2:
                            case Key.NumPad3:
                            case Key.NumPad4:
                            case Key.NumPad5:
                            case Key.NumPad6:
                            case Key.NumPad7:
                            case Key.NumPad8:
                            case Key.NumPad9:
                            case Key.LeftShift:
                            case Key.RightShift:
                                return true;
                        }
                    }
                    break;
            }
            return false;
        }

        void PreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            if (State != KeyTipState.None)
            {
                RoutedEvent routedEvent = e.StagingItem.Input.RoutedEvent;
                if (routedEvent == Keyboard.PreviewKeyUpEvent)
                {
                    KeyEventArgs keyArgs = (KeyEventArgs)e.StagingItem.Input;
                    if (IsKeyTipKey(keyArgs))
                    {
                        OnPreviewKeyTipKeyUp(keyArgs);
                    }
                }
                else if (routedEvent == Mouse.PreviewMouseDownEvent ||
                        routedEvent == Stylus.PreviewStylusDownEvent)
                        // || routedEvent == UIElement.PreviewTouchDownEvent)
                {
                    LeaveKeyTipMode(false);
                }
                else if (routedEvent == TextCompositionManager.PreviewTextInputEvent)
                {
                    OnPreviewTextInput((TextCompositionEventArgs)e.StagingItem.Input);
                }
                else if (routedEvent == Keyboard.PreviewKeyDownEvent)
                {
                    OnPreviewKeyDown((KeyEventArgs)e.StagingItem.Input);
                }
                else if (routedEvent == Mouse.PreviewMouseWheelEvent)
                {
                    e.StagingItem.Input.Handled = true;
                }
            }
        }

        void PostProcessInput(object sender, ProcessInputEventArgs e)
        {
            if (e.StagingItem.Input.RoutedEvent == Keyboard.KeyDownEvent)
            {
                KeyEventArgs keyArgs = (KeyEventArgs)e.StagingItem.Input;
                if (!keyArgs.Handled && IsKeyTipKey(keyArgs) && State == KeyTipState.None)
                {
                    OnKeyTipKeyDown(keyArgs);
                }
                else if (State != KeyTipState.None)
                {
                    if (keyArgs.Key == Key.Escape)
                    {
                        OnEscapeKeyDown(keyArgs);
                    }
                }
            }
        }

        private void OnPreviewKeyDown(KeyEventArgs keyArgs)
        {
            if (_modeEnterKey == Key.None &&
                _probableModeEnterKey == Key.None &&
                keyArgs.Key == Key.System &&
                (keyArgs.SystemKey == Key.LeftAlt || keyArgs.SystemKey == Key.RightAlt))
            {
                // set _probableModeEnterKey
                _probableModeEnterKey = keyArgs.SystemKey;
            }
            else if (IsKeyTipClosingKey(keyArgs))
            {
                LeaveKeyTipMode(false);
            }
        }

        private void OnKeyTipKeyDown(KeyEventArgs keyArgs)
        {
            // Tries to enter keytip mode and set various
            // members accordingly.
            DependencyObject globalScope = GetGlobalScopeForElement(keyArgs.OriginalSource as DependencyObject);
            if (globalScope != null)
            {
                if (EnterKeyTipMode(globalScope, (keyArgs.SystemKey == Key.F10 ? false : true), true))
                {
                    keyArgs.Handled = true;
                    _focusRibbonOnKeyTipKeyUp = true;
                    if (keyArgs.SystemKey != Key.F10)
                    {
                        _modeEnterKey = keyArgs.SystemKey;
                    }
                }
            }
        }

        private void OnPreviewKeyTipKeyUp(KeyEventArgs keyArgs)
        {
            if (State == KeyTipState.Pending &&
                ((_modeEnterKey == Key.None &&
                 keyArgs.SystemKey == Key.F10) ||
                 keyArgs.SystemKey == Key.LeftAlt ||
                 keyArgs.SystemKey == Key.RightAlt))
            {
                ShowKeyTips();
            }
            else if (_modeEnterKey == Key.None)
            {
                LeaveKeyTipMode();
                keyArgs.Handled = true;
            }

            if (_modeEnterKey == keyArgs.SystemKey)
            {
                _modeEnterKey = Key.None;
            }

            if (_probableModeEnterKey == keyArgs.SystemKey)
            {
                _probableModeEnterKey = Key.None;
            }

            if (_focusRibbonOnKeyTipKeyUp)
            {
                // Raise KeyTipEnterFocus event.
                if (State == KeyTipState.Enabled)
                {
                    PresentationSource targetSource = RibbonHelper.GetPresentationSourceFromVisual(_currentGlobalScope as Visual);
                    if (targetSource != null)
                    {
                        RaiseKeyTipFocusEvent(targetSource, EventArgs.Empty, _keyTipEnterFocusHandlers);
                    }
                }
                _focusRibbonOnKeyTipKeyUp = false;
            }
        }

        private void OnEscapeKeyDown(KeyEventArgs keyArgs)
        {
            if (IsAltPressed)
            {
                LeaveKeyTipMode();
            }
            else
            {
                PopKeyTipScope();
            }
            keyArgs.Handled = true;
        }

        private void OnPreviewTextInput(TextCompositionEventArgs textArgs)
        {
            string text = textArgs.Text;
            if (string.IsNullOrEmpty(text))
            {
                text = textArgs.SystemText;
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (text == " ")
            {
                // Do nothing.
            }
            else
            {
                if (State == KeyTipState.Pending)
                {
                    ShowKeyTips();
                }

                if (_currentActiveKeyTipElements.Count > 0)
                {
                    if (_probableModeEnterKey != Key.None &&
                        _modeEnterKey == Key.None)
                    {
                        // When text is pressed, probableModeEnterKey becomes
                        // modeEnterKey. KeyTips are not dismissed on up of
                        // modeEnterKey for once. This is useful when multiple
                        // text is pressed with alt pressed.
                        _modeEnterKey = _probableModeEnterKey;
                        _probableModeEnterKey = Key.None;
                    }

                    textArgs.Handled = true;
                    text = _prefixText + text;

                    DependencyObject exactMatchElement = null;
                    List<DependencyObject> activeKeyTipElements = FindKeyTipMatches(text, out exactMatchElement);
                    if (exactMatchElement != null)
                    {
                        OnKeyTipExactMatch(exactMatchElement);
                    }
                    else
                    {
                        OnKeyTipPartialMatch(activeKeyTipElements, text);
                    }
                }
            }
        }

        private List<DependencyObject> FindKeyTipMatches(string text, out DependencyObject exactMatchElement)
        {
            exactMatchElement = null;
            List<DependencyObject> activeKeyTipElements = null;
            foreach (DependencyObject element in _currentActiveKeyTipElements)
            {
                string keyTip = GetKeyTip(element);
                CultureInfo culture = GetCultureForElement(element);
                if (string.Compare(keyTip, text, true, culture) == 0)
                {
                    exactMatchElement = element;
                    return null;
                }
                else if (keyTip.StartsWith(text, true, culture))
                {
                    if (activeKeyTipElements == null)
                    {
                        activeKeyTipElements = new List<DependencyObject>();
                    }
                    activeKeyTipElements.Add(element);
                }
            }
            return activeKeyTipElements;
        }

        internal static CultureInfo GetCultureForElement(DependencyObject element)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            if (DependencyPropertyHelper.GetValueSource(element, FrameworkElement.LanguageProperty).BaseValueSource != BaseValueSource.Default)
            {
                XmlLanguage language = (XmlLanguage)element.GetValue(FrameworkElement.LanguageProperty);
                if (language != null && language != XmlLanguage.Empty)
                {
                    Dictionary<XmlLanguage, CultureInfo> cultureCache = Current._cultureCache;
                    if (cultureCache != null && cultureCache.ContainsKey(language))
                    {
                        culture = cultureCache[language];
                    }
                    else
                    {
                        CultureInfo computedCulture = RibbonHelper.GetCultureInfo(element);
                        if (computedCulture != null)
                        {
                            culture = computedCulture;
                            if (cultureCache == null)
                            {
                                Current._cultureCache = cultureCache = new Dictionary<XmlLanguage, CultureInfo>();
                            }
                            cultureCache[language] = culture;
                        }
                    }
                }
            }
            return culture;
        }

        private void OnKeyTipExactMatch(DependencyObject exactMatchElement)
        {
            if (!((bool)(exactMatchElement.GetValue(UIElement.IsEnabledProperty))))
            {
                RibbonHelper.Beep();
                return;
            }

            HideCurrentShowingKeyTips();
            _prefixText = string.Empty;
            KeyTipAccessedEventArgs eventArgs = new KeyTipAccessedEventArgs();
            eventArgs.RoutedEvent = PreviewKeyTipAccessedEvent;
            object oldFocusedElement = Keyboard.FocusedElement;
            IInputElement inputElement = exactMatchElement as IInputElement;
            if (inputElement != null)
            {
                inputElement.RaiseEvent(eventArgs);
                eventArgs.RoutedEvent = KeyTipAccessedEvent;
                inputElement.RaiseEvent(eventArgs);
            }

            // KeyTips might have been dismissed by one of the event handlers
            // hence check again.
            if (State != KeyTipState.None)
            {
                object newFocusedElement = Keyboard.FocusedElement;
                DependencyObject newScope = eventArgs.TargetKeyTipScope;
                if (newScope != null &&
                    !KeyTipService.GetIsKeyTipScope(newScope) &&
                    newScope != _currentGlobalScope)
                {
                    throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.ElementNotKeyTipScope));
                }
                if (newScope == null &&
                    KeyTipService.GetIsKeyTipScope(exactMatchElement))
                {
                    newScope = exactMatchElement;
                }

                if (newScope != null)
                {
                    // Show keytips for new scope in a dispatcher operation.
                    Dispatcher.CurrentDispatcher.BeginInvoke(
                        (Action)delegate()
                        {
                            ShowKeyTipsForScope(newScope, true);
                        },
                        DispatcherPriority.Loaded);
                }
                else
                {
                    LeaveKeyTipMode(oldFocusedElement == newFocusedElement /*restoreFocus*/);
                }
            }
        }

        private void OnKeyTipPartialMatch(List<DependencyObject> activeKeyTipElements, string text)
        {
            if (activeKeyTipElements == null ||
                activeKeyTipElements.Count == 0)
            {
                // Beep when there are no matches.
                RibbonHelper.Beep();
                return;
            }

            int currentActiveCount = _currentActiveKeyTipElements.Count;
            int newActiveCount = activeKeyTipElements.Count;
            int newActiveElementIndex = 0;
            DependencyObject newActiveElement = activeKeyTipElements[newActiveElementIndex++];

            // Hide keytips for all the elements which do not
            // match with the new prefix.
            for (int i = 0; i < currentActiveCount; i++)
            {
                DependencyObject currentActiveElement = _currentActiveKeyTipElements[i];
                if (currentActiveElement == newActiveElement)
                {
                    if (newActiveElementIndex < newActiveCount)
                    {
                        newActiveElement = activeKeyTipElements[newActiveElementIndex++];
                    }
                    else
                    {
                        newActiveElement = null;
                    }
                }
                else
                {
                    HideKeyTipForElement(currentActiveElement);
                }
            }
            _currentActiveKeyTipElements = activeKeyTipElements;
            _prefixText = text;
        }

        private void OnWindowDeactivated(object sender, EventArgs e)
        {
            LeaveKeyTipMode();
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            LeaveKeyTipMode();
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            LeaveKeyTipMode();
        }

        private DependencyObject GetGlobalScopeForElement(DependencyObject element)
        {
            if (element == null)
            {
                return null;
            }

            if (_scopeToElementMap.Count == 0 && _toBeScopedElements.Count == 0 && _unprocessedScopes.Count == 0)
            {
                return null;
            }

            return TreeHelper.FindRoot(element);
        }

        private bool EnterKeyTipMode(DependencyObject scope, bool showKeyTips, bool showAsync)
        {
            ProcessScoping();

            if (!_scopeToElementMap.ContainsKey(scope))
            {
                return false;
            }

            _currentGlobalScope = scope;
            _currentWindow = Window.GetWindow(scope);
            if (_currentWindow != null)
            {
                _currentWindow.Deactivated += new EventHandler(OnWindowDeactivated);
                _currentWindow.SizeChanged += new SizeChangedEventHandler(OnWindowSizeChanged);
                _currentWindow.LocationChanged += new EventHandler(OnWindowLocationChanged);
            }
            State = KeyTipState.Pending;

            if (showKeyTips)
            {
                if (showAsync)
                {
                    StartShowKeyTipsTimer();
                }
                else
                {
                    ShowKeyTips();
                }
            }

            return true;
        }

        private void StartShowKeyTipsTimer()
        {
            if (_showKeyTipsTimer == null)
            {
                _showKeyTipsTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _showKeyTipsTimer.Interval = TimeSpan.FromMilliseconds(ShowKeyTipsWaitTime);
                _showKeyTipsTimer.Tick += delegate(object sender, EventArgs e) { ShowKeyTips(); };
            }
            _showKeyTipsTimer.Start();
        }

        private void ShowKeyTips()
        {
            if (_showKeyTipsTimer != null)
            {
                _showKeyTipsTimer.Stop();
            }
            if (State == KeyTipState.Pending)
            {
                Debug.Assert(_currentGlobalScope != null);
                if (ShowKeyTipsForScope(_currentGlobalScope))
                {
                    // KeyTips might have been dismissed during
                    // show, hence check again.
                    if (State == KeyTipState.Pending)
                    {
                        State = KeyTipState.Enabled;
                    }
                }
                else
                {
                    LeaveKeyTipMode(true);
                }
            }
        }

        private bool ShowKeyTipsForScope(DependencyObject scopeElement)
        {
            return ShowKeyTipsForScope(scopeElement, false);
        }

        private bool ShowKeyTipsForScope(DependencyObject scopeElement, bool pushOnEmpty)
        {
            bool returnValue = false;
            ProcessScoping();
            if (_scopeToElementMap.ContainsKey(scopeElement))
            {
                WeakHashSet<DependencyObject> elementSet = _scopeToElementMap[scopeElement];
                AutoGenerateKeyTips(elementSet);
                if (elementSet != null && elementSet.Count > 0)
                {
                    foreach (DependencyObject element in elementSet)
                    {
                        returnValue |= ShowKeyTipForElement(element);
                    }
                }

                // KeyTips might have been dismissed during
                // show, hence check again.
                if (State != KeyTipState.None)
                {
                    if (_scopeStack.Count == 0 ||
                        _scopeStack.Peek() != scopeElement)
                    {
                        _scopeStack.Push(scopeElement);
                    }
                }
            }
            else if (pushOnEmpty)
            {
                // Push the scope even if it is empty.
                // Used for any non-global scope.
                if (_scopeStack.Count == 0 ||
                    _scopeStack.Peek() != scopeElement)
                {
                    _scopeStack.Push(scopeElement);
                }
            }
            return returnValue;
        }

        private void PopKeyTipScope()
        {
            Debug.Assert(_scopeStack.Count > 0);
            DependencyObject currentScope = _scopeStack.Pop();
            DependencyObject parentScope = FindScope(currentScope, false);
            DependencyObject stackParentScope = (_scopeStack.Count > 0 ? _scopeStack.Peek() : null);
            if (stackParentScope != null &&
                parentScope != null &&
                RibbonHelper.IsAncestorOf(stackParentScope, parentScope))
            {
                // If there is any intermediate ancestral scope between current scope
                // and the next scope in stack, then push it onto stack.
                _scopeStack.Push(parentScope);
            }
            HideCurrentShowingKeyTips();
            _prefixText = string.Empty;

            if (_scopeStack.Count > 0)
            {
                // Dispatcher operation to show keytips for topmost
                // scope on the stack.
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    (Action)delegate()
                    {
                        if (_scopeStack.Count > 0)
                        {
                            ShowKeyTipsForScope(_scopeStack.Peek(), true);
                        }
                    },
                    DispatcherPriority.Loaded);
            }
            else
            {
                LeaveKeyTipMode();
            }
        }

        private void HideCurrentShowingKeyTips()
        {
            foreach (DependencyObject element in _currentActiveKeyTipElements)
            {
                HideKeyTipForElement(element);
            }
            _currentActiveKeyTipElements.Clear();
        }

        private void Reset()
        {
            _keyTipAutoGeneratedElements.Clear();
            _autoGenerationKeyTipPrefix = string.Empty;
            _nextAutoGenerationIndex = 0;
            _cultureCache = null;
            _cachedKeyTipControls = null;
            _currentGlobalScope = null;
            if (_currentWindow != null)
            {
                _currentWindow.Deactivated -= new EventHandler(OnWindowDeactivated);
                _currentWindow.SizeChanged -= new SizeChangedEventHandler(OnWindowSizeChanged);
                _currentWindow.LocationChanged -= new EventHandler(OnWindowLocationChanged);
            }
            _currentWindow = null;
            if (_showKeyTipsTimer != null)
            {
                _showKeyTipsTimer.Stop();
            }
            _focusRibbonOnKeyTipKeyUp = false;
            _modeEnterKey = Key.None;
            _probableModeEnterKey = Key.None;
            _prefixText = string.Empty;
            _currentActiveKeyTipElements.Clear();
            _scopeStack.Clear();
            State = KeyTipState.None;
        }

        private void LeaveKeyTipMode()
        {
            LeaveKeyTipMode(true);
        }

        private void LeaveKeyTipMode(bool restoreFocus)
        {
            HideCurrentShowingKeyTips();
            ResetAutoGeneratedKeyTips();
            if (restoreFocus)
            {
                PresentationSource targetSource = RibbonHelper.GetPresentationSourceFromVisual(_currentGlobalScope as Visual);
                if (targetSource != null)
                {
                    RaiseKeyTipFocusEvent(targetSource, EventArgs.Empty, _keyTipExitRestoreFocusHandlers);
                }
            }
            Reset();
        }

        private static bool IsAltPressed
        {
            get
            {
                return ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt);
            }
        }

        #endregion

        #region KeyTip Creation

        internal static bool GetCanClipKeyTip(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(CanClipKeyTipProperty);
        }

        internal static void SetCanClipKeyTip(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(CanClipKeyTipProperty, value);
        }

        // Using a DependencyProperty as the backing store for CanClipKeyTip.  This enables animation, styling, binding, etc...
        private static readonly DependencyProperty CanClipKeyTipProperty =
            DependencyProperty.RegisterAttached("CanClipKeyTip", typeof(bool), typeof(KeyTipService), new UIPropertyMetadata(true));

        /// <summary>
        ///     Returns the adorner layer to be used
        ///     for keytips. The implementation is similar
        ///     to that of AdornerLayer's except we ignore
        ///     AdornerLayer of ScrollContentPresenter if its
        ///     ScrollViewer has CanClipKeyTip set to false.
        /// </summary>
        static private AdornerLayer GetAdornerLayer(Visual visual, out bool isScrollAdornerLayer)
        {
            if (visual == null)
                throw new ArgumentNullException("visual");

            isScrollAdornerLayer = false;
            Visual parent = VisualTreeHelper.GetParent(visual) as Visual;
            ScrollContentPresenter lastScp = null;

            while (parent != null)
            {
                AdornerDecorator adornerDecorator = parent as AdornerDecorator;
                if (adornerDecorator != null)
                {
                    return adornerDecorator.AdornerLayer;
                }

                ScrollContentPresenter scp = parent as ScrollContentPresenter;
                if (scp != null)
                {
                    lastScp = scp;
                }

                ScrollViewer sv = parent as ScrollViewer;
                if (sv != null && lastScp != null)
                {
                    if (GetCanClipKeyTip(sv))
                    {
                        isScrollAdornerLayer = true;
                        return lastScp.AdornerLayer;
                    }
                    lastScp = null;
                }

                parent = VisualTreeHelper.GetParent(parent) as Visual;
            }

            return null;
        }

        private static readonly DependencyProperty KeyTipAdornerProperty =
            DependencyProperty.RegisterAttached("KeyTipAdorner", typeof(KeyTipAdorner), typeof(KeyTipService), new UIPropertyMetadata(null));

        private static readonly DependencyProperty KeyTipAdornerHolderProperty =
            DependencyProperty.RegisterAttached("KeyTipAdornerHolder", typeof(UIElement), typeof(KeyTipService), new UIPropertyMetadata(null));

        private static readonly DependencyProperty ShowingKeyTipProperty =
            DependencyProperty.RegisterAttached("ShowingKeyTip", typeof(bool), typeof(KeyTipService),
                new UIPropertyMetadata(false, new PropertyChangedCallback(OnShowingKeyTipChanged)));

        private static void OnShowingKeyTipChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                UIElement uie = RibbonHelper.GetContainingUIElement(element);
                if (uie != null &&
                    uie.Visibility != Visibility.Visible)
                {
                    // revert the value if the element is not visible
                    element.SetValue(ShowingKeyTipProperty, false);
                    return;
                }

                // Raise the ActivatingKeyTip event.
                ActivatingKeyTipEventArgs activatingEventArgs = new ActivatingKeyTipEventArgs();
                IInputElement inputElement = element as IInputElement;
                if (inputElement != null)
                {
                    inputElement.RaiseEvent(activatingEventArgs);
                }

                // KeyTips could have been dismissed due to one
                // of the event handler, hence check again.
                KeyTipService current = Current;
                if (current.State != KeyTipState.None)
                {
                    if (activatingEventArgs.KeyTipVisibility == Visibility.Visible)
                    {
                        // Create the keytip and add it as the adorner.
                        UIElement adornedElement = RibbonHelper.GetContainingUIElement(activatingEventArgs.PlacementTarget == null ? element : activatingEventArgs.PlacementTarget);
                        if (adornedElement != null && adornedElement.IsVisible)
                        {
                            bool isScrollAdornerLayer = false;
                            AdornerLayer adornerLayer = GetAdornerLayer(adornedElement, out isScrollAdornerLayer);
                            if (adornerLayer != null)
                            {
                                KeyTipAdorner adorner = new KeyTipAdorner(adornedElement,
                                    activatingEventArgs.PlacementTarget,
                                    activatingEventArgs.KeyTipHorizontalPlacement,
                                    activatingEventArgs.KeyTipVerticalPlacement,
                                    activatingEventArgs.KeyTipHorizontalOffset,
                                    activatingEventArgs.KeyTipVerticalOffset,
                                    activatingEventArgs.Handled ? null : activatingEventArgs.OwnerRibbonGroup);
                                LinkKeyTipControlToAdorner(adorner, element);
                                adornerLayer.Add(adorner);
                                element.SetValue(KeyTipAdornerProperty, adorner);
                                element.SetValue(KeyTipAdornerHolderProperty, adornedElement);

                                // Begin invode an operation to nudge all the keytips into the
                                // adorner layer boundary unless the layer belongs to a scroll viewer.
                                if (!isScrollAdornerLayer)
                                {
                                    current.EnqueueAdornerLayerForPlacementProcessing(adornerLayer);
                                }
                            }
                        }
                    }

                    if (activatingEventArgs.KeyTipVisibility != Visibility.Collapsed)
                    {
                        // add the element to currentActiveKeyTipElement list.
                        current._currentActiveKeyTipElements.Add(element);
                    }
                    else
                    {
                        // Revert the value if it is asked by event handlers
                        // (i.e, by setting KeyTipVisibility to collapsed.
                        element.SetValue(ShowingKeyTipProperty, false);
                    }
                }
                else
                {
                    // Revert the value if we already dismissed keytips.
                    element.SetValue(ShowingKeyTipProperty, false);
                }
            }
            else
            {
                // Remove keytip from adorner.
                KeyTipAdorner adorner = (KeyTipAdorner)element.GetValue(KeyTipAdornerProperty);
                UIElement adornedElement = (UIElement)element.GetValue(KeyTipAdornerHolderProperty);
                if (adornedElement != null && adorner != null)
                {
                    UnlinkKeyTipControlFromAdorner(adorner);
                    bool isScrollAdornerLayer = false;
                    AdornerLayer adornerLayer = GetAdornerLayer(adornedElement, out isScrollAdornerLayer);
                    if (adornerLayer != null)
                    {
                        adornerLayer.Remove(adorner);
                    }
                }
                element.ClearValue(KeyTipAdornerProperty);
                element.ClearValue(KeyTipAdornerHolderProperty);
            }
        }

        private void EnqueueAdornerLayerForPlacementProcessing(AdornerLayer adornerLayer)
        {
            if (!_placementProcessingAdornerLayers.Contains(adornerLayer))
            {
                _placementProcessingAdornerLayers.Add(adornerLayer);
            }

            if (!_adornerLayerPlacementProcessingQueued)
            {
                _adornerLayerPlacementProcessingQueued = true;
                adornerLayer.Dispatcher.BeginInvoke(
                    (Action)delegate()
                    {
                        // Iterate through all the enqueued adorner layers and nudge all
                        // the keytip adorners belonging to them if needed.
                        foreach (AdornerLayer currentAdornerLayer in _placementProcessingAdornerLayers)
                        {
                            foreach (object child in LogicalTreeHelper.GetChildren(currentAdornerLayer))
                            {
                                KeyTipAdorner keyTipAdorner = child as KeyTipAdorner;
                                if (keyTipAdorner != null)
                                {
                                    keyTipAdorner.NudgeIntoAdornerLayerBoundary(currentAdornerLayer);
                                }
                            }
                        }
                        _placementProcessingAdornerLayers.Clear();
                        _adornerLayerPlacementProcessingQueued = false;
                    },
                    DispatcherPriority.Input,
                    null);
            }
        }

        private bool ShowKeyTipForElement(DependencyObject element)
        {
            if (State == KeyTipState.None)
            {
                return false;
            }

            bool returnValue = false;
            if (!string.IsNullOrEmpty(GetKeyTip(element)))
            {
                // Show keytip on the element if it has one.
                returnValue = true;
                element.SetValue(ShowingKeyTipProperty, true);
            }

            // If the element can provide custome keytip siblings
            // then show keytips on all such siblings.
            IEnumerable<DependencyObject> customSiblings = GetCustomSiblingKeyTipElements(element);
            if (customSiblings != null)
            {
                foreach (DependencyObject siblingElement in customSiblings)
                {
                    returnValue |= ShowKeyTipForElement(siblingElement);
                }
            }
            return returnValue;
        }

        private static void HideKeyTipForElement(DependencyObject element)
        {
            element.ClearValue(ShowingKeyTipProperty);
        }

        private static void LinkKeyTipControlToAdorner(KeyTipAdorner adorner, DependencyObject keyTipElement)
        {
            KeyTipService current = Current;
            KeyTipControl keyTipControl = null;
            if (current._cachedKeyTipControls != null &&
                current._cachedKeyTipControls.Count > 0)
            {
                // Retrieve a KeyTipControl from cache for reuse.
                int count = current._cachedKeyTipControls.Count;
                keyTipControl = current._cachedKeyTipControls[count - 1];
                current._cachedKeyTipControls.RemoveAt(count - 1);
            }
            if (keyTipControl == null)
            {
                keyTipControl = new KeyTipControl();
            }
            adorner.LinkKeyTipControl(keyTipElement, keyTipControl);
        }

        private static void UnlinkKeyTipControlFromAdorner(KeyTipAdorner adorner)
        {
            KeyTipControl keyTipControl = adorner.KeyTipControl;
            adorner.UnlinkKeyTipControl();
            if (keyTipControl != null)
            {
                // Save the unlinked KeyTipControl to cache for reuse.
                KeyTipService current = Current;
                if (current._cachedKeyTipControls == null)
                {
                    current._cachedKeyTipControls = new List<KeyTipControl>();
                }
                current._cachedKeyTipControls.Add(keyTipControl);
            }
        }

        #endregion

        #region Events

        public static readonly RoutedEvent ActivatingKeyTipEvent = EventManager.RegisterRoutedEvent("ActivatingKeyTip", RoutingStrategy.Bubble, typeof(ActivatingKeyTipEventHandler), typeof(KeyTipService));

        public static void AddActivatingKeyTipHandler(DependencyObject element, ActivatingKeyTipEventHandler handler)
        {
            RibbonHelper.AddHandler(element, ActivatingKeyTipEvent, handler);
        }

        public static void RemoveActivatingKeyTipHandler(DependencyObject element, ActivatingKeyTipEventHandler handler)
        {
            RibbonHelper.RemoveHandler(element, ActivatingKeyTipEvent, handler);
        }

        public static readonly RoutedEvent PreviewKeyTipAccessedEvent = EventManager.RegisterRoutedEvent("PreviewKeyTipAccessed", RoutingStrategy.Tunnel, typeof(KeyTipAccessedEventHandler), typeof(KeyTipService));

        public static void AddPreviewKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
        {
            RibbonHelper.AddHandler(element, PreviewKeyTipAccessedEvent, handler);
        }

        public static void RemovePreviewKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
        {
            RibbonHelper.RemoveHandler(element, PreviewKeyTipAccessedEvent, handler);
        }

        public static readonly RoutedEvent KeyTipAccessedEvent = EventManager.RegisterRoutedEvent("KeyTipAccessed", RoutingStrategy.Bubble, typeof(KeyTipAccessedEventHandler), typeof(KeyTipService));

        public static void AddKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
        {
            RibbonHelper.AddHandler(element, KeyTipAccessedEvent, handler);
        }

        public static void RemoveKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
        {
            RibbonHelper.RemoveHandler(element, KeyTipAccessedEvent, handler);
        }

        #endregion

        #region Focus Events

        internal delegate bool KeyTipFocusEventHandler(object sender, EventArgs e);

        /// <summary>
        ///     Event used to notify ribbon to obtain focus
        ///     while entering keytip mode.
        /// </summary>
        internal event KeyTipFocusEventHandler KeyTipEnterFocus
        {
            add
            {
                AddKeyTipFocusEventHandler(value, ref _keyTipEnterFocusHandlers);
            }
            remove
            {
                RemoveKeyTipFocusEventHandler(value, _keyTipEnterFocusHandlers);
            }
        }

        /// <summary>
        ///     Event used to notify ribbon to restore focus
        ///     while exiting keytip mode.
        /// </summary>
        internal event KeyTipFocusEventHandler KeyTipExitRestoreFocus
        {
            add
            {
                AddKeyTipFocusEventHandler(value, ref _keyTipExitRestoreFocusHandlers);
            }
            remove
            {
                RemoveKeyTipFocusEventHandler(value, _keyTipExitRestoreFocusHandlers);
            }
        }

        /// <summary>
        ///     Helper method to add weakhandler to the list
        /// </summary>
        private static void AddKeyTipFocusEventHandler(KeyTipFocusEventHandler handler, ref List<WeakReference> handlerList)
        {
            if (handlerList == null)
                handlerList = new List<WeakReference>(1);

            lock (handlerList)
            {
                // Cleanup the list in case some of the weakEnterMenuModeHandlers is disposed
                for (int i = 0; i < handlerList.Count; i++)
                {
                    if (!handlerList[i].IsAlive)
                    {
                        handlerList.RemoveAt(i);
                        i--;
                    }
                }
                handlerList.Add(new WeakReference(handler));
            }
        }

        /// <summary>
        ///     Helper method to remove weakhandler from the list.
        /// </summary>
        private static void RemoveKeyTipFocusEventHandler(KeyTipFocusEventHandler handler, List<WeakReference> handlerList)
        {
            if (handlerList != null)
            {
                lock (handlerList)
                {
                    for (int i = 0; i < handlerList.Count; i++)
                    {
                        KeyTipFocusEventHandler current = handlerList[i].Target as KeyTipFocusEventHandler;

                        if (current == null || current == handler)
                        {
                            handlerList.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Helper method to iterate through list of weak
        ///     event handlers and call them until the event is
        ///     handled.
        /// </summary>
        private static void RaiseKeyTipFocusEvent(object sender, EventArgs e, List<WeakReference> handlerList)
        {
            if (handlerList != null)
            {
                lock (handlerList)
                {
                    for (int i = 0; i < handlerList.Count; i++)
                    {
                        KeyTipFocusEventHandler current = handlerList[i].Target as KeyTipFocusEventHandler;

                        if (current == null)
                        {
                            handlerList.RemoveAt(i);
                            i--;
                        }
                        else
                        {
                            if (current(sender, e))
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public static void DismissKeyTips()
        {
            Current.LeaveKeyTipMode(false);
        }

        #endregion

        #region Instance

        internal static KeyTipService Current
        {
            get
            {
                if (_current == null)
                    _current = new KeyTipService();
                return _current;
            }
        }

        [ThreadStatic]
        private static KeyTipService _current;

        #endregion

        #region Private Data

        // Data members which exist across multiple keytip life times.
        WeakHashSet<DependencyObject> _toBeScopedElements = new WeakHashSet<DependencyObject>(); // List of one-off elements to be scoped.
        WeakDictionary<DependencyObject, bool> _unprocessedScopes = new WeakDictionary<DependencyObject, bool>(); // List of scopes which are to processed
        WeakDictionary<DependencyObject, WeakHashSet<DependencyObject>> _scopeToElementMap = new WeakDictionary<DependencyObject, WeakHashSet<DependencyObject>>();
        private List<WeakReference> _keyTipEnterFocusHandlers = null;
        private List<WeakReference> _keyTipExitRestoreFocusHandlers = null;
        private bool _adornerLayerPlacementProcessingQueued = false;
        private List<AdornerLayer> _placementProcessingAdornerLayers = new List<AdornerLayer>();

        // Data members which exist with in one keytip life time.
        private DependencyObject _currentGlobalScope = null;
        private Window _currentWindow = null;
        private DispatcherTimer _showKeyTipsTimer = null;
        private bool _focusRibbonOnKeyTipKeyUp = false;
        private Key _modeEnterKey = Key.None; // Next keyup of this key will not dimiss keytips.
        private Key _probableModeEnterKey = Key.None; // Key which is probable to become _modeEnterKey.
        private string _prefixText = string.Empty;
        private List<DependencyObject> _currentActiveKeyTipElements = new List<DependencyObject>(); // List of elements whose keytips are active.
        private Stack<DependencyObject> _scopeStack = new Stack<DependencyObject>();
        private List<KeyTipControl> _cachedKeyTipControls = null; // cache of keytip controls for re-use.
        private Dictionary<XmlLanguage, CultureInfo> _cultureCache = null; // cache of culture infos.
        private WeakHashSet<DependencyObject> _keyTipAutoGeneratedElements = new WeakHashSet<DependencyObject>();
        private int _nextAutoGenerationIndex = 0;
        private string _autoGenerationKeyTipPrefix = string.Empty;

        private const int ShowKeyTipsWaitTime = 500;
        private const int NonZeroDigitCount = 9;

        private static String QatKeyTipCharacters = Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.QATKeyTipCharacters);

        #endregion
    }
}
