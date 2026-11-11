// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MessagePack
{
    /// <summary>
    /// Preserves unknown members as raw MessagePack values when an object is read and written again.
    /// </summary>
    /// <remarks>
    /// Apply to one non-required instance property with public get and set accessors (not init). Its type must be
    /// Dictionary&lt;int, byte[]&gt; for integer keys or Dictionary&lt;string, byte[]&gt; for string keys.
    /// The property is not a serialized member itself. Known members take precedence over extension data.
    /// Each nested object that needs preservation must opt in separately.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MessagePackExtensionDataAttribute : IgnoreMemberAttribute
    {
    }
}
