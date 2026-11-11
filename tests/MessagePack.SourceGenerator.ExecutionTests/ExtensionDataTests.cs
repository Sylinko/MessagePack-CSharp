// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;

public class ExtensionDataTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RoundTrip_WhenUnknownFieldsAreNested_PreservesRawValuesAndKnownEdits(bool isDynamic)
    {
        var options = Options(isDynamic);
        var futureValue = new byte[] { 0xd4, 42, 0x77 }; // An application extension the old model does not understand.
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(5);
        writer.Write("original");
        writer.WriteRaw(futureValue); // A hole in the old schema.
        writer.WriteMapHeader(3);
        writer.Write("KnownLongNameA");
        writer.Write("nested");
        writer.Write("KnownLongNameB");
        writer.Write(5);
        writer.Write("KnownLongNameZ"); // Match the automaton's prefix before falling through.
        writer.WriteRaw(futureValue);
        writer.WriteNil();
        writer.WriteRaw(futureValue); // Beyond the old array's last key.
        writer.Flush();

        var value = MessagePackSerializer.Deserialize<ExtensionArray>(buffer.WrittenMemory, options);
        Assert.Equal(futureValue, value.ExtensionData![1]);
        Assert.Equal(futureValue, value.ExtensionData[4]);
        Assert.Equal(futureValue, value.Child.ExtensionData!["KnownLongNameZ"]);
        value.Text = "edited";
        value.Child.KnownLongNameA = "edited child";
        // Known members win even if a caller supplies colliding extension entries.
        value.ExtensionData[0] = MessagePackSerializer.Serialize("stale");
        value.Child.ExtensionData["KnownLongNameA"] = MessagePackSerializer.Serialize("stale");

        // Raw data must own its memory, not borrow the input buffer.
        buffer.Clear();
        buffer.GetSpan(128).Fill(0);
        var bytes = MessagePackSerializer.Serialize(value, options);
        var copy = MessagePackSerializer.Deserialize<ExtensionArray>(bytes, options);
        Assert.Equal("edited", copy.Text);
        Assert.Equal("edited child", copy.Child.KnownLongNameA);
        Assert.Equal(futureValue, copy.ExtensionData![1]);
        Assert.Equal(futureValue, copy.ExtensionData[4]);
        Assert.Equal(futureValue, copy.Child.ExtensionData!["KnownLongNameZ"]);
        Assert.False(copy.Child.ExtensionData.ContainsKey("KnownLongNameA"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RoundTrip_WhenOnlyExtensionDataExists_PreservesArrayAndMap(bool isDynamic)
    {
        var options = Options(isDynamic);
        var arrayBytes = new byte[] { 0x92, 0xc0, 0x2a };
        var array = MessagePackSerializer.Deserialize<ExtensionOnlyArray>(arrayBytes, options);
        Assert.Equal(arrayBytes, MessagePackSerializer.Serialize(array, options));
        var mapBytes = MessagePackSerializer.Serialize(new Dictionary<string, int> { ["future"] = 42 });
        var map = MessagePackSerializer.Deserialize<ExtensionOnlyMap>(mapBytes, options);
        Assert.Equal(mapBytes, MessagePackSerializer.Serialize(map, options));
        Assert.Null(MessagePackSerializer.Deserialize<ExtensionOnlyArray>(new byte[] { 0x90 }, options).ExtensionData);
        Assert.Null(MessagePackSerializer.Deserialize<ExtensionOnlyMap>(new byte[] { 0x80 }, options).ExtensionData);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RoundTrip_WhenMapUsesConstructorAndArrayIsStruct_RetainsExtensionData(bool isDynamic)
    {
        var options = Options(isDynamic);
        var mapBytes = MessagePackSerializer.Serialize(new Dictionary<string, string>
        {
            ["Text"] = "old",
            ["Future"] = "retained",
        });
        var map = MessagePackSerializer.Deserialize<ExtensionConstructorMap>(mapBytes, options);
        Assert.Equal("old", map.Text);
        Assert.Equal(mapBytes, MessagePackSerializer.Serialize(map, options));
        var arrayBytes = new byte[] { 0x92, 0x01, 0x2a };
        var value = MessagePackSerializer.Deserialize<ExtensionStruct>(arrayBytes, options);
        value.Number = 2;
        Assert.Equal(new byte[] { 0x92, 0x02, 0x2a }, MessagePackSerializer.Serialize(value, options));
    }

    private static MessagePackSerializerOptions Options(bool isDynamic)
    {
        IFormatterResolver objects = isDynamic ? DynamicObjectResolver.Instance : GeneratedMessagePackResolver.Instance;
        return MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(BuiltinResolver.Instance, objects));
    }
}

[MessagePackObject]
public class ExtensionArray
{
    [Key(0)]
    public string Text { get; set; }

    [Key(2)]
    public ExtensionMap Child { get; }

    [MessagePackExtensionData]
    public Dictionary<int, byte[]>? ExtensionData { get; set; }

    [SerializationConstructor]
    public ExtensionArray(string text, ExtensionMap child)
    {
        this.Text = text;
        this.Child = child;
    }
}

[MessagePackObject(true)]
public class ExtensionMap : ExtensionMapBase
{
    public string KnownLongNameA { get; set; } = string.Empty;

    public int KnownLongNameB { get; set; }
}

public class ExtensionMapBase
{
    [MessagePackExtensionData]
    public Dictionary<string, byte[]>? ExtensionData { get; set; }
}

[MessagePackObject]
public class ExtensionOnlyArray
{
    [MessagePackExtensionData]
    public Dictionary<int, byte[]>? ExtensionData { get; set; }
}

[MessagePackObject(true)]
public class ExtensionOnlyMap : ExtensionMapBase
{
}

[MessagePackObject(true)]
public class ExtensionConstructorMap : ExtensionMapBase
{
    public string Text { get; }

    [SerializationConstructor]
    public ExtensionConstructorMap(string text)
    {
        this.Text = text;
    }
}

[MessagePackObject]
public struct ExtensionStruct
{
    [Key(0)]
    public int Number { get; set; }

    [MessagePackExtensionData]
    public Dictionary<int, byte[]>? ExtensionData { get; set; }
}
