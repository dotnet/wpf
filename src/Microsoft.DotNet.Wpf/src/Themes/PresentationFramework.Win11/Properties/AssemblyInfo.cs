using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page,
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page,
                                              // app, or any theme specific resource dictionaries)
)]


[assembly: XmlnsPrefix("http://schemas.wpfmui.com/2019", "mui")]
[assembly: XmlnsDefinition("http://schemas.wpfmui.com/2019", "PresentationFramework.Win11")]
[assembly: XmlnsDefinition("http://schemas.wpfmui.com/2019", "PresentationFramework.Win11.Controls")]
[assembly: XmlnsDefinition("http://schemas.wpfmui.com/2019", "PresentationFramework.Win11.Controls.Primitives")]
[assembly: XmlnsDefinition("http://schemas.wpfmui.com/2019", "PresentationFramework.Win11.DesignTime")]
[assembly: XmlnsDefinition("http://schemas.wpfmui.com/2019", "PresentationFramework.Win11.Markup")]
[assembly: XmlnsDefinition("http://schemas.wpfmui.com/2019", "PresentationFramework.Win11.Media.Animation")]