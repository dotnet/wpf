# MilCodeGen
This directory contains MilCodeGen, which uses the Csp tool to generate source code for the MIL resources.

## How to run MilCodeGen
Run this command at the root of the WPF repo:
```
build.cmd -projects "src\Microsoft.DotNet.Wpf\src\WpfGfx\codegen\mcg\mcg.proj"
```

## Documentation
Control flow in these templates is as follows:
 * Execution starts at ResourceGenerator.Main()
 * That method invokes the Go() method on each subclass of GeneratorBase. 
   i.e. it invokes methods like ManagedResource.Go() and DataStruct.Go().
