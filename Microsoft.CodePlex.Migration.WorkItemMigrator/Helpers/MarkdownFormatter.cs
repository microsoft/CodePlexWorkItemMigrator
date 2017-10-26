using System;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal static class MarkdownFormatter
    {
        /// <summary>
        /// Builds a string that is bold in Markdown.
        /// </summary>
        public static string Bold(string s)
        {
            ArgValidate.IsNotNullNotEmpty(s, nameof(s));

            return $"**{s}**";
        }

        /// <summary>
        /// Builds a string that is an H4 in Markdown.
        /// </summary>
        public static string H4(string s)
        {
            ArgValidate.IsNotNullNotEmpty(s, nameof(s));

            return $"#### {s}";
        }

        /// <summary>
        /// Builds a string that is a hyper link in Markdown.
        /// </summary>
        public static string HyperLink(string anchorText, Uri url)
        {
            ArgValidate.IsNotNullNotEmpty(anchorText, nameof(anchorText));
            ArgValidate.IsNotNull(url, nameof(url));

            return $"[{anchorText}]({url})";
        }

        /// <summary>
        /// Builds a string that is italic in Markdown.
        /// </summary>
        public static string Italic(string s)
        {
            ArgValidate.IsNotNullNotEmpty(s, nameof(s));

            return $"_{s}_";
        }
    }
}