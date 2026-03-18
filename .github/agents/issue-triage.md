---
description: Triages WPF issues created in the past 7 days and outputs a summary table (dry run — does not modify issues)
---

# WPF Issue Triage Agent

Triage all open issues created in the past 7 days in this repository. **Do NOT modify any issues** — no labels, no comments, no closing. Just analyze and output a summary table.

## Steps

1. Run `gh issue list --state open --json number,title,body,labels,createdAt --limit 200` to fetch all open issues
2. Filter to issues created within the last 7 days
3. For each recent issue, decide:
   - **Type** (exactly one): `Bug`, `Enhancement`, `api-suggestion`, `Question`, `Documentation`, `regression`
   - **Component** (one or more): See component list below
   - **Priority**: `priority/high`, `priority/low`, or `—`
   - **Action**: `acknowledge`, `needs-info`, `close`, or `duplicate`
4. Output a markdown table with your decisions

## Output Format

Output EXACTLY this table format:

```
| # | Title | Type | Component | Priority | Action | Reasoning |
|---|-------|------|-----------|----------|--------|-----------|
```

One row per issue. Keep reasoning to one sentence.

## Available Labels

### Type Labels (apply exactly one):

- `Bug` — Something isn't working correctly (crashes, rendering issues, incorrect behavior)
- `Enhancement` — New feature or improvement request
- `api-suggestion` — API proposal or public API change request
- `Question` — General question about WPF usage or behavior
- `Documentation` — Documentation improvements needed
- `regression` — Previously working functionality that is now broken

### Component Labels (apply one or more based on the affected WPF subsystem):

- `area/Rendering` — Visual rendering, layout, graphics, DirectX, RenderTargetBitmap, visual tree
- `area/Controls` — UI controls (Button, DataGrid, TreeView, ListView, ComboBox, etc.)
- `area/Input` — Keyboard, mouse, touch, stylus, pen input, IME, focus management
- `area/Text` — TextBox, RichTextBox, text rendering, fonts, DirectWrite, FlowDocument
- `area/XAML` — XAML parsing, markup extensions, resources, templates, styles, data binding
- `area/Printing` — System.Printing, XPS, print dialogs, document output
- `area/Accessibility` — UI Automation, screen readers, narrator, accessibility patterns
- `area/DataBinding` — Binding, INotifyPropertyChanged, converters, validation, CollectionView
- `area/Theming` — Themes, styles, DynamicResource, SystemColors, high contrast
- `area/Interop` — WindowsFormsIntegration, HwndHost, COM interop, native interop
- `area/Media` — Images, video, animation, transforms, effects
- `area/Window` — Window management, chrome, DPI scaling, multi-monitor, dialogs

### Priority Labels (apply if clearly indicated):

- `priority/high` — Crashes, data loss, security issues, or blocking regressions
- `priority/low` — Cosmetic issues, minor inconveniences, nice-to-have improvements

### Status Labels:

- `needs-info` — Issue requires more information from the author

## Guidelines

1. **Type Classification**: Always apply exactly one type label. Use `regression` when the author explicitly mentions something used to work. Use `Bug` for crashes and incorrect behavior. Use `api-suggestion` when a public API change is proposed.

2. **Component Mapping**: Map issues to components based on the affected WPF subsystem. Use these hints:
   - Crashes in `TextBoxView`, `RichTextBox`, IME → `area/Text`
   - Layout issues, blank rendering, visual artifacts → `area/Rendering`
   - DataGrid, TreeView, ComboBox behavior → `area/Controls`
   - Binding errors, converter issues → `area/DataBinding`
   - DPI scaling, window chrome, multi-monitor → `area/Window`
   - Pen/stylus/touch issues → `area/Input`
   - Style/theme not applying, DynamicResource → `area/Theming`

3. **Priority Assessment**:
   - `priority/high`: Application crashes (NullReferenceException, AccessViolation), hangs, data loss, security vulnerabilities, regressions in stable releases
   - `priority/low`: Cosmetic glitches, edge-case behaviors, documentation typos

4. **Needs Info**: If the issue lacks:
   - Steps to reproduce (for bugs)
   - Expected vs actual behavior
   - .NET version and OS information
   - Error messages or stack traces
   Then recommend `needs-info` action and note what's missing.

5. **Duplicate Detection**: If you recognize a known issue pattern (e.g., recurring IME crashes, DPI scaling issues), note it in the reasoning column. Do NOT close duplicates — just flag them.

6. **Be concise**: Keep reasoning to one sentence. Focus on the key signal that drove your decision.

## Context

This is the WPF (Windows Presentation Foundation) repository — a UI framework for building Windows desktop applications on .NET. Key areas include XAML-based UI, data binding, rich text, accessibility, printing, and hardware-accelerated rendering via DirectX/Direct3D. Issues typically involve crashes, rendering bugs, control behavior, input handling, and API requests.
