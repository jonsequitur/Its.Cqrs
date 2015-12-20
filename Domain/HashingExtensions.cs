// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;

namespace Microsoft.Its.Domain
{
    internal static class HashingExtensions
    {
        public static string ToBase64String(this BitArray bits)
        {
            return Convert.ToBase64String(bits.ToBytes());
        }

        public static byte[] ToBytes(this BitArray bits)
        {
            var bytes = new byte[(bits.Length - 1)/8 + 1];
            bits.CopyTo(bytes, 0);
            return bytes;
        }

        public static BitArray ToBitArray(this string base64String)
        {
            var bytes = Convert.FromBase64String(base64String);
            return new BitArray(bytes);
        }

        public static int JenkinsOneAtATimeHash(this string value)
        {
            var hash = 0;
            for (var i = 0; i < value.Length; i++)
            {
                hash += i;
                hash += hash << 10;
                hash ^= hash >> 6;
            }
            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;
            return hash;
        }
    }
}