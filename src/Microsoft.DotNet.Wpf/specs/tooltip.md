Tooltip behavior
==
This document describes the behavior of tooltips, 
especially the changes needed to meet the accessibility requirements of WCAG 2.1.


# Background

Microsoft is commmitted to making its products and UI platforms meet the accessibility requirements of WCAG 2.1 in a uniform way.
This document describes how WPF's tooltips will honor that commitment.

The document includes the following sections:
- **Requirements**.  Links to sources of information that drove the decisions described here.
- **Applicability**.  How the new behavior will be delivered.
- **Conceptual pages**.  Specifies tooltip behavior.  The emphasis is on new behavior, but old behavior is included when helpful for context.  This is the public-facing spec, as it might appear in "overview" or "how-to" pages on docs.microsoft.com. 
- **API Pages**.  Public-facing spec for new APIs, as it might appear in "reference" pages on docs.microsoft.com.
- **Implementation**.  Internal-facing spec - details needed by implementers, testers, and reviewers, but not part of the public documentation.
- **Discussion**.  Remarks that aren't part of the spec, but help understand it.  Including how the spec meets the requirements, goals and non-goals, alternatives that were considered, etc.  
- **Summary of behavior changes**.  Consolidated in one place for convenience.
- **API Details**.  Consolidated in one place for convenience.


# Requirements 
This spec cites the following sources (the [brackets] contain abbreviations used elsewhere in this document):

1. WCAG 2.1, section 1.4.13: [Content on hover or focus](https://www.w3.org/TR/WCAG21/#content-on-hover-or-focus) [WCAG] 
2. WCAG 2.1:  [Understanding 1.4.13](https://www.w3.org/WAI/WCAG21/Understanding/content-on-hover-or-focus.html)   [WCAG]
3. MAS 1.4.13:  [Content on hover or focus](https://microsoft.sharepoint.com/:w:/r/sites/accessibility/_layouts/15/Doc.aspx?sourcedoc=%7B56729308-859e-4b32-ab27-04d4364b3003%7D&action=view&wdLOR=c9C9640FB-F04E-401A-BF46-A79887F322D3&wdAccPdf=0&wdparaid=38854747) [MAS]
4. The [Figma diagram](https://www.figma.com/file/8LeT0Eq7a3ylAyM19c0FJp/ASC%3A-Tooltip?node-id=110%3A0) (as of February 2021) [FD]
5. Email thread (MS internal) [ET] 
6. WPF [Tooltip Overview](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/controls/tooltip-overview?view=netframeworkdesktop-4.8) (and related links) [TO]

The first three describe behavior in general terms, guiding any UI platform (WCAG) or Microsoft UI platforms (MAS).
The Figma diagram has more specific requirements, but is not a complete spec.
The email thread among many relevant MS teams helped fill in omissions, corrected errors, and clarified ambiguities in the diagram.
The Tooltip Overview documents the existing behavior in WPF as of .NET 4.7.2.

# Applicability

All the changes described here are required to appear in .NET 6.0.

The behavior changes will also appear in servicing updates for earlier versions as follows:
- .NET 4.6-4.7.2, .NET 4.8:  the next biannual accessibility update (expected summer 2021)
- .NET Core 3.1, .NET 5.0:  same time frame as above, patch version TBD

The API changes (and behavior that depends on them) are only in .NET 6.0;
servicing updates cannot include changes to public API.

In the servicing updates, the new behavior is opt-in by setting
`Switch.UseLegacyAccessibilityFeatures.5` to false, as described [here](https://docs.microsoft.com/en-us/dotnet/framework/whats-new/whats-new-in-accessibility).



# Conceptual pages (How To)

## Declaring a tooltip
You can declare a tooltip in XAML or code as described in [TO].
Starting in .NET 6.0, your declarations can also use the properties
`ToolTip.ShowsToolTipOnKeyboardFocus`, and
`ToolTipService.ShowsToolTipOnKeyboardFocus`.

## Opening a tooltip
When a parent element declares a tooltip, there are four ways to open (show) the tooltip:

1. Hover the mouse over the parent.
2. Move keyboard focus to the parent.
3. Type a keyboard shortcut.
4. Open the tooltip programmatically.

Unless mentioned otherwise below, the tooltip is shown immediately and its size and position are governed by the placement properties described in [TO].

### Hover
A hover occurs when the mouse enters the parent element and stays there for a certain time, without any other activity.
The tooltip is opened after the hover time, whose value is a function of the `InitialShowDelay` and `BetweenShowDelay` properties, and whether another tooltip is already showing [TO].

### Keyboard focus
When you use keyboard navigation to move focus to the parent element, the tooltip opens.
(Using the mouse to move focus does not show the tooltip.)
You can override this behavior using the `ShowsToolTipOnKeyboardFocus` properties.

### Keyboard shortcut
When the parent has keyboard focus, typing Ctrl+Shift+F10 opens the tooltip.

When the tooltip opens due to either of the keyboard actions, a [PlacementMode](https://docs.microsoft.com/en-us/dotnet/api/system.windows.controls.primitives.placementmode?view=net-5.0) value of `Mouse` or `MousePoint` is replaced by `Bottom`.

### Programmatically
[TODO: Check if this is possible.  If so, describe it]

## Closing a tooltip
A tooltip closes for one of the following reasons.

- **Timeout**. When the tooltip's `ShowDuration` time expires, the tooltip closes.
- **Hover ends**.  When the tooltip opened due to hover, moving the mouse outside a "safe region" closes the tooltip.
The safe region is the union of the parent element, the tooltip, and the mouse buffer zone connecting the two (this is explained further in the next section).
- **Keyboard Shortcut**.  When the parent has keyboard focus, typing Ctrl+Shift+F10 or simply Ctrl (without any other keys) closes the tooltip.
- **Focus loss**.  When the tooltip opened due to keyboard activity, moving keyboard focus away from the parent element closes the tooltip.
- **Editor activity**.  When the parent has a text editor, any editor activity closes the tooltip.  
Elements with editors include `TextBoxBase`, `PasswordBox`, `DocumentViewerBase`, `FlowDocumentScrollViewer`, and classes derived from these.
Editor activity includes mouse clicks or keystrokes on the editing surface, and `UpdateComposition` requests from an IME.
- **Exclusion**.  Opening a tooltip or context menu (for any reason) closes the previously open tooltip, if any.
- **Programmatically**.  [TODO: check if this is possible. If so, describe it.]

## Mouse buffer zone
When you open a tooltip by hovering the mouse over its parent element, you should be able to move the mouse onto the tooltip without closing the tooltip.
To that end, WPF implicitly declares a *mouse buffer zone*: a rectangle that connects the parent element to the tooltip, in which you can move the mouse without closing the tooltip due to hover-end.
(Note that the mouse still interacts in the usual way with elements in the buffer zone, and that interaction might close the tooltip.)

After using the usual placement rules to determine the size and position of the tooltip, WPF calculates the buffer zone to be a rectangle with the following properties:
- it contains the shortest straight line between the parent and its tooltip
- it is large enough to accommodate reasonable drift as you move the mouse along that line
- it is no larger than needed to satisfy the first two properties

In the common case where the tooltip overlaps its parent, the buffer zone may be empty.

It may be impossible or impractical to find a suitable rectangle, in which case the buffer zone is empty and moving the mouse off the parent element will close the tooltip.
This only happens in extreme cases, for example when the tooltip and its parent are on different monitors. 


# API Pages

## ToolTipService.ShowsToolTipOnKeyboardFocus attached property
Gets or sets whether an element shows its tooltip when it acquires keyboard focus.

```
see GetShowsToolTipOnKeyboardFocus, and SetShowsToolTipOnKeyboardFocus
```

### Remarks
Setting this property on a parent element that has a tooltip helps control whether to open the tooltip when the element acquires focus by keyboard navigation.
(Note: when the parent acquires focus by mouse-click or touch, the tooltip is not opened.)
The tooltip is opened according to the following table, where the rows indicate the value of the `ToolTipService.ShowsToolTipOnKeyboardFocus` attached property on the parent element, and the columns indicate the value of the ToolTip element's `ShowsToolTipOnKeyboardFocus` property:

|           | False | null  | True  |
|-----------| ----- | ----  | ----  |
| **False** | False | False | False |
| **null**  | False | True  | True  |
| **True**  | True  | True  | True  |

If there is no ToolTip element, the 'null' column applies.

In other words, honor an explicit value (True or False); if both the parent element and the ToolTip element have explicit values, the parent's value takes precedence;  if neither has an explicit value, open the tooltip.  The latter case is the default.

## ToolTip.ShowsToolTipOnKeyboardFocus property
Gets or sets whether the ToolTip is shown when its parent element acquires keyboard focus.

```
    public bool? ShowsToolTipOnKeyboardFocus { get; set; }
```

### Remarks
Setting this property on ToolTip element helps control whether to open the tooltip when its parent element acquires focus by keyboard navigation.
(Note: when the parent acquires focus by mouse-click or touch, the tooltip is not opened.)
The tooltip is opened according to the following table, where the rows indicate the value of the `ToolTipService.ShowsToolTipOnKeyboardFocus` attached property on the parent element, and the columns indicate the value of the ToolTip element's `ShowsToolTipOnKeyboardFocus` property:

|           | False | null  | True  |
|-----------| ----- | ----  | ----  |
| **False** | False | False | False |
| **null**  | False | True  | True  |
| **True**  | True  | True  | True  |

If there is no ToolTip element, the 'null' column applies.

In other words, honor an explicit value (True or False); if both the parent element and the ToolTip element have explicit values, the parent's value takes precedence;  if neither has an explicit value, open the tooltip.  The latter case is the default.

# Implementation
This section describes details that are not part of the public documentation, but are nevertheless important.

**ShowDuration property**.
This property's default value is not documented.
In previous versions it was 5000 (5 seconds). 
Henceforth it will be `Int32.MaxValue` (~25 days).
When this "infinite" value is specified, the implementation may choose to honor it literally by closing the tool tip after 25 days, or to honor it in spirit by disabling the timer-dependent logic.

**Typing Ctrl to dismiss the tooltip**.
This response is triggered by the KeyUp event, and only if no other keys have been pressed since the KeyDown event.

**Mouse buffer zone**.
The public spec does not specify the exact size and position of the buffer zone;  this is intentional, so that users and apps don't take an inappropriate dependency.
The implementation can meet the spec's vague requirements as follows
(where D denotes a fixed number representing the allowable "drift" mentioned in the spec).

Consider the rectangles P and T corresponding respectively to the parent element and its tooltip as configured by the [standard placement logic](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-position-a-tooltip?view=netframeworkdesktop-4.8).
We are to determine a rectangle B for the buffer zone.
- If P and T intersect, no buffer is needed;  set B to empty.
- If P and T are an extreme case, set B to empty.
The implementation can define "extreme case" any way it likes, provided that the default values of the placement properties, or small variations lying within the guidelines set by WCAG and MAS, do not produce an "extreme case".
- Extend the four sides of P.
This divides the plane into 9 regions, like a tic-tac-toe board.
There are four corner regions, four side regions, and one center region (P itself).
T does not intersect the center region (that case was handled earlier), so it either lies entirely within one of the corner regions, or intersects exactly one of the side regions.
- If T lies within a corner region, there is a unique shortest straight line connecting P and T, running from a vertex of P to a vertex of T.
Form the bounding box of those two vertices, expand it by D pixels in all four directions, and set B to the result.
- If T intersects a side region, there are many shortest straight lines connecting P and T, each running vertically (resp. horizontally) from a point on an edge of P to a point on an edge of T.
Find the leftmost and rightmost (resp. topmost and bottommost) of these points, and form the bounding box of these four points (two from P, two from T).
Expand this if necessary, to ensure that its width (resp. height) is at least 2D, and set B to the result.

The constant D is intentionally not specified.  The implementation can choose it, bearing in mind the tradeoff for an end-user intending either to move the mouse to the tooltip or to move the mouse away from the parent element.
A larger value helps the former task, a smaller value helps the latter.
Something on the order of 10 (device-independent) pixels seems about right.

# Discussion
This section includes commentary on the spec.

Goals:
- Provide mechanisms for declaring, placing, opening, and closing tooltips.
- Maintain the ways apps can customize or extend these mechanisms.
- An app that uses the default values for the customizing properties meets the requirements of [WCAG] and [MAS].
(This is the "Pit of Success" principle.)

Non-goals:
- Disable or remove any behavior that was present in .NET 4.8.
- Prevent customizations that fail to meet the requirements.

In other words, it's easy for a WPF app to meet the requirements, but possible to fail.
Any such failure is not WPF's fault, but rather the fault of the app for requesting content or customized behavior that doesn't conform.

### Conformance to requirements

Much of [FD] concerns the usage of tooltips and the nature of their content.
This is wholly up to the app - WPF does not restrict the declaration of tooltips or their content.

[FD] says "each tooltip is a tab".
This does not mean tooltips should appear in the tab-order or be focusable - they shouldn't [ET].

[FD] mentions the "mouse buffer zone", but doesn't define its behavior.
The clarification that it inhibits dismissal due to mouse-away comes from [ET];  this is clearly motivated by the "hoverable" requirement of [WCAG] and [MAS].

[FD] does not specify the size and position of the buffer zone, beyond describing its width in a particular case.
[ET] has several suggestions, including "the union of P and T" (meaning the minimum rectangle bounding both P and T), which was deemed too large.
The definition used here, and the suggested implementation, are original in this spec.

[FD] does not specify the disposition of mouse events while moving through the buffer zone.
[ET] discussed whether to deliver them to underlying elements or swallow them.
Delivering them means the underlying elements will react in their normal arbitrary (i.e. app-defined) way, which could include actions that close the tooltip.
Swallowing them means breaking the fundamental design principle that "things behave the same whether a tooltip is showing or not".
Ultimately, we do not know the user's intent - move to the tooltip vs. interact with an underlying control - so we choose to deliver the events. 
This may defeat the user's intent to move to the tooltip, but only in rare cases;
the buffer zone is often empty, and elements rarely react to mouse-move events in any impactful way.

[FD] does not say what happens to the buffer zone if the layout changes (e.g. scrolling).
The spec says how to compute it when the tooltip opens, tacitly implying that it doesn't change if the parent or tooltip subsequently move.
This could leave the buffer zone dangling, but it's an unlikely case and not worth the effort to handle.

[FD] says "Do not use a timeout to hide the tooltip."
WPF will do this by default (or perhaps use a timeout of 25 days), but an app can request a timeout by setting `ShowDuration`.

[FD] says "Once a tooltip is dismissed, it should not re-appear on subsequent hovers", but does not define "subsequent hover".
Raising that question on [ET] produced no direct response, but rather the assertion that [FD] is wrong.
Thus this spec proscribes no changes.
WPF maintains the 4.8 behavior, which applies the rule only to tooltips dismissed by timeout;
this becomes moot with the change to infinite timeouts.

[FD] says "User tabs to an element with a tooltip.  The tooltip appears on focus."
This spec extends "user tabs" to any keyboard navigation, so that arrow keys, page-up, et al. have the same effect as tab.
This behavior was already added in 4.8.

[FD] says (or rather shows) that Tab and Ctrl-Shift-F10 both open the tooltip.
[ET] clarifies that "Tab" here means "tab-navigation to the parent element", rather than "typing TAB while the parent element has keyboard focus".

On the same subject, [ET] also says that the two keyboard actions should be labeled as optional alternatives for meeting the [WCAG] requirement.
The tab-navigation rule is intended for tooltips that follow the [FD] guidelines for tooltip usage and content, but apps that put lengthy or redundant text in tooltips may find the rule "too distracting".
They can use the Ctrl-Shift-F10 rule instead, to avoid the distraction while still giving keyboard-only users a way to open the tooltip.
Several legacy apps take advantage of this option, notably Office.

A framework like WPF should leave this option up to the app,
mirroring the app's choice of tooltip usage and content.
WPF 4.8 mistakenly baked in the option:  the tab-navigation rule applies unless the tooltip derives from RibbonToolTip. 
The ``ShowsToolTipOnKeyboardFocus`` properties return the option to the app.
Unfortunately this only helps in .NET 6.0, as servicing updates to older versions cannot include new public API.

[FD] says (shows) that Ctrl closes the tooltip.
[ET] discussed what that really means - whether to act on KeyDown or KeyUp, whether combinations should cause both the combination action and dismiss the tooltip (e.g. should Ctrl+C both copy text and dismiss the tooltip), etc.
The interpretation given here is the consensus.


# Summary of behavior changes

For convenience, here are the behavior changes described in detail above.

1. Change default value for `ToolTipService.ShowDuration` property from 5000 to `Int32.MaxValue`.
2. Moving the mouse within the buffer zone does not close the tooltip.
3. (6.0 only) Properties `ToolTip.ShowsToolTipOnKeyboardFocus` and `TooltipService.ShowsToolTipOnKeyboardFocus` control whether acquiring keyboard focus shows the tooltip.
4. Ctrl closes the tooltip.

Also, some changes were previously made in .NET 4.8 (and appear in .NET Core 3.0, 3.1, and .NET 5.0), without documentation:

1. Keyboard focus opens the tooltip.  (Except for RibbonToolTip.)
2. Ctrl-Shift-F10 opens or closes the tooltip.

# API Details

```c#
class ToolTipService
{
    public static DependencyProperty ShowsToolTipOnKeyboardFocus;
    public static bool? GetShowsToolTipOnKeyboardFocus(DependencyObject d);
    public static void SetShowsToolTipOnKeyboardFocus(DependencyObject d, bool? value);
}

class ToolTip
{
    public bool? ShowsToolTipOnKeyboardFocus { get; set; }
}

```
