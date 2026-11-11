// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MessagePack;

/// <summary>
/// Identifies a serialized subtype without colliding with the C# union marker.
/// Uses the same wire keys and runtime contract as <see cref="UnionAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MessagePackUnionAttribute : UnionAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackUnionAttribute"/> class with its stable wire key.
    /// </summary>
    /// <param name="key">The distinguishing value that identifies the subtype.</param>
    /// <param name="subType">The derived or implementing type.</param>
    public MessagePackUnionAttribute(int key, Type subType)
        : base(key, subType)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackUnionAttribute"/> class using an assembly-qualified type name.
    /// </summary>
    /// <param name="key">The distinguishing value that identifies the subtype.</param>
    /// <param name="subType">The assembly-qualified name of the subtype.</param>
    public MessagePackUnionAttribute(int key, string subType)
        : base(key, subType)
    {
    }
}
