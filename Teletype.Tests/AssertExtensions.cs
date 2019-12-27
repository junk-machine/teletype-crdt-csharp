using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Teletype.Tests
{
    /// <summary>
    /// Defines custom extensions for the <see cref="CollectionAssert"/> class.
    /// </summary>
    internal static class CollectionAssertExtensions
    {
        /// <summary>
        /// Checks if two dictionaries are equivalent (contain same records).
        /// </summary>
        /// <typeparam name="TKey">Type of the dictionary keys</typeparam>
        /// <typeparam name="TValue">Type of the dictionary values</typeparam>
        /// <param name="assert">Assert instance</param>
        /// <param name="expected">Expected dictionary</param>
        /// <param name="actual">Actual dictionary</param>
        /// <param name="valueAssertion">Assertion delegate to compare values with the same key</param>
        public static void DictionariesAreEquivalent<TKey, TValue>(
            this CollectionAssert assert,
            IReadOnlyDictionary<TKey, TValue> expected,
            IReadOnlyDictionary<TKey, TValue> actual,
            Action<TKey, TValue, TValue> valueAssertion)
        {
            if (expected == actual)
            {
                return;
            }

            if (expected == null || actual == null)
            {
                Assert.Fail($"Expected {expected}, got {actual}");
            }

            Assert.AreEqual(expected.Count, actual.Count, "Incorrect number of records in the dictionary");

            foreach (var expectedRecord in expected)
            {
                Assert.IsTrue(
                    actual.TryGetValue(expectedRecord.Key, out var actualValue),
                    $"Record with the key '{expectedRecord.Key}' is missing");

                valueAssertion(expectedRecord.Key, expectedRecord.Value, actualValue);
            }
        }
    }
}
