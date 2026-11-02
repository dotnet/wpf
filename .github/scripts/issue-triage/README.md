# WPF issue triage

These scripts and workflows assess human-created WPF issues, publish guarded
area/P0/P1 labels, and maintain a private top-10 bug shortlist. Automatic analysis
and publication stay off until maintainers configure the controls below.

## Scope

The bot will assess open, unlocked, human-created WPF issues, including reports
without labels. Bot-authored issues, pull requests, and unidentified authors must
be rejected before analysis and checked again before publication. A human
dispatching a bot-authored issue does not make it eligible.

The permitted issue changes are:

- Add one existing primary-area label when the evidence is clear and no primary
  area is already assigned.
- Add `Priority:0` or `Priority:1` under the [priority policy](priority-policy.md),
  after backlog calibration is reviewed and approved.

Existing labels and maintainer decisions, including label removals, take
precedence. Uncertain assessments leave the affected labels unchanged. The bot
must not create labels or add `Priority:2` or `Priority:3`.

The bot will not post issue comments, change issue Type, assign people, change
milestones, close issues, request reproductions, suggest duplicates, or add team,
feedback, or stale labels. Existing issue-management workflows remain unchanged.

## Operation

Automatic assessment covers issue opens, edits, reopens, and author
follow-ups. Manual dispatch supports previews and resumable backlog
assessment. Automatic publication remains disabled until its rollout checks pass.

Analysis receives bounded issue evidence and returns structured output. Issue
text, comments, code samples, and links are untrusted evidence, not instructions.
The model must not execute samples, fetch arbitrary links, or write issues.

A separate publisher requires successful threat detection, validates the
output against trusted policy and existing labels, and rechecks the issue before
an additive write. Stale, incomplete, or invalid results must not authorize writes.
Ambiguous mutation results require inspection rather than blind retries.

Versioned Actions artifacts hold public-issue assessments and resumable
checkpoints with explicit retention. Expired artifacts or incomplete coverage
require read-only repair before board publication resumes.

The board and priority publisher resolve a reviewed baseline plus trusted
assessment/publication deltas from successful `main` runs. Every eligible issue
needs current coverage under the same policy and runtime release metadata.
Artifacts expire after 90 days; publishing labels does not renew assessment age.
The manual board workflow defaults to preview and uploads no project data.

## Private top-10 project

The project must have private visibility and a selected-issues view
containing at most 10 open, human-created bugs. Private maintainer controls will
pin or exclude issues; pins count toward the limit. More than 10 eligible pins
requires maintainer resolution.

Selection uses eligible pins, authoritative or approved priority,
evidence-backed impact, compatibility/regression, issue-body thumbs-up reactions,
and issue number as a stable final tie-breaker. Unprioritized bugs may fill
remaining places without receiving P2/P3 labels.

A separate GitHub Action refreshes the project daily and on manual dispatch,
using current assessments without rerunning the model. It must preserve human
controls and stop without board writes when assessment coverage, access, private
visibility, or project schema cannot be verified.

Private controls, selections, ranks, reasons, and project API payloads must not
appear in public Actions logs, summaries, or artifacts. A private project does
not make workflow output in this public repository private.

## Setup and rollout prerequisites

Before live analysis, maintainers must confirm organization support for the
agentic workflow, Copilot billing, approved action/runtime pins, artifact
retention, and a protected approval mechanism. A backlog run needs an explicit
aggregate analysis budget as well as per-run limits.

Project setup requires an authorized maintainer to create the private project,
configure its fields and selected view, and grant the required team/app access.
Use a project-scoped GitHub App credential or an approved project token only in
the trusted project job. Do not expose it to analysis, detection, or PR jobs.
Issue publication uses a separate job-scoped `GITHUB_TOKEN`.

The private project needs these exact fields: `Selection control` (single select:
Auto, Pin, Exclude), `Selected` (single select: Yes, No), `Rank` (number),
`Priority` (single select: P0, P1), and `Selection reason` (text). Configure the
default view to filter `Selected: Yes` and sort by Rank. Keep the project number
and approved project credential in `WPF_TRIAGE_PROJECT_NUMBER` and
`WPF_TRIAGE_PROJECT_TOKEN` secrets, available only to the project job.

Rollout starts with offline fixtures and read-only previews. Priority publication
requires a reviewed complete backlog assessment. Area publication and project
publication each require an accepted narrow pilot before automatic activation.
Each capability must remain independently disableable.

Automatic analysis requires `WPF_TRIAGE_ANALYSIS_ENABLED=true`. Automatic area
publication additionally requires both `WPF_TRIAGE_AREA_ENABLED=true` and
`WPF_TRIAGE_AREA_PILOT_APPROVED=true`. Leave pilot acceptance unset until the
manual label pilot is verified. Manual publication still requires an explicit
publish request and the corresponding capability control. Priority publication
requires calibration even during a manual pilot.

Automatic priority writes require `WPF_TRIAGE_PRIORITY_ENABLED=true`,
`WPF_TRIAGE_PRIORITY_PILOT_APPROVED=true`, and `WPF_TRIAGE_APPROVAL_RUN` set to the
successful protected calibration run. The publisher checks complete current
coverage and the 20/50 limits before writing. A changed policy invalidates approval.

Manual board writes require `WPF_TRIAGE_BOARD_ENABLED=true` and an explicit
publish request. Daily publication at 04:23 UTC additionally requires
`WPF_TRIAGE_BOARD_PILOT_APPROVED=true`. The board uses the same approval run and
requires current coverage, configured private project secrets, and valid schema.
Unset or false controls disable the corresponding automatic capability.

Merge the stacked PRs in sequence, retargeting the next PR to `main` after its
predecessor merges. Configure setup, calibration, and pilot acceptance on the
final policy version; do not activate intermediate branches.

## Pilot and recovery

Offline fixtures cover assessment, additive area publication, refreshed coverage,
and private selection. They do not prove live billing, organization permissions,
or persistent project updates. This implementation is fixed to `dotnet/wpf`;
testing against a different repository requires a reviewed identity change.

Before a live pilot, obtain approval for its issue list and analysis budget.
Run a single manual assessment and inspect the validated artifact. Preview the
publisher, then explicitly authorize one area-only manual write and verify the
issue labels and absence of comments. Keep automatic publication off.

Inventory the backlog with batch size zero, then assess bounded batches under an
explicit cumulative credit budget. Resume from trusted runs; inspect error
request IDs before opting into retries. Review all P0/P1 proposals and deferred
samples, then approve the complete manifest through `wpf-triage-priority`.
Counts above 20 P0 or 50 P1 require policy review and reassessment.

Configure the private project and preview its refresh before an authorized
manual write. Verify the selected view, ordering, pin/exclude behavior, and
persistent read-back privately. Record pilot acceptance before automatic writes.
Do not post project selections or controls in public logs, artifacts, or PRs.

For an emergency stop, disable the analysis, area, priority, and board controls
and cancel active runs. Inspect ambiguous or partial writes before rerunning a
fresh preview. A resumed reconciliation preserves overrides and repairs managed
fields; it never retries an uncertain mutation automatically. Missing coverage
requires a read-only repair pass. An expired baseline requires fresh calibration.
Remove only confirmed bot-owned additions through an approved manual recovery.

## Area configuration and tests

`area-map.json` lists permitted primary-area labels and their routing guidance.
Validate every configured name against the current repository label catalog
before analysis. Names are case-sensitive; an unclear or unmapped area must
remain unlabelled.

The configuration loader and tests use Python 3.12 and the standard library.
Run from the repository root:

```powershell
python -m unittest discover -s .github\scripts\issue-triage\tests -v
```
