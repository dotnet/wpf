# Adding HeadingLevel and IsDialog properties report

In this report, will be the steps executed to the complete implementations of these two properties in WPF, including all the files modified in the proccess.

## Workflow

The workflow for working in WPF consisted on editing the code (used VS Code for this), building it, copying the generated ref files to the correct location inside dotnet folder, and testing it in Visual Studio.

Usually, the command uses to build was `Build /p:Platform=x64 /p:BaselineAllAPICompatError=true -pack`, using the `-pack` flag to generate the files and `/p:BaselineAllAPICompatError=true` to recreate the APICompat baseline files for the new property. More info about it [here](https://github.com/dotnet/wpf/blob/main/Documentation/api-compat.md). 

The generated files to be copied are the following: `.\artifacts\packaging\Debug\x64\Microsoft.DotNet.Wpf.GitHub.Debug\lib\net6.0\*` to `..\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\6.0.0-preview.6.21317.5\`; and `.\artifacts\packaging\Debug\x64\Microsoft.DotNet.Wpf.GitHub.Debug\ref\net6.0\*` to `..\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\6.0.0-preview.6.21317.5\ref\net6.0\` . I suggest to have a script to copy them and gain some time.

After copying the references, the files can be open in Visual Studio to set breakpoints for debugging.

# Files Changed

In this section, will be listed all the changes on the [Pull Request](https://github.com/dotnet/wpf/pull/4751) and some of the reasoning about them, in the order they were made (or the more logical order).
## OSVersionHelper.cs

Before everything, changes in this file were needed. The [support for RS4 was added](https://github.com/dotnet/wpf/pull/4751/files#diff-05f25cf93ed154b760ffd1f31b96af6ccf131f28b9f10c3cb4fac7fe19bb2d5bR53-R100) for HeadingLevel property, creagint the extern method [here](https://github.com/dotnet/wpf/pull/4751/files#diff-05f25cf93ed154b760ffd1f31b96af6ccf131f28b9f10c3cb4fac7fe19bb2d5bR146-R148).

## UIAutomationTypes
### AutomationIdentifierConstants.cs

This is the file where we declare the new porperties to be added. The two [if statements](https://github.com/dotnet/wpf/pull/4751/files#diff-326f5b75e658648db183d6a96e204cc6ed23e922ab94f7c54fe8cfaef8c3989fR34-R50) were added to set up the last supported events and properties according to the OS version. The HeadingLevel property is supported on RS4, and IsDialog on RS5.

Also, we add them in the [enum Properties](https://github.com/dotnet/wpf/pull/4751/files#diff-326f5b75e658648db183d6a96e204cc6ed23e922ab94f7c54fe8cfaef8c3989fR289-R296). We need to add the other 5 properties because the numeric values of the enum should match the [properties ids](https://docs.microsoft.com/en-us/windows/win32/winauto/uiauto-automation-element-propids). The last one implemented was `Size`, which id is 30167. And the `HeadingLevel` id is 30173, because there was five other properties in between. `IsDialog` comes just after with the id 30174. 

These are the five properties missing:
```
const long UIA_IsSelectionPattern2AvailablePropertyId = 30168;
const long UIA_Selection2FirstSelectedItemPropertyId = 30169;
const long UIA_Selection2LastSelectedItemPropertyId = 30170;
const long UIA_Selection2CurrentSelectedItemPropertyId = 30171;
const long UIA_Selection2ItemCountPropertyId = 30172;
```

### AutomationElementIdentifiers.cs

In this file, we [create the two properties](https://github.com/dotnet/wpf/pull/4751/files#diff-ea7f693cded819529c9149156deee9a7bbbcbeddfd2806100c983de3a6ff56b3R119-R124) of the class AutomationProperty, using the static method Register, receiving the id from the enum Properties.

### ref/UIAutomationTypes.cs

And in this one, [create the same properties](https://github.com/dotnet/wpf/pull/4751/files#diff-278bf69eb7c4b9176c567f08f1105b6b2bdd12afc10aeb10ba74ea340a8af31aR31-R35) in the reference file.
## UIAutomationClient
### AutomationElement.cs

Next, the AutomationElement class needs to [declare the properties](https://github.com/dotnet/wpf/pull/4751/files#diff-e86a5f335181123dd6804ccc6d309cef3f2c0ba24d35044225b055bcc24ca628R201-R210) getting them from AutomationElementIdentifiers.

### Schema.cs

The change was in the Schema.cs, where the two new property infos were [declared](https://github.com/dotnet/wpf/pull/4751/files#diff-06492e0436d0881739aa3bfa3052625528df07c8aefd87f86c51eab3683f7ac2R291-R292). In the consturctor, there are 4 arguments. The first is the converter, second is the AutomationProperty, got from AutomationElement class, the third is the type of the value (an enum [declared afterwards](https://github.com/dotnet/wpf/pull/4751/files#diff-06492e0436d0881739aa3bfa3052625528df07c8aefd87f86c51eab3683f7ac2R212-R225) for HeadingLevel and bool for IsDialog), and the default value comes last.

### ref/UIAutomationClient.cs

Finally, we [declare them in the reference file](https://github.com/dotnet/wpf/pull/4751/files#diff-08711d7b7f1b4b33e619ddd31b96005ca59095264e5c8edddc011f8574e45dc2R43-R47) inside AutomationElement class.

## PresentationCore

This is the part of project where most of the changes were made. We will change the AutomationProperties class and the AutomationPeer class to support the new properties, also change some of the main AutomationPeers to implement them.

### Adding AutomationHeadingLevel.cs enum

As HeadingLevel value is an enum, we need to [create it](https://github.com/dotnet/wpf/pull/4751/files#diff-302f99ca8bfc90091a2b7495d342b1b9dca89787033e5a290d1ea5cf6e534133R1-R68) and add it [to be compiled](https://github.com/dotnet/wpf/pull/4751/files#diff-2602bc5513a33eef826d95d5cba966d84e8e143bcb5e28fb8a7398fd6090ef3fR395) with the project.

### AutomationProperties.cs

Here, we will implement the properties to be set with the AutomationProperties class. The implementation for HeadingLevel is [here](https://github.com/dotnet/wpf/pull/4751/files#diff-292f16ac8e53da579b25b8c732224931dd04271f8eff5af074edd6f6efb46bd7R597-R630), and similiar was done for IsDialog [here](https://github.com/dotnet/wpf/pull/4751/files#diff-292f16ac8e53da579b25b8c732224931dd04271f8eff5af074edd6f6efb46bd7R632-R664). This is what allow the properties to be set in the DependencyObject.

### AutomationPeer.cs

Here we implement the base methods for all other peers. [GetHeadingLevel](https://github.com/dotnet/wpf/pull/4751/files#diff-2c34ffc37749fc235998a14c9a74dd042c2a413c18880a29c4ba842654c4217dR1279-R1304) returns the HeadingLevel of the DependencyObject owner of this peers, trying to get it from the method [GetHeadingLevelCore](https://github.com/dotnet/wpf/pull/4751/files#diff-2c34ffc37749fc235998a14c9a74dd042c2a413c18880a29c4ba842654c4217dR712-R717), which in this class, just returns the default value. 

As HeadingLevel is an enum value, in the docs we set its values to range from 0 (None) to 9 (Level9). But, [UIA intentifies](https://docs.microsoft.com/en-us/windows/win32/winauto/uiauto-heading-level-identifiers) are from 80050 (None) to 80059 (Level9). So, before handling the result to UIA, we [map them](https://github.com/dotnet/wpf/pull/4751/files#diff-2c34ffc37749fc235998a14c9a74dd042c2a413c18880a29c4ba842654c4217dR2174-R2177) to the correct values, using the [private enum](https://github.com/dotnet/wpf/pull/4751/files#diff-2c34ffc37749fc235998a14c9a74dd042c2a413c18880a29c4ba842654c4217dR1307-R1319), and a [converter method](https://github.com/dotnet/wpf/pull/4751/files#diff-2c34ffc37749fc235998a14c9a74dd042c2a413c18880a29c4ba842654c4217dR1320-R1346).

IsDialog is a bit simpler as it returns just a boolean value. [IsDialogCore](https://github.com/dotnet/wpf/pull/4751/files#diff-2c34ffc37749fc235998a14c9a74dd042c2a413c18880a29c4ba842654c4217dR655-R659) return false and the implementation of [IsDialog](https://github.com/dotnet/wpf/pull/4751/files#diff-2c34ffc37749fc235998a14c9a74dd042c2a413c18880a29c4ba842654c4217dR1354-R1375) is also simple. *Note that as IsDialog is a boolean property, I followed the pattern and choose for IsDialog instead of GetIsDialog, as other boolean properties.*

### ContentElementAutomationPeer.cs, UIElementAutomationPeer.cs and UiElement3DAutomationPeer.cs

These are the classes in the PresentationCore that are parents of almost every other peer in PresentationFramework. There are some exceptions that we will see later. 

So, these classes need to implement the IsDialogCore and GetHeadingLevelCore methods, or elset they would always return the default value. Here will be the contact between the automation peer and the AutomationProperties class, which is the one that actually gets the value of the property for us, and the peer returns it to UIA.

All the three implementations for these classes are the same, and are very simple, just calling automation properties and getting the value returned.

In ContenteElementAutomation peer are [here](https://github.com/dotnet/wpf/pull/4751/files#diff-04a0d1f88fbc6dc1f41662373c8ba94fcd2a51eebf7ae92c4bbd54064f9dc39dR216-R223) and [here](https://github.com/dotnet/wpf/pull/4751/files#diff-04a0d1f88fbc6dc1f41662373c8ba94fcd2a51eebf7ae92c4bbd54064f9dc39dR298-R306).
For UIElementAutomationPeer: [here](https://github.com/dotnet/wpf/pull/4751/files#diff-04a0d1f88fbc6dc1f41662373c8ba94fcd2a51eebf7ae92c4bbd54064f9dc39dR298-R306) and [here](https://github.com/dotnet/wpf/pull/4751/files#diff-46438e614debbc732b78f97523dec6218fcc790318cea23e5ebc27a6a47f0ac5R496-R504). And for UIElement3DAutomationPeer: [here](https://github.com/dotnet/wpf/pull/4751/files#diff-eea3a381ce717788d178965244e67012e68e1b88169a9d7539ca82c022b5225aR320-R325) and [here](https://github.com/dotnet/wpf/pull/4751/files#diff-eea3a381ce717788d178965244e67012e68e1b88169a9d7539ca82c022b5225aR395-R403).

### ref/PresentationCore.cs

Finally, we need to declare everything added in the reference file. We redeclare the [AutomationHeadingLevel enum](https://github.com/dotnet/wpf/pull/4751/files#diff-8e9f502efcdba2ed9b2e62342a20040d8c846ef2f77586310f09449bccbb6280R2129-R2143). Declare the [properties](https://github.com/dotnet/wpf/pull/4751/files#diff-8e9f502efcdba2ed9b2e62342a20040d8c846ef2f77586310f09449bccbb6280R2149-R2152) from AutomationProperties.cs, with the [Get and Set methods](https://github.com/dotnet/wpf/pull/4751/files#diff-8e9f502efcdba2ed9b2e62342a20040d8c846ef2f77586310f09449bccbb6280R2166-R2186) for both.

Now we declare the [methods created](https://github.com/dotnet/wpf/pull/4751/files#diff-8e9f502efcdba2ed9b2e62342a20040d8c846ef2f77586310f09449bccbb6280R2301-R2337) in AutomationPeer.

And the methods from the three classes of peers mentioned above: [ContentElement](https://github.com/dotnet/wpf/pull/4751/files#diff-8e9f502efcdba2ed9b2e62342a20040d8c846ef2f77586310f09449bccbb6280R2372-R2386), [UIElement3D](https://github.com/dotnet/wpf/pull/4751/files#diff-8e9f502efcdba2ed9b2e62342a20040d8c846ef2f77586310f09449bccbb6280R2444-R2458) and [UIElement](https://github.com/dotnet/wpf/pull/4751/files#diff-8e9f502efcdba2ed9b2e62342a20040d8c846ef2f77586310f09449bccbb6280R2480-R2494).

## PresentationFramework

After changing the classes in PresentationCore, we need to change some of the peers in PresentationFramework. Here is where the behaviour of the peers for the controls are actually implemented.

As said above, almost all of them inherit from `ContentElementAutomationPeer`, `UIElement3DAutomationPeer` and `UIElementAutomationPeer`.

There are three of them that do not inherit from them, so the `IsDialogCore` and `GetHeadingLevelCore` for them would always return the default value as implemented in `AutomationPeer.cs`.

So, we need to create the behavior for them, which are [DataGridCellItemAutomationPeer](https://github.com/dotnet/wpf/pull/4751/files#diff-0139b9e223f77e31f3bb35e27f4f292be36b70e32a43e0bfe273ecec50ebb336R366-R437), [DateTimeAutomationPeer](https://github.com/dotnet/wpf/pull/4751/files#diff-9ae60429705ff9904cee36c08798523093563fbf4d1deb7b92f0bbd3445ac9cbR462-R545) and [ItemAutomationPeer](https://github.com/dotnet/wpf/pull/4751/files#diff-45edde82c5a382f0229e7094b77478a5f6c08b9f919159a747e03d9c116eb536R235-R468).

### WindowAutomationPeer.cs

The WindowAutomationPeer is an example of peer that the behaviour for one of the new properties (IsDialog) should have a different implementation. 

Here, the owner of this peer could be showing as a dialog depending of how it was instantiated, and whe should get this information from the property `IsShowingAsDialog` from the owner.

Therefore, we implement this in the owner class [Window.cs](https://github.com/dotnet/wpf/pull/4751/files#diff-30ce94d35c72b94385aa439cafead5bd0677f7b4037f0fefcfc7a03d3730a681R1282-R1292), and after we [get the value if possible](https://github.com/dotnet/wpf/pull/4751/files#diff-53c3d38740b19f10d386f48fe04225d8a602b466b8a260d8cdd2a64a386f5b88R91-R103), or else we get the information from AutomationProperties class.

### ref/PresentationFramework.cs

Finally, we need to declare everything created above in the reference file.

 Starting with the peers changed: [DataGridCellItem](https://github.com/dotnet/wpf/pull/4751/files#diff-8c5e1efe3927fbf477c421d45b1896e23bbedbd69d8770ce9e994c223bff0414R2406-R2421), [DateTime](https://github.com/dotnet/wpf/pull/4751/files#diff-8c5e1efe3927fbf477c421d45b1896e23bbedbd69d8770ce9e994c223bff0414R2545-R2560) and [Item](https://github.com/dotnet/wpf/pull/4751/files#diff-8c5e1efe3927fbf477c421d45b1896e23bbedbd69d8770ce9e994c223bff0414R2802-R2816) automation peers.  And the new method in WindowAutomationPeer.

## Testing

After that, the new properties should be now possible to be set in a WPF project. Also, will be seen in AccessibilityInsights.