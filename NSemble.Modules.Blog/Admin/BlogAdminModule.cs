using System;
using System.Collections.Generic;
using System.Linq;
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

                               Model.RecentPosts = session.Query<BlogPost>().Take(10).ToArray();

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
                post.PublishedAt = DateTimeOffset.UtcNow;
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
                    post.CurrentState = BlogPost.State.Public;
                
                session.Store(post);
                session.Store(new PostComments(), post.Id + "/comments");
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
                    blogPost.CurrentState = BlogPost.State.Public;

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
                                                             }
                                                         }

                                                         return Response.AsText(ret.ToString(), "text/json");
                                                     };
        }
    }
}
