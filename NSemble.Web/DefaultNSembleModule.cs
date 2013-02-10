using NSemble.Core.Nancy;
using NSemble.Modules.ContentPages.Models;

namespace NSemble.Web
{
    public sealed class DefaultNSembleModule : NSembleModule
    {
        public DefaultNSembleModule() : base("Default")
        {
            Get["/"] = parameters =>
                           {
                               var cp = new ContentPage
                                            {
                                                Title = "Welcome!",
                                                Content = "foo bar",
                                            };

                               return View["Modules/ContentPages/Read", cp];
                           };

            // TODO: admin login Get and Post, since auth is on the module level
        }
    }
}