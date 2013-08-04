using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NSemble.Core.Nancy;
using NSemble.Web.Modules.Membership.Models;
using Nancy;
using Nancy.ModelBinding;
using Raven.Client;

namespace NSemble.Web.Modules.Membership
{
    public class MembershipModule : NSembleModule
    {
        public MembershipModule(IDocumentSession session)
            : base("Membership")
        {
            Get["/login"] = p => View["Login", new LoginInput()];

            Post["/login"] = p =>
                                 {
                                     var input = this.Bind<LoginInput>();
                                     var apiKey = NSembleUserAuthentication.ValidateUser(session, input.UserName, input.Password);
                                     if (apiKey != null)
                                     {
                                         return Response.AsRedirect("/admin").AddCookie("ApiKey", apiKey);
                                     }
                                     return View["Login", input];
                                 };

            Get["/logout"] = p =>
                                 {
                                     if (Context.Items.ContainsKey("ApiKey"))
                                     {
                                         NSembleUserAuthentication.RemoveApiKey(session, (string) Context.Items["ApiKey"]);
                                     }
                                     return Response.AsRedirect("/");
                                 };
        }
    }
}