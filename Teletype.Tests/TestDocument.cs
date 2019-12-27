using System;
using System.Collections.Generic;
using Teletype.Contracts;

namespace Teletype.Tests
{
    /// <summary>
    /// Teletype document that has extra metadata to facilitate unit-testing.
    /// </summary>
    public sealed class TestDocument : Document
    {
        /// <summary>
        /// Gets or sets raw text document associated with this Teletype document instance.
        /// </summary>
        /// <remarks>
        /// This document can ony deal with linear text positions (character index) and represents
        /// a simple text editor that will be at the front-end.
        /// </remarks>
        public RawDocument TestLocalDocument { get; set; }

        /// <summary>
        /// Gets or sets current timestamp.
        /// </summary>
        public long Now { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDocument"/> class
        /// with the provided site identifier.
        /// </summary>
        /// <param name="siteId">Identifier of the site</param>
        public TestDocument(int siteId)
            : base(siteId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDocument"/> class
        /// with the provided site identifier and initial text.
        /// </summary>
        /// <param name="siteId">Identifier of the site</param>
        /// <param name="text">Initial document text</param>
        public TestDocument(int siteId, string text)
            : base(siteId, text)
        {
        }

        /// <summary>
        /// Reverts changes done by the given set of operations.
        /// </summary>
        /// <param name="operationsToUndo">Operations to undo</param>
        /// <returns>Collection of counter-operations and associated text modifications.</returns>
        /// <remarks>
        /// This is a public member to expose internal functionality for unit testing.
        /// </remarks>
        public Tuple<IReadOnlyList<UndoOperation>, IReadOnlyList<TextUpdate>> UndoRedoOperations(IEnumerable<IOperation> operationsToUndo)
        {
            return UndoOrRedoOperations(operationsToUndo);
        }

        /// <summary>
        /// Retrieves current timestamp.
        /// </summary>
        /// <returns>Current timestamp.</returns>
        protected override long GetNow()
        {
            return Now;
        }
    }
}
