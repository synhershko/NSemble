using System;
using System.Linq;
using NSemble.Core;
using NSemble.Core.Models;
using Nancy;
using Nancy.ModelBinding;
using Nancy.ViewEngines;
using Raven.Client;

namespace NSemble.Web.Core.Admin
{
    public class TemplatesModule : NSembleCoreAdminModule
    {
        public TemplatesModule(IDocumentSession session, IViewLocator viewLocator)
            : base("Templates")
        {
            Get["/"] = p =>
                           {
                               var templates = session.Advanced.LoadStartingWith<ViewTemplate>("NSemble/Views/");
                               return View["List", templates];
                           };

            Get["/new/"] = p => View["Edit", new ViewTemplate {}];

            Get[@"/edit/{viewName*}"] = p =>
                                            {
                                                var viewName = (string) p.viewName;
                                                if (!viewName.StartsWith(Constants.RavenViewDocumentPrefix, StringComparison.InvariantCultureIgnoreCase))
                                                    viewName = Constants.RavenViewDocumentPrefix + viewName;
                                                var template = session.Load<ViewTemplate>(viewName);

                                                // Even if we don't have it stored in the DB, it might still exist as a resource. Try loading it from Nancy.
                                                if (template == null)
                                                {
                                                    var vlr = viewLocator.LocateView(viewName.Substring(Constants.RavenViewDocumentPrefix.Length), Context);
                                                    if (vlr == null)
                                                        return 404;

                                                    template = new ViewTemplate
                                                                   {
                                                                       Location = vlr.Location,
                                                                       Name = vlr.Name,
                                                                       Extension = vlr.Extension,
                                                                       Contents = vlr.Contents.Invoke().ReadToEnd(),
                                                                   };
                                                }

                                                return View["Edit", template];
                                            };

            Post[@"/edit/{viewName*}"] = p =>
                                                   {
                                                       var template = this.Bind<ViewTemplate>();
                                                       
                                                       var viewName = (string) p.viewName;
                                                       if (!viewName.StartsWith(Constants.RavenViewDocumentPrefix, StringComparison.InvariantCultureIgnoreCase))
                                                           viewName = Constants.RavenViewDocumentPrefix + viewName;

                                                       session.Store(template, string.Concat(Constants.RavenViewDocumentPrefix, template.Location, "/", template.Name, ".", template.Extension));
                                                       session.SaveChanges();

                                                       return "Success";
                                                   };

            Post["/new"] = p =>
                                {
                                    var template = this.Bind<ViewTemplate>();
                                    session.Store(template, string.Concat(Constants.RavenViewDocumentPrefix, template.Location, "/", template.Name, ".", template.Extension));

                                    return Response.AsRedirect("/");
                                };
        }
    }
}