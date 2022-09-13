#if (!csharpFeature_ImplicitUsings)
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
#else
using System.Configuration;
using System.Data;
using System.Windows;
#endif

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
