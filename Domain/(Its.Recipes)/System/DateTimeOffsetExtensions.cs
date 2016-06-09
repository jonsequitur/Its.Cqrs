// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System
{
#if !RecipesProject
    [DebuggerStepThrough]
    [ExcludeFromCodeCoverage]
#endif
    internal static partial class DateTimeOffsetExtensions
    {
        public static DateTimeOffset Floor(
            this DateTimeOffset dateTimeOffset,
            TimeSpan interval) =>
                new DateTimeOffset(
                    dateTimeOffset.UtcTicks - dateTimeOffset.UtcTicks%interval.Ticks,
                    TimeSpan.Zero);

        public static DateTimeOffset Ceiling(
            this DateTimeOffset dateTimeOffset,
            TimeSpan interval) =>
                dateTimeOffset.UtcTicks%interval.Ticks == 0
                    ? dateTimeOffset
                    : new DateTimeOffset(dateTimeOffset.UtcTicks - dateTimeOffset.UtcTicks%interval.Ticks, TimeSpan.Zero) + interval;

        public static DateTimeOffset Min(
            DateTimeOffset dateTimeOffset,
            DateTimeOffset otherDateTimeOffset) =>
                dateTimeOffset.CompareTo(otherDateTimeOffset) <= 0 ? dateTimeOffset : otherDateTimeOffset;

        public static DateTimeOffset Max(
            DateTimeOffset dateTimeOffset,
            DateTimeOffset otherDateTimeOffset) =>
                dateTimeOffset.CompareTo(otherDateTimeOffset) >= 0 ? dateTimeOffset : otherDateTimeOffset;
    }
}