﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MS.Internal.Telemetry.PresentationFramework;

// Disable CS3001: Warning as Error: not CLS-compliant
#pragma warning disable CS3001

namespace System.Windows.Controls
{
    /// <summary>
    ///     RadioButton implements option button with two states: true or false
    /// </summary>
    [Localizability(LocalizationCategory.RadioButton)]
    public class RadioButton : ToggleButton
    {
        #region Constructors

        static RadioButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RadioButton), new FrameworkPropertyMetadata(typeof(RadioButton)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(RadioButton));

            KeyboardNavigation.AcceptsReturnProperty.OverrideMetadata(typeof(RadioButton), new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));

            ControlsTraceLogger.AddControl(TelemetryControls.RadioButton);
        }

        /// <summary>
        ///     Default RadioButton constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public RadioButton() : base()
        {
        }

        #endregion

        #region private helpers

        private static void OnGroupNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RadioButton radioButton = (RadioButton)d;
            string groupName = e.NewValue as string;
            string currentlyRegisteredGroupName = _currentlyRegisteredGroupName.GetValue(radioButton);

            if (groupName != currentlyRegisteredGroupName)
            {
                // Unregister the old group name if set
                if (!string.IsNullOrEmpty(currentlyRegisteredGroupName))
                    Unregister(currentlyRegisteredGroupName, radioButton);

                // Register the new group name is set
                if (!string.IsNullOrEmpty(groupName))
                    Register(groupName, radioButton);
            }
        }

        private static void Register(string groupName, RadioButton radioButton)
        {
            _groupNameToElements ??= new Dictionary<string, List<WeakReference<RadioButton>>>(1);

            if (!_groupNameToElements.TryGetValue(groupName, out List<WeakReference<RadioButton>> elements))
            {
                elements = new List<WeakReference<RadioButton>>(2);
                _groupNameToElements[groupName] = elements;
            }
            else
            {
                // There were some elements there, remove dead ones
                PurgeDead(elements, null);
            }

            elements.Add(new WeakReference<RadioButton>(radioButton));

            _currentlyRegisteredGroupName.SetValue(radioButton, groupName);
        }

        private static void Unregister(string groupName, RadioButton radioButton)
        {
            Debug.Assert(_groupNameToElements is not null, "Unregister was called before Register");

            if (_groupNameToElements is null)
                return;

            // Get all elements bound to this key and remove this element
            if (_groupNameToElements.TryGetValue(groupName, out List<WeakReference<RadioButton>> elements))
            {
                PurgeDead(elements, radioButton);

                // If the group has zero elements, remove it
                if (elements.Count == 0)
                    _groupNameToElements.Remove(groupName);
            }

            _currentlyRegisteredGroupName.SetValue(radioButton, null);
        }

        private static void PurgeDead(List<WeakReference<RadioButton>> elements, RadioButton elementToRemove)
        {
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                if (!elements[i].TryGetTarget(out RadioButton element) || element == elementToRemove)
                {
                    elements.RemoveAt(i);
                }
            }
        }

        private void UpdateRadioButtonGroup()
        {
            string groupName = GroupName;
            if (!string.IsNullOrEmpty(groupName))
            {
                Visual rootScope = KeyboardNavigation.GetVisualRoot(this);

                _groupNameToElements ??= new Dictionary<string, List<WeakReference<RadioButton>>>(1);

                // Get all elements bound to this key and remove this element
                if (_groupNameToElements.TryGetValue(groupName, out List<WeakReference<RadioButton>> elements))
                {
                    for (int i = elements.Count - 1; i >= 0; i--)
                    {
                        if (elements[i].TryGetTarget(out RadioButton radioButton))
                        {
                            // Uncheck all checked RadioButtons different from the current one
                            if (radioButton != this && radioButton.IsChecked is true && rootScope == KeyboardNavigation.GetVisualRoot(radioButton))
                                radioButton.UncheckRadioButton();
                        }
                        else
                        {
                            // Remove dead instances
                            elements.RemoveAt(i);
                        }
                    }
                }
            }
            else // Logical parent should be the group
            {
                DependencyObject parent = this.Parent;
                if (parent != null)
                {
                    // Traverse logical children
                    IEnumerable children = LogicalTreeHelper.GetChildren(parent);
                    IEnumerator itor = children.GetEnumerator();
                    while (itor.MoveNext())
                    {
                        RadioButton rb = itor.Current as RadioButton;
                        if (rb != null && rb != this && string.IsNullOrEmpty(rb.GroupName) && (rb.IsChecked == true))
                            rb.UncheckRadioButton();
                    }
                }
            }
        }

        private void UncheckRadioButton()
        {
            SetCurrentValueInternal(IsCheckedProperty, MS.Internal.KnownBoxes.BooleanBoxes.FalseBox);
        }

        #endregion

        #region Properties and Events

        /// <summary>
        /// The DependencyID for the GroupName property.
        /// Default Value:      "String.Empty"
        /// </summary>
        public static readonly DependencyProperty GroupNameProperty = DependencyProperty.Register(
            "GroupName",
            typeof(string),
            typeof(RadioButton),
            new FrameworkPropertyMetadata(String.Empty, new PropertyChangedCallback(OnGroupNameChanged)));

        /// <summary>
        /// GroupName determine mutually excusive radiobutton groups
        /// </summary>
        [DefaultValue("")]
        [Localizability(LocalizationCategory.NeverLocalize)] // cannot be localized
        public string GroupName
        {
            get
            {
                return (string)GetValue(GroupNameProperty);
            }

            set
            {
                SetValue(GroupNameProperty, value);
            }
        }

        #endregion

        #region Override methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new System.Windows.Automation.Peers.RadioButtonAutomationPeer(this);
        }

        /// <summary>
        ///     This method is invoked when the IsChecked becomes true.
        /// </summary>
        /// <param name="e">RoutedEventArgs.</param>
        protected override void OnChecked(RoutedEventArgs e)
        {
            // If RadioButton is checked we should uncheck the others in the same group
            UpdateRadioButtonGroup();
            base.OnChecked(e);
        }

        /// <summary>
        /// This override method is called from OnClick().
        /// RadioButton implements its own toggle behavior
        /// </summary>
        protected internal override void OnToggle()
        {
            SetCurrentValueInternal(IsCheckedProperty, MS.Internal.KnownBoxes.BooleanBoxes.TrueBox);
        }

        /// <summary>
        /// The Access key for this control was invoked.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnAccessKey(AccessKeyEventArgs e)
        {
            if (!IsKeyboardFocused)
            {
                Focus();
            }

            base.OnAccessKey(e);
        }


        #endregion

        #region Accessibility

        #endregion Accessibility

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey

        #region private data

        [ThreadStatic]
        private static Dictionary<string, List<WeakReference<RadioButton>>> _groupNameToElements;

        private static readonly UncommonField<string> _currentlyRegisteredGroupName = new UncommonField<string>();

        #endregion private data
    }
}
