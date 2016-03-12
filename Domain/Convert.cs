// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;

namespace Microsoft.Its.Domain
{
    internal static class Convert
    {
        internal static string ToBase64String(this BitArray bits)
        {
            return System.Convert.ToBase64String(bits.ToBytes());
        }

        internal static byte[] ToBytes(this BitArray bits)
        {
            var bytes = new byte[(bits.Length - 1)/8 + 1];
            bits.CopyTo(bytes, 0);
            return bytes;
        }

        internal static BitArray ToBitArray(this string base64String) =>
            new BitArray(System.Convert.FromBase64String(base64String));
    }
}