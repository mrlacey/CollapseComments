// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using static Microsoft.VisualStudio.VSConstants;
using Task = System.Threading.Tasks.Task;

namespace CollapseComments
{
    [ProvideAutoLoad(UICONTEXT.CSharpProject_string, PackageAutoLoadFlags.BackgroundLoad)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidCollapseCommandPackageString)]
    [ProvideOptionPage(typeof(OptionsPageGrid), Vsix.Name, "General", 0, 0, true)]
    public sealed class CollapseCommandPackage : AsyncPackage
    {
        public OptionsPageGrid Options
        {
            get
            {
                return (OptionsPageGrid)this.GetDialogPage(typeof(OptionsPageGrid));
            }
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await CollapseCommand.InitializeAsync(this);
            await ExpandCommand.InitializeAsync(this);
            await ToggleCommand.InitializeAsync(this);

            var runningDocumentTable = new RunningDocumentTable(this);

            var dte = await this.GetServiceAsync(typeof(DTE)) as DTE2;

            MyRunningDocTableEvents.Instance.Initialize(this, runningDocumentTable, dte);

            runningDocumentTable.Advise(MyRunningDocTableEvents.Instance);

            await SponsorRequestHelper.CheckIfNeedToShowAsync();
        }
    }
}
