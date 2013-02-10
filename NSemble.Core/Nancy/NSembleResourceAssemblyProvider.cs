using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nancy;
using Nancy.Bootstrapper;

namespace NSemble.Core.Nancy
{
    public class NSembleResourceAssemblyProvider : IResourceAssemblyProvider
    {
        public IEnumerable<Assembly> GetAssembliesToScan()
        {
            return AppDomainAssemblyTypeScanner
                .Assemblies
                .Where(x => x.FullName.StartsWith("NSemble.Modules."));
        }
    }
}
