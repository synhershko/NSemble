using System;
using System.Collections.Generic;
using NSemble.Core.Tasks;
using NSemble.Modules.Blog.Helpers;
using NSemble.Modules.Blog.Models;
using Raven.Client;
using Raven.Client.Document;

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

		public AddCommentTask(IDocumentStore documentStore, BlogConfig config, string postId, PostComments.CommentInput commentInput, RequestValues requestValues)
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

            var post = DocumentSession.Load<BlogPost>(postId);
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
//
//			var viewModel = comment.MapTo<NewCommentEmailViewModel>();
//			viewModel.PostId = RavenIdResolver.Resolve(post.Id);
//			viewModel.PostTitle = HttpUtility.HtmlDecode(post.Title);
//			viewModel.PostSlug = SlugConverter.TitleToSlug(post.Title);
//			viewModel.BlogName = DocumentSession.Load<BlogConfig>("Blog/Config").Title;
//			viewModel.Key = post.ShowPostEvenIfPrivate.MapTo<string>();
//
//			var subject = string.Format("{2}Comment on: {0} from {1}", viewModel.PostTitle, viewModel.BlogName, comment.IsSpam ? "[Spam] " : string.Empty);
//
//			TaskExecutor.ExcuteLater(new SendEmailTask(viewModel.Email, subject, "NewComment", postAuthor.Email, viewModel));
//		}
	}
    }
}
