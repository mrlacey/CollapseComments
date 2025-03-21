﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;

namespace CollapseComments
{
    internal class BaseCommand
    {
        public static readonly Guid CommandSet = PackageGuids.guidCollapseCommandPackageCmdSet;

        protected CollapseCommandPackage package;

        private IWpfTextViewHost viewHost;
        private IOutliningManager subscribedMgr;

        protected IAsyncServiceProvider ServiceProvider => this.package;

        protected async Task ExecuteAsync(Mode mode)
        {
            try
            {
                var txtMgr = (IVsTextManager)await this.ServiceProvider.GetServiceAsync(typeof(SVsTextManager));
                if (txtMgr == null)
                {
                    throw new ArgumentNullException(nameof(txtMgr));
                }

                int mustHaveFocus = 1;
                txtMgr.GetActiveView(mustHaveFocus, null, out var vTextView);
                if (!(vTextView is IVsUserData userData))
                {
                    this.package.Log("No text view is currently open");
                    return;
                }

                var guidViewHost = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
                userData.GetData(ref guidViewHost, out var holder);
                this.viewHost = (IWpfTextViewHost)holder;

                var componentModel = (IComponentModel)await this.ServiceProvider.GetServiceAsync(typeof(SComponentModel));
                IOutliningManagerService outliningManagerService = null;

                int loopCounter = 0;

                while (outliningManagerService == null && loopCounter < 30)
                {
                    if (loopCounter > 0)
                    {
                        await Task.Delay(100);
                    }

                    outliningManagerService = componentModel?.GetService<IOutliningManagerService>();
                    loopCounter++;
                }

                loopCounter = 0;

                IOutliningManager mgr = null;

                while (mgr == null && loopCounter < 40)
                {
                    if (loopCounter > 0)
                    {
                        await Task.Delay(100);
                    }

                    mgr = outliningManagerService?.GetOutliningManager(this.viewHost.TextView);
                    loopCounter++;
                }

                if (mgr is null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to get outlining manager");
                    return;
                }

                List<ICollapsible> regions = null;

                loopCounter = 0;

                while ((regions == null || !regions.Any()) && loopCounter < 40)
                {
                    if (loopCounter > 0)
                    {
                        await Task.Delay(100);
                    }

                    try
                    {
                        regions = mgr?.GetAllRegions(new SnapshotSpan(this.viewHost.TextView.TextSnapshot, 0, this.viewHost.TextView.TextSnapshot.Length))
                                      .ToList();
                    }
                    catch (Exception exc)
                    {
                        System.Diagnostics.Debug.WriteLine(exc);
                        this.package.Log("Internal VS error prevented identifying collapsible regions.");
                        return;
                    }

                    loopCounter++;
                }

                if (regions is null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to get regions");
                    return;
                }

                void LocalActUponRegions() => this.ActUponRegions(mgr, regions, mode);

                // Avoid creating entries in the undo stack for each region if they're not wanted.
                if (!this.package.Options.CreateUndoEntries)
                {
                    var currentUndoValue = this.viewHost.TextView.Options.GetOptionValue(DefaultTextViewOptions.OutliningUndoOptionId);

                    this.viewHost.TextView.Options.SetOptionValue(DefaultTextViewOptions.OutliningUndoOptionId, false);

                    LocalActUponRegions();

                    this.viewHost.TextView.Options.SetOptionValue(DefaultTextViewOptions.OutliningUndoOptionId, currentUndoValue);
                }
                else
                {
                    LocalActUponRegions();
                }

                // Track usage after the command has been executed.
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    var settings = await InternalSettings.GetLiveInstanceAsync();
                    settings.UseCount += 1;
                    await settings.SaveAsync();
                });
            }
            catch (Exception exc)
            {
                this.package.Log(exc.Message);
                this.package.Log(exc.Source);
                this.package.Log(exc.StackTrace);
                this.package.Log(exc.InnerException?.Message);
            }
        }

        private void ActUponRegions(IOutliningManager mgr, List<ICollapsible> regions, Mode actionMode, bool isCallback = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

			var includeDirectives = this.package.Options.IncludeUsingDirectives;

            bool IsComment(string collapsedText)
            {
                return collapsedText.StartsWith("/")
                    || collapsedText.StartsWith("'")
                    || collapsedText.StartsWith("<!--");
            }

            bool IsUsing(string collapsedText)
            {
                // handle newline as \r\n or just \n
                return collapsedText.Contains("\nusing ");
            }

            if (regions != null && regions.Any())
            {
                var regionCount = regions.Count();

                this.package.Log($"Applying {actionMode} to {regionCount} regions");

                if (actionMode == Mode.CollapseAll)
                {
                    CollapseCommandPackage.Dte.ExecuteCommand("Edit.CollapseToDefinitions");
                    CollapseCommandPackage.Dte.ExecuteCommand("CollapseComments.Collapse");
                }
                else
                if (actionMode == Mode.DefinitionsPlusComments)
                {
                    CollapseCommandPackage.Dte.ExecuteCommand("Edit.CollapseToDefinitions");
                    CollapseCommandPackage.Dte.ExecuteCommand("CollapseComments.Expand");
                }
                else
                {
                    for (int i = 0; i < regionCount; i++)
                    {
                        var region = regions[i];

                        if (!region.IsCollapsible)
                        {
                            continue;
                        }

                        if (actionMode == Mode.CollapseComments && region.IsCollapsed)
                        {
                            // Don't change any non-comment regions.
                            continue;
                        }

                        var hiddenText = region.Extent.GetText(region.Extent.TextBuffer.CurrentSnapshot);

                        if (IsComment(hiddenText) || IsUsing(hiddenText))
                        {
                            if (IsUsing(hiddenText) && !includeDirectives)
                            {
                                continue;
                            }

                            if (actionMode == Mode.CollapseComments)
                            {
                                if (!region.IsCollapsed && region.IsCollapsible)
                                {
                                    var collapsed = mgr.TryCollapse(region);

                                    if (collapsed == null)
                                    {
                                        this.package.Log("Failed to collapse region");
                                    }
                                }
                            }
                            else if (actionMode == Mode.ExpandComments)
                            {
                                if (region.IsCollapsed && region is ICollapsed collapsed)
                                {
                                    mgr.Expand(collapsed);
                                }
                            }
                            else if (actionMode == Mode.ToggleComments)
                            {
                                if (!region.IsCollapsed)
                                {
                                    if (region.IsCollapsible)
                                    {
                                        mgr.TryCollapse(region);
                                    }
                                }
                                else if (region is ICollapsed collapsed)
                                {
                                    mgr.Expand(collapsed);
                                }
                            }
                        }
                    }
                }
            }
            else if (actionMode == Mode.CollapseComments && !isCallback)
            {
                if (mgr != null && mgr.Enabled)
                {
                    this.subscribedMgr = mgr;
                    mgr.RegionsChanged += this.Mgr_RegionsChanged;
                }
            }
        }

        private void Mgr_RegionsChanged(object sender, RegionsChangedEventArgs e)
        {
            if (sender is IOutliningManager outliningManager && outliningManager.Enabled)
            {
                this.subscribedMgr.RegionsChanged -= this.Mgr_RegionsChanged;
                this.subscribedMgr = null;

                if (this.viewHost != null)
                {
                    var regions = outliningManager?.GetAllRegions(new SnapshotSpan(this.viewHost.TextView.TextSnapshot, 0, this.viewHost.TextView.TextSnapshot.Length))
                                                   .ToList();

                    this.ActUponRegions(outliningManager, regions, Mode.CollapseComments, isCallback: true);
                }
            }
        }
    }
}
