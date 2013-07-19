using NSemble.Core.Nancy;
using Raven.Client;

namespace NSemble.Modules.Welcome
{
    public class WelcomeModule : NSembleModule
    {
        public WelcomeModule(IDocumentSession session)
            : base("Welcome")
        {
            Get["/"] = p => View["Welcome"];
        }
    }
}