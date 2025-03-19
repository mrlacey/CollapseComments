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
		var settings = await InternalSettings.GetLiveInstanceAsync();
		if (settings.FirstUse == DateTime.MinValue)
		{
			// Track first known use so don't prompt too soon
			settings.FirstUse = DateTime.UtcNow;
			await settings.SaveAsync();
		}

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

		if (settings.FirstUse < DateTime.UtcNow.AddDays(7)
			&& settings.UseCount > 50)
		{
		  OutputPane.Instance.Activate();
		}
	  }
	}

	private static void ShowThanksForSponsorshipMessage()
	{
	  ThreadHelper.ThrowIfNotOnUIThread();

	  OutputPane.Instance.WriteLine("Thank you for your sponsorship. It really helps.");
	  OutputPane.Instance.WriteLine("If you have ideas for new features or suggestions for new features");
	  OutputPane.Instance.WriteLine("please raise an issue at https://github.com/mrlacey/CollapseComments/issues");
	  OutputPane.Instance.WriteLine(string.Empty);
	}

	private static void ShowPromptForSponsorship()
	{
	  ThreadHelper.ThrowIfNotOnUIThread();

	  OutputPane.Instance.WriteLine("********************************************************************************************************");
	  OutputPane.Instance.WriteLine("This is a free extension that is made possible thanks to the kind and generous donations of:");
	  OutputPane.Instance.WriteLine("");
	  OutputPane.Instance.WriteLine("Daniel, James, Mike, Bill, unicorns39283, Martin, Richard, Alan, Howard, Mike, Dave, Joe, ");
	  OutputPane.Instance.WriteLine("Alvin, Anders, Melvyn, Nik, Kevin, Richard, Orien, Shmueli, Gabriel, Martin, Neil, Daniel, ");
	  OutputPane.Instance.WriteLine("Victor, Uno, Paula, Tom, Nick, Niki, chasingcode, luatnt, holeow, logarrhythmic, kokolorix, ");
	  OutputPane.Instance.WriteLine("Guiorgy, Jessé, pharmacyhalo, MXM-7, atexinspect, João, hals1010, WTD-leachA, andermikael, ");
	  OutputPane.Instance.WriteLine("spudwa, Cleroth, relentless-dev-purchases & 20+ more");
	  OutputPane.Instance.WriteLine("");
	  OutputPane.Instance.WriteLine("Join them to show you appreciation and ensure future maintenance and development by becoming a sponsor.");
	  OutputPane.Instance.WriteLine("");
	  OutputPane.Instance.WriteLine("Go to https://github.com/sponsors/mrlacey");
	  OutputPane.Instance.WriteLine("");
	  OutputPane.Instance.WriteLine("Any amount, as either a one-off or on a monthly basis, is appreciated more than you can imagine.");
	  OutputPane.Instance.WriteLine("");
	  OutputPane.Instance.WriteLine("I'll also tell you how to hide this message too.  ;)");
	  OutputPane.Instance.WriteLine("");
	  OutputPane.Instance.WriteLine("");
	  OutputPane.Instance.WriteLine("If you can't afford to support financially, you can always");
	  OutputPane.Instance.WriteLine("leave a positive review at https://marketplace.visualstudio.com/items?itemName=MattLaceyLtd.CollapseComments&ssr=false#review-details");
	  OutputPane.Instance.WriteLine("");
	  OutputPane.Instance.WriteLine("********************************************************************************************************");
	}
  }
}
