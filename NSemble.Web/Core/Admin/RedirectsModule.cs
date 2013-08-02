using System.Collections.Generic;
using NSemble.Core;
using Raven.Client;

namespace NSemble.Web.Core.Admin
{
    public class RedirectsModule : NSembleCoreAdminModule
    {
        public RedirectsModule(IDocumentSession session)
            : base("Templates")
        {
            Get["/"] = p =>
                           {
                               return "A screen for editing the redirects table defined in " + Constants.RedirectsTableDocumentId;
                           };
        }
    }
}