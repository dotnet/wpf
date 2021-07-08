Automation update
==

# Background

WPF exposes properties and events that wrap the features provided by the OS automation component `uiautomationcore.dll` (UIA).
This spec covers features added to UIA since RS2.

# Conceptual pages (How To)

_(This is conceptual documentation that will go to docs.microsoft.com "how to" page)_

<!-- 
All APIs have a page on DMC, some APIs or groups of APIs have an additional high level,
conceptual page (internally called a "how-to" page). This section can be used for that content.

For example, there are several Xaml controls for different forms of text input,
and then there's also a conceptual pages that discusses them collectively
(https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/text-controls)

Another way to use this section is as a draft of a blog post that introduces the new feature.

Sometimes it's difficult to decide if text belons on a how-to page or an API page.
It's not important to decide on that here, we can always adjust it when copying to DMC.
-->

# API Pages

## AutomationNotificationKind Enum
Indicates the type of notification when calling [RaiseNotificationEvent]()

```c#
public enum AutomationNotificationKind
{
    ItemAdded, 
    ItemRemoved, 
    ActionCompleted, 
    ActionAborted, 
    Other
}
```

### **Fields**

|||| 
|-|-|-|
| ActionAborted | 3 | The current element has a notification that an action was abandoned. |
| ActionCompleted | 2 | The current element has a notification that an action was completed. |
| ItemAdded | 0 | The current element container has had something added to it that should be presented to the user. |
| ItemRemoved | 1 | The current element has had something removed from inside it that should be presented to the user. |
| Other | 4 | The current element has a notification not an add, remove, completed, or aborted action. |


## AutomationNotificationProcessing Enum
Specifies the order in which to process a notification.

```c#
public enum AutomationNotificationProcessing
{
    ImportantAll,
    ImportantMostRecent,
    All,
    MostRecent,
    CurrentThenMostRecent
}
```

### **Fields**

|||| 
|-|-|-|
| All | 2 | These notifications should be presented to the user when possible. All of the notifications from this source should be delivered to the user. |
| CurrentThenMostRecent | 4 | These notifications should be presented to the user when possible. Don’t interrupt the current notification for this one. If new notifications come in from the same source while the current notification is being presented, then keep the most recent and ignore the rest until the current processing is completed. Then use the most recent message as the current message. |
| ImportantAll | 0 | These notifications should be presented to the user as soon as possible. All of the notifications from this source should be delivered to the user. **Warning:** Use this in a limited capacity as this style of message could cause a flooding for information to the end user due to the nature of the request to deliver all of the notifications.|
| ImportantMostRecent | 1 | These notifications should be presented to the user as soon as possible. The most recent notifications from this source should be delivered to the user because the most recent notification supersedes all of the other notifications. |
| MostRecent | 3 | These notifications should be presented to the user when possible. Interrupt the current notification for this one.|


## AutomationPeer.RaiseNotificationEvent(AutomationNotificationKind, AutomationNotificationProcessing, String, String) Method

Initiates a notification event.

```c#
public void RaiseNotificationEvent(AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId);
```

### Parameters

`notificationKind`
[AutomationNotificationKind]()
Specifies the type of the notification.

`notificationProcessing`
[AutomationNotificationProcessing]()
Specifies the order in which to process the notification.

`displayString`
[String]()
A display string describing the event.

`activityId`
[String]()
A unique non-localized string to identify an action or group of actions. Use this to pass additional information to the event handler.


## AutomationHeadingLevel Enum
Defines the heading levels for automation elements.

```c#
public enum AutomationHeadingLevel
{
    None,
    Level1, 
    Level2, 
    Level3, 
    Level4, 
    Level5, 
    Level6, 
    Level7, 
    Level8, 
    Level9 
}
```

### **Fields**

|||| 
|-|-|-|
| Level1 | 1 | Heading level 1. |
| Level2 | 2 | Heading level 2. |
| Level3 | 3 | Heading level 3. |
| Level4 | 4 | Heading level 4. |
| Level5 | 5 | Heading level 5. |
| Level6 | 6 | Heading level 6. |
| Level7 | 7 | Heading level 7. |
| Level8 | 8 | Heading level 8. |
| Level9 | 9| Heading level 9. |
| None | 0 | Not a heading. |


## AutomationProperties.HeadingLevelProperty Field
Identifies the [HeadingLevel]() attached property.

```c#
public static readonly System.Windows.DependencyProperty HeadingLevelProperty;
```

### **Field Value**
[DependencyProperty]()


## AutomationProperties.HeadingLevel Attached Property
Gets or sets a value that indicates the heading level for an element.

```c#
see GetHeadingLevel, and SetHeadingLevel
```

### **Remarks**
Heading elements organize the user interface and make it easier to navigate. Some assistive technology (AT) allows users to quickly jump between headings. Headings have a level from 1 to 9.

## AutomationProperties.GetHeadingLevel(DependencyObject) Method
Gets the [HeadingLevel]() attached property for the specified [DependencyObject]().

```c#
public static System.Windows.Automation.AutomationHeadingLevel GetHeadingLevel(System.Windows.DependencyObject element);
```

### **Parameters**
**element** [DependencyObject]()
The specified [DependencyObject]().

### **Returns**
[AutomationHeadingLevel]()

The value of the [AutomationProperties.HeadingLevel]() property.


## AutomationProperties.SetHeadingLevel(DependencyObject) Method
Sets the [HeadingLevel]() attached property for the specified [DependencyObject]().

```c#
public static void SetHeadingLevel(System.Windows.DependencyObject element, System.Windows.Automation.AutomationHeadingLevel value);
```

### **Parameters**
**element** [DependencyObject]()
The specified [DependencyObject]().

**value**
[AutomationHeadingLevel]()
The value for the heading level.


## AutomationElement.HeadingLevelProperty Field
Identifies the [HeadingLevel]() property

```c#
public static readonly System.Windows.Automation.AutomationProperty HeadingLevelProperty;
```
### **Field Value**
[AutomationProperty]()

### **Remarks**
This identifier is for use by UI Automation client applications. UI Automation providers should use the equivalent field from [AutomationElementIdentifiers]().

## AutomationElementIdentifiers.HeadingLevelProperty Field
Identifies the [HeadingLevel]() property

```c#
public static readonly System.Windows.Automation.AutomationProperty HeadingLevelProperty;
```
### **Field Value**
[AutomationProperty]()

### **Remarks**
This identifier is for use by UI Automation providers. UI Automation client applications should use the equivalent field from [AutomationElement]().


## AutomationProperties.IsDialogProperty Field
Identifies the [IsDialog]() attached property.

```c#
public static readonly System.Windows.DependencyProperty IsDialogProperty;
```

### **Field Value**
[DependencyProperty]()


## AutomationProperties.IsDialog Attached Property
Gets or sets a value that indicates whether an element is a dialog window.

```c#
see GetIsDialog, and SetIsDialog
```

### **Remarks**
In many cases, assistive technology (AT) treats a dialog window differently from other types of windows. A screen reader, for example, typically speaks the title of the dialog, the focused control in the dialog, and then the content of the dialog up to the focused control (for example, "Do you want to save your changes before closing"). For standard windows, a screen reader typically speaks the window title followed by the focused control.

When AutomationProperties.IsDialog is true, a client application should treat the element as a dialog window.

## AutomationProperties.GetIsDialog(DependencyObject) Method
Gets the [IsDialog]() attached property for the specified [DependencyObject]().

```c#
public static bool GetHeadingLevel(System.Windows.DependencyObject element);
```

### **Parameters**
**element** [DependencyObject]()
The specified [DependencyObject]().

### **Returns**
[Boolean]()

The value of the [AutomationProperties.IsDialog]() property.


## AutomationProperties.SetIsDialog(DependencyObject) Method
Sets the [IsDialog]() attached property for the specified [DependencyObject]().

```c#
public static void SetIsDialog(System.Windows.DependencyObject element, bool value);
```

### **Parameters**
**element** [DependencyObject]()
The specified [DependencyObject]().

**value**
[Boolean]()
**true** if the element should be identified as a dialog window; otherwise, **false**.


## AutomationElement.IsDialogProperty Field
Identifies the [IsDialog]() property

```c#
public static readonly System.Windows.Automation.AutomationProperty IsDialogProperty;
```
### **Field Value**
[AutomationProperty]()

### **Remarks**
This identifier is for use by UI Automation client applications. UI Automation providers should use the equivalent field from [AutomationElementIdentifiers]().

## AutomationElementIdentifiers.IsDialogProperty Field
Identifies the [IsDialog]() property

```c#
public static readonly System.Windows.Automation.AutomationProperty IsDialogProperty;
```
### **Field Value**
[AutomationProperty]()

### **Remarks**
This identifier is for use by UI Automation providers. UI Automation client applications should use the equivalent field from [AutomationElement]().


## ContentTextAutomationPeer.RaiseActiveTextPositionChangedEvent(TextPointer, TextPointer) Method

Raises an ActiveTextPositionChanged event.

```c#
public virtual void RaiseActiveTextPositionChangedEvent(TextPointer rangeStart, TextPointer rangeEnd);
```

### **Parameters**
**rangeStart** [TextPointer]()
The start of the range that changed.  
The value must be a valid [TextPointer]() for the peer's owner, or `null` to denote the start of the owner's text content.

**rangeEnd** [TextPointer]()
The end of the range that changed.
The value must be a valid [TextPointer]() for the peer's owner, or `null` to denote the end of the owner's text content.


## TextAutomationPeer.RaiseActiveTextPositionChangedEvent(TextPointer, TextPointer) Method

Raises an ActiveTextPositionChanged event.

```c#
public virtual void RaiseActiveTextPositionChangedEvent(TextPointer rangeStart, TextPointer rangeEnd);
```

### **Parameters**
**rangeStart** [TextPointer]()
The start of the range that changed.  
The value must be a valid [TextPointer]() for the peer's owner, or `null` to denote the start of the owner's text content.

**rangeEnd** [TextPointer]()
The end of the range that changed.
The value must be a valid [TextPointer]() for the peer's owner, or `null` to denote the end of the owner's text content.


# Spec notes
<!--
This is an optional section.  It's often clearer to put non-public
details in a separate section, rather than embed them in the public
spec.  And it makes the doc team's job easier.
-->
All these APIs follow the patterns and naming conventions of previous UIA wrappers.

All the properties and events are "passive", in the sense that they are provided for
the use and convenience of applications, but WPF does not itself use them.
With one exception:  WPF will automatically expose `true` as the value of `IsDialog` on a window that has been opened via `ShowDialog()`.

# Discussion
<!--
This is an optional section, not copied to DMC.
It's where you can add historical notes,
how design decisions were reached, alternatives not taken, 
intentional omissions, future directions, or anything else that
helps the internal audience - reviewers, implementers, testers - understand the feature.
-->
The UIA team mentioned two additions that are not included here:
1. Custom annotations and patterns.
2. Remote operations.

Neither is supported (yet) in WinUI or WinForms.
The first is for "screen readers talking to Edge and Office, so maybe not worth the significant effort it would take in the frameworks".  
The second is for use by clients, requiring no changes in providers.
Thus neither affects accessibility compliance, and are omitted for pragmatic reasons.

# API Details
<!--
List the APIs in "signature form" - no code, no comments.
-->
For convenience, here is a summary of the new public API surface.

```c#
namespace System.Windows.Automation
{
    public enum AutomationHeadingLevel
    {
        None,
        Level1, 
        Level2, 
        Level3, 
        Level4, 
        Level5, 
        Level6, 
        Level7, 
        Level8, 
        Level9 
    }

    public enum AutomationNotificationKind
    {
        ItemAdded, 
        ItemRemoved, 
        ActionCompleted, 
        ActionAborted, 
        Other
    }

    public enum AutomationNotificationProcessing
    {
        ImportantAll,
        ImportantMostRecent,
        All,
        MostRecent,
        CurrentThenMostRecent
    }

    public class AutomationProperties
    {
        public static readonly DependencyProperty HeadingLevelProperty;
        public static AutomationHeadingLevel GetHeadingLevel(DependencyObject element);
        public static void SetHeadingLevel(DependencyObject element, AutomationHeadingLevel value);

        public static readonly DependencyProperty IsDialogProperty;
        public static bool GetHeadingLevel(DependencyObject element);
        public static void SetIsDialog(DependencyObject element, bool value);
   }

   public class AutomationElement
   {
       public static readonly AutomationProperty HeadingLevelProperty;
       public static readonly AutomationProperty IsDialogProperty;
   }

   public class AutomationElementIdentifiers
   {
       public static readonly AutomationProperty HeadingLevelProperty;
       public static readonly AutomationProperty IsDialogProperty;
   }
}

namespace System.Windows.Automation.Peers
{
    public class AutomationPeer
    {
        public AutomationHeadingLevel GetHeadingLevel();
        protected virtual AutomationHeadingLevel GetHeadingLevelCore();

        public bool IsDialog();
        protected virtual bool IsDialogCore();

        public void RaiseNotificationEvent(
                      AutomationNotificationKind notificationKind,
                      AutomationNotificationProcessing notificationProcessing,
                      string displayString,
                      string activityId);
    }

    public class ContentElementAutomationPeer
    {
        protected override AutomationHeadingLevel GetHeadingLevelCore();
        protected override bool IsDialogCore();
    }

    public class UIElementAutomationPeer
    {
        protected override AutomationHeadingLevel GetHeadingLevelCore();
        protected override bool IsDialogCore();
    }

    public class UIElement3DAutomationPeer
    {
        protected override AutomationHeadingLevel GetHeadingLevelCore();
        protected override bool IsDialogCore();
    }

    public class DataGridCellItemAutomationPeer
    {
        protected override AutomationHeadingLevel GetHeadingLevelCore();
        protected override bool IsDialogCore();
    }

    public class DateTimeAutomationPeer
    {
        protected override AutomationHeadingLevel GetHeadingLevelCore();
        protected override bool IsDialogCore();
    }

    public class ItemAutomationPeer
    {
        protected override AutomationHeadingLevel GetHeadingLevelCore();
        protected override bool IsDialogCore();
    }

    public class WindowAutomationPeer
    {
        protected override bool IsDialogCore();
    }

    public class ContentTextAutomationPeer
    {
        public virtual void RaiseActiveTextPositionChangedEvent(
                    TextPointer rangeStart,
                    TextPointer rangeEnd);
    }

    public class TextAutomationPeer
    {
        public virtual void RaiseActiveTextPositionChangedEvent(
                    TextPointer rangeStart,
                    TextPointer rangeEnd);
    }
}
```
