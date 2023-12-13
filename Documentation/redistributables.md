# Projects and Assemblies

This repository redistributes two assemblies. 

```
- vcruntime140.dll
- d3dcompiler_47.dll 
```

## Overview

WPF has two C++/CLI `/clr:pure` assemblies - `DirectWriteForwarder.dll` and `System.Printing.dll`. 

`/clr:pure` assemblies have a dependeny on VC Runtime assemblies today - i.e., they require `vcruntime140.dll` (or `vcruntime140d.dll` when compiled in `Debug` mode). 

This requires that end-users deploying WPF applications to install VC runtime redistributables. **We would like to avoid this.**

`wpfgfx_cor3.dll` - WPF's renderer - depends on `d3dcompiler_47.dll` - which is not available on all platforms (Operating Systems) universally. Some platforms have shipped `d3dcompiler_47.dll` via WU or Download Center and requires customers/consumers to download and install it manually. 

We would like to assure that `d3dcompiler_47.dll` to be available at runtime along with WPF on .NET Core.

Both `vcruntime140.dll` and `d3dcompiler_47.dll` are redistributable assemblies, and we are allowed to bundle it with .NET Core 3.0 - and **therefore we choose to redistribute these assemblies with our runtime assemblies.**

Since applications are free to redistribute these assemblies as well, and sometimes they take dependences on specific versions of these assemblies, we should create a design that does not clash with application's choices. In order to acheive this, we **rename the assemblies and change the import table in WPF assemblies to point to the renamed assemblies**. 

In other words, 

```
 - vcruntime140.dll -> renamed to vcruntime140_cor3.dll 
 - d3dcompiler_47.dll -> renamed to d3dcompiler_47_cor3.dll 
```


The following files are used to orchestrate the renaming of DLL import tables, and copying of renamed assemblies and packaging them: 

```
eng\WpfArcadeSdk\Redist.props
eng\WpfArcadeSdk\Redist.targets
src\Microsoft.DotNet.Wpf\redist\**\*
```