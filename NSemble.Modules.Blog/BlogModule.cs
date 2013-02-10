using System;
using System.Collections.Generic;
using System.Linq;
using NSemble.Core.Models;
using NSemble.Core.Nancy;
using NSemble.Modules.Blog.Models;
using NSemble.Modules.Blog.Widgets;
using Nancy;
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

            const string blogPostRoute = @"/(?<year>19[0-9]{2}|2[0-9]{3})/(?<month>0[1-9]|1[012])/(?<id>\d+)-{slug}";

            LoadWidgets(session);

            Get[blogPostRoute] = p =>
                                     {
                                         BlogPost post;
                                         try
                                         {
                                             post = GetBlogPost((int) p.id, session);
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
                                             return Response.AsRedirect(string.Concat(AreaRoutePrefix.TrimEnd('/'), "/", post.Id, "/", post.Slug));

                                         // TODO load comments

                                         Model.BlogPost = post;

                                         return View["ReadBlogPost", Model];
                                     };

            Post[blogPostRoute + "/new-comment"] = p =>
                                                      {
                                                          BlogPost post;
                                                          try
                                                          {
                                                              post = GetBlogPost((int)p.id, session);
                                                          }
                                                          catch (ArgumentException e)
                                                          {
                                                              return e.Message;
                                                          }

                                                          if (!post.AllowComments)
                                                              return "Comments are closed for this post";

                                                          // TODO add comment via scripted patching API

                                                          return Response.AsRedirect(string.Concat(AreaRoutePrefix.TrimEnd('/'), "/", post.Id, "/", post.Slug));
                                                      };

            Get["/"] = o =>
                           {
                               var posts = session.Query<BlogPost>()
                                                       .Where(x => x.CurrentState == BlogPost.State.Public)
                                                       .OrderByDescending(x => x.PublishedAt)
                                                       .ToList();

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
                                                                         .ToList();

                                                                     Model.BlogPosts = posts;
                                                                     Model.ListTitle = String.Format("All blog posts of the year {0}", p.year);

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
                                                                                                 .ToList();

                                                                                             Model.BlogPosts = posts;
                                                                                             Model.ListTitle = String.Format("All blog posts of month {0} of the year {1}", p.month, p.year);

                                                                                             return View["ListBlogPosts", Model];
                                                                                         };
        }

        private BlogPost GetBlogPost(int id, IDocumentSession session)
        {
            var post = session.Load<BlogPost>(id);
            if (post == null)
                throw new ArgumentException("Requested page could not be found");

            if (post.CurrentState != BlogPost.State.Public)
            {
                string key;
                if ((key = Request.Query.key as string) == null || !(key.Equals(post.PrivateViewingKey)))
                    throw new ArgumentException("Requested page could not be found");
            }
            return post;
        }

        protected override void LoadWidgets(IDocumentSession session)
        {
            // TODO use area config doc to load these
            var widgets = new List<Widget>();
            var widget = new RecentPostsWidget("RecentPosts", "Region");
            widget.Content = session.Query<BlogPost>().OrderByDescending(x => x.PublishedAt).Take(5).ToArray();
            widgets.Add(widget);

            Model.Widgets = widgets;
        }
    }
}
