> See comments in Markdown for how to use this spec template

<!-- The purpose of this spec is to describe new APIs, in a way
that will transfer to docs.microsoft.com (DMC).

There are two audiences for the spec. The first are people that want to evaluate and give feedback on the API, as part of
the submission process.  When it's complete it will be incorporated into the public documentation at
http://docs.microsoft.com (DMC).
Hopefully we'll be able to copy it mostly verbatim. So the second audience is everyone that reads there to learn how
and why to use this API. Some of this text also shows up in Visual Studio Intellisense.

For example, much of the examples and descriptions in the RadialGradientBrush API spec
(https://github.com/microsoft/microsoft-ui-xaml-specs/blob/master/active/RadialGradientBrush/RadialGradientBrush.md)
were carried over to the public API page on DMC
(https://docs.microsoft.com/en-us/windows/winui/api/microsoft.ui.xaml.media.radialgradientbrush?view=winui-2.5)


Once the API is on DMC, that becomes the official copy, and this spec becomes an archive. For example if the description is updated,
that only needs to happen on DMC and needn't be duplicated here.

Samples:
* New class (RadialGradientBrush):
  https://github.com/microsoft/microsoft-ui-xaml-specs/blob/master/active/RadialGradientBrush/RadialGradientBrush.md
* New member on an existing class (UIElement.ProtectedCursor):
  https://github.com/microsoft/microsoft-ui-xaml-specs/blob/master/active/UIElement/ElementCursor.md

[TODO - replace the examples with ones from the dotnet-wpf repo.
The only example there now is the tooltip spec, which isn't typical because it refers so much to information that's external
to WPF (or to MS for that matter).]

Style guide:
* Speak to the developer who will be learning/using this API.
("You use this to..." rather than "the developer uses this to...")
* Use hard returns to keep the page width within ~100 columns.
(Otherwise it's more difficult to leave comments in a GitHub PR.)
* Talk about an API's behavior, not its implementation
(Speak to the developer using this API, not to the team implementing this API)
* A picture says a thousand words.
* An example says a million words.
* Keep examples realistic but simple; don't add unrelated complications
(An example that passes a stream needn't show the process of launching the File-Open dialog.)

-->

Title
==

# Background

<!-- 
Use this section to provide background context for the new API(s) 
in this spec. This is where to explain (briefly) the intent of the APIs,
why they're needed, how they relate to existing APIs, and other similar
information that helps reviewers read the rest of the document.

This section doesn't get copied to DMC; it's just an aid to reading this spec.

For a simple example see the spec for the UIElement.ProtectedCursor property
(TBD)
which has some of the thinking about how this Xaml API relates to existing
Composition and WPF APIs. This is interesting background but not the kind of information
that would land on DMC.
-->

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

_(Each of the following L2 sections correspond to a page that will be on docs.microsoft.com)_

<!--
Notes:
* The first line of each of these sections should become that first line on the DMC page,
  which then becomes the description you see in Intellisense.
* Each page can have description, examples, and remarks.
  Remarks are where the documentation calls out special considerations that the developer should be aware of.
* It can be helpful at the top of an API page (or after the Intellisense text) to add the API signature in C#
* Add a "_Spec note: ..._" to add a note that's useful in this spec but shouldn't go to DMC.
* Show _examples_, not _samples_; an example is a snippet, a sample is a full working app.

It's not necessary to have a section for every class member:
* If its purpose and usage is obvious from it's name/type, it's not necessary to include it.
* If its purpose and usage is obvious other than a brief description, put it in a table in the "Other [class] Members" section.
-->

## MyExample class

Brief description of this class.

Introduction to one or more example usages of a MyExample class:

```c#
...

```

Remarks about the MyExample class


## MyExample.PropertyOne property

Brief description about the MyExample.PropertyOne property.

_Spec note: internal comment about this property that won't go into the public docs._

Introduction to one or more usages of the MyExample.PropertyOne property.


## Other MyExample members


| Name | Description
|-|-|
| PropertyTwo | Brief description of the PropertyTwo property (defaults to ...) |
| MethodOne | Brief description of the MethodOne method |

# Spec notes
<!--
This is an optional section.  It's often clearer to put non-public
details in a separate section, rather than embed them in the public
spec.  And it makes the doc team's job easier.
-->
This section describes details that are not part of the public documentation, but are nevertheless important.

# Discussion
<!--
This is an optional section, not copied to DMC.
It's where you can add historical notes,
how design decisions were reached, alternatives not taken, 
intentional omissions, future directions, or anything else that
helps the internal audience - reviewers, implementers, testers - understand the feature.
-->

# Summary of behavior changes
<!-- This is an optional sectional, useful when your work involves
substantial changes to existing behavior (as opposed to, or in addition
to, new APIs).  List them here, as a checklist for the internal
audience.  Don't include explanations or any other new information -
that should have been covered earlier.
-->
For convenience, here are the behavior changes described in detail above.

# API Details
<!--
List the APIs in "signature form" - no code, no comments.
-->
For convenience, here is a summary of the new public API surface.

```c#
runtimeclass MyExample
{
    int PropertyOne { get; set; }
    string PropertyTwo { get; private set; }
    void MethodOne();
}
```
