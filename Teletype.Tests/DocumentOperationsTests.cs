using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Teletype.Contracts;

namespace Teletype.Tests
{
    /// <summary>
    /// Defines unit tests for basic text operations of the <see cref="Document"/> class.
    /// </summary>
    [TestClass]
    public class DocumentOperationsTests : DocumentTestsBase
    {
        [TestMethod]
        [Description("Concurrent inserts at 0")]
        public void ConcurrentInsertsAt0()
        {
            var replica1 = BuildDocument(1);
            var replica2 = ReplicateDocument(2, replica1);

            var ops1 = PerformInsert(replica1, Point.Zero, "a");
            var ops2 = PerformInsert(replica2, Point.Zero, "b");
            IntegrateOperations(replica1, ops2);
            IntegrateOperations(replica2, ops1);

            Assert.AreEqual("ab", replica1.TestLocalDocument.Text);
            Assert.AreEqual("ab", replica2.TestLocalDocument.Text);
        }

        [TestMethod]
        [Description("Concurrent inserts at the same position inside a previous insertion")]
        public void ConcurrentInsertsAtSamePositionWithinExisting()
        {
            var replica1 = BuildDocument(1, "ABCDEFG");
            var replica2 = ReplicateDocument(2, replica1);

            var ops1 = PerformInsert(replica1, P(0, 2), "+++");
            var ops2 = PerformInsert(replica2, P(0, 2), "***");
            IntegrateOperations(replica1, ops2);
            IntegrateOperations(replica2, ops1);

            Assert.AreEqual("AB+++***CDEFG", replica1.TestLocalDocument.Text);
            Assert.AreEqual("AB+++***CDEFG", replica2.TestLocalDocument.Text);
        }

        [TestMethod]
        [Description("Concurrent inserts at different positions inside a previous insertion")]
        public void ConcurrentInsertsAtDifferentPositionWithinExisting()
        {
            var replica1 = BuildDocument(1, "ABCDEFG");
            var replica2 = ReplicateDocument(2, replica1);

            var ops1 = PerformInsert(replica1, P(0, 6), "+++");
            var ops2 = PerformInsert(replica2, P(0, 2), "***");
            IntegrateOperations(replica1, ops2);
            IntegrateOperations(replica2, ops1);

            Assert.AreEqual("AB***CDEF+++G", replica1.TestLocalDocument.Text);
            Assert.AreEqual("AB***CDEF+++G", replica2.TestLocalDocument.Text);
        }

        [TestMethod]
        [Description("Concurrent overlapping deletions")]
        public void ConcurrentOverlappingDeletions()
        {
            var replica1 = BuildDocument(1, "ABCDEFG");
            var replica2 = ReplicateDocument(2, replica1);

            var ops1 = PerformDelete(replica1, P(0, 2), P(0, 5));
            var ops2 = PerformDelete(replica2, P(0, 4), P(0, 6));
            IntegrateOperations(replica1, ops2);
            IntegrateOperations(replica2, ops1);

            Assert.AreEqual("ABG", replica1.TestLocalDocument.Text);
            Assert.AreEqual("ABG", replica2.TestLocalDocument.Text);
        }

        [TestMethod]
        [Description("Undoing an insertion containing other insertions")]
        public void UndoInsertionContainingOtherInsertions()
        {
            var replica1 = BuildDocument(1);
            var replica2 = ReplicateDocument(2, replica1);

            var ops1 = PerformInsert(replica1, P(0, 0), "ABCDEFG");
            IntegrateOperations(replica2, ops1);

            var ops2 = PerformInsert(replica1, P(0, 3), "***");
            IntegrateOperations(replica2, ops2);

            var ops1Undo = PerformUndoOrRedoOperations(replica1, ops1);
            IntegrateOperations(replica2, ops1Undo);

            Assert.AreEqual("***", replica1.TestLocalDocument.Text);
            Assert.AreEqual("***", replica2.TestLocalDocument.Text);
        }

        [TestMethod]
        [Description("Undoing an insertion containing a deletion")]
        public void UndoInsertionContainingDeletion()
        {
            var replica1 = BuildDocument(1);
            var replica2 = ReplicateDocument(2, replica1);

            var ops1 = PerformInsert(replica1, P(0, 0), "ABCDEFG");
            IntegrateOperations(replica2, ops1);

            var ops2 = PerformDelete(replica1, P(0, 3), P(0, 6));
            IntegrateOperations(replica2, ops2);

            var ops1Undo = PerformUndoOrRedoOperations(replica1, ops1);
            IntegrateOperations(replica2, ops1Undo);

            Assert.AreEqual("", replica1.TestLocalDocument.Text);
            Assert.AreEqual("", replica2.TestLocalDocument.Text);
        }

        [TestMethod]
        [Description("Undoing a deletion that overlaps another concurrent deletion")]
        public void UndoDeletionOverlappingAnotherDeletion()
        {
            var replica1 = BuildDocument(1, "ABCDEFG");
            var replica2 = ReplicateDocument(2, replica1);

            var ops1 = PerformDelete(replica1, P(0, 1), P(0, 4));
            var ops2 = PerformDelete(replica2, P(0, 3), P(0, 6));
            IntegrateOperations(replica1, ops2);
            IntegrateOperations(replica2, ops1);
            var ops2Undo = PerformUndoOrRedoOperations(replica1, ops2);
            IntegrateOperations(replica2, ops2Undo);

            Assert.AreEqual("AEFG", replica1.TestLocalDocument.Text);
            Assert.AreEqual("AEFG", replica2.TestLocalDocument.Text);
        }

        [TestMethod]
        [Description("Inserting in the middle of an undone deletion and then redoing the deletion")]
        public void InsertWithinUndoneDeletionThenRedoDeletion()
        {
            var document = BuildDocument(1, "ABCDEFG");

            var deleteOps = PerformDelete(document, P(0, 1), P(0, 6));
            PerformUndoOrRedoOperations(document, deleteOps);
            PerformInsert(document, P(0, 3), "***");
            PerformUndoOrRedoOperations(document, deleteOps); // Redo

            Assert.AreEqual("A***G", document.TestLocalDocument.Text);
        }

        [TestMethod]
        [Description("Applying remote operations generated by a copy of the local replica")]
        public void ApplyRemoteOperationsFromReplica()
        {
            var localReplica = BuildDocument(1);
            var remoteReplica = BuildDocument(1);

            IntegrateOperations(localReplica, PerformInsert(remoteReplica, P(0, 0), "ABCDEFG"));
            IntegrateOperations(localReplica, PerformInsert(remoteReplica, P(0, 3), "+++"));
            PerformInsert(localReplica, P(0, 1), "***");

            Assert.AreEqual("A***BC+++DEFG", localReplica.TestLocalDocument.Text);
        }

        [TestMethod]
        [Description("Updating marker layers")]
        public void UpdateMarkerLayers()
        {
            var replica1 = BuildDocument(1, "ABCDEFG");
            var replica2 = ReplicateDocument(2, replica1);

            var insert1 = PerformInsert(replica1, P(0, 6), "+++");
            PerformInsert(replica2, P(0, 2), "**");
            IntegrateOperations(replica2, insert1);

            IntegrateOperations(
                replica2,
                PerformUpdateMarkers(
                    replica1,
                    MarkersForLayers()
                        // Create a marker layer with 1 marker
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(false, false, true, new Range(P(0, 1), P(0, 9)))))));

            AssertMarkersForSites(
                replica1.GetMarkers(),
                MarkersForSites()
                    // Site 1
                    .Set(1, s => s
                        // Markers layer 1
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(false, false, true, new Range(P(0, 1), P(0, 9)))))).AsReadOnly());

            AssertMarkersForSites(
                replica2.GetMarkers(),
                MarkersForSites()
                    // Site 1
                    .Set(1, s => s
                        // Markers layer 1
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(false, false, true, new Range(P(0, 1), P(0, 11)))))).AsReadOnly());

            AssertMarkersForSites(replica2.GetMarkers(), replica2.TestLocalDocument.Markers.AsReadOnly());

            IntegrateOperations(
                replica2,
                PerformUpdateMarkers(
                    replica1,
                    MarkersForLayers()
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(true, true, true, new Range(P(0, 2), P(0, 10))))
                            .Set(2, new Marker<Range>(new Range(P(0, 0), P(0, 1)))))
                        .Set(2, l => l
                            .Set(1, new Marker<Range>(new Range(P(0, 1), P(0, 2)))))));

            AssertMarkersForSites(
                replica1.GetMarkers(),
                MarkersForSites()
                    .Set(1, s => s
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(true, true, true, new Range(P(0, 2), P(0, 10))))
                            .Set(2, new Marker<Range>(false, false, true, new Range(P(0, 0), P(0, 1)))))
                        .Set(2, l => l
                            .Set(1, new Marker<Range>(false, false, true, new Range(P(0, 1), P(0, 2)))))).AsReadOnly());

            AssertMarkersForSites(
                replica2.GetMarkers(),
                MarkersForSites()
                    .Set(1, s => s
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(true, true, true, new Range(P(0, 4), P(0, 12))))
                            .Set(2, new Marker<Range>(false, false, true, new Range(P(0, 0), P(0, 1)))))
                        .Set(2, l => l
                            .Set(1, new Marker<Range>(false, false, true, new Range(P(0, 1), P(0, 4)))))).AsReadOnly());

            AssertMarkersForSites(replica2.GetMarkers(), replica2.TestLocalDocument.Markers.AsReadOnly());

            IntegrateOperations(
                replica2,
                PerformUpdateMarkers(
                    replica1,
                    MarkersForLayers()
                        .Set(1, l => l
                            // Delete marker
                            .Set(2, null))
                        // Delete marker layer
                        .Set(2, (Dictionary<int, Marker<Range>>)null)));

            AssertMarkersForSites(
                replica1.GetMarkers(),
                MarkersForSites()
                    .Set(1, s => s
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(true, true, true, new Range(P(0, 2), P(0, 10)))))).AsReadOnly());

            AssertMarkersForSites(
                replica2.GetMarkers(),
                MarkersForSites()
                    .Set(1, s => s
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(true, true, true, new Range(P(0, 4), P(0, 12)))))).AsReadOnly());

            AssertMarkersForSites(replica2.GetMarkers(), replica2.TestLocalDocument.Markers.AsReadOnly());
        }

        [TestMethod]
        [Description("Deferring marker updates until the dependencies of their logical ranges arrive")]
        public void DefferMarkerUpdates()
        {
            var replica1 = BuildDocument(1);
            var replica2 = ReplicateDocument(2, replica1);

            var insertion1 = PerformInsert(replica1, P(0, 0), "ABCDEFG");
            var insertion2 = PerformInsert(replica1, P(0, 4), "WXYZ");

            var layerUpdate1 =
                replica1.UpdateMarkers(
                    MarkersForLayers()
                        .Set(1, l => l
                            // This only depends on insertion 1
                            .Set(1, new Marker<Range>(new Range(P(0, 1), P(0, 3))))

                            // This depends on insertion 2
                            .Set(2, new Marker<Range>(new Range(P(0, 5), P(0, 7))))

                            // This depends on insertion 2 but will be overwritten before
                            // insertion 2 arrives at site 2
                            .Set(3, new Marker<Range>(new Range(P(0, 5), P(0, 7))))));

            var layerUpdate2 =
                replica1.UpdateMarkers(
                    MarkersForLayers()
                        .Set(1, l => l
                            .Set(3, new Marker<Range>(new Range(P(0, 1), P(0, 3))))));

            replica2.IntegrateOperations(new[] { insertion1 });

            {
                var integratedOperations = replica2.IntegrateOperations(new[] { layerUpdate1, layerUpdate2 });
                AssertMarkersForSites(
                    integratedOperations.MarkerUpdates.AsReadOnly(),
                    MarkersForSites()
                        .Set(1, s => s
                            .Set(1, l => l
                                .Set(1, new Marker<Range>(false, false, true, new Range(P(0, 1), P(0, 3))))
                                .Set(3, new Marker<Range>(false, false, true, new Range(P(0, 1), P(0, 3)))))).AsReadOnly());
            }

            {
                var integratedOperations = replica2.IntegrateOperations(new[] { insertion2 });
                AssertMarkersForSites(
                    integratedOperations.MarkerUpdates.AsReadOnly(),
                    MarkersForSites()
                        .Set(1, s => s
                            .Set(1, l => l
                                .Set(2, new Marker<Range>(false, false, true, new Range(P(0, 5), P(0, 7)))))).AsReadOnly());
            }
        }

        #region Helper methods

        /// <summary>
        /// Reverts changes done by the given set of operations.
        /// </summary>
        /// <param name="replica">Teletype document instance</param>
        /// <param name="operationToUndo">Operations to undo</param>
        /// <returns>Collection of counter-operations.</returns>
        private IReadOnlyList<UndoOperation> PerformUndoOrRedoOperations(TestDocument replica, IOperation operationToUndo)
        {
            var (operations, textUpdates) = replica.UndoRedoOperations(new[] { operationToUndo });
            replica.TestLocalDocument.UpdateText(textUpdates);
            return operations;
        }

        /// <summary>
        /// Updates markers in the document.
        /// </summary>
        /// <param name="replica">Teletype document instance</param>
        /// <param name="markerUpdates">Marksers updates</param>
        /// <returns>Markers updates operation that can be shared with other sites.</returns>
        private MarkersUpdateOperation PerformUpdateMarkers(TestDocument replica, Dictionary<int, Dictionary<int, Marker<Range>>> markerUpdates)
        {
            replica.TestLocalDocument.UpdateMarkers(
                new Dictionary<int, Dictionary<int, Dictionary<int, Marker<Range>>>>
                {
                    { replica.SiteId, markerUpdates }
                });

            return replica.UpdateMarkers(markerUpdates);
        }

        #endregion Helper methods
    }
}
