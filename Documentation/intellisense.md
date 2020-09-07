# Intellisense XML Incorporation into Ref-Pack


Intellisense XML's are produced in the `dotnet/dotnet-api-docs` repo. They are currently **not** published to a NuGet package or another easily consumable artifact. Thus, the process of ingestion of these XML files is a manual one at this time. 

1. Go to OPS build site at https://ops.microsoft.com/#/sites/Docs/docsets/dotnet-api-docs?tabName=builds and obtain the latest build artifacts
   - Filter by `Build Type = Intellisense`
    - Download latest package (it's a `zip` file)
2. Extract the zip contents and retain only the contents of `_intellisense\netcore-3.0` subfolder
   - Copy these contents over to a new folder hierarchy that looks like this: 
  
    ```
    DOTNET-API-DOCS_NETCOREAPP3.0-0.0.0.1-WIN32-X86
    \---_intellisense
        \---netcore-3.0
    ```

    - Create `version.txt` directly under the top-level folder, and save the commit-sha of the build obtained from the OPS site. 

 3. Repeat the process (using the same files) and create `dotnet-api-docs_netcoreapp3.0-0.0.0.1-win64-x64\` folder. 

*FUTURE NOTE: 
	The version number `0.0.0.1` would change for each subsequent update to a new value, like `0.0.0.2`, etc.* 


4. Compress each of the above folders like this: 

  ```PowerShell
  Compress-Archive -Path .\dotnet-api-docs_netcoreapp3.0-0.0.0.1-win32-x86\* -DestinationPath .\dotnet-api-docs_netcoreapp3.0-0.0.0.1-win32-x86.zip
  Compress-Archive -path .\dotnet-api-docs_netcoreapp3.0-0.0.0.1-win64-x64\* -DestinationPath .\dotnet-api-docs_netcoreapp3.0-0.0.0.1-win64-x64.zip
  ```

   - It's very important to use Powershell, and no other tools, to create these zip files. 

5. Upload the zip files using Azure Storage Explorer to `netcorenativeassets` blob store under this path: 
  - `resource-packages -> external -> windows -> dotnet-api-docs_netcoreapp3.0` 
6. Update the versions
    - `global.json` for `native-tools.dotnet-api-docs_netcoreapp3.0`
    - `ReferenceAssembly.targets` for `DotNetApiDocsNetCoreApp30` property
    - Also update `global.json` in `dotnet-wpf-int` repository (if applicable).
