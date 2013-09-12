using System.Collections.Generic;
using NSemble.Core;
using NSemble.Core.Nancy;
using NSemble.Core.Models;
using Nancy;
using Nancy.ModelBinding;
using Raven.Client;

namespace NSemble.Modules.Welcome
{
    public class WelcomeModule : NSembleModule
    {
        public WelcomeModule(IDocumentSession session)
            : base("Welcome")
        {
            Get["/"] = p => View["Welcome"];

            Post["/"] = p =>
            {
                var user = this.Bind<User>("Password", "Salt", "Claims");
                user.Claims = new List<string> {"admin"};
                NSembleUserAuthentication.SetUserPassword(user, Request.Form.Password);
                session.Store(user, "users/" + user.Email);

                session.Store(new Dictionary<string, AreaConfigs>
                                      {
                                          //{"/blog", new AreaConfigs { AreaName = "MyBlog", ModuleName = "Blog" }},
                                          //{"/content", new AreaConfigs { AreaName = "MyContent", ModuleName = "ContentPages" }},
                                          {"/auth", new AreaConfigs { AreaName = "Auth", ModuleName = "Membership" }}
                                      }, Constants.AreasDocumentName);

                session.SaveChanges();

                // Refresh the Areas configs
                AreasResolver.Instance.LoadFromStore(session);

                return Response.AsRedirect("/");
            };
        }
    }
}