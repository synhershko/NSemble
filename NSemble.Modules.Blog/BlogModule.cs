using System;
using System.Collections.Generic;
using System.Linq;
using NSemble.Core.Models;
using NSemble.Core.Nancy;
using NSemble.Core.Tasks;
using NSemble.Modules.Blog.Helpers;
using NSemble.Modules.Blog.Models;
using NSemble.Modules.Blog.Tasks;
using NSemble.Modules.Blog.Widgets;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Raven.Client;
using Raven.Client.Linq;

namespace NSemble.Modules.Blog
{
    public sealed class BlogModule : NSembleModule
    {
        public BlogModule(IDocumentSession session)
            : base("Blog")
        {
            // TODO blog module configs by area name

            const string blogPostRoute = @"/(?<year>19[0-9]{2}|2[0-9]{3})/(?<month>0[1-9]|1[012])/(?<id>\d+)-(?<slug>.+)";

            LoadWidgets(session);

            Get[blogPostRoute] = p =>
                                     {
                                         BlogPost post;
                                         try
                                         {
                                             post = GetBlogPost((int)p.id, Request.Query.key, session);
                                         }
                                         catch (ArgumentException e)
                                         {
                                             return e.Message;
                                         }

                                         int year = (int) p.year;
                                         int month = (int) p.month;
                                         if (post.PublishedAt.Month != month || post.PublishedAt.Year != year)
                                             return 404;

                                         if (!post.Slug.Equals(p.slug))
                                             return Response.AsRedirect(post.ToUrl(AreaRoutePrefix.TrimEnd('/')), RedirectResponse.RedirectType.Permanent);

                                         ((PageModel)Model.Page).Title = post.Title;
                                         Model.AreaPrefix = AreaRoutePrefix;
                                         Model.BlogPost = post;
                                         Model.Comments = session.Load<PostComments>(post.Id + "/comments");

                                         return View["ReadBlogPost", Model];
                                     };

            Post[blogPostRoute + "/new-comment"] = p =>
                                                      {
                                                          BlogPost post;
                                                          try
                                                          {
                                                              post = GetBlogPost((int)p.id, Request.Query.key, session);
                                                          }
                                                          catch (ArgumentException e)
                                                          {
                                                              return e.Message;
                                                          }

                                                          if (!post.AllowComments)
                                                              return 403; // Comments are closed for this post

                                                          var commentInput = this.Bind<PostComments.CommentInput>();
                                                          if (!commentInput.IsValid())
                                                              return "Error"; // TODO

                                                          TaskExecutor.ExcuteLater(new AddCommentTask(post.Id, commentInput, new AddCommentTask.RequestValues { UserAgent = Request.Headers.UserAgent, UserHostAddress = Request.UserHostAddress}));

                                                          Model.AreaPrefix = AreaRoutePrefix;

                                                          return Response.AsRedirect(post.ToUrl(AreaRoutePrefix.TrimEnd('/')));
                                                      };

            Get["/"] = o =>
                           {
                               var posts = session.Query<BlogPost>()
                                                       .Where(x => x.CurrentState == BlogPost.State.Public)
                                                       .OrderByDescending(x => x.PublishedAt)
                                                       .Take(15)
                                                       .ToList();

                               ((PageModel)Model.Page).Title = "Blog roll";
                               Model.AreaPrefix = AreaRoutePrefix;
                               Model.BlogPosts = posts;
                               Model.ListTitle = string.Empty;

                               return View["ListBlogPosts", Model];
                           };

            Get[@"/(?<year>19[0-9]{2}|2[0-9]{3})"] = p =>
                                                                 {
                                                                     int year = p.year;
                                                                     var posts = session.Query<BlogPost>()
                                                                         .Where(x => x.PublishedAt.Year == year)
                                                                         .Where(x => x.CurrentState == BlogPost.State.Public)
                                                                         .OrderByDescending(x => x.PublishedAt)
                                                                         .Take(15)
                                                                         .ToList();

                                                                     Model.AreaPrefix = AreaRoutePrefix;
                                                                     Model.BlogPosts = posts;
                                                                     ((PageModel)Model.Page).Title = Model.ListTitle = String.Format("All blog posts of the year {0}", p.year);

                                                                     return View["ListBlogPosts", Model];
                                                                 };

            Get[@"/(?<year>19[0-9]{2}|2[0-9]{3})/(?<month>0[1-9]|1[012])"] = p =>
                                                                                         {
                                                                                             int year = p.year;
                                                                                             int month = p.month;
                                                                                             var posts = session.Query<BlogPost>()
                                                                                                 .Where(x => x.PublishedAt.Year == year && x.PublishedAt.Month == month)
                                                                                                 .Where(x => x.CurrentState == BlogPost.State.Public)
                                                                                                 .OrderByDescending(x => x.PublishedAt)
                                                                                                 .Take(15)
                                                                                                 .ToList();

                                                                                             Model.AreaPrefix = AreaRoutePrefix;
                                                                                             Model.BlogPosts = posts;
                                                                                             ((PageModel)Model.Page).Title = Model.ListTitle = String.Format("All blog posts of month {0} of the year {1}", p.month, p.year);

                                                                                             return View["ListBlogPosts", Model];
                                                                                         };
        }

        private static BlogPost GetBlogPost(int id, string key, IDocumentSession session)
        {
            var post = session.Load<BlogPost>(id);
            if (post == null)
                throw new ArgumentException("Requested page could not be found");

            if (!post.IsPublic(key))
                throw new ArgumentException("Requested page could not be found");

            return post;
        }

        protected override void LoadWidgets(IDocumentSession session)
        {
            session.Load<BlogConfig>("NSemble/Configs/" + "areaName");// TODO: Constants, admin create

            // TODO use area config doc to load these
            var widgets = new List<Widget>();
            var widget = new RecentPostsWidget("RecentPosts", "Region");
            widget.Content = session.Query<BlogPost>().Where(x => x.CurrentState == BlogPost.State.Public).OrderByDescending(x => x.PublishedAt).Take(10).ToArray();
            widgets.Add(widget);

            Model.Widgets = widgets;
        }
    }
}
