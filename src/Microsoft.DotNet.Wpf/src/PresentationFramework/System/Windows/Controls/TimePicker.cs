using System;
using System.Windows;
using System.Windows.Controls;

namespace System.Windows.Controls
{
    public class TimePicker : Control
    {
        static TimePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePicker), new FrameworkPropertyMetadata(typeof(TimePicker)));
        }

        public static readonly DependencyProperty SelectedTimeProperty =
            DependencyProperty.Register("SelectedTime", typeof(DateTime?), typeof(TimePicker), new PropertyMetadata(null));

        public DateTime? SelectedTime
        {
            get { return (DateTime?)GetValue(SelectedTimeProperty); }
            set { SetValue(SelectedTimeProperty, value); }
        }

        public TimePicker()
        {
            this.Loaded += TimePicker_Loaded;
        }

        public static readonly DependencyProperty ClockStyleProperty =
            DependencyProperty.Register("ClockStyle", typeof(Style), typeof(TimePicker), new PropertyMetadata(null));

        public Style ClockStyle
        {
            get { return (Style)GetValue(ClockStyleProperty); }
            set { SetValue(ClockStyleProperty, value); }
        }

        private void TimePicker_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeTimeComponents();
        }

        private void InitializeTimeComponents()
        {
            if (Template != null)
            {
                var hoursComboBox = Template.FindName("HoursComboBox", this) as ComboBox;
                var minutesComboBox = Template.FindName("MinutesComboBox", this) as ComboBox;
                var amPmComboBox = Template.FindName("AmPmComboBox", this) as ComboBox;

                if (hoursComboBox != null && minutesComboBox != null && amPmComboBox != null)
                {
                    for (int i = 1; i <= 12; i++)
                        hoursComboBox.Items.Add(i);

                    for (int i = 0; i < 60; i++)
                        minutesComboBox.Items.Add(i.ToString("00"));

                    amPmComboBox.Items.Add("AM");
                    amPmComboBox.Items.Add("PM");

                    hoursComboBox.SelectionChanged += TimeComponentChanged;
                    minutesComboBox.SelectionChanged += TimeComponentChanged;
                    amPmComboBox.SelectionChanged += TimeComponentChanged;
                }
            }
        }

        private void TimeComponentChanged(object sender, SelectionChangedEventArgs e)
        {
            var hoursComboBox = Template.FindName("HoursComboBox", this) as ComboBox;
            var minutesComboBox = Template.FindName("MinutesComboBox", this) as ComboBox;
            var amPmComboBox = Template.FindName("AmPmComboBox", this) as ComboBox;

            if (hoursComboBox.SelectedItem != null && minutesComboBox.SelectedItem != null && amPmComboBox.SelectedItem != null)
            {
                int hours = (int)hoursComboBox.SelectedItem;
                int minutes = int.Parse((string)minutesComboBox.SelectedItem);
                string ampm = (string)amPmComboBox.SelectedItem;

                if (ampm == "PM" && hours != 12)
                    hours += 12;
                else if (ampm == "AM" && hours == 12)
                    hours = 0;

                SelectedTime = new DateTime(1, 1, 1, hours, minutes, 0);
            }
        }
    }
}
