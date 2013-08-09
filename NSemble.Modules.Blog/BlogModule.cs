using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        const int PageSize = 10;

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
                                         ViewBag.AreaRoutePrefix = AreaRoutePrefix;
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

                                                          ViewBag.AreaRoutePrefix = AreaRoutePrefix;

                                                          return Response.AsRedirect(post.ToUrl(AreaRoutePrefix.TrimEnd('/')));
                                                      };

            // Home
            Get["/"] = o =>
                           {
                               ((PageModel)Model.Page).Title = "Blog roll";
                               Model.ListTitle = string.Empty;

                               return GetPosts(session);
                           };
            Get[@"/page/(?<page>\d+)"] = o =>
                           {
                               ((PageModel)Model.Page).Title = "Blog roll";
                               Model.ListTitle = string.Empty;

                               return GetPosts(session, null, null, null, o.page ?? 1);
                           };

            // Archive
            Get[@"/(?<year>19[0-9]{2}|2[0-9]{3})"] = p => GetPosts(session, p.year, null, null, null);
            Get[@"/(?<year>19[0-9]{2}|2[0-9]{3})/page/(?<page>\d+)"] = p => GetPosts(session, p.year, null, null, p.page);
            Get[@"/(?<year>19[0-9]{2}|2[0-9]{3})/(?<month>0[1-9]|1[012])"] = p => GetPosts(session, p.year, p.month, null, null);
            Get[@"/(?<year>19[0-9]{2}|2[0-9]{3})/(?<month>0[1-9]|1[012])/page/(?<page>\d+)"] = p => GetPosts(session, p.year, p.month, null, p.page);

            // By tag
            Get[@"/tagged/{tagname}"] = p => GetPosts(session, null, null, new[] { (string)p.tagname });
            Get[@"/tagged/{tagname}/page/{page?1}"] = p => GetPosts(session, null, null, new[] { (string)p.tagname }, (int)p.page);
        }

        private object GetPosts(IDocumentSession session, int? year = null, int? month = null, IEnumerable<string> tags = null, int? page = null)
        {
            StringBuilder pageHeader = null;
                   
            var postsQuery = session.Query<BlogPost>();
            if (year != null && month != null)
            {
                pageHeader = new StringBuilder(String.Format(" of month {0} of the year {1}", month.Value, year.Value));
                postsQuery = postsQuery.Where(x => x.PublishedAt.Year == year && x.PublishedAt.Month == month);
            }
            else if (year != null)
            {
                pageHeader = new StringBuilder(String.Format(" of the year {0}", year.Value));
                postsQuery = postsQuery.Where(x => x.PublishedAt.Year == year);
            }

            if (tags != null)
            {
                if (pageHeader == null) pageHeader = new StringBuilder();
                pageHeader.AppendFormat(" tagged {0}", String.Join(", ", tags));
                foreach (var tag in tags)
                {
                    postsQuery = postsQuery.Where(x => x.Tags.Any(t => t == tag));
                }
            }

            RavenQueryStatistics stats;
            var posts = postsQuery.Where(x => x.CurrentState == BlogPost.State.Public)
                .OrderByDescending(x => x.PublishedAt)
                .Statistics(out stats)
                .Skip(((page ?? 1) - 1) * PageSize).Take(PageSize)
                .ToList();

            ViewBag.AreaRoutePrefix = AreaRoutePrefix;
            Model.Year = year;
            Model.Month = month;

            Model.BlogPosts = posts;

            // Paging info
            Model.TotalBlogPosts = stats.TotalResults;
            Model.CurrentPage = page ?? 1;
            Model.PageSize = PageSize;

            if (pageHeader != null)
            {
                pageHeader.Insert(0, "All blog posts");
                ((PageModel) Model.Page).Title = Model.ListTitle = pageHeader.ToString();
            }

            return View["ListBlogPosts", Model];
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
            var widgets = new List<WidgetViewModel>();

            var blogConfig = session.Load<BlogConfig>("NSemble/Configs/MyBlog");// TODO: Use AreaConfigs, Constants, admin create
            if (blogConfig != null)
            {
                widgets.AddRange(blogConfig.Widgets.Select(widget => new WidgetViewModel(session, widget)));
            }

            Model.Widgets = widgets;
        }
    }
}
