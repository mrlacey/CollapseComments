﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CollapseComments
{
    public class SponsorRequestHelper
    {
        public static async Task CheckIfNeedToShowAsync()
        {
            if (await SponsorDetector.IsSponsorAsync())
            {
                if (new Random().Next(1, 10) == 2)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    ShowThanksForSponsorshipMessage();
                }
            }
            else
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                ShowPromptForSponsorship();
            }
        }

        private static void ShowThanksForSponsorshipMessage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            GeneralOutputPane.Instance.WriteLine("Thank you for your sponsorship. It really helps.");
            GeneralOutputPane.Instance.WriteLine("If you have ideas for new features or suggestions for new features");
            GeneralOutputPane.Instance.WriteLine("please raise an issue at https://github.com/mrlacey/CollapseComments/issues");
            GeneralOutputPane.Instance.WriteLine(string.Empty);
        }

        private static void ShowPromptForSponsorship()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            GeneralOutputPane.Instance.WriteLine("Sorry to interrupt. I know your time is busy, presumably that's why you installed this extension (Collapse Comments).");
            GeneralOutputPane.Instance.WriteLine("I'm happy that the extensions I've created have been able to help you and many others");
            GeneralOutputPane.Instance.WriteLine("but I also need to make a living, and limited paid work over the last few years has been a challenge. :(");
            GeneralOutputPane.Instance.WriteLine(string.Empty);
            GeneralOutputPane.Instance.WriteLine("Show your support by making a one-off or recurring donation at https://github.com/sponsors/mrlacey");
            GeneralOutputPane.Instance.WriteLine(string.Empty);
            GeneralOutputPane.Instance.WriteLine("If you become a sponsor, I'll tell you how to hide this message too. ;)");
            GeneralOutputPane.Instance.WriteLine(string.Empty);
            GeneralOutputPane.Instance.Activate();
        }
    }
}
