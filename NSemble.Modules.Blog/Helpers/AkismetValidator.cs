using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;
using NSemble.Modules.Blog.Models;
using Nancy.Helpers;

namespace NSemble.Modules.Blog.Helpers
{
    /// <summary>
    /// This class is responsible validating information against Akismet.
    /// </summary>
    /// <example>
    ///     Comment comment = new Comment
    ///     {
    ///         blog = "Your-Akismet-Domain",
    ///         comment_type = "comment",
    ///         comment_author = "Sam Mulder",
    ///         comment_author_email = "samm@gmail.com",
    ///         comment_content = "Does this really working?",
    ///         permalink = String.Empty,
    ///         referrer = httpContext.Request.ServerVariables["HTTP_REFERER"],
    ///         user_agent = httpContext.Request.ServerVariables["HTTP_USER_AGENT"],
    ///         user_ip = httpContext.Request.ServerVariables["REMOTE_ADDR"]
    ///     };
    /// 
    ///     Validator validator = new Validator("Your-Akismet-Key");
    ///     if(validator.IsSpam(comment))
    ///     { // do something with the spam comment
    ///     }
    ///     else
    ///     { // this comment is not spam
    ///     }
    /// </example>
    public class AkismetValidator
    {
        /// <summary>
        /// This class defines an Akismet's comment format.
        /// </summary>
        public class AkismetComment
        {
            /// <summary>
            /// The front page or home URL of the instance making the request. For a blog or wiki this would be the front page. Must be a full URI, including http://.
            /// </summary>
            /// <remarks>This property is required.</remarks>
            public String blog { get; set; }

            /// <summary>
            /// IP address of the comment submitter.
            /// </summary>
            /// <remarks>This property is required.</remarks>
            public String user_ip { get; set; }

            /// <summary>
            /// User agent string of the web browser submitting the comment - typically the HTTP_USER_AGENT cgi variable. Not to be confused with the user agent of your Akismet library.
            /// </summary>
            /// <remarks>This property is required.</remarks>
            public String user_agent { get; set; }

            /// <summary>
            /// The content of the HTTP_REFERER header should be sent here.
            /// </summary>
            public String referrer { get; set; }

            /// <summary>
            /// The permanent location of the entry the comment was submitted to.
            /// </summary>
            public String permalink { get; set; }

            /// <summary>
            /// May be blank, comment, trackback, pingback, or a made up value like "registration".
            /// </summary>
            public String comment_type { get; set; }

            /// <summary>
            /// Name submitted with the comment.
            /// </summary>
            public String comment_author { get; set; }

            /// <summary>
            /// Email address submitted with the comment.
            /// </summary>
            public String comment_author_email { get; set; }

            /// <summary>
            /// URL submitted with comment.
            /// </summary>
            public String comment_author_url { get; set; }

            /// <summary>
            /// The content that was submitted.
            /// </summary>
            public String comment_content { get; set; }

            /// <summary>
            /// Check is current comment is valid.
            /// </summary>
            public bool IsValid
            {
                get { return !(String.IsNullOrEmpty(blog) || String.IsNullOrEmpty(user_ip) || String.IsNullOrEmpty(user_agent)); }
            }
        }

        #region Class members

        /// <summary>
        /// The Akismet key, if any.
        /// </summary>
        protected String m_key = String.Empty;
        
        #endregion

        #region Class constructors
        
        /// <summary>
        /// Initialize class members based on the input parameters
        /// </summary>
        /// <param name="key">The input Akismet key.</param>
        public AkismetValidator(String key)
        {
            m_key = key;
        }

        #endregion

        #region IValidator implementation

        /// <summary>
        /// Check if the validator's key is valid or not.
        /// </summary>
        /// <returns>True if the key is valid, false otherwise.</returns>
        public Boolean VerifyKey(String domain)
        {
            // prepare pars for the request
            NameValueCollection pars = PreparePars(m_key, domain);
            if (null != pars)
            {
                // extract result from the request
                return ExtractResult(PostRequest("http://rest.akismet.com/1.1/verify-key", pars));
            }

            // return failure
            return false;
        }

        /// <summary>
        /// Check if the input comment is valid or not.
        /// </summary>
        /// <param name="comment">The input comment to be checked.</param>
        /// <returns>True if the comment is valid, false otherwise.</returns>
        public Boolean IsSpam(AkismetComment comment)
        {
            // prepare pars for the request
            NameValueCollection pars = PreparePars(comment);
            if (null != pars)
            {
                // extract result from the request
                return ExtractResult(PostRequest(String.Format("http://{0}.rest.akismet.com/1.1/comment-check", m_key), pars));
            }

            // return no spam
            return false;
        }

        /// <summary>
        /// This call is for submitting comments that weren't marked as spam but should've been.
        /// </summary>
        /// <param name="comment">The input comment to be sent as spam.</param>
        /// <returns>True if the comment was successfully sent, false otherwise.</returns>
        public void SubmitSpam(AkismetComment comment)
        {
            // prepare pars for the request
            NameValueCollection pars = PreparePars(comment);
            if (null != pars)
            {
                PostRequest(String.Format("http://{0}.rest.akismet.com/1.1/submit-spam", m_key), pars);
            }
        }

        /// <summary>
        /// This call is intended for the marking of false positives, things that were incorrectly marked as spam.
        /// </summary>
        /// <param name="comment">The input comment to be sent as ham.</param>
        /// <returns>True if the comment was successfully sent, false otherwise.</returns>
        public void SubmitHam(AkismetComment comment)
        {
            // prepare pars for the request
            NameValueCollection pars = PreparePars(comment);
            if (null != pars)
            {
                PostRequest(String.Format("http://{0}.rest.akismet.com/1.1/submit-spam", m_key), pars);
            }
        }
       
        #endregion

        #region Class operations
        
        /// <summary>
        /// Post parameters to the input url and return the response.
        /// </summary>
        /// <param name="url">The input url (absolute).</param>
        /// <param name="pars">The input parameters to send.</param>
        /// <returns>The response, if any.</returns>
        protected virtual String PostRequest(String url, NameValueCollection pars)
        {
            // check input data
            if (String.IsNullOrEmpty(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute) || (null == pars))
                return String.Empty;

            String content = String.Empty;
            // create content for the post request
            foreach (String key in pars.AllKeys)
            {
                if (String.IsNullOrEmpty(content))
                    content = String.Format("{0}={1}", key, pars[key]);
                else
                    content += String.Format("&{0}={1}", key, pars[key]);
            }

            // initialize request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentLength = content.Length;
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.UserAgent = "Akismet.NET";

            StreamWriter writer = null;
            try
            {
                // write request content
                writer = new StreamWriter(request.GetRequestStream());
                writer.Write(content);
            }
            catch (Exception)
            { // return failure
                return String.Empty;
            }
            finally
            { // close the writer, if any
                if (null != writer)
                    writer.Close();
            }

            // retrieve the response
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                // retrieve response
                String result = reader.ReadToEnd();

                // close the reader
                reader.Close();

                // return result
                return result;
            }
        }

        /// <summary>
        /// Prepare request parameters based on the input comment.
        /// </summary>
        /// <param name="comment">The input comment.</param>
        /// <returns>The prepared parameters if any.</returns>
        protected virtual NameValueCollection PreparePars(AkismetComment comment)
        {
            // check the input parameters
            if ((null != comment) && (!comment.IsValid))
                return null;

            // initialize result
            NameValueCollection result = new NameValueCollection();

            // add required information
            result["blog"] = HttpUtility.UrlEncode(comment.blog);
            result["user_ip"] = HttpUtility.UrlEncode(comment.user_ip);
            result["user_agent"] = HttpUtility.UrlEncode(comment.user_agent);
            // add optional information
            result["referrer"] = String.IsNullOrEmpty(comment.referrer) ? String.Empty : HttpUtility.UrlEncode(comment.referrer);
            result["permalink"] = String.IsNullOrEmpty(comment.permalink) ? String.Empty : HttpUtility.UrlEncode(comment.permalink);
            result["comment_type"] = String.IsNullOrEmpty(comment.comment_type) ? String.Empty : HttpUtility.UrlEncode(comment.comment_type);
            result["comment_author"] = String.IsNullOrEmpty(comment.comment_author) ? String.Empty : HttpUtility.UrlEncode(comment.comment_author);
            result["comment_author_email"] = String.IsNullOrEmpty(comment.comment_author_email) ? String.Empty : HttpUtility.UrlEncode(comment.comment_author_email);
            result["comment_author_url"] = String.IsNullOrEmpty(comment.comment_author_url) ? String.Empty : HttpUtility.UrlEncode(comment.comment_author_url);
            result["comment_content"] = String.IsNullOrEmpty(comment.comment_content) ? String.Empty : HttpUtility.UrlEncode(comment.comment_content);

            // return result
            return result;
        }

        /// <summary>
        /// Prepare request parameters based on the input parameters.
        /// </summary>
        /// <param name="key">The input key.</param>
        /// <param name="domain">The input domain.</param>
        /// <returns>The prepared parameters if any.</returns>
        protected virtual NameValueCollection PreparePars(String key, String domain)
        {
            // check the input parameters
            if (String.IsNullOrEmpty(key) || String.IsNullOrEmpty(domain))
                return null;

            // initialize result
            NameValueCollection result = new NameValueCollection();

            // add required information
            result["key"] = key; // no need for encoding
            result["blog"] = HttpUtility.UrlEncode(domain);
            
            // return result
            return result;
        }

        /// <summary>
        /// Check the input data for valid content: "valid" string or "true" string.
        /// </summary>
        /// <param name="content">The input content.</param>
        /// <returns>True if the content is valid, false otherwise.</returns>
        protected virtual Boolean ExtractResult(String content)
        {
            // check the input parameters
            if (String.IsNullOrEmpty(content))
                return false;

            // check for valid content
            if (content.ToLower().Equals("valid") || content.ToLower().Equals("true"))
                return true;

            // return failure
            return false;
        }

        #endregion

        public static bool IsSpam(string ApiKey, string domain, PostComments.Comment comment)
        {
#if DEBUG
            return false;
#endif
            var validator = new AkismetValidator(ApiKey);
            if (!validator.VerifyKey(domain)) throw new Exception("Akismet API key invalid.");

            var akismetComment = new AkismetComment
            {
                blog = domain,
                user_ip= comment.UserHostAddress,
                user_agent = comment.UserAgent,
                comment_content= comment.Content,
                comment_type= "comment",
                comment_author = comment.Author,
                comment_author_email = comment.Email,
                comment_author_url= comment.Website,
            };

            //Check if Akismet thinks this comment is spam. Returns TRUE if spam.
            return validator.IsSpam(akismetComment);
        }
    }
}
