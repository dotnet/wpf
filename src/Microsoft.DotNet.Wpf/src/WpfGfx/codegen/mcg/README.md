# MilCodeGen
This directory contains MilCodeGen, which uses the Csp tool to generate source code for the MIL resources.

## How to run MilCodeGen
Run this command at the root of the WPF repo:
```
build.cmd -projects "src\Microsoft.DotNet.Wpf\src\WpfGfx\codegen\mcg\mcg.proj"
```

## Generated files
List of generated resources:
```
src\Microsoft.DotNet.Wpf\src\WpfGfx\Include\Generated\wgx_resource_types.*
src\Microsoft.DotNet.Wpf\src\WpfGfx\Include\Generated\wgx_sdk_version.*
src\Microsoft.DotNet.Wpf\src\WpfGfx\Include\Generated\*_command*
src\Microsoft.DotNet.Wpf\src\WpfGfx\Include\Generated\wgx_misc.*
src\Microsoft.DotNet.Wpf\src\WpfGfx\Include\Generated\wgx_render_types.h
src\Microsoft.DotNet.Wpf\src\WpfGfx\Include\Generated\wincodec_private_generated.h
src\Microsoft.DotNet.Wpf\src\WpfGfx\Include\Generated\wgx_render_types_generated.h
src\Microsoft.DotNet.Wpf\src\WindowsBase\System\Windows\Converters\Generated\.
src\Microsoft.DotNet.Wpf\src\WindowsBase\System\Windows\Generated\.
src\Microsoft.DotNet.Wpf\src\WindowsBase\System\Windows\Media\Generated\.
src\Microsoft.DotNet.Wpf\src\WindowsBase\System\Windows\Media\Converters\Generated\.
src\Microsoft.DotNet.Wpf\src\PresentationCore\System\Windows\Generated\.
src\Microsoft.DotNet.Wpf\src\PresentationCore\System\Windows\Media3D\Converters\Generated\.
src\Microsoft.DotNet.Wpf\src\PresentationCore\System\Windows\Media3D\Generated\.
src\Microsoft.DotNet.Wpf\src\PresentationCore\System\Windows\Media\Animation\Generated\.
src\Microsoft.DotNet.Wpf\src\PresentationCore\System\Windows\Media\Converters\Generated\.
src\Microsoft.DotNet.Wpf\src\PresentationCore\System\Windows\Media\Effects\Generated\.
src\Microsoft.DotNet.Wpf\src\PresentationCore\System\Windows\Media\Generated\.
src\Microsoft.DotNet.Wpf\src\PresentationCore\System\Windows\Media\Imaging\Generated\.
src\Microsoft.DotNet.Wpf\src\PresentationFramework\System\Windows\Generated\.
src\Microsoft.DotNet.Wpf\src\PresentationFramework\System\Windows\Media\Animation\Generated\.
src\Microsoft.DotNet.Wpf\src\Shared\MS\Internal\Generated\.
src\Microsoft.DotNet.Wpf\src\WpfGfx\codegen\mcg\ResourceModel\Generated\.
src\Microsoft.DotNet.Wpf\src\WpfGfx\core\resources\*_generated.*
src\Microsoft.DotNet.Wpf\src\WpfGfx\core\uce\generated_*
src\Microsoft.DotNet.Wpf\src\WpfGfx\exts\cmdstruct.h
```

List of generated elements:
```
src\Microsoft.DotNet.Wpf\src\PresentationCore\System\Windows\Generated\UIElement.cs
src\Microsoft.DotNet.Wpf\src\PresentationCore\System\Windows\Generated\UIElement3D.cs
src\Microsoft.DotNet.Wpf\src\PresentationCore\System\Windows\Generated\ContentElement.cs
```

## Documentation
Control flow in these templates is as follows:
 * Execution starts at ResourceGenerator.Main()
 * That method invokes the Go() method on each subclass of GeneratorBase. 
   i.e. it invokes methods like ManagedResource.Go() and DataStruct.Go().
