// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MessagePack.Internal
{
    /// <summary>
    /// Raw extension-value operations used by generated and dynamic object formatters.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ExtensionDataHelper
    {
        /// <summary>Copies an unknown array member out of the reader's borrowed storage.</summary>
        public static void ReadArrayValue(ref Dictionary<int, byte[]>? data, int key, ref MessagePackReader reader)
        {
            data ??= new Dictionary<int, byte[]>();
            data[key] = reader.ReadRaw().ToArray();
        }

        /// <summary>Copies an unknown map member out of the reader's borrowed storage.</summary>
        public static void ReadMapValue(ref Dictionary<string, byte[]>? data, ReadOnlySpan<byte> key, ref MessagePackReader reader)
        {
            data ??= new Dictionary<string, byte[]>(StringComparer.Ordinal);
            data[Encoding.UTF8.GetString(key.ToArray())] = reader.ReadRaw().ToArray();
        }

        /// <summary>Writes an array header covering both known and retained members.</summary>
        public static void WriteArrayHeader(ref MessagePackWriter writer, Dictionary<int, byte[]>? data, int knownLength)
        {
            writer.WriteArrayHeader(GetArrayLength(data, knownLength));
        }

        /// <summary>Writes a retained array member, or nil for an unoccupied slot.</summary>
        public static void WriteArrayValue(ref MessagePackWriter writer, Dictionary<int, byte[]>? data, int key)
        {
            if (data != null && data.TryGetValue(key, out var value))
            {
                writer.WriteRaw(value);
            }
            else
            {
                writer.WriteNil();
            }
        }

        /// <summary>Writes retained array members beyond the known schema.</summary>
        public static void WriteArrayTail(ref MessagePackWriter writer, Dictionary<int, byte[]>? data, int knownLength)
        {
            var length = GetArrayLength(data, knownLength);
            for (var key = knownLength; key < length; key++)
            {
                WriteArrayValue(ref writer, data, key);
            }
        }

        /// <summary>Writes a map header excluding extension keys now owned by known members.</summary>
        public static void WriteMapHeader(ref MessagePackWriter writer, Dictionary<string, byte[]>? data, string[] knownKeys)
        {
            var count = knownKeys.Length;
            if (data != null)
            {
                foreach (var key in data.Keys)
                {
                    if (Array.IndexOf(knownKeys, key) < 0)
                    {
                        count++;
                    }
                }
            }

            writer.WriteMapHeader(count);
        }

        /// <summary>Writes unknown map members without duplicating known keys.</summary>
        public static void WriteMapValues(ref MessagePackWriter writer, Dictionary<string, byte[]>? data, string[] knownKeys)
        {
            if (data == null)
            {
                return;
            }

            foreach (var pair in data)
            {
                if (Array.IndexOf(knownKeys, pair.Key) < 0)
                {
                    writer.Write(pair.Key);
                    writer.WriteRaw(pair.Value);
                }
            }
        }

        private static int GetArrayLength(Dictionary<int, byte[]>? data, int knownLength)
        {
            var length = knownLength;
            if (data != null)
            {
                foreach (var key in data.Keys)
                {
                    if (key < 0)
                    {
                        throw new MessagePackSerializationException("Extension array keys must be non-negative.");
                    }

                    length = Math.Max(length, checked(key + 1));
                }
            }

            return length;
        }
    }
}
