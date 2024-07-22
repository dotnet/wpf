using System;
using System.Windows.Controls;

namespace System.Windows.Controls
{
    public class TimePickerTextBox : TextBox
    {
        static TimePickerTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePickerTextBox), new FrameworkPropertyMetadata(typeof(TimePickerTextBox)));
        }
    }
}
