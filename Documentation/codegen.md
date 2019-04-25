# CodeGen in the dotnet/wpf repo

The following document describes how code generation in this repo works. The goal is to have all our code generation done through the use of T4 text generation. See the offical [Visual Studio T4 documentation](https://docs.microsoft.com/en-us/visualstudio/modeling/design-time-code-generation-by-using-t4-text-templates?view=vs-2019) for more information.

## Design-Time vs Run-Time T4 templates
Currently, we are evaluating the use of design-time text templates. This gives us the ability to simply add the templates and associated targets to the build, without the need of maintaining a separate tool to do run-time generation. Including the `Microsoft.TextTemplating.targets` requires us to manually import Sdk.Targets because it needs to be imported after Sdk.targets. This causes the `BuildDependsOn` variable, which is modified by the T4 targets, to be overwritten, so the `TransformAll` target doesn’t run before the Build target. The boilerplait for including design-time templates has been encapsulated in the `$(WpfCodeGenDir)DesignTimeTextTemplating.targets` file, so the pattern for enabling these in a project looks like this: 

```
<Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
<Import Project="$(WpfCodeGenDir)AvTrace\GenTraceSources.targets" />
<Import Project="$(WpfCodeGenDir)AvTrace\GenAvMessages.targets" />
<Import Project="$(WpfCodeGenDir)DesignTimeTextTemplating.targets" />
```

## Basic T4 code generation philosophy and guidelines
T4 templates can be a powerful tool, however without a conscious effort, they can quickly become unmaintainable and difficult to understand.  When authoring/modifying a T4 template, extra care should be taken to ensure that the templates are as readable as possible. While the "readibility" of a template may be a bit subjective, there are a few common guidelines that really help. Note that these are guidelines, and are not mandatory. The readability of the template is paramount to all things.

* When multiple lines of code need to be written inside of "<# #>" blocks, the "<#" and "#>" tags should be on seperate lines. This makes it easier to tell where the code starts and stops. If we follow this policy, we can know that a line that only contains a "<#" tag is the start of a multi-line code block.

**Correct**
```
<#
    string helloWorld = " Hello World ";
    hellowWorld = helloWorld.Trim();
#>
<#= hellowWorld #>
```
**Incorrect**
```
<# string helloWorld = " Hello World ";
   helloWorld = helloWorld.Trim(); #>
<#= hellowWorld #>
```

* In similar fashion, single-line code statements should contain the "<#" and "#>" tags on the same line as the code. This way we can know that any line that starts with a "<#" that has code next to it is only a one-line statement.

* if/else/elseif statements, and the closing bracket, should all be contained on a single line

**Correct**
```
<# if (WriteAsFunction()){ #>
bool GetFoo()
{
    ...
}
<# } else { #>
bool Foo
{
    get {...}
}
<# } #>
```
**Incorrect**
```
<#
if (WriteAsFunction())
{ 
#>
bool GetFoo()
{
    ...
}
<#
}
else
{
#>
bool Foo
{
    get {...}
}
<#
}
#>
```
* T4 generation allows you to write functions that you can invoke in the template inside of "<#+ #>" blocks. If the function is intended to be re-used to output some common text, the name of the function should start with "Output" so that is clear to the reader the intent of the function. Also, these functions should not impose any extra indentation (or it should be minimal), as this makes it more complicated to re-use and plug
in the function anywhere throughout the template.

**Correct**
```
<#+ void OutputFooFunction() { #>
bool GetFoo()
{
    ...
}
<#+ } #>
```
**Incorrect**
```
<#+ void FooFunction() { #>
        bool GetFoo()
        {
            ...
        }           
<#+ } #>
```
 
 ## Location of CodeGen targets
 Unless there is a good reason (that should be documented), all codegen related targets should go into the $(WpfArcadeSdk)tools\CodeGen folder. This way we have a clean and clear location where we are able to keep track of all code generation in the codebase.

 ## GenTraceSources and GenAvMessages
 These two projects codegen the files the WPF codebase uses for tracing, and both use the AvTraceMessages.xml files located in the project location that includes it, located via the MSBuild property $(MSBuildProjectFileDirectory).

 **Note**: GenTraceSources should currently only be used by WindowsBase. It generates the [PresentationTraceSources](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.presentationtracesources?view=netcore3.0) class, which is a public class. Changing this file can impact the public API surface of WindowsBase or other WPF assemblies. 