// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Description:
* Implements label control.
*
\***************************************************************************/
using System;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.ComponentModel;
using System.Xaml;
using MS.Internal.Telemetry.PresentationFramework;

namespace System.Windows.Controls
{
    /// <summary>
    /// The Label control provides two basic pieces of functionality.  It provides the 
    /// ability to label a control -- called the "target" herein -- thus providing information 
    /// to the application user about the target.  The labeling is done through both 
    /// the actual text of the Label control and through information surfaced through 
    /// the UIAutomation API.  The second function of the Label control is to provide 
    /// mnemonics support -- both functional and visual -- the target.  For example, the 
    /// TextBox has no way of displaying additional information outside of its content.  
    /// A Label control could help solve this problem.  Note, the Label control is 
    /// frequently used in dialogs to allow quick keyboard access to controls in the dialog.
    /// </summary>
    /// <ExternalAPI />
    [Localizability(LocalizationCategory.Label)]
    public class Label : ContentControl
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        
        #region Constructors
        /// <summary>
        /// Static constructor
        /// </summary>
        static Label()
        {
            EventManager.RegisterClassHandler(typeof(Label), AccessKeyManager.AccessKeyPressedEvent, new AccessKeyPressedEventHandler(OnAccessKeyPressed));

            DefaultStyleKeyProperty.OverrideMetadata(typeof(Label), new FrameworkPropertyMetadata(typeof(Label)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(Label));

            // prevent label from being a tab stop and focusable
            IsTabStopProperty.OverrideMetadata(typeof(Label), new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));
            FocusableProperty.OverrideMetadata(typeof(Label), new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));

            ControlsTraceLogger.AddControl(TelemetryControls.Label);
        }

        /// <summary>
        ///     Default Label constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public Label() : base()
        {
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// DependencyProperty for RecognizesAccessKey
        /// </summary>
        public static readonly DependencyProperty RecognizesAccessKeyProperty =
            DependencyProperty.Register(
                "RecognizesAccessKey",
                typeof(bool),
                typeof(Label),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Property that allows for disabling the RecognizesAccesKey behaviour of the label.
        /// For Backwards compatibility its default value is false.
        /// </summary>
        public bool RecognizesAccessKey
        {
            get { return (bool)GetValue(RecognizesAccessKeyProperty); }
            set { SetValue(RecognizesAccessKeyProperty, value); }
        }

        


        /// <summary>
        /// DependencyProperty for Target property.
        /// </summary>
        public static readonly DependencyProperty TargetProperty =
                DependencyProperty.Register(
                        "Target", 
                        typeof(UIElement), 
                        typeof(Label),
                        new FrameworkPropertyMetadata(
                                (UIElement) null,
                                new PropertyChangedCallback(OnTargetChanged)));

        /// <summary>
        /// The target of this label.
        /// </summary>

        //The Target property can be a name reference.  <Label Target="myTextBox">First Name</Label>
        [TypeConverter(typeof(NameReferenceConverter))]
        public UIElement Target
        {
            get { return (UIElement) GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Label label = (Label) d;

            UIElement oldElement = (UIElement) e.OldValue;
            UIElement newElement = (UIElement) e.NewValue;

            // If the Target property has changed, set the LabeledByProperty on 
            // the new Target and clear the LabeledByProperty on the old Target.
            if (oldElement != null)
            {
                object oldValueLabeledBy = oldElement.GetValue(LabeledByProperty);

                // If this Label was actually labelling the oldValue 
                // then clear the LabeledByProperty
                if (oldValueLabeledBy == label)
                {
                    oldElement.ClearValue(LabeledByProperty);
                }
            }

            if (newElement != null)
            {
                newElement.SetValue(LabeledByProperty, label);
            }
        }

        /// <summary>
        ///     Attached DependencyProperty which indicates the element that is labeling
        ///     another element.
        /// </summary>
        private static readonly DependencyProperty LabeledByProperty =
                DependencyProperty.RegisterAttached(
                        "LabeledBy", 
                        typeof(Label), 
                        typeof(Label), 
                        new FrameworkPropertyMetadata((Label)null));

        /// <summary>
        ///     Returns the Label that an element might have been labeled by.
        /// </summary>
        /// <param name="o">The element that might have been labeled.</param>
        /// <returns>The Label that labels the element. Null otherwise.</returns>
        /// <remarks>
        ///     This internal method is used by UIAutomation's FrameworkElementProxy.
        /// </remarks>
        internal static Label GetLabeledBy(DependencyObject o)
        {
            if (o == null)
            {
                throw new ArgumentNullException("o");
            }

            return (Label)o.GetValue(LabeledByProperty);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() 
        {
            return new System.Windows.Automation.Peers.LabelAutomationPeer(this);
        }

        #region Private Methods

        private static void OnAccessKeyPressed(object sender, AccessKeyPressedEventArgs e)
        {
            Label label = sender as Label;
            // ISSUE: if this is handled in Control then we need to check here as well
            if (!e.Handled && e.Scope == null && (e.Target == null || e.Target == label))
            {
                e.Target = label.Target;
            }
        }

        #endregion

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default 
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey
    }
}
