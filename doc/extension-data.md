# Retaining unknown fields

An older model can edit known fields without discarding fields written by a newer
model. Opt in on each object whose unknown fields must survive a round trip:

```csharp
[MessagePackObject]
public class ChatOptions
{
    [Key(0)]
    public string Title { get; set; } = string.Empty;

    [MessagePackExtensionData]
    public Dictionary<int, byte[]>? ExtensionData { get; set; }
}
```

For string-key objects, use `Dictionary<string, byte[]>` instead. The extension
property must be a single non-required instance property with public get/set
accessors, not an init-only property or constructor parameter. Do not add a Key
attribute. Inherited extension properties are supported. Source generation reports
`MsgPack019` for invalid declarations; dynamic object formatters reject them too.

Both source-generated and dynamic object formatters copy unknown values as raw
MessagePack bytes. Values can include maps, arrays, nil and application extension
types without resolving their CLR types. Copies do not borrow the input buffer.
No dictionary is allocated unless an unknown member is encountered.

Integer-key arrays retain unknown values both in gaps and beyond the last known
key. String-key maps retain unknown names and values. Known members take precedence
when an extension dictionary contains the same key. Extension data is assigned
before `OnAfterDeserialize`; serialization reads it after `OnBeforeSerialize`.

Opt in separately on nested models. A custom formatter owns its own wire contract
and must handle extension data itself. Unknown union cases are not made readable by
this feature: they still need an application compatibility/version policy. Keep key
and union identifiers stable and never reuse retired identifiers.

## Local verification

The execution tests cover both formatter implementations, nested values, inherited
properties, constructor-based models, structs, empty schemas and raw-buffer lifetime.
The execution-test target is net11.0 to match this fork's runtime. Its existing
hidden-member fixtures are partial, as required by this fork's nested formatters.

Regenerate changed templates with Visual Studio 2022 `TextTransformCore.exe
--preprocess -N MessagePack.SourceGenerator.Transforms -C <TemplateName> -out
<TemplateName>.cs <TemplateName>.tt`. The VS 18 preprocessor generates an async API
that does not match this repository's synchronous `IFormatterTemplate` contract.

Local packages use `999.0.0` and the existing `tests/PackageSmoke` flow. Publishing
and consumer package-version changes are separate operations.

## Union annotation spelling

Use `MessagePackUnionAttribute` (or `[MessagePackUnion]`) for new declarations. It avoids the .NET 11 compiler union attribute name collision and preserves the existing union wire keys. `UnionAttribute` remains available for compiled dependencies and older source consumers. No discriminator values change when the annotation spelling changes.
