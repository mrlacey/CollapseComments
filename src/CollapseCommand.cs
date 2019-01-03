using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;

namespace CollapseComments
{
    internal sealed class CollapseCommand
    {
        public const int CommandId = 0x0100;

        public static readonly Guid CommandSet = new Guid("fafe8ebd-e623-491e-8e27-5543153918c8");

        private readonly AsyncPackage package;

        private CollapseCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static CollapseCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new CollapseCommand(package, commandService);
        }

        private async void Execute(object sender, EventArgs e)
        {
            IVsTextManager txtMgr = (IVsTextManager)await ServiceProvider.GetServiceAsync(typeof(SVsTextManager));
            if (txtMgr == null) throw new ArgumentNullException(nameof(txtMgr));
            int mustHaveFocus = 1;
            txtMgr.GetActiveView(mustHaveFocus, null, out var vTextView);
            if (!(vTextView is IVsUserData userData))
            {
                Console.WriteLine("No text view is currently open");
                return;
            }

            var guidViewHost = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
            userData.GetData(ref guidViewHost, out var holder);
            var viewHost = (IWpfTextViewHost)holder;

            var componentModel = (IComponentModel)await ServiceProvider.GetServiceAsync(typeof(SComponentModel));
            var outliningManagerService = componentModel?.GetService<IOutliningManagerService>();

            var mgr = outliningManagerService?.GetOutliningManager(viewHost.TextView);

            var regions = mgr?.GetAllRegions(new SnapshotSpan(viewHost.TextView.TextSnapshot, 0, viewHost.TextView.TextSnapshot.Length));

            if (regions != null)
                foreach (var region in regions)
                {
                    if (!region.IsCollapsible || region.IsCollapsed)
                    {
                        continue;
                    }

                    var collapsedText = region.CollapsedForm?.ToString();

                    if (string.IsNullOrWhiteSpace(collapsedText))
                    {
                        continue;
                    }

                    if (collapsedText == "...")
                    {
                        var hiddenText = region.Extent.GetText(region.Extent.TextBuffer.CurrentSnapshot);

                        if (hiddenText.Contains("\r\nusing ") || hiddenText.Contains("\r\nImports"))
                        {
                            mgr.TryCollapse(region);
                        }
                    }
                    else if (collapsedText.StartsWith("/") || collapsedText.StartsWith("'"))
                    {
                        mgr.TryCollapse(region);
                    }
                }
        }
    }
}
