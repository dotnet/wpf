# WPF Roadmap 2023

Over the last year, we have made efforts in improving the testing infrastructure, merging community PRs to address long-standing issues and enhancing accessibility features. The efforts to open-source our tests and automate the testing process through CI/CD pipelines, tackle persistent issues, and make the product more accessible, all came together in bringing WPF on .NET 7.  

Here is an update on the goals we set out to achieve in [2022 July-December](https://github.com/dotnet/wpf/discussions/6744):

| Goal | Description | Status |
| ------------- |:--------------|:-------------|
| Test repo migration | Move all tests (Developer Regression Tests (DRTs), Microsuites, and Feature tests) to public domain to make it possible to run tests locally and make it possible to run them in PR validation. <br/> | Finished: DRTs moved to wpf-test repo. <br/> Not Started: Microsuites and Feature tests were not moved. |
| Test automation | Run basic tests (DRTs) in public pipelines. | Completed: Basic tests run on every build. |
| Community Top Picks | Shortlist of ~24 issues/PRs that should be prioritized for resolution/merges. | Finished: 20 prioritized issues/PRs <br /> Not Started: [4 issues/PRs](https://github.com/orgs/dotnet/projects/146/views/1) | 
| Implicit Usings | Enable implicit usings in WPF project templates | Completed: Part of .NET 8.0 Preview 1
---

We are fully dedicated to achieving our goals, but recent priorities including XPS security improvements, .NET releases and regression issues required our focused attention. As a result, some previously planned items were temporarily put on hold. However, we remain committed to delivering on all of our objectives.

Note - We want to create a larger vision for WPF. We are not ready to do it just yet (the team is fairly new). We aim to get the vision document started with community collaboration in ~6 months. 

The following sections highlight the main areas we'll be prioritizing for .NET 8.0 in 2023, including the unfinished items from the previous roadmap. Along these items, we will continue to support updates and maintenance releases of .NET.

# Modernizing WPF

Long-term vision for modernization of WPF contains investments like support for nullability annotations, trimming and NativeAOT support, DirectX upgrades and integration of newer .NET features and abstractions. In the short-term, we have shortlisted the below items for 2023.  

The look and feel of WPF controls has not changed in years. We believe that updating our styles to match those used in Windows 11 will help WPF developers create more consistent Windows experiences.


| Goal | Description | Rationale |
| ------------- |:--------------|:-------------|
| Windows 11 Theming | Bringing Windows 11 look and feel for majority of WPF controls. <br/> Support for Win11 features such as snap layout, rounded corners for controls and newer color schemes would bring enhanced experience for WPF applications. <br/> We will iterate on full scope of the work with WPF community. | For all consumer applications that are built on WPF running on Win11, this feature would ensure that applications can take advantage of modern design elements and behaviors. | 
| Newer controls | [WPF FolderBrowserDialog](https://github.com/dotnet/wpf/issues/438) - Introducing native support for FolderBrowserDialog for WPF | This has been a top ask from the community since .NET Core 3. This feature would reduce dependency on WinForms and other third-party alternatives. |
| Nullability annotations | Enable nullability annotations in WPF | This increases the quality of the code base, as well as the quality for all WPF apps consuming it and reduces time spent debugging `ArgumentNullException`s and `NullReferenceException`s. <br/>Rest of the dotnet (eg. winforms) repo is already moving in the direction and this goal would bring WPF to latest standards as well. <br/> We welcome any community contributions in this area.  |


We do not believe we will be able to deliver all 3 items above. Therefore we want to ask community to help us prioritize these items for .NET 8.0 timeframe -- please go vote [HERE](https://github.com/dotnet/wpf/discussions/7555).

---

# Infrastructure Upgrades

In order to expedite acceptance/turnaround of community PRs, we are working to improve our CI/CD pipeline(s) that can run more functional tests on each PR (along with the existing DRTs) and share results to the developer in automated fashion. This will prevent WPF team from being the bottleneck (when running tests manually for community PRs) and will enable us to focus more on top community pain points. 

_This item is a pre-requiste for us to efficiently deliver on [Modernizing WPF](#modernizing-wpf)._

| Goal | Description | Rationale |
| ------------- |:--------------|:-------------|
| Test Automation | Enable tests (Microsuites & Feature tests) on each PR, including detailed results. | Better reporting, predictability and reduced turnaround time for PRs. | 
| Test Migration | [Leftover from 2022] <br/> Move Microsuites & Feature tests into [wpf-test](https://github.com/dotnet/wpf-test) repo. | More tests is the best way we can ensure that we are not introducing unintended regressions in the product. <br/> Due to the high number of tests that are coupled to infrastructure dependencies, we plan to first move the tests that are decoupled from the infrastructure. Next, we prioritize tests that deliver higher impact (tests that assert critical behaviors) and move them to open-source.
---

# Open Issues / PRs

The current backlog of issues and PRs in WPF stands in higher numbers and we intend to reduce this by addressing high priority issues and impactful PRs which improve the quality of the product and encourages community involvement. Similar to previous effort where we asked community to [help us prioritize issues and PRs](https://github.com/dotnet/wpf/discussions/6556). We intend to continue such initative and seek input on prioritization of issues/PRs from community.

| Goal | Description | Rationale |
| ------------- |:--------------|:-------------|
| Addressing high priority issues | Reduce the total number of issues/PRs. <br/> Use community feedback for prioritization. | Improving quality of WPF as a product. <br/> Enabling better developer experience for WPF repo contributors. |
---


# Others/Fundamentals

With increasing numbers of ARM64 devices, we believe that enhancing the performance of WPF apps and fixing issues related to WPF rendering on ARM64 devices will deliver great customer value. Also, accessibility is one of our primary pillars where we would like to deliver improved support for our specially abled users. 

| Goal | Description | Rationale |
| ------------- |:--------------|:-------------|
| Accessibility Improvements | Improving accessibility support for WPF controls | This enables WPF applications to be more inclusive & would be able to better serve needs of specially abled users. This involves addressing high priority accessibility bugs. |
| ARM64 Performance | Benchmarking and optimizing WPF for better support on ARM64 devices | WPF on ARM64 requires more investment on product and tests. Currently, WPF on ARM64 relies on emulation techniques. We intend to bridge the gap between performance of WPF controls as compared to that of other frameworks. 

Note - Some servicing issues (eg. Security issues) and high priority issues are not tracked in GitHub.

---
# SLA

We will continue to adhere to 3 business days timeline for first triage/response to new issues and non-code PRs, including documentation enhancements or general questions not related to source code issues, that are filed on GitHub. We will prioritize these issues according to their urgency and our available resources. We also evaluate code PRs based on factors such as recency, complexity, and test coverage and aim to merge 5-6 community contributions per month after testing. 

---
# Feedback welcome

We understand the excitement and passion surrounding the WPF community's desire for new features such as hyphen ligatures, SVG support, and colorful emoji, as well as fixing long standing bugs and issues. While we strive to bring these to life, we had to prioritize above issues, considering available resources, existing test coverage, feature complexity, and compatibility risks. 

Our aim is to deliver valuable updates to the WPF product, and we welcome and appreciate any support from the community in achieving these aspirations. 
