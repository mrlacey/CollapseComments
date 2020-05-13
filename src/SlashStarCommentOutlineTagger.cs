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
    public sealed class SlashStarCommentOutlineTagger : ITagger<IOutliningRegionTag>
    {
        private readonly string startHide = "/*"; // the characters that start the outlining region
        private readonly string endHide = "*/";   // the characters that end the outlining region
        private ITextBuffer buffer;
        private ITextSnapshot snapshot;
        private List<Region> regions;

        public SlashStarCommentOutlineTagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
            this.snapshot = buffer.CurrentSnapshot;
            this.regions = new List<Region>();
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

            List<Region> currentRegions = this.regions;
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

                    var shortText = startLine.GetText().Trim();

                    if (shortText.Equals(this.startHide))
                    {
                        shortText += " ...";
                    }

                    var hoverText = new StringBuilder();

                    for (int i = region.StartLine; i <= region.EndLine; i++)
                    {
                        var toAdd = currentSnapshot.GetLineFromLineNumber(i).GetText().Trim();

                        if (i == region.EndLine)
                        {
                            hoverText.Append(toAdd);
                        }
                        else
                        {
                            hoverText.AppendLine(toAdd);
                        }
                    }

                    yield return new TagSpan<IOutliningRegionTag>(
                        new SnapshotSpan(startLine.Start + region.StartOffset, endLine.End),
                        new OutliningRegionTag(false, false, shortText, hoverText.ToString()));
                }
            }
        }

        private static bool TryGetLevel(string text, int startIndex, out int level)
        {
            level = -1;

            if (text.Length > startIndex + 3)
            {
                if (int.TryParse(text.Substring(startIndex + 1), out level))
                {
                    return true;
                }
            }

            return false;
        }

        private static SnapshotSpan AsSnapshotSpan(Region region, ITextSnapshot snapshot)
        {
            var startLine = snapshot.GetLineFromLineNumber(region.StartLine);
            var endLine = (region.StartLine == region.EndLine)
                        ? startLine
                        : snapshot.GetLineFromLineNumber(region.EndLine);

            return new SnapshotSpan(startLine.Start + region.StartOffset, endLine.End);
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
            List<Region> newRegions = new List<Region>();

            // keep the current (deepest) partial region, which will have
            // references to any parent partial regions.
            PartialRegion currentRegion = null;

            foreach (var line in newSnapshot.Lines)
            {
                int regionStart = -1;
                string text = line.GetText();

                if ((regionStart = text.IndexOf(this.startHide, StringComparison.Ordinal)) != -1)
                {
                    int currentLevel = (currentRegion != null) ? currentRegion.Level : 1;
                    int newLevel;
                    if (!TryGetLevel(text, regionStart, out newLevel))
                    {
                        newLevel = currentLevel + 1;
                    }

                    // levels are the same and we have an existing region;
                    // end the current region and start the next
                    if (currentLevel == newLevel && currentRegion != null)
                    {
                        newRegions.Add(new Region()
                        {
                            Level = currentRegion.Level,
                            StartLine = currentRegion.StartLine,
                            StartOffset = currentRegion.StartOffset,
                            EndLine = line.LineNumber,
                        });

                        currentRegion = new PartialRegion()
                        {
                            Level = newLevel,
                            StartLine = line.LineNumber,
                            StartOffset = regionStart,
                            PartialParent = currentRegion.PartialParent,
                        };
                    }
                    else
                    {
                        // this is a new (sub)region
                        currentRegion = new PartialRegion()
                        {
                            Level = newLevel,
                            StartLine = line.LineNumber,
                            StartOffset = regionStart,
                            PartialParent = currentRegion,
                        };
                    }
                }
                else if ((regionStart = text.IndexOf(this.endHide, StringComparison.Ordinal)) != -1)
                {
                    int currentLevel = (currentRegion != null) ? currentRegion.Level : 1;
                    int closingLevel;
                    if (!TryGetLevel(text, regionStart, out closingLevel))
                    {
                        closingLevel = currentLevel;
                    }

                    // the regions match
                    if (currentRegion != null &&
                        currentLevel == closingLevel)
                    {
                        newRegions.Add(new Region()
                        {
                            Level = currentLevel,
                            StartLine = currentRegion.StartLine,
                            StartOffset = currentRegion.StartOffset,
                            EndLine = line.LineNumber,
                        });

                        currentRegion = currentRegion.PartialParent;
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

            NormalizedSpanCollection oldSpanCollection = new NormalizedSpanCollection(oldSpans);
            NormalizedSpanCollection newSpanCollection = new NormalizedSpanCollection(newSpans);

            // the changed regions are regions that appear in one set or the other, but not both.
            NormalizedSpanCollection removed =
            NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

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
                if (this.TagsChanged != null)
                {
                    this.TagsChanged(
                        this,
                        new SnapshotSpanEventArgs(
                          new SnapshotSpan(this.snapshot, Span.FromBounds(changeStart, changeEnd))));
                }
            }
        }

        private class PartialRegion
        {
            public int StartLine { get; set; }

            public int StartOffset { get; set; }

            public int Level { get; set; }

            public PartialRegion PartialParent { get; set; }
        }

        private class Region : PartialRegion
        {
            public int EndLine { get; set; }
        }
    }
}
