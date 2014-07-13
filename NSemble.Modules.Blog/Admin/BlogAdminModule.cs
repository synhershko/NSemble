using System;
using System.Collections.Generic;
using System.Linq;
using NSemble.Core.Extensions;
using NSemble.Core.Models;
using NSemble.Modules.Blog.Helpers;
using NSemble.Modules.Blog.Models;
using Nancy;
using Nancy.ModelBinding;
using Raven.Client;
using Raven.Json.Linq;

namespace NSemble.Modules.Blog.Admin
{
    public class BlogAdminModule : Core.Nancy.NSembleAdminModule
    {
        public BlogAdminModule(IDocumentSession session)
            : base("Blog")
        {
            var blogConfig = session.Load<BlogConfig>("NSemble/Configs/MyBlog");

            Get["/"] = o =>
                           {
                               ViewBag.ModulePrefix = AreaRoutePrefix.TrimEnd('/');

                               Model.RecentPosts = session.Query<BlogPost>()
                                          .Where(x => x.CurrentState == BlogPost.State.Public)
                                          .OrderByDescending(x => x.PublishedAt)
                                          .Take(10)
                                          .ToArray();

                               Model.Drafts = session.Query<BlogPost>()
                                          .Where(x => x.CurrentState == BlogPost.State.Draft)
                                          .OrderByDescending(x => x.PublishedAt)
                                          .Take(10)
                                          .ToArray();

                               return View["Home", Model];
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
                post.AllowComments = true;
                
                var identity = (User)Context.CurrentUser;
                post.AuthorId = identity.Id;
                
                string tags = Request.Form.TagsAsString;
                post.Tags = new HashSet<string>();
                if (!String.IsNullOrWhiteSpace(tags))
                {
                    foreach (var tag in tags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        post.Tags.Add(tag.Trim());
                    }
                }

                if ("Publish".Equals(Request.Form["SubmitAction"]))
                {
                    post.CurrentState = BlogPost.State.Public;
                    post.PublishedAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    post.CurrentState = BlogPost.State.Draft;
                    post.PublishedAt = DateTimeOffset.MinValue;
                }

                // Render and cache the output
                post.CachedRenderedContent = post.CompiledContent(true).ToHtmlString();

                session.Store(post, "BlogPosts/");
                session.SaveChanges();

                return Response.AsRedirect(post.ToUrl(AreaRoutePrefix.TrimEnd('/')));
            };


            Get[@"/edit/(?<id>\d+)"] = p =>
            {
                var blogPost = session.Load<BlogPost>((int)p.id);
                if (blogPost == null)
                    return 404;

                return View["Edit", blogPost];
            };

            Post[@"/edit/(?<id>\d+)"] = p =>
            {
                var blogPost = session.Load<BlogPost>((int)p.id);
                if (blogPost == null)
                    return 404;

                var input = this.Bind<BlogPost>();

                bool validated = true;
                if (!validated)
                {
                    //ModelState.AddModelError("Id", "");
                    return View["Edit", input];
                }

                blogPost.Title = input.Title;
                blogPost.Content = input.Content;

                string tags = Request.Form.TagsAsString;
                blogPost.Tags = new HashSet<string>();
                if (!String.IsNullOrWhiteSpace(tags))
                {
                    foreach (var tag in tags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        blogPost.Tags.Add(tag.Trim());
                    }
                }
                blogPost.LastEditedAt = DateTimeOffset.UtcNow;

                if ("Publish".Equals(Request.Form["SubmitAction"]))
                {
                    blogPost.CurrentState = BlogPost.State.Public;
                    if (blogPost.PublishedAt == DateTimeOffset.MinValue)
                    {
                        blogPost.PublishedAt = DateTimeOffset.UtcNow;
                    }
                }

                // Update the cached rendered page
                blogPost.CachedRenderedContent = blogPost.CompiledContent(true).ToHtmlString();

                session.SaveChanges();

                return Response.AsRedirect(input.ToUrl(AreaRoutePrefix.TrimEnd('/')));
            };

            Get[@"/stats/{days?7}/{type?all}"] = o =>
                                                     {
                                                         var ret = new RavenJObject();

                                                         if (blogConfig != null)
                                                         {
                                                             var url = string.Format(@"http://stats.wordpress.com/csv.php?api_key={0}&blog_id={1}&format=json",
                                                                 blogConfig.WordPressAPIKey, blogConfig.WordPressBlogId);
                                                             using (var webClient = new System.Net.WebClient())
                                                             {
                                                                 // TODO async, not UTF8 compatible
                                                                 switch ((string)o.type)
                                                                 {
                                                                     case "searchterms":
                                                                         ret.Add("searchterms", RavenJToken.Parse(webClient.DownloadString(url + "&table=searchterms")));
                                                                         break;
                                                                     case "clicks":
                                                                         ret.Add("clicks", RavenJToken.Parse(webClient.DownloadString(url + "&table=clicks")));
                                                                         break;
                                                                     case "referrers":
                                                                         ret.Add("referrers", RavenJToken.Parse(webClient.DownloadString(url + "&table=referrers_grouped")));
                                                                         break;
                                                                     case "views":
                                                                     default:
                                                                         ret.Add("histogram", RavenJToken.Parse(webClient.DownloadString(url)));
                                                                         break;
                                                                 }

                                                                 if ("all".Equals((string) o.type))
                                                                 {
                                                                     ret.Add("searchterms", RavenJToken.Parse(webClient.DownloadString(url + "&table=searchterms&days=2")));
                                                                     ret.Add("clicks", RavenJToken.Parse(webClient.DownloadString(url + "&table=clicks&days=2")));
                                                                     ret.Add("referrers", RavenJToken.Parse(webClient.DownloadString(url + "&table=referrers_grouped&days=2")));
                                                                 }
                                                             }
                                                         }

                                                         return Response.AsText(ret.ToString(), "text/json");
                                                     };

            Get[@"/config"] = o =>
                                  {
                                      using (session.Advanced.DocumentStore.DisableAggressiveCaching())
                                      {
                                          var config = session.Load<BlogConfig>("NSemble/Configs/" + AreaConfigs.AreaName);
                                          return View["Config", config];
                                      }
                                  };

            Get[@"/config/widgets"] = o =>
                                          {
                                              using (session.Advanced.DocumentStore.DisableAggressiveCaching())
                                              {
                                                  var config =
                                                      session.Load<BlogConfig>("NSemble/Configs/" + AreaConfigs.AreaName);
                                                  return View["ConfigWidgets", config.Widgets.ToArray()];
                                              }
                                          };
        }
    }
}
