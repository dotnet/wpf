# Testing WPF on .NET Core
This guide provides an outline of the tests available in WPF on .NET Core, how to utilize them, and contribution guidelines.

## Running Tests
Available tests can be run directly by using using the [test script](https://github.com/dotnet/wpf/tree/master/test.cmd) once a local build has been completed.

This will run the currently available tests against the binaries from the most recent local build (see [TestHost](https://github.com/dotnet/wpf/tree/master/Documentation/TestHost%20Infrastructure.md)).

## Unit Tests
Unit tests for WPF are intended to be small, fast tests and are categorized by the assembly being tested.

The general guidelines are the following:
* Must be able to execute fully in parallel with other unit tests running in the same process
* Must not use any input injection or other techniques that rely on application focus
* Any static state that is instantiated must either be process isolated or guaranteed not to interfere with other unit tests
* Must not require resources external to the repository
* Must be in the form of an [XUnit](https://github.com/xunit/xunit) test

### Contributing to Unit Tests
Each assembly built in this repo will have a unit test project associated with it.  You can contribute unit tests by adding code underneath
the appropriate unit test project.

For example, System.Xaml's unit tests are located [here](https://github.com/dotnet/wpf/tree/master/src/Microsoft.DotNet.Wpf/test/UnitTest/System.Xaml).
To add a unit test for System.Xaml, you just have to put the C# file containing the new test under that directory.  The [System.Xaml test project](https://github.com/dotnet/wpf/tree/master/src/Microsoft.DotNet.Wpf/test/UnitTest/System.Xaml/System.Xaml.Tests.csproj) will automatically
pick up and compile the new test and it will run during the next test invocation.  Of course, if the new test requires references that are not already within the unit test csproj, those will have to be added as well.

## Developer Regression Tests (DRTs)
These are a set of integration style tests that you can use to verify basic product functionality.  

More of these are currently being ported to .NET Core and will be available as soon as possible.

### Contributing to DRTs
At current, WPF will only take bug fix PRs for DRTs.

## Feature Tests
WPF on .NET Framework has an assortment of integration style tests (~24k) that cover the rest of the product functionality.  We are working
diligently to port these tests over in order to ensure the quality of WPF on .NET Core.  This is a large undertaking and we are
still in the first steps of porting needed infrastructure and removing dependencies that cannot be ported to .NET Core.