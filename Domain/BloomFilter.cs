// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Implementation adapted from http://blogs.msdn.com/b/devdev/archive/2006/01/23/516431.aspx

using System;
using System.Collections;


namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides probabilistic tests for membership of a string in an unbounded set compressed into a finite bit array.
    /// </summary>
    /// <remarks>For more information, see https://en.wikipedia.org/wiki/Bloom_filter.</remarks>
    public class BloomFilter
    {
        private const int DefaultCapacity = 10000;
        private const double DefaultProbabilityOfFalsePositive = .0001;

        private readonly double ln2 = Math.Log(2.0);
        private readonly double optimalRateBase = 1.0/Math.Pow(2, Math.Log(2.0));
        private readonly BitArray table;
        private int numberOfTimesToHash;
        private int tableSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="BloomFilter" /> class based on the base 64 string reprentation of a BloomFilter.
        /// </summary>
        /// <param name="base64BloomFilter">A base64 string obtained by calling <see cref="BloomFilter.ToString" />.</param>
        /// <param name="capacity">The capacity of the Bloom filter, in bits.</param>
        /// <param name="probabilityOfFalsePositive">The probability of a false positive when <see cref="MayContain" /> is called.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <remarks>
        /// This method can be used to deserialize a stored <see cref="BloomFilter" /> instance.
        /// </remarks>
        public BloomFilter(
            string base64BloomFilter,
            int capacity = DefaultCapacity,
            double probabilityOfFalsePositive = DefaultProbabilityOfFalsePositive)
        {
            Initialize(capacity, probabilityOfFalsePositive);

            table = base64BloomFilter.ToBitArray();

            if (table.Count != tableSize)
            {
                throw new ArgumentException(string.Format("base64BloomFilter is of the incorrect size. Expected {0} but was {1}.", tableSize, table.Count));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BloomFilter"/> class.
        /// </summary>
        /// <param name="capacity">The capacity of the Bloom filter, in bits.</param>
        /// <param name="probabilityOfFalsePositive">The probability of a false positive when <see cref="MayContain" /> is called.</param>
        public BloomFilter(
            int capacity = DefaultCapacity,
            double probabilityOfFalsePositive = DefaultProbabilityOfFalsePositive)
        {
            Initialize(capacity, probabilityOfFalsePositive);

            table = new BitArray(tableSize);
        }

        private void Initialize(int capacity, double probabilityOfFalsePositive)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException("capacity", "capacity must greater than 0");
            }

            if (probabilityOfFalsePositive < 0 || probabilityOfFalsePositive > 1)
            {
                throw new ArgumentOutOfRangeException("probabilityOfFalsePositive", "probabilityOfFalsePositive must be between 0 and 1");
            }

            tableSize = (int) Math.Ceiling(capacity*Math.Log(probabilityOfFalsePositive, optimalRateBase));

            // round up to the nearest byte so that the table can be round-tripped as a base 64 string
            tableSize = ~7 & (tableSize + 7);

            numberOfTimesToHash = (int) Math.Round(ln2*tableSize/capacity);
        }

        /// <summary>
        /// Adds the specified value to the Bloom filter.
        /// </summary>
        public void Add(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var firstHash = value.GetHashCode();
            var secondHash = value.JenkinsOneAtATimeHash();

            unchecked
            {
                firstHash = (int) ((uint) firstHash%table.Count);
                secondHash = (int) ((uint) secondHash%table.Count);
            }

            for (var i = 0; i < numberOfTimesToHash; i++)
            {
                table[firstHash] = true;

                unchecked
                {
                    firstHash = (int) ((uint) (firstHash + secondHash)%table.Count);
                    secondHash = (int) ((uint) (secondHash + i)%table.Count);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified value may be contained in the Bloom filter.
        /// </summary>
        /// <remarks>A result of true indicates that the value definitely has not been added to the Bloom filter. A result of false indicates that the value may have been added.</remarks>
        public bool MayContain(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var firstHash = value.GetHashCode();
            var secondHash = value.JenkinsOneAtATimeHash();

            unchecked
            {
                firstHash = (int) ((uint) firstHash%table.Count);
                secondHash = (int) ((uint) secondHash%table.Count);
            }

            for (var i = 0; i < numberOfTimesToHash; i++)
            {
                if (table[firstHash] == false)
                {
                    return false;
                }

                unchecked
                {
                    firstHash = (int) ((uint) (firstHash + secondHash)%table.Count);
                    secondHash = (int) ((uint) (secondHash + i)%table.Count);
                }
            }
            return true;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <remarks>The value returned can be used to instantiate another <see cref="BloomFilter" />, with same content value.</remarks>
        public override string ToString()
        {
            return table.ToBase64String();
        }
    }
}