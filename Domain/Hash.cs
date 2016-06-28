// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Methods for hashing.
    /// </summary>
    public static class Hash
    {
        // a guid namespace to avoid collision with other V3 UUIDs
        private static readonly byte[] GuidNamespaceBytes = new Guid("AF64CD8A-11FD-4AEB-81A1-FB882221E30C").ToByteArray();

        /// <summary>
        /// Generates an etag which is repeatable from a given source string.
        /// </summary>
        public static string ToETag(this string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var inputBytes = Encoding.ASCII.GetBytes(value);
            
            byte[] hash;
            using (var md5 = new MD5CryptoServiceProvider())
            {
                hash = md5.ComputeHash(inputBytes);
            }

            return System.Convert.ToBase64String(hash);
        }
        
        /// <summary>
        /// Hashes the specified string into a V3 UUID.
        /// </summary>
        /// <param name="value">The string to be hashed.</param>
        /// <remarks>This method can be used to generate the same guid from the same input string. This can be useful when a given string should map to a specific guid in order to create an id where several callers could be the first to initialize the object.</remarks>
        public static Guid ToGuidV3(this string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // concatenate the namespace and input string
            var valueBytes = Encoding.UTF8.GetBytes(value);
            var concatenatedBytes = new int[GuidNamespaceBytes.Length + valueBytes.Length];
            GuidNamespaceBytes.CopyTo(concatenatedBytes, 0);
            valueBytes.CopyTo(concatenatedBytes, GuidNamespaceBytes.Length);

            // hash them using MD5
            byte[] hashedBytes;
            using (var md5 = new MD5CryptoServiceProvider())
            {
                hashedBytes = md5.ComputeHash(valueBytes);
            }

            // truncate to a guid-sized number of bytes
            Array.Resize(ref hashedBytes, 16);

            // set the version to 3
            //            74738ff5-5367-3958-9aee-98fffdcd1876
            //                          ^ this one 
            hashedBytes[7] = 0x3F;

            return new Guid(hashedBytes);
        }
        
        // https://en.wikipedia.org/wiki/Jenkins_hash_function
        internal static int ToJenkinsOneAtATimeHash(this string value)
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