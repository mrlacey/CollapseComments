// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace CollapseComments
{
    public class OptionsPageGrid : DialogPage
    {
        [Category("General")]
        [DisplayName("Collapse Using Directives")]
        [Description("Collapse using directives (or imports in VB.NET) as well as comments.")]
        public bool CollapseUsingDirectives { get; set; } = true;
    }
}
