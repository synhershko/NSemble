using NSemble.Core.Models;
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
		                       return "Blog admin";
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

                session.Store(post);
                session.SaveChanges();

                return Response.AsRedirect(string.Concat(AreaRoutePrefix.TrimEnd('/'), "/", post.Id, "/", post.Slug));
            };
		}
	}
}
