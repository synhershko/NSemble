using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MarkdownDeep;
using NSemble.Core.Models;
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
                    };

                    var contents = contentItem.Content;
                    // TODO contents = CodeBlockFinder.Replace(contents, match => GenerateCodeBlock(match.Groups[1].Value.Trim(), match.Groups[2].Value));
                    contents = md.Transform(contents);
                    return new NonEncodedHtmlString(contents);
                case DynamicContentType.Html:
                    return trustContent ? new NonEncodedHtmlString(contentItem.Content) : NonEncodedHtmlString.Empty;
            }
            return NonEncodedHtmlString.Empty;
        }
    }
}