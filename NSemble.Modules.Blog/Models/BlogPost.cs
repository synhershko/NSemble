using System;
using System.Collections.Generic;
using NSemble.Core.Extensions;
using NSemble.Core.Models;

namespace NSemble.Modules.Blog.Models
{
	public class BlogPost : IDynamicContent
	{
		public enum State
		{
			Public,
			Draft,
			Private,
			Deleted,
		}

		public BlogPost()
		{
			ContentType = DynamicContentType.Markdown;
			CurrentState = State.Draft;
			PrivateViewingKey = Guid.NewGuid().ToString();
		}

		public string Id { get; set; }
		public string Title { get; set; }
		public string Content { get; set; }
		public DynamicContentType ContentType { get; set; }
        public string CachedRenderedContent { get; set; }
		public ICollection<string> Tags { get; set; }

	    public string Slug
	    {
	        get { return DynamicContentHelpers.TitleToSlug(Title ?? string.Empty); }
	    }

        public bool IsPublic(string key)
        {
            return CurrentState == State.Public || (key != null && CurrentState == State.Private && PrivateViewingKey.Equals(key));
        }

		public string AuthorId { get; set; }
		public DateTimeOffset PublishedAt { get; set; }
		public DateTimeOffset? LastEditedAt { get; set; }

		public State CurrentState { get; set; }
		public bool AllowComments { get; set; }
		public string PrivateViewingKey { get; set; }
        public int CommentsCount { get; set; }
	}
}
