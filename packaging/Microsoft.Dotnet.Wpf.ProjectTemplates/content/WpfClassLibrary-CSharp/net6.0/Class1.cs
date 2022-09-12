#if (!csharpFeature_ImplicitUsings)
using System;
#endif

#if (csharpFeature_FileScopedNamespaces)
namespace Company.ClassLibrary1;

public class Class1
{
}
#else
namespace Company.ClassLibrary1
{
    public class Class1
    {
    }
}
#endif

