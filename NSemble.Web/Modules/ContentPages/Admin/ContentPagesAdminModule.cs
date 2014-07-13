using System;
using System.Linq;
using NSemble.Core.Extensions;
using NSemble.Core.Models;
using NSemble.Core.Nancy;
using NSemble.Modules.ContentPages.Models;
using Nancy;
using Nancy.ModelBinding;
using Raven.Client;

namespace NSemble.Modules.ContentPages.Admin
{
    public class ContentPagesAdminModule : NSembleAdminModule
    {
        public ContentPagesAdminModule(IDocumentSession session)
            : base("ContentPages")
        {
            Get["/"] = p =>
                           {
                               var list = session.Advanced.LoadStartingWith<ContentPage>(DocumentPrefix + ContentPage.FullContentPageId(string.Empty), null, 0, 25);
                               return View["List", list.ToArray()];
                           };

            Get["/create/"] = p => View["Edit", new ContentPage {ContentType = DynamicContentType.Markdown}];

            Post["/create/"] = p =>
                                   {
                                       var cp = this.Bind<ContentPage>();

                                       var pageId = ContentPage.FullContentPageId(DynamicContentHelpers.TitleToSlug(cp.Title));
                                       var page = session.Load<ContentPage>(pageId);
                                       if (page != null)
                                       {
                                           //ModelState.AddModelError("Id", "Page already exists for the slug you're trying to create it under");
                                           return View["Edit", cp];
                                       }

                                       // Render and cache the output
                                       cp.CachedRenderedContent = cp.CompiledContent(true).ToHtmlString();

                                       session.Store(cp, pageId);
                                       session.SaveChanges();

                                       return Response.AsRedirect(string.Concat(AreaRoutePrefix.TrimEnd('/'), "/", cp.Slug));
                                   };

            Get["/edit/{slug}"] = p =>
                                      {
                                          var cp = session.Load<ContentPage>(DocumentPrefix + ContentPage.FullContentPageId((string) p.slug));
                                          if (cp == null)
                                              return 404;

                                          return View["Edit", cp];
                                      };

            Post["/edit/{slug}"] = p =>
                                      {
                                          var input = this.Bind<ContentPage>();
                                          var cp = session.Load<ContentPage>(DocumentPrefix + ContentPage.FullContentPageId((string)p.slug));
                                          if (cp == null)
                                              return 404;

                                          cp.Content = input.Content;
                                          cp.ContentType = input.ContentType;
                                          cp.CachedRenderedContent = cp.CompiledContent(true).ToHtmlString();
                                          cp.Title = input.Title;
                                          cp.LastChanged = DateTimeOffset.Now;
                                          
                                          session.SaveChanges();

                                          return Response.AsRedirect(string.Concat(AreaRoutePrefix.TrimEnd('/'), "/", cp.Slug));
                                      };

//			Post["/delete/{slug}"] = p =>
//			                             {
//
//			                             };
        }
    }
}