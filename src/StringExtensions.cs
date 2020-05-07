// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System.Text.RegularExpressions;

namespace CollapseComments
{
    internal static class StringExtensions
    {
        public static bool ContainsSurroundedByWhitespaceInsensitive(this string original, string toFind)
        {
            return new Regex($"\\s({toFind})\\s", RegexOptions.IgnoreCase).IsMatch(original);
        }

        public static bool ContainsFollowedByWhitespaceInsensitive(this string original, string toFind)
        {
            return new Regex($"({toFind})\\s", RegexOptions.IgnoreCase).IsMatch(original);
        }
    }
}
