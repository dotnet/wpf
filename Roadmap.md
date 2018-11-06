# Roadmap

This repository will serve as an example for all who wish to migrate to VSTS for their builds. Therefore, we want it to provide clear examples of common use cases.

## Short Term
In the short term, we want to immediately fill the needs of [CoreCLR](https://github.com/dotnet/coreclr). This means that we will have a `.vsts-ci.yml` file which does the following:

* Runs CI and PR builds
* Runs debug and release builds
* Runs builds on Windows, Linux, Macs, and in Docker containers
* Have a pretty CI badge link to show build status

This should take advantage of YAML's templating and build-reason directives and generally should strive to be concise and efficient.

## The Slightly Less Short Term
Over the next few weeks, the repository should be developed to show a fuller example of Arcade's capabilities:

* Examples of plug & play telemetry
* Integration with and configuration of Maestro
* Consumption of Darc