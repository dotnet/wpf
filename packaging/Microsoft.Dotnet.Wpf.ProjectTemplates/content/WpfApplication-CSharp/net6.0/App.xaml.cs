#if (!csharpFeature_ImplicitUsings)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#endif
using System.Configuration;
using System.Data;
using System.Windows;

#if (csharpFeature_FileScopedNamespaces)
namespace Company.WpfApplication1;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
}

#else
namespace Company.WpfApplication1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }
}
#endif
