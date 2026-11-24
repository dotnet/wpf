# WPF triage priority policy

This policy defines candidate priorities for the planned triage bot. It does not
authorize publication before a complete backlog assessment has been reviewed.
Priority labels are triage signals, not a promise to ship a fix.

## Candidate decisions

Only supported bug assessments may receive an automatic high-priority label in
this rollout. Other issue kinds may still receive area assessments.

| Result | Required evidence |
|---|---|
| `Priority:0` candidate | An actual process crash or a demonstrated latest-stable regression whose reported effect warrants release-blocking attention. |
| `Priority:1` candidate | Broken functionality with evidence of a critical scenario impact, assessed together with workaround availability. |
| No priority addition | Lower-impact issues, including P2/P3 assessments. |
| Defer | Unclear impact, conflicting evidence, incomplete context, or an unsupported high-priority claim. |

These are starting criteria for read-only calibration. A crash keyword, caught
exception, regression label, requested priority, or generic claim of importance
is insufficient. Record the reported failing operation, affected versions,
scenario reach, and impact rationale.

A latest-stable regression needs evidence that previously working behavior
stopped working in a version matched against fixed, trusted .NET release
metadata. An unqualified "latest", a main-branch build, or the WPF repository's
latest GitHub release does not establish that match.

A workaround must bypass the same failure. A proposed product fix, failed
attempt, or unrelated sample is not a workaround. No mentioned workaround means
the report supplies none; it does not prove that none exists. A viable workaround,
narrow configuration, or unsupported runtime can reduce confidence or defer
priority. Truncated evidence cannot authorize a P0/P1 addition.

The initial automatic rule requires HIGH confidence, a broad or core scenario,
and no reported workaround. A viable or uncertain workaround defers priority.
The affected runtime must match a supported channel's current stable patch from
trusted metadata. P0 additionally requires the latest stable runtime across those
channels; older supported-channel crashes or regressions can be P1 candidates.

## Calibration before publication

1. Enumerate the full open issue backlog with pagination, excluding pull requests,
   bots, and other ineligible issues. Do not use a search result set limited to
   1,000 issues as the inventory.
2. Assess every eligible human-created issue read-only, including unlabeled
   reports. Record completed, skipped, deferred, failed, and stale assessments
   separately. Failed or missing assessments leave coverage incomplete.
3. Count projected P0/P1 totals across open eligible issues, including existing
   authoritative labels without double counting. Preserve existing P2/P3 labels
   rather than proposing an additional priority.
4. Review every proposed P0/P1 rationale and a representative lower-priority or
   deferred sample for false positives and missed impact.
5. Require projected totals of at most **20 P0** and **50 P1** before approving
   automatic priority publication.
6. Bind approval to the complete assessment manifest and exact policy version.
   Reassess affected issues after criteria change; an unreviewed policy change
   invalidates approval.

The limits are publication guardrails, not quotas. If either projected total
exceeds its limit, stop priority publication and review the criteria. Tighten
them using affected-version evidence, actual process termination, scenario
impact, demonstrated regressions, and viable workarounds. Do not downgrade real
blockers or remove existing labels merely to meet a count.

## Publication constraints

Preserve any existing priority label, including P2/P3. Conflicting priorities or
maintainer removals require deferral rather than replacement or reapplication.
Only `Priority:0` and `Priority:1` may be added; never add P2/P3 labels or comments.

Before an addition, verify the approved policy and assessment, current issue
evidence, and projected live totals. Serialize bot priority writes across the
repository and read back mutations. Stop on stale input, incomplete coverage,
quota overflow, or ambiguous writes.

GitHub cannot make these checks atomic with a human edit. Recheck immediately
before publication; a detected race or resulting overflow stops further
automatic priority writes and requires maintainer inspection.
