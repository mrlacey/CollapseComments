// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CollapseComments
{
    internal class MyRunningDocTableEvents : IVsRunningDocTableEvents
    {
        private static MyRunningDocTableEvents instance;

        private CollapseCommandPackage package;
        private RunningDocumentTable runningDocumentTable;
        private DTE2 dte;

        public MyRunningDocTableEvents()
        {
        }

        public static MyRunningDocTableEvents Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MyRunningDocTableEvents();
                }

                return instance;
            }
        }

        public void Initialize(CollapseCommandPackage package, RunningDocumentTable runningDocumentTable, DTE2 dte)
        {
            this.package = package;
            this.runningDocumentTable = runningDocumentTable;
            this.dte = dte;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            // Don't trigger this when switching between open documents
            if (fFirstShow != 0)
            {
                if (this.package.Options.RunOnDocumentOpen)
                {
                    // Offload to a background thread
                    this.package.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await Task.Yield(); // get off the caller's callstack.
                        await Task.Delay(2000); // Give the document time to load
                        this.dte.ExecuteCommand("Edit.CollapseComments");
                    });
                }
            }

            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }
    }
}
