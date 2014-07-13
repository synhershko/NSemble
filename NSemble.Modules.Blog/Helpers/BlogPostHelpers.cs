using System;
using System.Security.Cryptography;
using System.Text;
using NSemble.Modules.Blog.Models;
using Nancy.ViewEngines.Razor;

namespace NSemble.Modules.Blog.Helpers
{
    public static class BlogPostHelpers
    {
        public static IHtmlString Gravatar(this PostComments.Comment comment, int size, string extras = null)
        {
            var ret = string.Format(@"<img src=""http://www.gravatar.com/avatar.php?gravatar_id={0}&size={1}&default=identicon"" alt=""{2}"" style=""width: {1}px; height: {1}px;"" {3}>"
                    , GetHashedEmail(comment.Email), size, comment.Author, extras);

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

        public const string WordPressTrackingCode = @"<script src=""http://stats.wordpress.com/e-201252.js"" type=""text/javascript""></script>
<script type=""text/javascript"">
st_go({blog:'{0}',v:'ext',post:'0'});
var load_cmc = function(){linktracker_init({0},0,2);};
if ( typeof addLoadEvent != 'undefined' ) addLoadEvent(load_cmc);
else load_cmc();
</script>";
    }
}
