using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSemble.Modules.Blog.Models;

namespace NSemble.Modules.Blog.Helpers
{
    public static class BlogPostHelpers
    {
        public static string ToUrl(this BlogPost post, string prefix)
        {
            return string.Concat(prefix, "/", post.PublishedAt.Year, "/", post.PublishedAt.Month.ToString("D2"), "/",
                                 post.Id.Substring(post.Id.IndexOf("/", StringComparison.Ordinal) + 1), "-", post.Slug);
        }
    }
}
