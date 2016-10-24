// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class HashTests
    {
        [Test]
        public void ToGuidV3_produces_deterministic_output_based_on_input()
        {
            var s = Any.String(1, 100);

            var guid1 = s.ToGuidV3();
            var guid2 = s.ToGuidV3();

            guid1.Should().Be(guid2);
        }

        [Test]
        public void ToGuidV3_accepts_an_empty_string()
        {
            var guid1 = "".ToGuidV3();
            var guid2 = "".ToGuidV3();

            guid1.Should().Be(guid2);
        }

        [Test]
        public void ToGuidV3_does_not_collide_for_slightly_different_long_strings()
        {
            var sourceString = Any.String(10000, 10000);
            var guid1 = (sourceString + "a").ToGuidV3();
            var guid2 = (sourceString + "b").ToGuidV3();

            guid1.Should().NotBe(guid2);
        }

        [Test]
        public void The_version_bit_is_set_to_3()
        {
            var guid = Any.String(1, 100).ToGuidV3();

            //            74738ff5-5367-3958-9aee-98fffdcd1876
            //                          ^ this one is the version

            Console.WriteLine(guid);

            guid.ToString()[14].Should().Be('3');
        }
    }
}