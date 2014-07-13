using System;
using System.Collections.Generic;
using System.Text;
using NSemble.Modules.Blog.Models;
using Nancy.ViewEngines.Razor;

namespace NSemble.Modules.Blog.Helpers
{
    public static class UrlHelpers
    {
        public static string ToUrl(this BlogPost post, string prefix)
        {
            return string.Concat(prefix, "/", post.PublishedAt.Year, "/", post.PublishedAt.Month.ToString("D2"), "/",
                                 post.Id.Substring(post.Id.IndexOf("/", StringComparison.Ordinal) + 1), "-", post.Slug);
        }

        public static string ToAdminEditUrl(this BlogPost post, string prefix)
        {
            return string.Concat(prefix, "/", post.PublishedAt.Year, "/", post.PublishedAt.Month.ToString("D2"), "/",
                                 post.Id.Substring(post.Id.IndexOf("/", StringComparison.Ordinal) + 1), "-", post.Slug);
        }

        public static IHtmlString TagLinks(string prefix, IEnumerable<string> tags)
        {
            if (tags == null) return NonEncodedHtmlString.Empty;

            var sb = new StringBuilder();
            int i = 0;
            foreach (var tag in tags)
            {
                if (i++ > 0) sb.Append(", ");
                sb.AppendFormat(@"<a href=""{1}/tagged/{0}"">{0}</a>", tag, prefix);
            }
            return new NonEncodedHtmlString(sb.ToString());
        }

        public static IHtmlString CommentsCountText(this BlogPost post)
        {
            if (post != null && post.CommentsCount > 0)
                return new NonEncodedHtmlString(string.Format("Comments ({0})", post.CommentsCount));
            return new NonEncodedHtmlString("No comments");
        }

        public static string BlogUrl(string prefix, int? year, int? month, string tag, int? page)
        {
            var sb = new StringBuilder(prefix);
            sb.Append('/');
            if (year != null && year > 0)
            {
                sb.Append(year.Value.ToString("D2"));
                sb.Append('/');
            }
            if (month != null && month > 0)
            {
                sb.Append(month.Value.ToString("D2"));
                sb.Append('/');
            }
            if (tag != null)
            {
                sb.Append("tagged/");
                sb.Append(tag);
            }
            if (page != null && page > 1)
            {
                sb.Append("page/");
                sb.Append(page.Value);
            }
            return sb.ToString();
        }
    }
}
