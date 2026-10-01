using MessagePack;
using MessagePack.Resolvers;

// Compilation must find the packaged generator. Only its payload formatter and
// built-in primitive formatters are allowed; no reflection fallback is used.
var resolver = CompositeResolver.Create(GeneratedMessagePackResolver.Instance, BuiltinResolver.Instance);
var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
var expected = new SmokePayload { Number = 42, Text = "package smoke" };
var bytes = MessagePackSerializer.Serialize(expected, options);
var actual = MessagePackSerializer.Deserialize<SmokePayload>(bytes, options);
if (actual is null || actual.Number != expected.Number || actual.Text != expected.Text)
{
    throw new InvalidOperationException("The generated formatter did not round-trip the payload.");
}

Console.WriteLine("MessagePack package smoke passed: generated formatter round-trip.");

/// <summary>Payload whose formatter must be supplied by the packaged generator.</summary>
[MessagePackObject]
public sealed class SmokePayload
{
    /// <summary>Gets or sets the numeric smoke-test value.</summary>
    [Key(0)]
    public int Number { get; set; }

    /// <summary>Gets or sets the text smoke-test value.</summary>
    [Key(1)]
    public string Text { get; set; } = string.Empty;
}
