using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MarkdownDeep;
using NSemble.Core.Models;
using Nancy.Helpers;
using Nancy.ViewEngines.Razor;

namespace NSemble.Core.Extensions
{
    public static class DynamicContentHelpers
    {
        public static string TitleToSlug(string title)
        {
            // 2 - Strip diacritical marks using Michael Kaplan's function or equivalent
            title = RemoveDiacritics(title);

            // 3 - Lowercase the string for canonicalization
            title = title.ToLowerInvariant();

            // 4 - Replace all the non-word characters with dashes
            title = ReplaceNonWordWithDashes(title);

            // 1 - Trim the string of leading/trailing whitespace
            title = title.Trim(' ', '-');

            return title;
        }


        // http://blogs.msdn.com/michkap/archive/2007/05/14/2629747.aspx
        /// <summary>
        /// Strips the value from any non English character by replacing those with their English equivalent.
        /// </summary>
        /// <param name="value">The string to normalize.</param>
        /// <returns>A string where all characters are part of the basic English ANSI encoding.</returns>
        /// <seealso cref="http://stackoverflow.com/questions/249087/how-do-i-remove-diacritics-accents-from-a-string-in-net"/>
        private static string RemoveDiacritics(string value)
        {
            var stFormD = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var t in stFormD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(t);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(t);
                }
            }

            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }

        private static string ReplaceNonWordWithDashes(string title)
        {
            // Remove Apostrophe Tags
            title = Regex.Replace(title, "[’'“”\"&]{1,}", "", RegexOptions.None);

            // Replaces all non-alphanumeric character by a space
            var builder = new StringBuilder();
            foreach (var t in title)
            {
                builder.Append(char.IsLetterOrDigit(t) ? t : ' ');
            }

            title = builder.ToString();

            // Replace multiple spaces to a single dash
            title = Regex.Replace(title, @"\s{1,}", "-", RegexOptions.None);

            return title;
        }

        static readonly Regex CodeBlockFinder = new Regex(@"\[code lang=(.+?)\s*\](.*?)\[/code\]", RegexOptions.Compiled | RegexOptions.Singleline);
        static readonly Regex FirstLineSpacesFinder = new Regex(@"^(\s|\t)+", RegexOptions.Compiled);

        public static IHtmlString CompiledContent(this IDynamicContent contentItem, bool trustContent = false)
        {
            if (contentItem == null) return NonEncodedHtmlString.Empty;

            switch (contentItem.ContentType)
            {
                case DynamicContentType.Markdown:
                    var md = new Markdown
                    {
                        AutoHeadingIDs = true,
                        ExtraMode = true,
                        NoFollowLinks = !trustContent,
                        SafeMode = false,
                        NewWindowForExternalLinks = true,
                        FormatCodeBlock = null,
                    };

                    var contents = contentItem.Content;
                    contents = CodeBlockFinder.Replace(contents, match => GenerateCodeBlock(match.Groups[1].Value.Trim(), match.Groups[2].Value));
                    contents = md.Transform(contents);
                    return new NonEncodedHtmlString(contents);
                case DynamicContentType.Html:
                    return trustContent ? new NonEncodedHtmlString(contentItem.Content) : NonEncodedHtmlString.Empty;
            }
            return NonEncodedHtmlString.Empty;
        }

        private static string GenerateCodeBlock(string lang, string code)
        {
            code = HttpUtility.HtmlDecode(code);
            return string.Format("{0}{1}{0}", Environment.NewLine,
                                 ConvertMarkdownCodeStatment(code)//.Replace("<", "&lt;"), // to support syntax highlighting on pre tags
                                 , lang
                );
        }

        private static string ConvertMarkdownCodeStatment(string code)
        {
            var line = code.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var firstLineSpaces = GetFirstLineSpaces(line.FirstOrDefault());
            var firstLineSpacesLength = firstLineSpaces.Length;
            var formattedLines = line.Select(l => string.Format("    {0}", l.Substring(l.Length < firstLineSpacesLength ? 0 : firstLineSpacesLength)));
            return string.Join(Environment.NewLine, formattedLines);
        }

        private static string GetFirstLineSpaces(string firstLine)
        {
            if (firstLine == null)
                return string.Empty;

            var match = FirstLineSpacesFinder.Match(firstLine);
            if (match.Success)
            {
                return firstLine.Substring(0, match.Length);
            }
            return string.Empty;
        }

    }
}