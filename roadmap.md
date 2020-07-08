# WPF for .NET 5 Roadmap
WPF is a .NET Core UI framework for building Windows desktop applications. The ownership of WPF was recently transitioned to our team (Developer Ecosystem and Platform team under Windows). This transition enables investments across UI frameworks namely WINUI and WPF to stay aligned and remain future-proof as new technology trends & devices are introduced in the industry.

The roadmap below communicates priorities for evolving and extending the scope of WPF for .NET Core through 2020 and into 2021. It will continue to evolve based on market changes and customer feedback, so please note that the plans outlined here are not exhaustive or guaranteed. We welcome your feedback on the roadmap: please feel free to contribute to existing issues or [file a new issue](https://github.com/dotnet/wpf/issues/new/choose "file a new issue").

### PROPOSED ROADMAP
| #  | Milestone  |  Target Delivery |
| :------------: | :------------: | :------------: |
|1  |Incorporating .NET Framework servicing fixes into .NET Core 3.1 and .NET 5 |Ongoing|
|2  |Ongoing integration with .NET 5 |20H2|
|3  |Build out Test Infrastructure to add tests to validate and merge community PRs|20H2|
|4  |Accessibility updates to .NET 5   |21H1|
|5  |ARM64 Support|21H1|

### Our SLA
#### TRIAGING GITHUB ISSUES
We are committed to a 72 hour (3 working days) turn around on triaging and responding to new issues filed on Github starting July 10th.
Additionally,  Issues filed prior to July 10th are also being triaged. Issues filed in 2020 will be triaged first based on number of reactions/comments, and we expect to complete doing so by the middle of 20H2. Issues filed before 2020 will be triaged after that based on number of reactions/comments.

#### EVALUATING PULL REQUESTS
We will begin merging contributions from the community on the WPF repo by picking 1-2 PRs from the community to manually test and integrate per month. When the test infrastructure work is completed we will enable broader community pull request merging. This is our team's commitment until our resourcing for WPF ramps-up.
#### Code PRs considered at this time will be based on:
-	Recency
-	Extent of Reactions (Likes, comments)
-	Fixes to existing issues prioritized over new features
-	Complexity of the fix
-	Breadth of impact of the fix
-	Low potential for regression
-	Has adequate test coverage
-	Is not a new feature request

#### About non-code PRs
We are committed to a 72 hour (3 working days) turn around on triaging and responding to new non-code PRs filed on Github starting July 10th. These are PRs for documentations bugs, documentation enhancements or a general question not related to source code issues/bugs.

### COMMUNITY CALLS
We will host a WPF segment in the existing [WinUI community calls](https://github.com/microsoft/microsoft-ui-xaml#winui-community-calls "WinUI community calls") once every two months (beginning in July 2020).In these calls we’ll discuss the WPF roadmap, our status and your feedback.

### RESOURCING STATUS
Our team’s management has approved additional resourcing needs for this project. However, the global COVID-19 pandemic has caused hiring to be slower than usual. With the available resources on the current team, the roadmap above is what we are committed to getting scheduled and done in the foreseeable future. Stay tuned for schedule and/or investment updates as soon as we are able to actively expand resourcing for our team.
