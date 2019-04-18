# CodeGen in the dotnet/wpf repo

The following document describes how code generation in this repo works. The goal is to have all our code generation done through the use of T4 text generation. See the offical [Visual Studio T4 documentation](https://docs.microsoft.com/en-us/visualstudio/modeling/design-time-code-generation-by-using-t4-text-templates?view=vs-2019) for more information.

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