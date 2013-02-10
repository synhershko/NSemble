using System.Collections.Concurrent;
using System.Collections.Generic;
using NSemble.Core;
using NSemble.Core.Nancy;

namespace NSemble.Web.Core
{
    public class NSembleCoreAdminModule : NSembleAdminModule
    {
        internal static readonly IDictionary<string, AreaConfigs> AvailableModules = new ConcurrentDictionary<string, AreaConfigs>();

        static NSembleCoreAdminModule()
        {
            AvailableModules.Add("home", new AreaConfigs { ModuleName = "home" });
            AvailableModules.Add(string.Empty, new AreaConfigs { ModuleName = "home" });
            AvailableModules.Add("templates", new AreaConfigs { ModuleName = "templates" });
        }

        public NSembleCoreAdminModule() : this("home")
        {
            Get["/"] = p =>
                                {
                                    return "Admin home";
                                };
        }

        protected NSembleCoreAdminModule(string moduleName) : base(moduleName)
        {
        }
    }
}