// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;

namespace System.Collections.Generic
{
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class DictionaryExtensions
    {
        /// <summary>
        ///     Adds a key/value pair to the dictionary if the key does not already exist.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="valueFactory">The function used to generate a value for the key.</param>
        /// <returns>
        ///     The value for the key. This will be either the existing value for the key if the key is already in the dictionary, or the new value for the key as returned by valueFactory if the key was not in the dictionary.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">dictionary</exception>
        public static TValue GetOrAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, TValue> valueFactory)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }
            if (valueFactory == null)
            {
                throw new ArgumentNullException("valueFactory");
            }

            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            value = valueFactory(key);
            dictionary.Add(key, value);
            return value;
        }

        /// <summary>
        ///     Merges two dictionaries into a new dictionary.
        /// </summary>
        /// <typeparam name="TKey"> The type of the key. </typeparam>
        /// <typeparam name="TValue"> The type of the value. </typeparam>
        /// <param name="dictionary1"> The first dictionary from which to merge. </param>
        /// <param name="dictionary2"> The second dictionary from which to merge. </param>
        /// <param name="replace">
        ///     if set to <c>true</c> , replace values in dictionary1 with values in dictionary2 for keys that are present in both dictionaries; otherwise, values in dictionary1 are preserved.
        /// </param>
        /// <param name="comparer"> The key comparer. </param>
        /// <returns> A new dictionary containing the merged values from both source dictionaries. </returns>
        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary1,
            IDictionary<TKey, TValue> dictionary2,
            bool replace = false,
            IEqualityComparer<TKey> comparer = null)
        {
            IDictionary<TKey, TValue> result = comparer == null
                                                   ? new Dictionary<TKey, TValue>()
                                                   : new Dictionary<TKey, TValue>(comparer);

            var first = dictionary2;
            var second = dictionary1;

            if (replace)
            {
                first = dictionary1;
                second = dictionary2;
            }

            if (first != null)
            {
                foreach (var pair in first)
                {
                    result[pair.Key] = pair.Value;
                }
            }

            if (second != null)
            {
                foreach (var pair in second)
                {
                    result[pair.Key] = pair.Value;
                }
            }

            return result;
        }

        /// <summary>
        ///     Attempts to add the specified key and value to the dictionary.
        /// </summary>
        /// <typeparam name="TKey"> The type of the key. </typeparam>
        /// <typeparam name="TValue"> The type of the value. </typeparam>
        /// <param name="dictionary"> The dictionary. </param>
        /// <param name="key"> The key of the element to add. </param>
        /// <param name="value"> The value of the element to add. The value can be a null reference for reference types. </param>
        /// <returns> true if the key/value pair was added to the dictionary successfully. If the key already exists, this method returns false. </returns>
        public static bool TryAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key, TValue value)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }
            if (dictionary.ContainsKey(key))
            {
                return false;
            }
            dictionary.Add(key, value);
            return true;
        }
    }
}