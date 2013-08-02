using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NSemble.Modules.Blog.Models;
using Nancy.ViewEngines.Razor;

namespace NSemble.Modules.Blog.Helpers
{
    public static class BlogPostHelpers
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

        public static IHtmlString Gravatar(this PostComments.Comment comment, int size)
        {
            var ret = string.Format(@"<img src=""http://www.gravatar.com/avatar.php?gravatar_id={0}&size={1}&default=identicon"" alt=""{2}"" width=""{1}"" height=""{1}"">"
                    , GetHashedEmail(comment.Email), size, comment.Author);

            return new NonEncodedHtmlString(ret);
        }

        private static string GetHashedEmail(string email)
        {
            if (email == null) return null;

            var str = email.Trim().ToLowerInvariant();
            return GetMd5Hash(str);
        }

        private static string GetMd5Hash(string input)
        {
            // Create a new Stringbuilder to collect the bytes  
            // and create a string.  
            var sBuilder = new StringBuilder();

            // Create a new instance of the MD5CryptoServiceProvider object.  
            using (var md5Hasher = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.  
                var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

                // Loop through each byte of the hashed data  
                // and format each one as a hexadecimal string.  
                for (var i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
            }

            return sBuilder.ToString();  // Return the hexadecimal string.  
        }  
    }
}
