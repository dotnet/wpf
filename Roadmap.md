# WPF on .NET Core Roadmap

With the introduction of .NET Core 3, WPF exists as one of [several layers](https://github.com/dotnet/core/blob/master/Documentation/core-repos.md) of .NET Core.  Although .NET Core is cross-platform, the WPF framework relies heavily on Windows-specific platform pieces.  This roadmap communicates priorities for evolving and extending the scope of WPF on .NET Core.

At present, our team's primary focus is making additional components of WPF available as open source in this repo, and adding the ability to run tests publicly so we can accept PRs from the WPF community.  As we progress in this effort, we'll update our roadmap to include additional feature/capability areas we will focus the project on.

## Timeline for Open Source
| Milestone | Release Date |
|---|---|
|Initial launch of WPF on .NET Core repository (with System.Xaml open source)|Dec 4, 2018|
|Ability to accept PRs from community for open sourced assemblies|Early 2019|
|Roadmap update for feature focus areas|Early 2019|
|Add remaining WPF on .NET Core assemblies to repository as open source|Continues thru 2019|

## Feedback
The best way to give feedback is to [create issues in the dotnet/wpf repo](https://github.com/dotnet/wpf/issues/).  Double-check to see if you're creating your issue in the appropriate place:

* This repo is specifically focused on WPF on .NET Core, which is separate from WPF that runs on the Desktop Framework.  If you have feedback for the latter, please report it on [developercommunity.visualstudio.com](https://developercommunity.visualstudio.com/) using the "Report a problem" or "Suggest a feature" buttons.
* If you have general feedback about .NET Core 3, please use the [dotnet/core](https://github.com/dotnet/core) repo or one of the other [.NET Core repos](https://github.com/dotnet/core/blob/master/Documentation/core-repos.md) suitable for the topic you'd like to discuss.

Some of the feedback we find most valuable is feedback on:

* Existing features that are missing some capability or otherwise don't work well enough.
* Missing features that should be added to the product.
* Design choices for a feature that is currently in-progress.
* Which WPF assemblies we should prioritize making available as open source as quickly as possible.

Some important caveats / notes:

* It's best to give design feedback quickly for improvements that are in-development.  We're unlikely to hold a feature being part of a release on late feedback.
* We are most likely to include improvements that either have a positive impact on a broad scenario or have very significant positive impact on a niche scenario.  We are less likely to prioritize modest improvements to niche scenarios.
* Compatibility will almost always be given a higher priority than improvements.