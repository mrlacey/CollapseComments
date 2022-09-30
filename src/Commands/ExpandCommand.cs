// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CollapseComments
{
    internal sealed class ExpandCommand : BaseCommand
    {
        private ExpandCommand(CollapseCommandPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, PackageIds.ExpandCommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static ExpandCommand Instance { get; private set; }

        public static async Task InitializeAsync(CollapseCommandPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ExpandCommand(package, commandService);
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Execute(object sender, EventArgs ea)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                await this.ExecuteAsync(Mode.ExpandComments);
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine(exc);
            }
        }
    }
}
