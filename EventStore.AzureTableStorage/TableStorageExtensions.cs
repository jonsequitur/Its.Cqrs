// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.EventStore.AzureTableStorage
{
    public static class TableStorageExtensions
    {
        private static string maxRowKey = long.MaxValue.ToRowKey();

        public static string ToRowKey(this long sequenceNumber)
        {
            return (long.MaxValue - sequenceNumber).ToString("D20");
        }

        public static string ToRowKey(this int sequenceNumber)
        {
            return (long.MaxValue - sequenceNumber).ToString("D20");
        }

        public static long FromRowKeyToSequenceNumber(this string rowKey)
        {
            if (rowKey == maxRowKey)
            {
                return long.MaxValue;
            }

            var m = long.Parse(rowKey.TrimStart('0'));

            return Math.Abs(long.MaxValue - m);
        }
    }
}