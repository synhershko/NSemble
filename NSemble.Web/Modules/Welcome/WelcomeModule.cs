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
                User user = new User();
                //this.BindToAndValidate<User>(user);

                user.FirstName = Request.Form.firstName;
                user.LastName = Request.Form.lastName;
                user.UserName = Request.Form.username;
                user.Email = Request.Form.email;
                
                session.Store(user);

                NSembleUserAuthentication.SetUserPassword(user, Request.Form.password);

                session.SaveChanges();

                return Response.AsRedirect("/");
            };
        }
    }
}