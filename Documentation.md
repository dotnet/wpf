# Documentation

This documents [@jonfortescue](https://github.com/jonfortescue)'s process in creating this repository.

1. Created a new repository on GitHub to test this whole VSTS integrated PR deal.
2. Created the default .NET Core console app project in Visual Studio which runs "Hello World" and committed it to the repository.
3. Since this test repository will not be doing internal builds (only PR and CI), we will not be creating a VSTS mirror of the repository.
4. Added a Windows queue with a basic set up for builds (use .NET CLI, run `restore`, `build`, `publish`). As part of troubleshooting this, added `Build.Repository.Clean: true` to the build to ensure binaries were cleaned from the build machine. Also added `targetFramework: netcoreapp2.0` as a build variable and referenced it during the `publish` step to prevent build breaks.
5. Use matrices to run debug and release builds in simultaneous phases
6. Broke out the build steps into a `build.yml` template to prepare for code reuse on step 7
7. Added Linux and OSX queues. For the OSX queue, initially ran into authorization problem; issue was fixed following the steps detailed in [Arcade's Azure DevOps Onboarding doc](https://github.com/dotnet/arcade/blob/master/Documentation/AzureDevOps/AzureDevOpsOnboarding.md#Troubleshooting) under the section **Troubleshooting/Queuing builds** (second bullet point).
8. As part of troubleshooting step 7: added a step for installing the .NET CLI and ensured the most recent version was used (caused segfaults on Mac otherwise). Also added `DOTNET_SKIP_FIRST_TIME_EXPERIENCE: 1` and `DOTNET_MULTILEVEL_LOOKUP: 0` as environment variables for the `restore` step to prevent restoring the entire cache to the build machine.
9. Added `{{ if }}` directives for the publish step based on build configuration. As of right now, is not working. *TODO: update with fix when working*.
10. Added `Build.Reason` if-directives to prevent `Release` builds from running on pull requests.
11. Added a CI integration trigger linked to the `master` branch.
12. Added a CI build status badge to the Readme.