// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CollapseComments
{
    internal sealed class ToggleCommand : BaseCommand
    {
        private ToggleCommand(CollapseCommandPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, PackageIds.ToggleCommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static ToggleCommand Instance { get; private set; }

        public static async Task InitializeAsync(CollapseCommandPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ToggleCommand(package, commandService);
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Execute(object sender, EventArgs ea)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				await this.ExecuteAsync(Mode.ToggleComments);
            }
            catch (Exception exc)
            {
                this.package.Log(exc.Message);
                this.package.Log(exc.Source);
                this.package.Log(exc.StackTrace);
                this.package.Log(exc.InnerException?.Message);
            }
        }
    }
}
