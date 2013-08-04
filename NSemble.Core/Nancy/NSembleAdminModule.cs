using System;
using Nancy.Security;

namespace NSemble.Core.Nancy
{
    public abstract class NSembleAdminModule : NSembleModule
    {
        protected NSembleAdminModule(string moduleName)
            : base(true, string.Concat(Constants.ResolverAdminAreaPrefix, "/", moduleName))
        {
            this.RequiresAuthentication();

            if (string.IsNullOrWhiteSpace(moduleName))
                throw new ArgumentException("Module name cannot be empty", "moduleName");

            if (moduleName.IndexOf('/') > -1)
                throw new ArgumentException("Module name cannot contain slashes", "moduleName");
        }
    }
}