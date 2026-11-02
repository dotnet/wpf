# WPF issue triage

This directory defines the policy for a planned issue triage bot. It does not
install a workflow or change issues. Area labeling, priority labeling, and project
updates will be introduced separately, starting with read-only previews.

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

## Intended operation

Automatic assessment will cover issue opens, edits, reopens, and author
follow-ups. Manual dispatch will support previews and resumable backlog
assessment. Automatic publication remains disabled until its rollout checks pass.

Analysis will receive bounded issue evidence and return structured output. Issue
text, comments, code samples, and links are untrusted evidence, not instructions.
The model must not execute samples, fetch arbitrary links, or write issues.

A separate publisher must require successful threat detection, validate the
output against trusted policy and existing labels, and recheck the issue before
an additive write. Stale, incomplete, or invalid results must not authorize writes.
Ambiguous mutation results require inspection rather than blind retries.

Versioned Actions artifacts will hold public-issue assessments and resumable
checkpoints with explicit retention. Expired artifacts or incomplete coverage
require read-only repair before board publication resumes.

## Private top-10 project

The planned project will have private visibility and a selected-issues view
containing at most 10 open, human-created bugs. Private maintainer controls will
pin or exclude issues; pins count toward the limit. More than 10 eligible pins
requires maintainer resolution.

Selection will use eligible pins, authoritative or approved priority,
evidence-backed impact, compatibility/regression, issue-body thumbs-up reactions,
and issue number as a stable final tie-breaker. Unprioritized bugs may fill
remaining places without receiving P2/P3 labels.

A separate GitHub Action will refresh the project daily and on manual dispatch,
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

Rollout starts with offline fixtures and read-only previews. Priority publication
requires a reviewed complete backlog assessment. Area publication and project
publication each require an accepted narrow pilot before automatic activation.
Each capability must remain independently disableable.

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
