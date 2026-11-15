// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.ExecutionTests;

public partial class NestedFormatterTests
{
    [Fact]
    public void RoundTrip_WhenUnionCasesArePublicNestedTypes_UsesGeneratedFormatters()
    {
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(BuiltinResolver.Instance, GeneratedMessagePackResolver.Instance));

        var successBytes = MessagePackSerializer.Serialize<Outcome>(new Outcome.Success(1920), options);
        var success = Assert.IsType<Outcome.Success>(MessagePackSerializer.Deserialize<Outcome>(successBytes, options));
        Assert.Equal(1920, success.Width);

        var failureBytes = MessagePackSerializer.Serialize<Outcome>(new Outcome.Failure("Missing image"), options);
        var failure = Assert.IsType<Outcome.Failure>(MessagePackSerializer.Deserialize<Outcome>(failureBytes, options));
        Assert.Equal("Missing image", failure.Error);
    }

    [MessagePackObject]
    [MessagePackUnion(0, typeof(Success))]
    [MessagePackUnion(1, typeof(Failure))]
    public abstract partial class Outcome
    {
        [MessagePackObject]
        public sealed class Success : Outcome
        {
            [Key(0)]
            public int Width { get; }

            public Success(int width)
            {
                this.Width = width;
            }
        }

        [MessagePackObject]
        public sealed class Failure : Outcome
        {
            [Key(0)]
            public string Error { get; }

            public Failure(string error)
            {
                this.Error = error;
            }
        }
    }
}
