using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace System.Windows.Controls
{
    public class TimePicker : Control
    {
        static TimePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePicker),
                new FrameworkPropertyMetadata(typeof(TimePicker)));
        }

        public TimePicker()
        {
            this.Loaded += OnLoaded;
            _previousTime = DateTime.Now; // Initialize _previousTime here
        }

        #region Properties

        public static readonly DependencyProperty SelectedTimeFormatProperty =
            DependencyProperty.Register("SelectedTimeFormat", typeof(string), typeof(TimePicker));

        public string SelectedTimeFormat
        {
            get { return (string)GetValue(SelectedTimeFormatProperty); }
            set { SetValue(SelectedTimeFormatProperty, value); }
        }

        public static readonly DependencyProperty TimeStyleProperty =
            DependencyProperty.Register("TimeStyle", typeof(Style), typeof(TimePicker));

        public Style TimeStyle
        {
            get { return (Style)GetValue(TimeStyleProperty); }
            set { SetValue(TimeStyleProperty, value); }
        }

        public static readonly DependencyProperty SelectedTimeProperty =
            DependencyProperty.Register(
                nameof(SelectedTime),
                typeof(DateTime?),
                typeof(TimePicker),
                new FrameworkPropertyMetadata(null, OnSelectedTimeChanged));

        public DateTime? SelectedTime
        {
            get => (DateTime?)GetValue(SelectedTimeProperty);
            set => SetValue(SelectedTimeProperty, value);
        }

        public static readonly DependencyProperty TimeFormatProperty =
            DependencyProperty.Register(
                nameof(TimeFormat),
                typeof(TimePickerFormat),
                typeof(TimePicker),
                new FrameworkPropertyMetadata(TimePickerFormat.Short, OnTimeFormatChanged));

        public TimePickerFormat TimeFormat
        {
            get => (TimePickerFormat)GetValue(TimeFormatProperty);
            set => SetValue(TimeFormatProperty, value);
        }

        private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimePicker picker)
            {
                picker.UpdateTimeControl();
            }
        }

        private static void OnTimeFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimePicker picker)
            {
                picker.UpdateTimeControl();
            }
        }

        #endregion

        #region Private Fields

        private Time _timeControl;
        private bool _isPopupOpen;
        private DateTime _previousTime; // Use non-nullable DateTime

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var textBox = GetTemplateChild("PART_TextBox") as TextBox;
            var dropDownButton = GetTemplateChild("PART_Button") as Button;
            var popup = GetTemplateChild("PART_Popup") as Popup;

            if (textBox != null)
            {
                textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
            }

            if (dropDownButton != null)
            {
                dropDownButton.Click += DropDownButton_Click;
            }

            if (popup != null)
            {
                popup.Opened += Popup_Opened;
                popup.Closed += Popup_Closed;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateTimeControl();
        }

        private void DropDownButton_Click(object sender, RoutedEventArgs e)
        {
            var popup = GetTemplateChild("PART_Popup") as Popup;
            if (popup != null)
            {
                _isPopupOpen = !_isPopupOpen;
                popup.IsOpen = _isPopupOpen;
            }
        }

        private void Popup_Opened(object sender, EventArgs e)
        {
            InitializeTimeControl(); // Ensure _timeControl is initialized

            var popup = GetTemplateChild("PART_Popup") as Popup;
            if (popup != null)
            {
                if (_timeControl != null)
                {
                    if (SelectedTime.HasValue)
                    {
                        _timeControl.SelectedTime = SelectedTime.Value;
                    }
                    _timeControl.DataContext = this;

                    popup.Child = _timeControl; // Add _timeControl to the Popup's visual tree
                }
            }
        }

        private void Popup_Closed(object sender, EventArgs e)
        {
            _isPopupOpen = false;

            // Clear the Popup's content to remove the _timeControl
            var popup = GetTemplateChild("PART_Popup") as Popup;
            if (popup != null)
            {
                popup.Child = null;
            }
        }

        private void TimeControl_AcceptButtonClick(object sender, RoutedEventArgs e)
        {
            if (_timeControl != null)
            {
                SelectedTime = _timeControl.SelectedTime;
                UpdateTextBox();

                _isPopupOpen = false;
                var popup = GetTemplateChild("PART_Popup") as Popup;
                if (popup != null)
                {
                    popup.IsOpen = false;
                }
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    DateTime? newTime = ParseText(textBox.Text);
                    if (newTime.HasValue)
                    {
                        SelectedTime = newTime.Value;
                        _previousTime = newTime.Value; // Update _previousTime when valid input is parsed
                    }
                    else
                    {
                        textBox.Text = _previousTime.ToString("hh:mm tt");
                    }
                }
            }
        }

        private void UpdateTextBox()
        {
            var textBox = GetTemplateChild("PART_TextBox") as TextBox;
            if (textBox != null)
            {
                textBox.Text = SelectedTime?.ToString("hh:mm tt") ?? string.Empty;
            }
        }

        private void UpdateTimeControl()
        {
            if (_timeControl != null)
            {
                _timeControl.SelectedTime = SelectedTime.GetValueOrDefault();
                _timeControl.DataContext = this;
                UpdateTextBox();
            }
        }

        private void TimeControl_CancelButtonClick(object sender, RoutedEventArgs e)
        {
            _isPopupOpen = false;
            var popup = GetTemplateChild("PART_Popup") as Popup;
            if (popup != null)
            {
                popup.IsOpen = false;
            }
        }

        private void InitializeTimeControl()
        {
            _timeControl = new Time();
            if (_timeControl != null)
            {
                _timeControl.AcceptButtonClick += TimeControl_AcceptButtonClick;
                _timeControl.CancelButtonClick += TimeControl_CancelButtonClick;
            }
        }

        private DateTime? ParseText(string text)
        {
            try
            {
                var dtfi = CultureInfo.CurrentCulture.DateTimeFormat;
                return DateTime.Parse(text, dtfi);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
