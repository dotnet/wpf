Public Class CustomControl1
    Inherits Control

    ''' To use this custom control in a XAML file in another project, complete the following steps:
    '''
    ''' 1. Add a reference to this project
    ''' 2. Add the following line to the root element of the XAML file where you wish to use this control:
    '''   xmlns:MyNamespace="clr-namespace:Company.WpfCustomControlLibrary;assembly=Company.WpfCustomControlLibrary"
    ''' 2. Use the control in the XAML file:
    '''   <MyNamespace:CustomControl1/>

    Shared Sub New()
        'This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
        'This style is defined in Themes\Generic.xaml
        DefaultStyleKeyProperty.OverrideMetadata(GetType(CustomControl1), New FrameworkPropertyMetadata(GetType(CustomControl1)))
    End Sub

End Class
