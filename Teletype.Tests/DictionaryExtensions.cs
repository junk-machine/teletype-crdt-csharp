using System;
using System.Collections.Generic;
using System.Linq;

namespace Teletype.Tests
{
    /// <summary>
    /// Defines extension methods for <see cref="Dictionary{TKey, TValue}"/> class.
    /// </summary>
    internal static class DictionaryExtensions
    {
        /// <summary>
        /// Adds or updates the value in the nested dictionary.
        /// Nested dictionary is created, if it does not exist.
        /// </summary>
        /// <typeparam name="TKey">Type of the key in the dictionary</typeparam>
        /// <typeparam name="TNestedKey">Type of the key in the nested dictionary</typeparam>
        /// <typeparam name="TNestedValue">Type of the value in the nested dictionary</typeparam>
        /// <param name="dictionary">Dictionary instance to set the value in</param>
        /// <param name="key">Dictionary key</param>
        /// <param name="valueSetter">Value setter delegate to update nested dictionary</param>
        /// <returns>Original dictionary with new nested value set</returns>
        public static Dictionary<TKey, Dictionary<TNestedKey, TNestedValue>> Set<TKey, TNestedKey, TNestedValue>(
            this Dictionary<TKey, Dictionary<TNestedKey, TNestedValue>> dictionary,
            TKey key,
            Action<Dictionary<TNestedKey, TNestedValue>> valueSetter)
        {
            if (!dictionary.TryGetValue(key, out var subDictionary))
            {
                dictionary[key] = subDictionary = new Dictionary<TNestedKey, TNestedValue>();
            }

            valueSetter(subDictionary);
            return dictionary;
        }

        /// <summary>
        /// Adds or updates a value in the dictionary.
        /// </summary>
        /// <typeparam name="TKey">Type of the key in the dictionary</typeparam>
        /// <typeparam name="TValue">Type of the value in the dictionary</typeparam>
        /// <param name="dictionary">Dictionary instance to set the value in</param>
        /// <param name="key">Dictionary key</param>
        /// <param name="value">Value to set</param>
        /// <returns>Original dictionary with new value set</returns>
        public static Dictionary<TKey, TValue> Set<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            dictionary[key] = value;
            return dictionary;
        }

        /// <summary>
        /// Converts one level of nested <see cref="Dictionary{TKey, TValue}"/> to read-only <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of the key in the dictionary</typeparam>
        /// <typeparam name="TKey1">Type of the key in the nested dictionary</typeparam>
        /// <typeparam name="TValue1">Type of the value in the nested dictionary</typeparam>
        /// <param name="dictionary">Input dictionary</param>
        /// <returns>New nested read-only dictionary.</returns>
        public static IReadOnlyDictionary<TKey, IReadOnlyDictionary<TKey1, TValue1>> AsReadOnly<TKey, TKey1, TValue1>(
            this Dictionary<TKey, Dictionary<TKey1, TValue1>> dictionary)
        {
            return
                dictionary.ToDictionary(
                    r => r.Key,
                    r => (IReadOnlyDictionary<TKey1, TValue1>)r.Value.ToDictionary(
                        r1 => r1.Key,
                        r1 => r1.Value));
        }
        
        /// <summary>
        /// Converts two levels of nested <see cref="Dictionary{TKey, TValue}"/> to read-only <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of the key in the dictionary</typeparam>
        /// <typeparam name="TKey1">Type of the key in the nested dictionary</typeparam>
        /// <typeparam name="TKey2">Type of the key in the second level nested dictionary</typeparam>
        /// <typeparam name="TValue2">Type of the value in the second level nested dictionary</typeparam>
        /// <param name="dictionary">Input dictionary</param>
        /// <returns>New nested read-only dictionary.</returns>
        public static IReadOnlyDictionary<TKey, IReadOnlyDictionary<TKey1, IReadOnlyDictionary<TKey2, TValue2>>> AsReadOnly<TKey, TKey1, TKey2, TValue2>(
            this Dictionary<TKey, Dictionary<TKey1, Dictionary<TKey2, TValue2>>> dictionary)
        {
            return
                dictionary.ToDictionary(
                    r => r.Key,
                    r => (IReadOnlyDictionary<TKey1, IReadOnlyDictionary<TKey2, TValue2>>)r.Value.ToDictionary(
                        r1 => r1.Key,
                        r1 => (IReadOnlyDictionary<TKey2, TValue2>)r1.Value.ToDictionary(
                            r2 => r2.Key,
                            r2 => r2.Value)));
        }
    }
}
