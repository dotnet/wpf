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


## AutomationEvents Enum
*(This is an existing enum to which two new values are added.  See spec notes.)*

### **Fields**

|||| 
|-|-|-|
| ActiveTextPositionChanged | 20 | [ActiveTextPositionChangedEvent](). Available starting with .NET 6.0. |
| Notification | 19 | [NotificationEvent](). Available starting with .NET 6.0. |


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


## AutomationElement.NotificationEvent Field
Identifies an event raised to notify an element.

```c#
public static readonly AutomationEvent NotificationEvent;
```

### **Field Value**
[AutomationEvent]()

### **Remarks**
This identifier is used by UI Automation client applications. 
UI Automation providers should use the equivalent identifier in [AutomationElementIdentifiers]().


## AutomationElementIdentifiers.NotificationEvent Field
Identifies an event raised to notify an element.

```c#
public static readonly AutomationEvent NotificationEvent;
```

### **Field Value**
[AutomationEvent]()

### **Remarks**
This identifier is for use by UI Automation providers. UI Automation client applications should use the equivalent field from [AutomationElement]().


## NotificationEventArgs Class
Provides data for a [NotificationEvent]().

```c#
public sealed class NotificationEventArgs : AutomationEventArgs
```

## NotificationEventArgs() Constructor
Initializes a new instance of the [NotificationEventArgs]() class.

```c#
public NotificationEventArgs(
                AutomationNotificationKind notificationKind,
                AutomationNotificationProcessing notificationProcessing,
                string displayString,
                string activityId);
```

### **Parameters**

`notificationKind`
[AutomationNotificationKind]()
The type of the notification.

`notificationProcessing`
[AutomationNotificationProcessing]()
The order in which to process the notification.

`displayString`
[String]()
A display string describing the event.

`activityId`
[String]()
A unique non-localized string to identify an action or group of actions. Use this to pass additional information to the event handler.


## NotificationEventArgs.NotificationKind Property
Gets the type of the notification.

```c#
public AutomationNotificationKind NotificationKind { get; }
```

### **Property Value**
[AutomationNotificationKind]()

One of the [AutomationNotificationKind]() values.


## NotificationEventArgs.NotificationProcessing Property
Gets the order in which to process the notification.

```c#
public AutomationNotificationProcessing NotificationProcessing { get; }
```

### **Property Value**
[NotificationProcessing]()

One of the [NotificationProcessing]() values.


## NotificationEventArgs.DisplayString Property
Gets the display string of the notification.

```c#
public string DisplayString { get; }
```

### **Property Value**
[string]()

A display string describing the event.


## NotificationEventArgs.ActivityId Property
Gets the activity ID string of the notification.

```c#
public string ActivityId { get; }
```

### **Property Value**
[string]()

A unique non-localized string to identify an action or group of actions.

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


## AutomationPeer.GetHeadingLevel Method
Gets the heading level of the element that is associated with this automation peer.

```c#
public AutomationHeadingLevel GetHeadingLevel();
```

### **Returns**
[AutomationHeadingLevel]()

The heading level.

### **Exceptions**
[InvalidOperationException]()

A public call to this method is currently in progress.


## AutomationPeer.GetHeadingLevelCore Method
When overridden in a derived class, is called by [GetHeadingLevel]().

```c#
protected virtual AutomationHeadingLevel GetHeadingLevelCore();
```

### **Returns**
[AutomationHeadingLevel]()

The heading level.


## ContentElementAutomationPeer.GetHeadingLevelCore Method
Gets the heading level of the [ContentElement]() that is associated with this [ContentElementAutomationPeer]().
Called by [GetHeadingLevel()]().

```c#
protected override AutomationHeadingLevel GetHeadingLevelCore();
```

### **Returns**
[AutomationHeadingLevel]()

The heading level.


## UIElementAutomationPeer.GetHeadingLevelCore Method
Gets the heading level of the [UIElement]() that is associated with this [UIElementAutomationPeer]().
Called by [GetHeadingLevel()]().

```c#
protected override AutomationHeadingLevel GetHeadingLevelCore();
```

### **Returns**
[AutomationHeadingLevel]()

The heading level.


## UIElement3DAutomationPeer.GetHeadingLevelCore Method
Gets the heading level of the [UIElement3D]() that is associated with this [UIElement3DAutomationPeer]().
Called by [GetHeadingLevel()]().

```c#
protected override AutomationHeadingLevel GetHeadingLevelCore();
```

### **Returns**
[AutomationHeadingLevel]()

The heading level.


## DataGridCellItemAutomationPeer.GetHeadingLevelCore Method
Gets the heading level of the element that is associated with this automation peer.
Called by [GetHeadingLevel()]().

```c#
protected override AutomationHeadingLevel GetHeadingLevelCore();
```

### **Returns**
[AutomationHeadingLevel]()

The heading level.


## DateTimeAutomationPeer.GetHeadingLevelCore Method
Gets the heading level of the element that is associated with this automation peer.
Called by [GetHeadingLevel()]().

```c#
protected override AutomationHeadingLevel GetHeadingLevelCore();
```

### **Returns**
[AutomationHeadingLevel]()

The heading level.


## ItemAutomationPeer.GetHeadingLevelCore Method
Gets the heading level of the element that is associated with this automation peer.
Called by [GetHeadingLevel()]().

```c#
protected override AutomationHeadingLevel GetHeadingLevelCore();
```

### **Returns**
[AutomationHeadingLevel]()

The heading level.


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
public static bool GetIsDialog(System.Windows.DependencyObject element);
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


## AutomationPeer.IsDialog Method
Gets a value that indicates whether the element associated with this automation peer is a dialog window.

```c#
public bool IsDialog();
```

### **Returns**
[Boolean]()

**true** if the element is a dialog; otherwise, **false**.

### **Exceptions**
[InvalidOperationException]()

A public call to this method is currently in progress.


## AutomationPeer.IsDialogCore Method
When overridden in a derived class, is called by [IsDialog]().

```c#
protected virtual bool IsDialogCore();
```

### **Returns**
[Boolean]()

**true** if the element is a dialog; otherwise, **false**.


## ContentElementAutomationPeer.IsDialogCore Method
Gets a value that indicates whether the element associated with this automation peer is a dialog window.
Called by [IsDialog]().

```c#
protected override bool IsDialogCore();
```

### **Returns**
[Boolean]()

**true** if the element is a dialog; otherwise, **false**.


## UIElementAutomationPeer.IsDialogCore Method
Gets a value that indicates whether the element associated with this automation peer is a dialog window.
Called by [IsDialog]().

```c#
protected override bool IsDialogCore();
```

### **Returns**
[Boolean]()

**true** if the element is a dialog; otherwise, **false**.


## UIElement3DAutomationPeer.IsDialogCore Method
Gets a value that indicates whether the element associated with this automation peer is a dialog window.
Called by [IsDialog]().

```c#
protected override bool IsDialogCore();
```

### **Returns**
[Boolean]()

**true** if the element is a dialog; otherwise, **false**.


## DataGridCellItemAutomationPeer.IsDialogCore Method
Gets a value that indicates whether the element associated with this automation peer is a dialog window.
Called by [IsDialog]().

```c#
protected override bool IsDialogCore();
```

### **Returns**
[Boolean]()

**true** if the element is a dialog; otherwise, **false**.


## DateTimeAutomationPeer.IsDialogCore Method
Gets a value that indicates whether the element associated with this automation peer is a dialog window.
Called by [IsDialog]().

```c#
protected override bool IsDialogCore();
```

### **Returns**
[Boolean]()

**true** if the element is a dialog; otherwise, **false**.


## ItemAutomationPeer.IsDialogCore Method
Gets a value that indicates whether the element associated with this automation peer is a dialog window.
Called by [IsDialog]().

```c#
protected override bool IsDialogCore();
```

### **Returns**
[Boolean]()

**true** if the element is a dialog; otherwise, **false**.



## WindowAutomationPeer.IsDialogCore Method
Gets a value that indicates whether the element associated with this automation peer is a dialog window.
Called by [IsDialog]().

```c#
protected override bool IsDialogCore();
```

### **Returns**
[Boolean]()

**true** if the element is a dialog; otherwise, **false**.


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


## AutomationElement.ActiveTextPositionChangedEvent Field
Identifies an event raised when the position changes within a text element.

```c#
public static readonly AutomationEvent ActiveTextPositionChangedEvent;
```

### **Field Value**
[AutomationEvent]()

### **Remarks**
This identifier is used by UI Automation client applications. 
UI Automation providers should use the equivalent identifier in [AutomationElementIdentifiers]().


## AutomationElementIdentifiers.ActiveTextPositionChangedEvent Field
Identifies an event raised to notify an element.

```c#
public static readonly AutomationEvent ActiveTextPositionChangedEvent;
```

### **Field Value**
[AutomationEvent]()

### **Remarks**
This identifier is for use by UI Automation providers.
UI Automation client applications should use the equivalent field from [AutomationElement]().


## ActiveTextPositionChangedEventArgs Class
Provides data for an [ActiveTextPositionChangedEvent]().

```c#
public sealed class ActiveTextPositionChangedEventArgs : AutomationEventArgs
```

## ActiveTextPositionChangedEventArgs(ITextRangeProvider) Constructor
Initializes a new instance of the [ActiveTextPositionChangedEventArgs]() class.

```c#
public ActiveTextPositionChangedEventArgs(ITextRangeProvider textRange);
```

### **Parameters**

`textRange`
[ITextRangeProvider]()
The text range where the change occurred, if applicable.


## ActiveTextPositionChangedEventArgs.TextRange Property
Gets the text range where the change occurred, if applicable.

```c#
public ITextRangeProvider TextRange { get; }
```

### **Property Value**
[ITextRangeProvider]()

The text range where the change occurred, or `null` to indicate the entire content of the text provider.


## IRawElementProviderSimple Interface
Assembly: UIAutomationTypes.dll (type-forwarded from UIAutomationProvider.dll).

*(no other changes)*


## ITextRangeProvider Interface
Assembly: UIAutomationTypes.dll (type-forwarded from UIAutomationProvider.dll).

*(no other changes)*


## ProviderOptions Enum
Assembly: UIAutomationTypes.dll (type-forwarded from UIAutomationProvider.dll).

*(no other changes)*


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

The work adds two new fields to the `AutomationEvents` enum.
There is precedent for this - .NET 4.7.1 added a field for the LiveRegionChangedEvent.

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
       public static readonly AutomationEvent NotificationEvent;
       public static readonly AutomationEvent ActiveTextPositionChangedEvent;
   }

   public class AutomationElementIdentifiers
   {
       public static readonly AutomationProperty HeadingLevelProperty;
       public static readonly AutomationProperty IsDialogProperty;
       public static readonly AutomationEvent NotificationEvent;
       public static readonly AutomationEvent ActiveTextPositionChangedEvent;
   }

   public sealed class NotificationEventArgs : AutomationEventArgs
   {
       public NotificationEventArgs(
                AutomationNotificationKind notificationKind,
                AutomationNotificationProcessing notificationProcessing,
                string displayString,
                string activityId);
        public AutomationNotificationKind NotificationKind { get; }
        public AutomationNotificationProcessing NotificationProcessing { get; }
        public string DisplayString { get; }
        public string ActivityId { get; }
   }

   public sealed class ActiveTextPositionChangedEventArgs : AutomationEventArgs
   {
        public ActiveTextPositionChangedEventArgs(
                ITextRangeProvider textRange);
        public ITextRangeProvider TextRange { get; }
   }
}

namespace System.Windows.Automation.Peers
{
    public enum AutomationEvents
    {
        (... previously defined values...),
        Notification = 19,
        ActiveTextPositionChanged = 20,
    }

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

namespace System.Windows.Automation.Provider
{
    // These three types have been type-forwarded (with no
    // other changes) from UIAutomationProvider.dll to
    // UIAutomationTypes.dll
    public interface IRawElementProviderSimple;
    public interface ITextRangeProvider;
    enum ProviderOptions;
}
```
