// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace CollapseComments
{
    public class OptionsPageGrid : DialogPage
    {
        [Category("General")]
        [DisplayName("Include using directives")]
        [Description("Treat using directives (or imports in VB.NET) like comments.")]
        public bool IncludeUsingDirectives { get; set; } = true;

        [Category("General")]
        [DisplayName("Run when document opened")]
        [Description("Collapse all comments (and using/import directives when a document is opened.")]
        public bool RunOnDocumentOpen { get; set; } = false;

        [Category("General")]
        [DisplayName("Create undo/redo entries")]
        [Description("Create entries in the Undo/Redo stack when regions are collapsed or expanded.")]
        public bool CreateUndoEntries { get; set; } = true;

        [Category("General")]
        [DisplayName("Enable detailed logging")]
        [Description("Include detailed logging in the output window.")]
        public bool EnableDetailedLogging { get; set; } = false;
    }
}
