// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace CollapseComments
{
    public sealed class MultiLineAttributeOutlineTagger : ITagger<IOutliningRegionTag>
    {
        private readonly ITextBuffer buffer;
        private ITextSnapshot snapshot;
        private List<MultiLineAttributeRegion> regions;

        public MultiLineAttributeOutlineTagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
            this.snapshot = buffer.CurrentSnapshot;
            this.regions = new List<MultiLineAttributeRegion>();
            this.ReParse();
            this.buffer.Changed += this.BufferChanged;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            List<MultiLineAttributeRegion> currentRegions = this.regions;
            ITextSnapshot currentSnapshot = this.snapshot;
            SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
            int startLineNumber = entire.Start.GetContainingLine().LineNumber;
            int endLineNumber = entire.End.GetContainingLine().LineNumber;

            foreach (var region in currentRegions)
            {
                if (region.StartLine <= endLineNumber &&
                    region.EndLine >= startLineNumber)
                {
                    var startLine = currentSnapshot.GetLineFromLineNumber(region.StartLine);
                    var endLine = currentSnapshot.GetLineFromLineNumber(region.EndLine);

                    var shortText = startLine.GetText().Substring(region.StartOffset);
                    shortText += "...)]";

                    var hoverText = new StringBuilder();

                    string GetHoverLineText(string fullLineText)
                    {
                        var trimmed = fullLineText.Trim();

                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            return trimmed;
                        }

                        return null;
                    }

                    hoverText.AppendLine(currentSnapshot.GetLineFromLineNumber(region.StartLine).GetText().Substring(region.StartOffset));

                    for (int i = region.StartLine + 1; i <= region.EndLine - 1; i++)
                    {
                        var toAppend = GetHoverLineText(currentSnapshot.GetLineFromLineNumber(i).GetText());
                        if (toAppend != null)
                        {
                            hoverText.AppendLine(toAppend);
                        }
                    }

                    hoverText.Append(GetHoverLineText(currentSnapshot.GetLineFromLineNumber(region.EndLine).GetText()));

                    yield return new TagSpan<IOutliningRegionTag>(
                        new SnapshotSpan(startLine.Start + region.StartOffset, endLine.End),
                        new OutliningRegionTag(false, false, shortText, hoverText.ToString()));
                }
            }
        }

        private static SnapshotSpan AsSnapshotSpan(MultiLineAttributeRegion region, ITextSnapshot snapshot)
        {
            var startLine = snapshot.GetLineFromLineNumber(region.StartLine);
            var endLine = (region.StartLine == region.EndLine)
                        ? startLine
                        : snapshot.GetLineFromLineNumber(region.EndLine);

            return new SnapshotSpan(startLine.Start + region.StartOffset, endLine.Start + region.EndOffset);
        }

        private void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // If this isn't the most up-to-date version of the buffer, then ignore it for now (we'll eventually get another change event).
            if (e.After != this.buffer.CurrentSnapshot)
            {
                return;
            }

            this.ReParse();
        }

        private void ReParse()
        {
            ITextSnapshot newSnapshot = this.buffer.CurrentSnapshot;
            var newRegions = new List<MultiLineAttributeRegion>();

            MultiLineAttributeRegion currentRegion = null;

            foreach (var line in newSnapshot.Lines)
            {
                string text = line.GetText();

                var trimText = text.Trim();

                var startIndex = text.IndexOf("[");
                var startOffset = 1;

                var bracketIndex = text.IndexOf('(');

                if (!trimText.EndsWith("("))
                {
                    startIndex = -1;
                }
                else
                {
                    startIndex = bracketIndex;
                }

                var endIndex = text.IndexOf(")]");

                if (startIndex > 0 && endIndex < 0)
                {
                    if (currentRegion == null)
                    {
                        currentRegion = new MultiLineAttributeRegion
                        {
                            StartLine = line.LineNumber,
                            StartOffset = startIndex + startOffset,
                        };
                    }
                }
                else if (startIndex < 0 && endIndex > 0)
                {
                    if (currentRegion != null)
                    {
                        var endLineNumber = line.LineNumber;

                        if (endLineNumber != currentRegion.StartLine)
                        {
                            currentRegion.EndLine = endLineNumber;
                            currentRegion.EndOffset = text.IndexOf("\";");
                            newRegions.Add(currentRegion);
                        }

                        currentRegion = null;
                    }
                }
            }

            // determine the changed span, and send a changed event with the new spans
            List<Span> oldSpans =
                new List<Span>(this.regions.Select(r => AsSnapshotSpan(r, this.snapshot)
                    .TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive)
                    .Span));
            List<Span> newSpans =
                    new List<Span>(newRegions.Select(r => AsSnapshotSpan(r, newSnapshot).Span));

            var oldSpanCollection = new NormalizedSpanCollection(oldSpans);
            var newSpanCollection = new NormalizedSpanCollection(newSpans);

            // the changed regions are regions that appear in one set or the other, but not both.
            var removed = NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

            int changeStart = int.MaxValue;
            int changeEnd = -1;

            if (removed.Count > 0)
            {
                changeStart = removed[0].Start;
                changeEnd = removed[removed.Count - 1].End;
            }

            if (newSpans.Count > 0)
            {
                changeStart = Math.Min(changeStart, newSpans[0].Start);
                changeEnd = Math.Max(changeEnd, newSpans[newSpans.Count - 1].End);
            }

            this.snapshot = newSnapshot;
            this.regions = newRegions;

            if (changeStart <= changeEnd)
            {
                ITextSnapshot snap = this.snapshot;

                this.TagsChanged?.Invoke(
                    this,
                    new SnapshotSpanEventArgs(
                        new SnapshotSpan(this.snapshot, Span.FromBounds(changeStart, changeEnd))));
            }
        }

        private class MultiLineAttributeRegion
        {
            public int StartLine { get; set; }

            public int StartOffset { get; set; }

            public int EndLine { get; set; }

            public int EndOffset { get; set; }
        }
    }
}
