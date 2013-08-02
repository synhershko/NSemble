using System;
using System.Linq;
using NSemble.Core.Models;
using NSemble.Modules.Blog.Helpers;
using NSemble.Modules.Blog.Models;
using Nancy;
using Nancy.ModelBinding;
using Raven.Client;

namespace NSemble.Modules.Blog.Admin
{
	public class BlogAdminModule : Core.Nancy.NSembleAdminModule
	{
		public BlogAdminModule(IDocumentSession session)
			: base("Blog")
		{
		    Get["/"] = o =>
		                   {
		                       ViewBag.ModulePrefix = AreaRoutePrefix.TrimEnd('/');
		                       return View["List", session.Query<BlogPost>().ToArray()];
		                   };

		    Get["/post-new/"] = p => View["Edit", new BlogPost
		                {
		                    ContentType = DynamicContentType.Markdown,
		                    AllowComments = true,
		                    CurrentState = BlogPost.State.Draft,
		                }];

            Post["/post-new/"] = p =>
            {
                var post = this.Bind<BlogPost>();

                bool validated = true;
                if (!validated)
                {
                    //ModelState.AddModelError("Id", "");
                    return View["Edit", post];
                }

                // Set some defaults
                post.PublishedAt = DateTimeOffset.UtcNow;
                post.AllowComments = true;
                post.AuthorId = "users/itamar";

                string tags = Request.Form.TagsAsString;
                if (!String.IsNullOrWhiteSpace(tags))
                {
                    post.Tags = tags.Split(new[] {' ', ','}, StringSplitOptions.RemoveEmptyEntries);
                }

                session.Store(post);
				session.Store(new PostComments(), post.Id + "/comments");
                session.SaveChanges();

                return Response.AsRedirect(post.ToUrl(AreaRoutePrefix.TrimEnd('/')));
            };
		}
	}
}
