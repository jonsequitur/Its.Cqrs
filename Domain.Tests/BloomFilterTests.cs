// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Its.Log.Instrumentation;
using Microsoft.Its.Domain.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class BloomFilterTests
    {
        [Test]
        public void ToBase64String_and_string_constructor_correctly_serialize_and_deserialize_BloomFilter_state()
        {
            var filter = new BloomFilter();
            var item = Guid.NewGuid().ToString();
            filter.Add(item);
            Console.WriteLine(filter.ToString());

            filter = new BloomFilter(filter.ToString());

            filter.MayContain(item)
                  .Should()
                  .BeTrue();
        }

        [Test]
        public void For_a_value_that_was_added_Contains_returns_Maybe()
        {
            var filter = new BloomFilter();
            var item = Guid.NewGuid().ToString();
            filter.Add(item);
            filter.MayContain(item).Should().BeTrue();
        }

        [Test]
        public void For_a_value_that_was_not_added_Contains_returns_DefinitelyNot()
        {
            var filter = new BloomFilter();
            filter.Add(Guid.NewGuid().ToString());
            filter.MayContain(Guid.NewGuid().ToString())
                  .Should()
                  .BeFalse();
        }

        [Test]
        public void Probability_of_false_positive_is_accurate_when_filter_is_at_capacity()
        {
            var filter = new BloomFilter(1000, .01);

            var stringsInFilter = Enumerable.Range(1, 1000).Select(_ => Guid.NewGuid().ToString());

            foreach (var s in stringsInFilter)
            {
                filter.Add(s);
            }

            var falsePositives = Enumerable.Range(1001, 10000)
                                           .Select(i => i.ToString())
                                           .Where(s => filter.MayContain(s))
                                           .ToList();

            Console.WriteLine(falsePositives.Count() + " false positives");
            Console.WriteLine(falsePositives.ToLogString());

            falsePositives.Count.Should().BeInRange(70, 120);
        }

        [Test]
        public void when_Contains_is_DefinitelyNot_then_it_definitely_is_not_in_the_set()
        {
            var set = new HashSet<string>(Enumerable.Range(1, 100000).Select(_ => Guid.NewGuid().ToString()));

            var filter = new BloomFilter(100, .7);

            foreach (var s in set)
            {
                filter.Add(s);
            }

            foreach (var s in set.Where(s => !filter.MayContain(s)))
            {
                set.Contains(s).Should().Be(false);
            }
        }

        [Test]
        public async Task BloomFilter_can_be_round_tripped_through_JSON_serialization()
        {
            var filter = new BloomFilter(capacity: 10000);

            filter.Add("one");
            filter.Add("two");
            filter.Add("three");

            var json = JsonConvert.SerializeObject(filter, Formatting.Indented);

            Console.WriteLine(json);

            var filter2 = JsonConvert.DeserializeObject<BloomFilter>(json);

            filter2.MayContain("one").Should().BeTrue();
            filter2.MayContain("two").Should().BeTrue();
            filter2.MayContain("three").Should().BeTrue();
            filter2.MayContain("false").Should().BeFalse();
        }

        [Test]
        [Ignore("Just a benchmark")]
        public void Size_comparison_vs_full_set()
        {
            var capacity = 100000;
            var filter = new BloomFilter(capacity, .0000001);
            var list = Enumerable.Range(1, capacity).Select(i => Guid.NewGuid().ToString()).ToList();

            foreach (var s in list)
            {
                filter.Add(s);
            }

            File.WriteAllText(@"c:\temp\list.txt", string.Join("", list));
            File.WriteAllText(@"c:\temp\filter.txt", filter.ToString());
        }
    }
}