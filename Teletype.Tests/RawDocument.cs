using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;
using Teletype.Contracts;

namespace Teletype.Tests
{
    /// <summary>
    /// Simple document that contains text and allows linear insert/delete modifications.
    /// </summary>
    public sealed class RawDocument
    {
        /// <summary>
        /// Gets the current text of the document.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// Gets all markers in the document.
        /// </summary>
        public Dictionary<int, Dictionary<int, Dictionary<int, Marker<Range>>>> Markers { get; }

        /// <summary>
        /// Intializes a new instance of the <see cref="RawDocument"/> class
        /// with the provided initial text.
        /// </summary>
        /// <param name="text">Initial document text</param>
        public RawDocument(string text)
        {
            Text = text;
            Markers = new Dictionary<int, Dictionary<int, Dictionary<int, Marker<Range>>>>();
        }

        /// <summary>
        /// Updates markers in the document.
        /// </summary>
        /// <param name="updatesBySiteId">Mapping between site identifier, markers layer identifier, marker identifier and actual marker</param>
        public void UpdateMarkers(Dictionary<int, Dictionary<int, Dictionary<int, Marker<Range>>>> updatesBySiteId)
        {
            foreach (var siteUpdates in updatesBySiteId)
            {
                if (!Markers.TryGetValue(siteUpdates.Key, out var layersById))
                {
                    layersById = new Dictionary<int, Dictionary<int, Marker<Range>>>();
                    Markers.Add(siteUpdates.Key, layersById);
                }

                var updatesByLayerId = siteUpdates.Value;
                foreach (var layerUpdates in updatesByLayerId)
                {
                    var updatesByMarkerId = layerUpdates.Value;

                    if (updatesByMarkerId == null)
                    {
                        Trace.Assert(layersById.ContainsKey(layerUpdates.Key), "Layer should exist");
                        layersById.Remove(layerUpdates.Key);
                    }
                    else
                    {
                        if (!layersById.TryGetValue(layerUpdates.Key, out var markersById))
                        {
                            markersById = new Dictionary<int, Marker<Range>>();
                            layersById.Add(layerUpdates.Key, markersById);
                        }

                        foreach (var markerUpdate in updatesByMarkerId)
                        {
                            var marker = markerUpdate.Value;
                            if (marker == null)
                            {
                                Trace.Assert(markersById.ContainsKey(markerUpdate.Key), "Marker should exist");
                                markersById.Remove(markerUpdate.Key);
                            }
                            else
                            {
                                markersById[markerUpdate.Key] = marker;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the document text according to given <paramref name="changes"/>.
        /// </summary>
        /// <param name="changes">Text modifications to apply</param>
        /// <remarks>
        /// Changes are appllied in reverse, because it is assumed that all of these changes happened at the same time.
        /// If we apply them in original order, then we would need to adjust all the start/end positions of subsequent
        /// text updates. Instead we just start from the end, so that all positions before the current change are still
        /// valid.
        /// </remarks>
        public void UpdateText(IReadOnlyList<TextUpdate> changes)
        {
            for (var changeIndex = changes.Count - 1; changeIndex >= 0; --changeIndex)
            {
                var change = changes[changeIndex];
                SetTextInRange(change.OldStart, change.OldEnd, change.NewText);
            }
        }

        /// <summary>
        /// Sets document text at given range.
        /// </summary>
        /// <param name="oldStart">Range start position</param>
        /// <param name="oldEnd">Range end position</param>
        /// <param name="text">New text</param>
        public void SetTextInRange(Point oldStart, Point oldEnd, string text)
        {
            if (oldEnd.CompareTo(oldStart) > 0)
            {
                Delete(oldStart, oldEnd);
            }

            if (text.Length > 0)
            {
                Insert(oldStart, text);
            }

            SpliceMarkers(oldStart, oldEnd, oldStart.Traverse(Point.GetExtentForText(text)));
        }

        /// <summary>
        /// Updates range for markers that intersect with the given text range.
        /// For example, in cases when text is inserted within the active selection - it should be expanded.
        /// </summary>
        /// <param name="oldStart">Original start of the text range</param>
        /// <param name="oldEnd">Original end of the text range</param>
        /// <param name="newEnd">New end of the text range</param>
        private void SpliceMarkers(Point oldStart, Point oldEnd, Point newEnd)
        {
            var isInsertion = oldStart.CompareTo(oldEnd) == 0;

            foreach (var layersForSite in Markers)
            {
                var layersById = layersForSite.Value;

                foreach (var markersForLayer in layersById)
                {
                    var markersById = markersForLayer.Value;

                    foreach (var markerWithId in markersById)
                    {
                        var existingMarker = markerWithId.Value;
                        var range = existingMarker.Range;
                        var exclusive = existingMarker.Exclusive;

                        var rangeIsEmpty = range.Start.CompareTo(range.End) == 0;

                        var moveMarkerStart =
                            oldStart.CompareTo(range.Start) < 0
                            || (exclusive
                                && (!rangeIsEmpty || isInsertion)
                                && oldStart.CompareTo(range.Start) == 0);

                        var moveMarkerEnd =
                            moveMarkerStart
                            || oldStart.CompareTo(range.End) < 0
                            || (!exclusive && oldEnd.CompareTo(range.End) == 0);

                        Point? newMarkerStart = null, newMarkerEnd = null;

                        if (moveMarkerStart)
                        {
                            if (oldEnd.CompareTo(range.Start) <= 0)
                            {
                                // splice precedes marker start
                                newMarkerStart = newEnd.Traverse(Point.Traversal(range.Start, oldEnd));
                            }
                            else
                            {
                                // splice surrounds marker start
                                newMarkerStart = newEnd;
                            }
                        }

                        if (moveMarkerEnd)
                        {
                            if (oldEnd.CompareTo(range.End) <= 0)
                            {
                                // splice precedes marker end
                                newMarkerEnd = newEnd.Traverse(Point.Traversal(range.End, oldEnd));
                            }
                            else
                            {
                                // splice surrounds marker end
                                newMarkerEnd = newEnd;
                            }
                        }

                        markersById[markerWithId.Key] =
                            new Marker<Range>(
                                existingMarker.Exclusive,
                                existingMarker.Reversed,
                                existingMarker.Tailed,
                                new Range(
                                    newMarkerStart.HasValue ? newMarkerStart.Value : existingMarker.Range.Start,
                                    newMarkerEnd.HasValue ? newMarkerEnd.Value : existingMarker.Range.End));
                    }
                }
            }
        }

        /// <summary>
        /// Inserts a <paramref name="text"/> at given <paramref name="position"/>.
        /// </summary>
        /// <param name="position">Position to insert the text at</param>
        /// <param name="text">Text to insert</param>
        private void Insert(Point position, string text)
        {
            Text = Text.Insert(Point.CharacterIndexForPosition(Text, position), text);
        }

        /// <summary>
        /// Deletes text from given <paramref name="startPosition"/> to the <paramref name="endPosition"/>.
        /// </summary>
        /// <param name="startPosition">Start of the deletion range</param>
        /// <param name="endPosition">End of the deletion range</param>
        private void Delete(Point startPosition, Point endPosition)
        {
            var textExtent = Point.GetExtentForText(Text);
            Assert.IsTrue(startPosition.CompareTo(textExtent) < 0);
            Assert.IsTrue(endPosition.CompareTo(textExtent) <= 0);
            var startIndex = Point.CharacterIndexForPosition(Text, startPosition);
            var endIndex = Point.CharacterIndexForPosition(Text, endPosition);
            Text = Text.Remove(startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// Retrieve text for the given <paramref name="row"/> in the document.
        /// </summary>
        /// <param name="row">Row number</param>
        /// <returns>Text for the given <paramref name="row"/>.</returns>
        private string LineForRow(int row)
        {
            var startIndex = Point.CharacterIndexForPosition(Text, new Point(row, 0));
            var endIndex = Point.CharacterIndexForPosition(Text, new Point(row + 1, 0)) - 1;
            return Text.Substring(startIndex, endIndex - startIndex + 1);
        }

        /// <summary>
        /// Retrieves number of rows in the document.
        /// </summary>
        /// <returns>Number of rows in the document.</returns>
        private int GetLineCount()
        {
            return Point.GetExtentForText(Text).Row + 1;
        }

        /// <summary>
        /// Retrieves text for the given range.
        /// </summary>
        /// <param name="start">Start of the range</param>
        /// <param name="end">End of the range</param>
        /// <returns>Text within the requested range.</returns>
        private string GetTextInRange(Point start, Point end)
        {
            var startIndex = Point.CharacterIndexForPosition(Text, start);
            var endIndex = Point.CharacterIndexForPosition(Text, end);
            return Text.Substring(startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// Gets the current document text.
        /// </summary>
        /// <returns>Document text.</returns>
        public override string ToString()
        {
            return Text;
        }
    }
}
