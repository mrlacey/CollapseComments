// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CollapseComments
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.1", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(CollapseCommandPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class CollapseCommandPackage : AsyncPackage
    {
        public const string PackageGuidString = "02438993-d9fa-42ae-b30e-c4058e2136b3";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await CollapseCommand.InitializeAsync(this);
        }
    }
}
