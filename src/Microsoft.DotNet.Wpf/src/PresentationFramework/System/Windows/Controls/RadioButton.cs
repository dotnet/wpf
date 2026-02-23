// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Windows.Data;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
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
            t_groupNameToElements ??= new Dictionary<string, List<WeakReference<RadioButton>>>(1);

            ref List<WeakReference<RadioButton>> elements = ref CollectionsMarshal.GetValueRefOrAddDefault(t_groupNameToElements, groupName, out bool exists);
            if (!exists)
            {
                // Create new collection
                elements = new List<WeakReference<RadioButton>>(2);
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
            Debug.Assert(t_groupNameToElements is not null, "Unregister was called before Register");

            if (t_groupNameToElements is null)
                return;

            // Get all elements bound to this key and remove this element
            if (t_groupNameToElements.TryGetValue(groupName, out List<WeakReference<RadioButton>> elements))
            {
                PurgeDead(elements, radioButton);

                // If the group has zero elements, remove it
                if (elements.Count == 0)
                    t_groupNameToElements.Remove(groupName);
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
                t_groupNameToElements ??= new Dictionary<string, List<WeakReference<RadioButton>>>(1);

                // Get all elements bound to this key
                if (t_groupNameToElements.TryGetValue(groupName, out List<WeakReference<RadioButton>> elements))
                {
                    for (int i = elements.Count - 1; i >= 0; i--)
                    {
                        // Either remove the dead element or uncheck if we're checked
                        if (elements[i].TryGetTarget(out RadioButton radioButton))
                        {
                            // Uncheck all checked RadioButtons but this one
                            if (radioButton != this && radioButton.IsChecked is true)
                            {
                                DependencyObject rootScope = InputElement.GetRootVisual(this);
                                DependencyObject otherRoot = InputElement.GetRootVisual(radioButton);

                                // If elements have the same group name but the visual roots are different, we still treat them
                                // as unique since we want to promote reuse of group names to make them easier to work with.
                                if (rootScope != otherRoot)
                                    continue;

                                // Allow binding sharing under the same visual root
                                BindingExpression rootBindingSource = GetBindingExpression(IsCheckedProperty);
                                BindingExpression otherBindingSource = radioButton.GetBindingExpression(IsCheckedProperty);

                                if (rootBindingSource is not null && otherBindingSource is not null &&
                                    rootBindingSource.SourceItem == otherBindingSource.SourceItem &&
                                    rootBindingSource.SourcePropertyName == otherBindingSource.SourcePropertyName)
                                    continue;

                                radioButton.UncheckRadioButton();
                            }
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
                DependencyObject parent = Parent;
                if (parent is not null)
                {
                    // Traverse logical children
                    IEnumerable children = LogicalTreeHelper.GetChildren(parent);
                    IEnumerator itor = children.GetEnumerator();
                    while (itor.MoveNext())
                    {
                        if (itor.Current is RadioButton radioButton && radioButton.IsChecked is true &&
                            radioButton != this && string.IsNullOrEmpty(radioButton.GroupName))
                        {
                            radioButton.UncheckRadioButton();
                        }
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
            new FrameworkPropertyMetadata(string.Empty, new PropertyChangedCallback(OnGroupNameChanged)));

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
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RadioButtonAutomationPeer(this);
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

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static readonly DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey

        #region private data

        [ThreadStatic]
        private static Dictionary<string, List<WeakReference<RadioButton>>> t_groupNameToElements;

        private static readonly UncommonField<string> _currentlyRegisteredGroupName = new UncommonField<string>();

        #endregion private data
    }
}
