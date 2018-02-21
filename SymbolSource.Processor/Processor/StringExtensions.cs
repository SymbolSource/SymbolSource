using System;

namespace SymbolSource.Processor.Processor
{
    public static class StringExtensions
    {
        public static string SubstringSafe(this string text, int max)
        {
            return text.Substring(0, Math.Min(text.Length, max));
        }

        public static string AddSuffix(this string text, string suffix, int max, params char[] trimChars)
        {
            return text.Substring(0, Math.Min(text.Length, max - suffix.Length)).TrimEnd(trimChars) + suffix;
        }
    }
}