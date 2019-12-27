using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using Teletype.Contracts;

namespace Teletype.Tests
{
    /// <summary>
    /// Base class for Teletype <see cref="Document"/> tests.
    /// </summary>
    public abstract class DocumentTestsBase
    {
        /// <summary>
        /// Creates new Teletype document for a given site with the provided initial text.
        /// </summary>
        /// <param name="siteId">Site identifier for the document instance</param>
        /// <param name="text">Initial document text</param>
        /// <returns>New Teletype document.</returns>
        protected TestDocument BuildDocument(int siteId, string text = null)
        {
            var document = new TestDocument(siteId, text);
            document.TestLocalDocument = new RawDocument(document.GetText());
            return document;
        }

        /// <summary>
        /// Creates a copy of the given Teletype document with new site identifier.
        /// </summary>
        /// <param name="siteId">Site identifier for the new document instance</param>
        /// <param name="document">Original Teletype document</param>
        /// <returns>Copy of the original document with new site identifier.</returns>
        protected TestDocument ReplicateDocument(int siteId, Document document)
        {
            var replica = new TestDocument(siteId);
            replica.IntegrateOperations(document.GetOperations().ToList());
            replica.TestLocalDocument = new RawDocument(replica.GetText());
            return replica;
        }

        /// <summary>
        /// Inserts <paramref name="text"/> into the Teletype document at a given <paramref name="position"/>.
        /// </summary>
        /// <param name="replica">Teletype document instance</param>
        /// <param name="position">Position to insert the text at</param>
        /// <param name="text">Text to insert</param>
        /// <returns>Teletype splice operation that represents text insertion.</returns>
        protected SpliceOperation PerformInsert(TestDocument replica, Point position, string text)
        {
            return PerformSetTextInRange(replica, position, position, text);
        }

        /// <summary>
        /// Deletes the text in the Teletype document.
        /// </summary>
        /// <param name="replica">Teletype document instance</param>
        /// <param name="start">Start of the deletion range</param>
        /// <param name="end">End of the deletion range</param>
        /// <returns>Teletype splice operation that represents text deletion.</returns>
        protected SpliceOperation PerformDelete(TestDocument replica, Point start, Point end)
        {
            return PerformSetTextInRange(replica, start, end, "");
        }

        /// <summary>
        /// Updates text in the given range in the provided Teletype document.
        /// </summary>
        /// <param name="replica">Teletype document instance</param>
        /// <param name="start">Start of the range</param>
        /// <param name="end">End of the range</param>
        /// <param name="text">New text for the range</param>
        /// <returns>Teletype splice operation that represents text modification.</returns>
        protected SpliceOperation PerformSetTextInRange(TestDocument replica, Point start, Point end, string text)
        {
            replica.TestLocalDocument.SetTextInRange(start, end, text);
            var spliceOperation = replica.SetTextInRange(start, end, text);
            return spliceOperation;
        }

        /// <summary>
        /// Integrates single operation into the Teletype document.
        /// </summary>
        /// <param name="replica">Teletype document instance</param>
        /// <param name="operation">Operation to integrate</param>
        protected void IntegrateOperations(TestDocument replica, IOperation operation)
        {
            IntegrateOperations(replica, new[] { operation });
        }

        /// <summary>
        /// Integrates one or more operations into the Teletype document.
        /// </summary>
        /// <param name="replica">Teletype document instance</param>
        /// <param name="operations">Operations to integrate</param>
        protected void IntegrateOperations(TestDocument replica, IEnumerable<IOperation> operations)
        {
            var documentUpdates = replica.IntegrateOperations(operations.ToList());
            replica.TestLocalDocument.UpdateText(documentUpdates.TextUpdates);
            replica.TestLocalDocument.UpdateMarkers(documentUpdates.MarkerUpdates);
        }

        /// <summary>
        /// Verifies that <paramref name="actual"/> markers updates for sites are the same as <paramref name="expected"/>.
        /// </summary>
        /// <param name="expected">Expected markers updates</param>
        /// <param name="actual">Actual markers updates</param>
        protected static void AssertMarkersForSites(
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>>> actual,
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>>> expected)
        {
            CollectionAssert.That.DictionariesAreEquivalent(
                expected,
                actual,
                (siteId, expectedLayers, actualLayers) => AssertMarkersForLayers(actualLayers, expectedLayers, siteId));
        }

        /// <summary>
        /// Verifies that <paramref name="actual"/> markers updates for layers are the same as <paramref name="expected"/>.
        /// </summary>
        /// <param name="expected">Expected markers updates</param>
        /// <param name="actual">Actual markers updates</param>
        /// <param name="siteId">Optional. Site identifier, if processing layers for a particular site</param>
        protected static void AssertMarkersForLayers(
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> actual,
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> expected,
            int siteId = -1)
        {
            CollectionAssert.That.DictionariesAreEquivalent(
                expected,
                actual,
                (layerId, expectedMarkers, actualMarkers) => CollectionAssert.That.DictionariesAreEquivalent(
                    expectedMarkers,
                    actualMarkers,
                    (markerId, expectedMarker, actualMarker) =>
                    {
                        var markerPath = 
                            siteId == -1
                                ? $"(LayerId: {layerId}; MarkerId: {markerId})"
                                : $"(SiteId: {siteId}; LayerId: {layerId}; MarkerId: {markerId})";

                        if (expectedMarker == null)
                        {
                            Assert.IsNull(actualMarker, $"Marker {markerPath} is expected to be null");
                            return;
                        }

                        Assert.IsNotNull(actualMarker, $"Marker {markerPath} should not be null");

                        Assert.AreEqual(expectedMarker.Exclusive, actualMarker.Exclusive, $"'Exclusive' property for marker {markerPath} is incorrect");
                        Assert.AreEqual(expectedMarker.Reversed, actualMarker.Reversed, $"'Reversed' property for marker {markerPath} is incorrect");
                        Assert.AreEqual(expectedMarker.Tailed, actualMarker.Tailed, $"'Tailed' property for marker {markerPath} is incorrect");

                        if (expectedMarker.Range == null)
                        {
                            Assert.IsNull(actualMarker.Range, $"Range for marker {markerPath} is expected to be null");
                        }
                        else
                        {
                            Assert.IsNotNull(actualMarker.Range, $"Range for marker {markerPath} should not be null");
                            Assert.IsTrue(
                                expectedMarker.Range.Start.CompareTo(actualMarker.Range.Start) == 0,
                                $"Range start for the marker {markerPath} is incorrect");
                            Assert.IsTrue(
                                expectedMarker.Range.End.CompareTo(actualMarker.Range.End) == 0,
                                $"Range end for the marker {markerPath} is incorrect");
                        }
                    }));
        }

        /// <summary>
        /// Creates a top-level dictionary for marker layers to markers map.
        /// </summary>
        /// <returns>Empty map between sites, marker layers and associated markers.</returns>
        protected static Dictionary<int, Dictionary<int, Dictionary<int, Marker<Range>>>> MarkersForSites()
        {
            return new Dictionary<int, Dictionary<int, Dictionary<int, Marker<Range>>>>();
        }

        /// <summary>
        /// Creates a top-level dictionary for marker layers to markers map.
        /// </summary>
        /// <returns>Empty map between marker layers and associated markers.</returns>
        protected static Dictionary<int, Dictionary<int, Marker<Range>>> MarkersForLayers()
        {
            return new Dictionary<int, Dictionary<int, Marker<Range>>>();
        }

        /// <summary>
        /// Creates new <see cref="Point"/> with the given <paramref name="row"/> and <paramref name="column"/>.
        /// </summary>
        /// <param name="row">Row index</param>
        /// <param name="column">Column index</param>
        /// <returns>New <see cref="Point"/> with the given coordinates.</returns>
        protected static Point P(int row, int column)
        {
            return new Point(row, column);
        }
    }
}
