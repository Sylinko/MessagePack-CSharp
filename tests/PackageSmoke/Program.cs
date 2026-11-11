using MessagePack;
using MessagePack.Resolvers;

// Compilation must find the packaged generator. Only its payload formatter and
// built-in primitive formatters are allowed; no reflection fallback is used.
var resolver = CompositeResolver.Create(GeneratedMessagePackResolver.Instance, BuiltinResolver.Instance);
var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
var expected = new SmokePayload
{
    Number = 42,
    Text = "package smoke",
    ExtensionData = new() { [3] = new byte[] { 0xd4, 42, 0x77 } },
};
var bytes = MessagePackSerializer.Serialize(expected, options);
var actual = MessagePackSerializer.Deserialize<SmokePayload>(bytes, options);
if (actual is null || actual.Number != expected.Number || actual.Text != expected.Text)
{
    throw new InvalidOperationException("The generated formatter did not round-trip the payload.");
}

if (actual.ExtensionData is null || !actual.ExtensionData[3].AsSpan().SequenceEqual(expected.ExtensionData[3]))
{
    throw new InvalidOperationException("The packaged formatter did not preserve unknown fields.");
}

// Both marker spellings must preserve existing wire keys across generated and runtime resolvers.
var explicitBytes = MessagePackSerializer.Serialize<IExplicitUnion>(expected, options);
var legacyBytes = MessagePackSerializer.Serialize<ILegacyUnion>(expected, options);
var dynamicOptions = options.WithResolver(CompositeResolver.Create(
    DynamicUnionResolver.Instance, GeneratedMessagePackResolver.Instance, BuiltinResolver.Instance));
var dynamicBytes = MessagePackSerializer.Serialize<IExplicitUnion>(expected, dynamicOptions);
if (!explicitBytes.AsSpan().SequenceEqual(legacyBytes))
{
    throw new InvalidOperationException("The explicit union marker changed the generated serialization contract.");
}

// The generated writer uses fixed-width union keys; the runtime writer may encode them compactly.
// Both encodings must remain mutually readable, including retained unknown payload fields.
foreach (var decoded in new[]
{
    MessagePackSerializer.Deserialize<IExplicitUnion>(legacyBytes, dynamicOptions),
    MessagePackSerializer.Deserialize<IExplicitUnion>(dynamicBytes, options),
})
{
    if (decoded is not SmokePayload { Number: 42, Text: "package smoke", ExtensionData: { } extensionData }
        || !extensionData[3].AsSpan().SequenceEqual(expected.ExtensionData[3]))
    {
        throw new InvalidOperationException("Union readers did not preserve the payload and unknown fields.");
    }
}

Console.WriteLine("MessagePack package smoke passed: generated formatters, unknown fields and compatible union markers.");

/// <summary>Payload whose formatter must be supplied by the packaged generator.</summary>
[MessagePackObject]
public sealed class SmokePayload : IExplicitUnion, ILegacyUnion
{
    /// <summary>Gets or sets the numeric smoke-test value.</summary>
    [Key(0)]
    public int Number { get; set; }

    /// <summary>Gets or sets the text smoke-test value.</summary>
    [Key(1)]
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets raw unknown fields retained by the generated formatter.</summary>
    [MessagePackExtensionData]
    public Dictionary<int, byte[]>? ExtensionData { get; set; }
}

/// <summary>Uses the explicit marker with the generated and runtime resolvers.</summary>
[MessagePackUnion(7, typeof(SmokePayload))]
public interface IExplicitUnion
{
}

/// <summary>Retains the original public annotation and the same stable wire key.</summary>
[MessagePack.Union(7, typeof(SmokePayload))]
public interface ILegacyUnion
{
}
