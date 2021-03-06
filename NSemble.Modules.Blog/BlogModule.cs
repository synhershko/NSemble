﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSemble.Core.Extensions;
using NSemble.Core.Models;
using NSemble.Core.Nancy;
using NSemble.Core.Tasks;
using NSemble.Modules.Blog.Helpers;
using NSemble.Modules.Blog.Models;
using NSemble.Modules.Blog.Tasks;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Raven.Client;
using Raven.Client.Linq;

namespace NSemble.Modules.Blog
{
    public sealed class BlogModule : NSembleModule
    {
        const int DefaultPageSize = 10;

        public BlogModule(IDocumentSession session)
            : base("Blog")
        {
            // TODO blog module configs by area name

            const string blogPostRoute = @"/(?<year>19[0-9]{2}|2[0-9]{3})/(?<month>0[1-9]|1[012])/(?<id>\d+)-(?<slug>.+)";

            BlogConfig blogConfig;
            using (session.Advanced.DocumentStore.AggressivelyCacheFor(TimeSpan.FromMinutes(5)))
            {
                blogConfig = session.Load<BlogConfig>("NSemble/Configs/MyBlog") ?? new BlogConfig { BlogTitle = "My Blog" };
            }
            LoadWidgets(session, blogConfig);

            Get[blogPostRoute] = p =>
                                     {
                                         BlogPost post;
                                         try
                                         {
                                             post = GetBlogPost((int)p.id, Request.Query.key, session);
                                             session.Advanced.MarkReadOnly(post);
                                         }
                                         catch (ArgumentException e)
                                         {
                                             Context.Response.StatusCode = HttpStatusCode.NotFound;
                                             return e.Message;
                                         }

                                         int year = (int)p.year;
                                         int month = (int)p.month;
                                         if (post.PublishedAt.Month != month || post.PublishedAt.Year != year)
                                             return 404;

                                         if (!post.Slug.Equals(p.slug))
                                             return Response.AsRedirect(post.ToUrl(AreaRoutePrefix.TrimEnd('/')), RedirectResponse.RedirectType.Permanent);

                                         // Disable commenting if time has come to close comments
                                         if (post.AllowComments && post.PublishedAt.AddDays(blogConfig.KeepCommentsOpenFor.Days) > DateTimeOffset.UtcNow)
                                         {
                                             post.AllowComments = false;
                                         }

                                         ((PageModel)Model.Page).Title = post.Title;
                                         ViewBag.AreaRoutePrefix = AreaRoutePrefix;
                                         Model.BlogPost = post;
                                         Model.Comments = session.Load<PostComments>(post.Id + "/comments") ?? new PostComments();

                                         return View["ReadBlogPost", Model];
                                     };

            Post[blogPostRoute + "/new-comment"] = p =>
                                                      {
                                                          var commentInput = this.Bind<PostComments.CommentInput>();
                                                          if (!commentInput.IsValid() || !"8".Equals(Request.Form["HumanVerification"]))
                                                              return "Error"; // TODO pass errors in Model

                                                          BlogPost post;
                                                          try
                                                          {
                                                              post = GetBlogPost((int)p.id, Request.Query.key, session);
                                                          }
                                                          catch (ArgumentException e)
                                                          {
                                                              Context.Response.StatusCode = HttpStatusCode.NotFound;
                                                              return e.Message;
                                                          }

                                                          if (!post.AllowComments)
                                                              return "Comments are closed for this post";

                                                          // Don't allow impersonation if we know the commenter - require users to login before commenting
                                                          var author = session.Load<User>(commentInput.Author);
                                                          if (author != null && !author.Equals(Context.CurrentUser))
                                                              return "Please login to post a new comment";

                                                          TaskExecutor.ExcuteLater(new AddCommentTask(session.Advanced.DocumentStore, blogConfig, post.Id, commentInput, new AddCommentTask.RequestValues { UserAgent = Request.Headers.UserAgent, UserHostAddress = Request.UserHostAddress }));

                                                          return Response.AsRedirect(post.ToUrl(AreaRoutePrefix.TrimEnd('/')));
                                                      };

            // Home
            Get["/"] = o =>
            {
                               ((PageModel)Model.Page).Title = blogConfig.TagLine ?? blogConfig.BlogDescription;
                               ((PageModel)Model.Page).SubTitle = "Welcome to my blog!";
                               Model.ListTitle = string.Empty;

                               return View["BlogHome", GetPosts(session)];
                           };
            Get[@"/page/(?<page>\d+)"] = o =>
                           {
                               ((PageModel)Model.Page).Title = blogConfig.BlogDescription;
                               Model.ListTitle = string.Empty;

                               return View["ListBlogPosts", GetPosts(session, null, null, null, o.page ?? 1)];
                           };

            // Archive
            Get[@"/(?<year>19[0-9]{2}|2[0-9]{3})"] = p => View["ListBlogPosts", GetPosts(session, p.year, null, null, null)];
            Get[@"/(?<year>19[0-9]{2}|2[0-9]{3})/page/(?<page>\d+)"] = p => View["ListBlogPosts", GetPosts(session, p.year, null, null, p.page)];
            Get[@"/(?<year>19[0-9]{2}|2[0-9]{3})/(?<month>0[1-9]|1[012])"] = p => View["ListBlogPosts", GetPosts(session, p.year, p.month, null, null)];
            Get[@"/(?<year>19[0-9]{2}|2[0-9]{3})/(?<month>0[1-9]|1[012])/page/(?<page>\d+)"] = p => View["ListBlogPosts", GetPosts(session, p.year, p.month, null, p.page)];

            // By tag
            Get[@"/tagged/{tagname}"] = p => View["ListBlogPosts", GetPosts(session, null, null, ((string)p.tagname ?? "").Split(','))];
            Get[@"/tagged/{tagname}/page/{page?1}"] = p => View["ListBlogPosts", GetPosts(session, null, null, ((string)p.tagname ?? "").Split(','), (int)p.page)];

            // RSS feed
            Get[@"/rss"] = p =>
                               {
                                   // Limit older posts from appearing in the feed
                                   var dateThreshold = DateTimeOffset.UtcNow.AddMonths(-6);
                                   dateThreshold = new DateTimeOffset(dateThreshold.Year, dateThreshold.Month, dateThreshold.Day,
                                                                      0, 0, 0, dateThreshold.Offset);

                                   RavenQueryStatistics stats;
                                   var postsQuery = session.Query<BlogPost>().Statistics(out stats)
                                                           .Where(x => x.PublishedAt >= dateThreshold)
                                                           .Where(x => x.CurrentState == BlogPost.State.Public)
                                                           .OrderByDescending(x => x.PublishedAt)
                                                           .Take(20);

                                   if (!string.IsNullOrWhiteSpace(Request.Query.tagged))
                                   {
                                       var tags = ((string)Request.Query.tagged).Split(',');
                                       foreach (var tag in tags)
                                       {
                                           postsQuery = postsQuery.Where(x => x.Tags.Any(t => t == tag));
                                       }
                                   }

                                   var blogPosts = postsQuery.ToList();

                                   string responseETagHeader;
                                   if (CheckEtag(stats, out responseETagHeader))
                                       return HttpStatusCode.NotModified;
                                  
                                   return new RssResponse(blogPosts, new Uri(Context.Request.Url, AreaRoutePrefix), blogConfig);
                               };
        }

        private dynamic GetPosts(IDocumentSession session, int? year = null, int? month = null, IEnumerable<string> tags = null, int? page = null)
        {
            StringBuilder pageHeader = null;
            int pageSize = DefaultPageSize;

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

            var sortingOrder = SortOrder.Desc;
            if (tags != null && tags.Any())
            {
                if (pageHeader == null) pageHeader = new StringBuilder();
                pageHeader.AppendFormat(" tagged \"{0}\"", String.Join("\", \"", tags));
                foreach (var tag in tags)
                {
                    postsQuery = postsQuery.Where(x => x.Tags.Any(t => t == tag));
                }

                if (tags.Count() == 1)
                {
                    var tagDescriptor = session.Load<TagDescriptor>("Blog/TagDescriptors/" + tags.First());
                    if (tagDescriptor != null)
                    {
                        sortingOrder = tagDescriptor.DefaultSortingOrder;
                        pageSize = tagDescriptor.PostsPerPage ?? DefaultPageSize;
                        Model.TagDescription = tagDescriptor.CompiledContent();
                    }
                }
            }

            if (sortingOrder == SortOrder.Asc)
                postsQuery = postsQuery.OrderBy(x => x.PublishedAt);
            else
                postsQuery = postsQuery.OrderByDescending(x => x.PublishedAt);

            RavenQueryStatistics stats;
            var posts = postsQuery.Where(x => x.CurrentState == BlogPost.State.Public)
                .Statistics(out stats)
                .Skip(((page ?? 1) - 1) * pageSize).Take(pageSize)
                .ToList();
            
            ViewBag.AreaRoutePrefix = AreaRoutePrefix;
            Model.Year = year;
            Model.Month = month;
            Model.Tags = tags;
            Model.BlogPosts = posts;

            // Paging info
            Model.TotalBlogPosts = stats.TotalResults;
            Model.CurrentPage = page ?? 1;
            Model.PageSize = pageSize;

            if (pageHeader != null)
            {
                pageHeader.Insert(0, "All blog posts");
                ((PageModel)Model.Page).SubTitle = ((PageModel)Model.Page).Title = Model.ListTitle = pageHeader.ToString();
            }

            return Model;
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

        private static readonly string EtagInitValue = Guid.NewGuid().ToString();
        private bool CheckEtag(RavenQueryStatistics stats, out string responseETagHeader)
        {
            responseETagHeader = stats.Timestamp.ToString("o") + EtagInitValue;
            var requestETagHeader = Request.Headers["If-None-Match"];
            if (requestETagHeader == null) return false;
            return (requestETagHeader.FirstOrDefault() ?? string.Empty) == responseETagHeader;
        }

        private void LoadWidgets(IDocumentSession session, BlogConfig blogConfig)
        {
            var widgets = new List<WidgetViewModel>();

            // TODO: Use AreaConfigs, Constants, admin create
            if (blogConfig != null && blogConfig.Widgets != null)
            {
                widgets.AddRange(blogConfig.Widgets.Select(widget => new WidgetViewModel(session, widget)));
            }

            Model.Widgets = widgets;
        }
    }
}
