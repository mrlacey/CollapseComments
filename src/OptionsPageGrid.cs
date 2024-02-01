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

        // TODO: Add an advanced logging option and remove the calls to debug out.
    }
}
