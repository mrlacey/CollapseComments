﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CollapseComments
{
    [ProvideAutoLoad(UIContextGuids.SolutionHasMultipleProjects, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.SolutionHasSingleProject, PackageAutoLoadFlags.BackgroundLoad)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidCollapseCommandPackageString)]
    [ProvideOptionPage(typeof(OptionsPageGrid), Vsix.Name, "General", 0, 0, true)]
    public sealed class CollapseCommandPackage : AsyncPackage
    {
        internal static DTE2 Dte;

        public OptionsPageGrid Options
        {
            get
            {
                return (OptionsPageGrid)this.GetDialogPage(typeof(OptionsPageGrid));
            }
        }

        // TODO: see if can check for anything else that may have registered the same command shortcut keys.
        // Prompt if there are conflicts and advise on how to change them.
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			OutputPane.Instance.WriteLine($"{Vsix.Name} v{Vsix.Version}");

			await CollapseCommand.InitializeAsync(this);
            await ExpandCommand.InitializeAsync(this);
            await ToggleCommand.InitializeAsync(this);
            await DefinitionsPlusCommand.InitializeAsync(this);
            await CollapseAllCommand.InitializeAsync(this);

            var runningDocumentTable = new RunningDocumentTable(this);

            CollapseCommandPackage.Dte = await this.GetServiceAsync(typeof(DTE)) as DTE2;

            MyRunningDocTableEvents.Instance.Initialize(this, runningDocumentTable, Dte);

            runningDocumentTable.Advise(MyRunningDocTableEvents.Instance);

            await SponsorRequestHelper.CheckIfNeedToShowAsync();

			TrackBasicUsageAnalytics();
		}

		private static void TrackBasicUsageAnalytics()
		{
#if !DEBUG
			try
			{
				if (string.IsNullOrWhiteSpace(AnalyticsConfig.TelemetryConnectionString))
				{
					return;
				}

				var config = new TelemetryConfiguration
				{
					ConnectionString = AnalyticsConfig.TelemetryConnectionString,
				};

				var client = new TelemetryClient(config);

				var properties = new Dictionary<string, string>
				{
					{ "VsixVersion", Vsix.Version },
					{ "VsVersion", Microsoft.VisualStudio.Telemetry.TelemetryService.DefaultSession?.GetSharedProperty("VS.Core.ExeVersion") },
					{ "Architecture", RuntimeInformation.ProcessArchitecture.ToString() },
					{ "MsInternal", Microsoft.VisualStudio.Telemetry.TelemetryService.DefaultSession?.IsUserMicrosoftInternal.ToString() },
				};

				client.TrackEvent(Vsix.Name, properties);
			}
			catch (Exception exc)
			{
				System.Diagnostics.Debug.WriteLine(exc);
				OutputPane.Instance.WriteLine("Error tracking usage analytics: " + exc.Message);
			}
#endif
		}

		public void Log(string message)
        {
            if (this.Options.EnableDetailedLogging)
            {
				OutputPane.Instance.WriteLine(message);
            }
        }
    }
}
