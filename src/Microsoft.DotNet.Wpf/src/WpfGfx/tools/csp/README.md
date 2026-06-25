# Csp
Csp is an 'interpreter' (compile & run in one step), for projects written in either C#, or "C# Prime".

We use it for [MilCodeGen](../../codegen/mcg/README.md).

Run it with "-h" for a list of options.
For a "Hello World" style example, look at test\CsProject\*.

## Tests
To run the Csp tests, run this command at the root of the WPF repo:
```
build.cmd -projects "src\Microsoft.DotNet.Wpf\src\WpfGfx\tools\csp\RunUnitTests.proj"
```