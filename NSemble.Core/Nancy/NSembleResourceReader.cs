using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Nancy.ViewEngines;

namespace NSemble.Core.Nancy
{
    public class NSembleResourceReader : IResourceReader
    {
        /// <summary>
        /// Gets information about the resources that are embedded in the assembly.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to retrieve view information from.</param>
        /// <param name="supportedViewEngineExtensions">A list of view extensions to look for.</param>
        /// <returns>A <see cref="IList{T}"/> of resource locations and content readers.</returns>
        public IList<Tuple<string, Func<StreamReader>>> GetResourceStreamMatches(Assembly assembly, IEnumerable<string> supportedViewEngineExtensions)
        {
            var resourceStreams =
                from resourceName in assembly.GetManifestResourceNames()
                from viewEngineExtension in supportedViewEngineExtensions
                where GetResourceExtension(resourceName).Equals(viewEngineExtension, StringComparison.OrdinalIgnoreCase)
                select new Tuple<string, Func<StreamReader>>(
                    DuplicateModuleNameByConvention(resourceName),
                    () => new StreamReader(assembly.GetManifestResourceStream(resourceName)));

            return resourceStreams.ToList();
        }

        private static string GetResourceExtension(string resourceName)
        {
            var extension = Path.GetExtension(resourceName);
            return string.IsNullOrEmpty(extension) ? string.Empty : extension.Substring(1);
        }

        /// <summary>
        /// This one is important - it ensures the view locations loaded from DLLs are in line with the NSemble convention,
        /// e.g. Modules/Blog/Views/SomeView.cshtml
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        private static string DuplicateModuleNameByConvention(string resourceName)
        {
            if (resourceName.StartsWith("NSemble.Modules."))
            {
                int pos = resourceName.IndexOf('.', 16);
                resourceName = string.Concat(resourceName.Substring(0, pos), ".", resourceName.Substring(8));
            }
            return resourceName;
        }
    }
}
