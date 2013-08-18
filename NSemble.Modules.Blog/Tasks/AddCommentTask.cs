using System;
using System.Collections.Generic;
using NSemble.Core.Models;
using NSemble.Core.Tasks;
using NSemble.Modules.Blog.Helpers;
using NSemble.Modules.Blog.Models;
using Raven.Client;

namespace NSemble.Modules.Blog.Tasks
{
    public class AddCommentTask : ExecutableTask
    {
        public class RequestValues
        {
            public string UserAgent { get; set; }
            public string UserHostAddress { get; set; }
            public bool IsAuthenticated { get; set; }
        }

        private readonly BlogConfig _config;
        private readonly PostComments.CommentInput commentInput;
        private readonly RequestValues requestValues;
        private readonly string postId;

        public AddCommentTask(IDocumentStore documentStore, BlogConfig config, string postId,
                              PostComments.CommentInput commentInput, RequestValues requestValues)
        {
            _config = config;
            this.commentInput = commentInput;
            this.requestValues = requestValues;
            this.postId = postId;
            this.RavenDocumentStore = documentStore;
        }

        public override void Execute()
        {
            var comment = new PostComments.Comment
                              {
                                  Author = commentInput.Author,
                                  Approved = true,
                                  Content = commentInput.Body,
                                  CreatedAt = DateTimeOffset.UtcNow,
                                  Email = commentInput.Email,
                                  Website = commentInput.Website,
                                  UserAgent = requestValues.UserAgent,
                                  UserHostAddress = requestValues.UserHostAddress,
                                  Replies = new List<PostComments.Comment>(),
                              };

            var isSpam = false;
            if (!string.IsNullOrWhiteSpace(_config.AkismetAPIKey) && !string.IsNullOrWhiteSpace(_config.AkismetDomain))
            {
                try
                {
                    isSpam = AkismetValidator.IsSpam(_config.AkismetAPIKey, _config.AkismetDomain, comment);
                }
                catch (Exception ignored_ex)
                {
                    // TODO log
                }
            }
            
            var post = DocumentSession.Include<BlogPost>(blogPost => blogPost.AuthorId).Load(postId);
            var postAuthor = DocumentSession.Load<User>(post.AuthorId);
            var author = DocumentSession.Load<User>(commentInput.Author);
            DocumentSession.Advanced.MarkReadOnly(post);
            DocumentSession.Advanced.MarkReadOnly(postAuthor);
            DocumentSession.Advanced.MarkReadOnly(author);

            var comments = DocumentSession.Load<PostComments>(postId + "/comments");
            // TODO if (comments == null)

            if (isSpam)
            {
                comments.Spam.Add(comment);
            }
            else
            {
                if (commentInput.InReplyTo > 0)
                {
                    foreach (var c in comments.Comments)
                    {
                        if (c.Id != commentInput.InReplyTo) continue;
                        if (c.Replies == null) c.Replies = new List<PostComments.Comment>();
                        c.Replies.Add(comment);
                        break;
                    }
                }
                else
                {
                    comment.Id = comments.GenerateNewCommentId();
                    comments.Comments.Add(comment);
                }
                post.CommentsCount++;
            }

            //			if (requestValues.IsAuthenticated == false && comment.IsSpam)
            //			{
            //				if (commenter.NumberOfSpamComments > 4)
            //					return;
            //				comments.Spam.Add(comment);
            //			}
            //			else
            //			{
            //				post.CommentsCount++;
            //				comments.Comments.Add(comment);
            //			}
            //
            //          // Now send out email notifications
            //			if (requestValues.IsAuthenticated)
            //				return; // we don't send email for authenticated users

            var subject = string.Format("{1} New comment posted: {0}", post.Title, isSpam ? "[Spam] " : string.Empty);
            var commentEmailHTML = string.Format(SendEmailTask.basicEmailHtml,
                                                 string.Format(
                                                     @"<div>New comment on post titled {0} by {1}, click <a href=""{2}"">here</a> to view it</div>",
                                                     post.Title, commentInput.Author, post.ToUrl(""))
                );

            var notify = new HashSet<string> {_config.OwnerEmail};
            if (author != null) notify.Add(author.Email);
            TaskExecutor.ExcuteLater(new SendEmailTask("noreply", subject, notify, commentEmailHTML));
        }
    }
}