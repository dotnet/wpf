# Pull Request Acceptance Criteria

Thank you for contributing to **WPF for .NET Core!**  We ask that before you start work on a feature that you would like to contribute, please file an issue describing your proposed change: We will be happy to work with you to figure out the best approach, provide guidance and mentorship throughout feature development, and help avoid any wasted or duplicate effort.

#### What types of pull requests will be accepted before WPF for .NET Core General Availability in September?

WPF for .NET Core is seeking functional and performance parity with .NET Framework to offer customers a direct porting path from .NET Framework. Given this, only the following will be considered:

*	Bug fixes that are not reproducible on .NET Framework
*	IL-neutral formatting changes done by Roslyn analyzers and automatic code fixers
*	Performance fixes that bring .NET Core to performance parity with .NET Framework

All other changes will be tagged as 'future' and postponed until after .NET Core 3.0 GA in September.

**Please see the chart below for more details.**

| Category | Description | Accepted pre-GA |
| --- | --- | :---: |
| .NET Core code defect | .NET Core bugs that *are not* reproducible on .NET Framework will be accepted pre-GA. | :heavy_check_mark: |
| .NET Framework code defect | Fixes for .NET Core bugs that are **also** reproducible on .NET Framework will not be accepted.  (If this was a recent regression in .NET Framework 4.8, or this blocks a core scenario, then we will likely consider bugs in this category). | :x: |
| .NET Core API change | An **enhancement** of an existing .NET Core API will not be accepted pre-GA. (We are also seeking to minimize deprecation and removal of APIs, unless they are unsupported by .NET core.)| :x: |
| New API (feature addition, public API) | New features and public APIs will not be considered until after .NET Core 3.0 GA. | :x: |
| Performance Optimization | We would like to achieve performance parity with .NET Framework.  Fixes for performance regressions in .NET Core will be accepted, and other performance optimizations will be considered. | :heavy_check_mark: |
| Automated code formatting change | Automated IL-neutral changes with an associated Roslyn analyzer or editor config update will be accepted.  Automated changes that result in differences to the IL will be individually considered. | :heavy_check_mark: |
| Manual code formatting change | Manual code formatting changes without an associated Roslyn analyzer or editor config update will not be accepted pre-GA. | :x: |

### General Guidelines

> 👉 **Remember\!** Your contributions may be incorporated into future versions of WPF\! Because of this, all pull requests will be subject to the same level of scrutiny for quality, coding standards, performance, globalization, accessibility, and compatibility as those of our internal contributors.


* **DO** create an issue before creating a pull request.
* **DO** create one pull request per Issue, and ensure that the Issue is linked in the pull request.
* **DO** follow our [coding and style guidelines](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md), and keep code changes as small as possible.
* **DO** check for additional occurrences of the same problem in other parts of the codebase before submitting your PR.
* **DO** link the issue you are addressing in the pull request.
* **DO** write a good description for your pull request. More detail is better. Describe why the change is being made and why you have chosen a particular solution. Describe any manual testing you performed to validate your change.
* **DO** take an IL diff for code formatting changes and include IL-neutral or not IL-neutral in the PR.
* **DO NOT** submit a PR unless it is linked to an Issue marked triage approved. This enables us to have a discussion on the idea before anyone invests time in an implementation.
* **DO NOT** merge multiple changes into one PR unless they have the same root cause.
* **DO NOT** submit pure formatting/typo changes to code that has not been modified otherwise.



# Pull Request Process

### All pull requests must have an issue

1. If any guidance is required, please read the [contribution guidelines](contributing.md) and/or submit an issue requesting clarification. 

2. Review the pre-GA acceptance table and open a new issue.  

3. The issue will be triaged by someone from the WPF team.

4. If the issue meets the pre-GA acceptance criteria, the contributor will be assigned a WPF developer to help with the submission process.  The issue will be assigned as a work item to the WPF developer.

5. The issue will be discussed with community members and WPF developers to understand the problem and review possible solutions. 

6. When the a solution has been agreed upon, implement and **validate the change locally**.
    > Please follow the testing requirements [Developer Guide](developer-guide.md).
    
    > Verify your change works. Create and test the updated feature area locally with a WPF test application compiled against a version of the WPF Framework that contains your changes.  See the [Developer Guide](developer-guide.md) for instructions on testing against local builds.
    
7. When the code is completed and has been verified locally, the contributor should submit a pull request that references the associated issue.  Please complete the Contributor License Agreement if required.


8. GitHub checks will run against each new commit
    - *GitHub Checks*
    
        - License Check
        - Style and Formatting
        - Commit Message
        - PR Build with Roslyn analyzers enabled
    
9. When the PR has been submitted and all GitHub checks pass (no build breaks or other issues) community members and WPF developers will review the code.

10. Additional internal testing may be performed by WPF developers if the change is determined to be high risk.

11. Community members and WPF developers need to review the change and sign-off on the pull request before the PR can be merged.

12. After the PR has been signed off, a WPF developer will manually squash and merge the PR.

#### At this point, the change is in master.  Further internal testing will be performed.  If there are no regressions, the change will be included in the next milestone release. 

13. The internal developer regression test loop will be run against a build that includes the merged changes from the PR.

14. If a test fails, the squash-merge commit will be undone, and the WPF developer assigned to the issue will work with the submitter to root-cause and fix the regression. A new PR will need to be created by the submitter with the fixed code.

15. If the internal DRT test loop succeeds, the internal feature test loop will run.  If this fails, the assigned WPF developer will work with the submitter to fix the regression and to submit a new PR with fixed code.

16.  Finally, pre-milestone release, a full internal test pass is run.  This test pass contains tens of thousands of tests across a large number of operating systems and machine configurations.  If everything succeeds, the change (along with any others) will remain in master to be included in the next milestone release.

17. The repo is 'snapped' to a milestone release containing the PR.



### Would you like to contribute new features after September?  

Please open an issue describing what feature area you would like to work on.  We will begin assigning community owners and WPF developer peers some time in the future.  Thanks!
