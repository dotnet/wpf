# Clipboard API behavior changes from .NET 9 to .NET 10

This document summarizes observable differences in the WPF `Clipboard`, `DataObject`, and related data-transfer APIs between the `release/9.0` and `release/10.0` branches.

## Coordinated compatibility fix

The coordinated WPF and `System.Private.Windows.Core` changes restore the .NET 9 behavior for the existing WPF Clipboard APIs while preserving the .NET 10 WinForms behavior:

- WPF `Clipboard.GetDataObject()` requests the OLE proxy instead of unwrapping the original managed data object.
- WPF `Contains*` APIs use native `IsClipboardFormatAvailable` checks, including Windows-generated formats, and can again be called from an MTA thread.
- WPF `Get*` APIs retrieve through the OLE proxy, restoring Windows format synthesis for live `copy: false` data objects.
- Shared format mapping now permits platform-specific synonyms, restoring WPF's `BitmapSource` mapping without adding it to WinForms.
- WinForms continues to unwrap original managed data objects by default.

The remaining sections describe the behavior of the unmodified `release/9.0` and `release/10.0` branches that motivated the fix.

## Summary

| Area | .NET 9 | .NET 10 |
|---|---|---|
| Nonpersistent ANSI text and synthesized Unicode text | `Clipboard.SetDataObject(dataObject)` exposes the system-generated `UnicodeText` format. | The default `copy: false` path preserves the live `DataObject` formats and does not expose the synthesized `UnicodeText` format. Use `copy: true` to render the data and expose system-generated formats. |
| Thread apartment requirements | `Contains*` APIs can be called from an MTA thread because they query native format availability directly. Other OLE-backed Clipboard APIs require STA. | All Clipboard APIs, including `Contains*`, require STA because format queries go through the OLE-backed clipboard data object. |
| `SetData` format validation | Rejects null and empty format names, but permits whitespace-only names. | Rejects null, empty, and whitespace-only format names. |
| Empty-format exception metadata | Some `ArgumentException` instances do not identify the `format` parameter. | Validation helpers consistently identify `format`. |
| `SetFileDropList(null)` parameter name | Reports `fileDropList`. | Reports `filePaths` through the shared clipboard implementation. |
| Bitmap mapped formats | Auto-conversion advertises `Bitmap`, `System.Drawing.Bitmap`, and `BitmapSource`. | Auto-conversion advertises `Bitmap` and `System.Drawing.Bitmap`; `BitmapSource` is no longer part of the mapped-format group. |
| Typed and safe data APIs | Not available on `Clipboard` or `DataObject`. | Adds typed retrieval, JSON data storage, `ITypedDataObject`, and `DataObjectExtensions`. |
| Nullable annotations | Clipboard getters are declared as non-nullable even when absence is represented by `null`. | Nullable return annotations reflect that audio, data objects, images, and arbitrary data may be absent. |

## ANSI text and Unicode text synthesis

Consider the following code:

```csharp
DataObject dataObject = new();
dataObject.SetText("ahoj", TextDataFormat.Text);

Clipboard.SetDataObject(dataObject);

bool present = Clipboard.GetDataObject().GetDataPresent("UnicodeText");
bool hasText = Clipboard.ContainsText();
bool hasUnicode = Clipboard.ContainsText(TextDataFormat.UnicodeText);
```

`DataObject.SetText(value, TextDataFormat.Text)` stores the ANSI `Text` format with managed auto-conversion disabled.

### .NET 9

The OLE clipboard path exposes the system-generated Unicode representation even though the source `DataObject` did not advertise it:

| Value | Result |
|---|---:|
| `present` | `true` |
| `hasText` | `true` |
| `hasUnicode` | `true` |

### .NET 10 with the default `copy: false`

The default `SetDataObject(object)` overload preserves the live, nonpersistent data object. Clipboard queries observe its native formats instead of an immediately rendered system representation:

| Value | Result |
|---|---:|
| `present` | `false` |
| `hasText` | `false` |
| `hasUnicode` | `false` |

`ContainsText()` checks `UnicodeText`, so it returns `false` even though the native ANSI `Text` format is present. `ContainsText(TextDataFormat.Text)` continues to return `true`.

### .NET 10 with `copy: true`

Rendering the data into the system clipboard causes Windows to synthesize the related text formats:

```csharp
Clipboard.SetDataObject(dataObject, copy: true);
```

After rendering, `present`, `hasText`, and `hasUnicode` are all `true`. Both `GetText(TextDataFormat.Text)` and `GetText(TextDataFormat.UnicodeText)` return `"ahoj"`.

Applications that depend on Windows-generated clipboard formats should therefore use `copy: true`.

## Thread apartment requirements

### .NET 9

The `Contains*` methods use native clipboard format checks and do not create an OLE clipboard context:

- `ContainsAudio`
- `ContainsData`
- `ContainsFileDropList`
- `ContainsImage`
- `ContainsText`

These methods can be called from an MTA thread. APIs that get, set, clear, flush, or inspect clipboard ownership require STA and throw `ThreadStateException` from an MTA thread.

### .NET 10

Format availability is determined through `GetDataObject()` and `IDataObject.GetDataPresent`. This uses the shared OLE clipboard implementation, so the `Contains*` methods now have the same STA requirement as the other Clipboard APIs.

Code that previously performed only a `Contains*` check on a background MTA thread must move that operation to an STA thread.

## Format argument validation

### `Clipboard.SetData`

.NET 9 separately checks for null and `string.Empty`. Whitespace-only format names such as `" "` and `"\t"` pass this validation.

.NET 10 uses `ArgumentException.ThrowIfNullOrWhiteSpace`, so null, empty, and whitespace-only format names are rejected.

### `Clipboard.ContainsData` and `Clipboard.GetData`

Both releases reject null and empty format names. In .NET 10, the common argument helpers consistently include `format` in the exception's `ParamName`. Some .NET 9 empty-format paths construct an `ArgumentException` without a parameter name.

### `Clipboard.SetFileDropList`

The null-argument exception parameter changes from `fileDropList` in .NET 9 to `filePaths` in .NET 10 because the implementation delegates validation to the shared clipboard layer.

Applications should generally avoid depending on exception message text or parameter names unless those values are part of their explicit compatibility requirements.

## Auto-conversion behavior

`DataObject` has built-in synonym groups used by `GetData`, `GetDataPresent`, and `GetFormats` when auto-conversion is enabled:

- `Text`, `UnicodeText`, and `System.String`
- `FileDrop`, `FileNameW`, and `FileName`
- Bitmap formats
- Enhanced metafile formats

The `autoConvert` argument affects format advertisement:

- `GetFormats(autoConvert: true)` can return every mapped synonym.
- `GetFormats(autoConvert: false)` returns only native formats.
- `GetDataPresent(format, autoConvert: true)` can report a mapped synonym.
- `GetDataPresent(format, autoConvert: false)` reports only a native format.

For text data, the parameterless `GetData(format)` path can still search the mapped text group even when `GetDataPresent` and `GetFormats` do not advertise the mapped format. Callers that require native-format-only retrieval should use `GetData(format, autoConvert: false)`.

### Clipboard-level auto-conversion

The Clipboard convenience APIs intentionally request managed auto-conversion only for:

- `DataFormats.FileDrop`
- `DataFormats.Bitmap`

Text formats do not receive managed auto-conversion through `Clipboard.GetText`, `Clipboard.GetData`, or `Clipboard.ContainsText`.

During .NET 10 development, typed Clipboard getters temporarily requested auto-conversion for every format. Commit `0987b406b` (`Restoring autoConvert behavior in Clipboard APIs`) restored the legacy file-drop/bitmap-only rule and made the new `Clipboard.TryGetData` overloads use `autoConvert: false` by default.

## Bitmap mapped formats

The .NET 9 `DataObject` mapping group includes:

- `DataFormats.Bitmap`
- `System.Drawing.Bitmap`
- `BitmapSource`

The .NET 10 shared data-object implementation advertises:

- `DataFormats.Bitmap`
- `System.Drawing.Bitmap`

Consequently, code that enumerates `GetFormats()` or calls `GetDataPresent(typeof(BitmapSource).FullName)` can observe a different result even when the clipboard still contains a retrievable image through `Clipboard.GetImage()`.

## New .NET 10 public APIs

.NET 10 adds typed and safer serialization alternatives.

### `Clipboard`

```csharp
public static bool TryGetData<T>(string format, out T data);

public static bool TryGetData<T>(
    string format,
    Func<TypeName, Type?> resolver,
    out T data);

public static void SetDataAsJson<T>(string format, T data);
```

### `DataObject`

`DataObject` implements the new `ITypedDataObject` interface and adds:

```csharp
public bool TryGetData<T>(out T data);
public bool TryGetData<T>(string format, out T data);
public bool TryGetData<T>(string format, bool autoConvert, out T data);

public bool TryGetData<T>(
    string format,
    Func<TypeName, Type?> resolver,
    bool autoConvert,
    out T data);

public void SetDataAsJson<T>(T data);
public void SetDataAsJson<T>(string format, T data);
```

`DataObjectExtensions` supplies equivalent `TryGetData` extension methods for implementations exposed only as `IDataObject`.

These APIs allow callers to avoid unchecked object casts and provide safer alternatives to legacy formatter-based data exchange.

## Nullable API annotations

The following .NET 10 Clipboard methods explicitly declare nullable return values:

```csharp
Stream? Clipboard.GetAudioStream();
object? Clipboard.GetData(string format);
IDataObject? Clipboard.GetDataObject();
BitmapSource? Clipboard.GetImage();
```

This is primarily a source-analysis and compiler-warning improvement. It documents existing absence behavior rather than guaranteeing that every runtime path changed.

## Behaviors intentionally preserved

After the .NET 10 auto-conversion restoration, the following legacy behaviors remain aligned with .NET 9:

- `Clipboard.SetText` defaults to `UnicodeText`.
- `GetText` returns `string.Empty` when the requested format is unavailable.
- Managed Clipboard auto-conversion is enabled only for file-drop and bitmap formats.
- `SetDataObject(data)` remains equivalent to `SetDataObject(data, copy: false)`.
- `copy: true` renders clipboard data and releases ownership of the source object.
- `Flush` renders delayed data and releases the current source object.
- `IsCurrent` reports whether a previously supplied OLE data object still owns the clipboard.

## Test coverage

The compatibility test suite added with this change includes coverage for:

- The exact ANSI Text to UnicodeText scenario for rendered clipboard data.
- Non-conversion through `Clipboard.GetText` for a live `DataObject`.
- Both `autoConvert` values for the complete text mapping group.
- Every built-in mapped-format family.
- STA enforcement for all Clipboard APIs.
- Argument validation and exception parameter names.
- `Clear`, `Flush`, `IsCurrent`, and `SetDataObject` copy semantics.
- Audio, file-drop, image, custom-format, and text round trips.

The relevant test file is:

```text
src/Microsoft.DotNet.Wpf/tests/UnitTests/PresentationCore.Tests/System/Windows/ClipboardTests.cs
```
