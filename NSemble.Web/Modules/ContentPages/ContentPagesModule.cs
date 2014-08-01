using System;
using NSemble.Core.Models;
using NSemble.Core.Nancy;
using NSemble.Modules.ContentPages.Models;
using Raven.Client;

namespace NSemble.Modules.ContentPages
{
    public class ContentPagesModule : NSembleModule
    {
        public static readonly string HomepageSlug = "home";

        public ContentPagesModule(IDocumentSession session)
            : base("ContentPages")
        {
            Get["/{slug*}"] = p =>
                                 {
                                     var slug = (string)p.slug;
                                     if (string.IsNullOrWhiteSpace(slug))
                                         slug = HomepageSlug;

                                     // For fastest loading, we define the content page ID to be the slug. Therefore, slugs have to be < 50 chars, probably
                                     // much shorter for readability.
                                     var cp = session.Load<ContentPage>(DocumentPrefix + ContentPage.FullContentPageId(slug));
                                     if (cp == null)
                                         return "<p>The requested content page was not found</p>"; // we will return a 404 instead once the system stabilizes...

                                     Model.ContentPage = cp;
                                     ((PageModel) Model.Page).Title = cp.Title;

                                     return View["Read", Model];
                                 };

            Get["/error"] = o =>
                                {
                                    throw new NotSupportedException("foo");
                                };
        }
    }
}