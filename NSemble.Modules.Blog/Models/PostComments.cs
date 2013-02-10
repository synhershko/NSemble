using System;
using System.Collections.Generic;

namespace NSemble.Modules.Blog.Models
{
    public class PostComments
    {
        public List<Comment> Comments { get; set; }
        public List<Comment> Spam { get; set; }

        public int CommentsCount { get { return Comments.Count; } set { } }

        public int LastCommentId { get; set; }

        public int GenerateNewCommentId()
        {
            return ++LastCommentId;
        }

        public class Comment
        {
            public int Id { get; set; }
            public string Author { get; set; }
            public string Email { get; set; }
            public string Website { get; set; }
            public string Body { get; set; }
            public bool Approved { get; set; }
            public DateTimeOffset CreatedAt { get; set; }

            public string UserHostAddress { get; set; }
            public string UserAgent { get; set; }

            public bool IsValid()
            {
                if (String.IsNullOrWhiteSpace(Author) || String.IsNullOrWhiteSpace(Email))
                    return false;

                bool validAddress = true;
                try
                {
                    var address = new System.Net.Mail.MailAddress(Email).Address;
                }
                catch (FormatException)
                {
                    validAddress = false;
                }

                if (!validAddress) return false;

                if (String.IsNullOrWhiteSpace(Body))
                {
                    return false;
                }
                else
                {
                    Uri uri;
                    if (!System.Uri.TryCreate(Website, UriKind.Absolute, out uri)) return false;
                }


                return true;
            }
        }
    }
}
