using System;
using System.Collections.Generic;

namespace NSemble.Modules.Blog.Models
{
    public class PostComments
    {
        public PostComments()
        {
            Comments = new List<Comment>();
            Spam = new List<Comment>();
        }

        public List<Comment> Comments { get; set; }
        public List<Comment> Spam { get; set; }

        public int CommentsCount { get { return Comments.Count; } set { } }

        public int LastCommentId { get; set; }

        public int GenerateNewCommentId()
        {
            return ++LastCommentId;
        }

        public class CommentInput
        {
            //[Required]
            //[Display(Name = "Name")]
            public string Author { get; set; }

            //[Required]
            //[Display(Name = "Email")]
            //[Email]
            public string Email { get; set; }

            //[Display(Name = "Url")]
            public string Website { get; set; }

            //[AllowHtml]
            //[Required]
            //[Display(Name = "Comments")]
            //[DataType(DataType.MultilineText)]
            public string Body { get; set; }

            //[HiddenInput]
            public int InReplyTo { get; set; }

            //[HiddenInput]
            public Guid? CommenterKey { get; set; }

            public bool IsValid()
            {
                if (String.IsNullOrWhiteSpace(Author) || String.IsNullOrWhiteSpace(Email))
                    return false;

                var validAddress = true;
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

                Uri uri;
                return Uri.TryCreate(Website, UriKind.Absolute, out uri);
            }
        }

        public class Comment
        {
            public int Id { get; set; }
            public string Author { get; set; }
            public string Email { get; set; }
            public string Website { get; set; }
            public string Body { get; set; }
            public bool Approved { get; set; }
            public List<Comment> Replies { get; set; }
            public DateTimeOffset CreatedAt { get; set; }

            public string UserHostAddress { get; set; }
            public string UserAgent { get; set; }
        }
    }
}
