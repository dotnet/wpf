# C++ Private Tools

### What is msvcurt-c1xx in global.json? 

In late Feb 2019, we discovered a bug in the C++/CLI compiler for .NET Core that requried the front-end (`c1xx.dll`) and the libraries (`msvcurt(d)_netcore.lib`) to be rebuilt. 

A fix was available in mid Mar, 2019, but this change could not make it into Dev16.0, and Dev16.1 Preview1 build weren't coming out until later in Apr, 2019. 

In collaboration with the C++ team, we decided to build our repo using private (but signed) copies of `c1xx.dll` and `msvcurt(d)_netcore.dll`. 

We have uploaded these private DLL's to an Azure blob at this location using Arcade's [_Native Toolset Bootstrapping_](https://github.com/dotnet/arcade/blob/master/Documentation/NativeToolBootstrapping.md) process. 

This results in the the packages being uploaded to locations like these: 

```
- https://netcorenativeassets.blob.core.windows.net/resource-packages/external/windows/msvcurt-c1xx/msvcurt-c1xx-0.0.0.3-win32-x86.zip
- https://netcorenativeassets.blob.core.windows.net/resource-packages/external/windows/msvcurt-c1xx/msvcurt-c1xx-0.0.0.3-win64-x64.zip
```

PS: The version at the time this note was written is `0.0.0.3`. The version might be updated in the future and `global.json` would contain the correct version. 

The version (`0.0.0.3`) is arbitrary, and simply refers to the the number of different versions we have uploaded as part of a trial-and-error approach to making our build work with these private compiler-bits. 

In Azure Storage Explorer, the blobs can be found at this location: 

```
 - Dotnet Engineering Services
  - Storage Accounts
   - netcorenativeassets
    - Blob Containers
     - resource-packages
      - external
       - windows
        - msvcurt-c1xx
         - msvcurt-c1xx-0.0.0.3-win64-x64.zip
         - msvcurt-c1xx-0.0.0.3-win32-x86.zip
```

Writing to this storage account requires special permissions and is only available to Microsoft Employees - please work with DncEng if you need to do this. Also refer to [Native Toolset Bootstrapping documentation](https://github.com/dotnet/arcade/blob/master/Documentation/NativeToolBootstrapping.md).


#### How does msvcurt-c1xx work? 

- global.json + Arcade's toolset restore-phase will download the blobs and lay it out under `$(RepoRoot).tools\native\bin\msvcurt-c1xx\0.0.0.3\`
 - This will happen before build starts. 
- WPF's `Wpf.Cpp.props` + `Wpf.Cpp.targets` identifies the right copy of `c1xx.dll` and `msvcurt(d)_netcore.lib` and passes it to `cl.exe` and `link.exe` - thus overriding the copies that shipeed with Visual Studio.
- For testing, you can prepare appropriate zip-bundles as described in the _Native Toolset Bootstrapping_ documentation and place them under `$env:USERPROFILE\.netcoreeng\native\temp` folder. 
  - This is the _cache_ location used by the Arcade scripts for downloading the zip bundles from Azure Blob storage to local disk. 
  - When the build-scripts observe that the zip file is already present in this location, they will not attempt to download the zip file from the blob. 