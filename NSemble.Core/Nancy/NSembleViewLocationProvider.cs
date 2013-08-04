using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NSemble.Core.Models;
using Nancy;
using Nancy.ViewEngines;
using Raven.Client;

namespace NSemble.Core.Nancy
{
	public class NSembleViewLocationProvider : IViewLocationProvider
	{
	    private readonly IDocumentStore _documentStore;
	    private readonly FileSystemViewLocationProvider fsViewLocationProvider;
        private readonly ResourceViewLocationProvider resourcesViewLocationProvider;

        public NSembleViewLocationProvider(IRootPathProvider rootPathProvider, global::Nancy.TinyIoc.TinyIoCContainer container)
		{
            _documentStore = container.Resolve<IDocumentStore>("DocStore");
		    fsViewLocationProvider = new FileSystemViewLocationProvider(rootPathProvider);
            resourcesViewLocationProvider = new ResourceViewLocationProvider(new NSembleResourceReader(), new NSembleResourceAssemblyProvider());
		}

        public NSembleViewLocationProvider(IRootPathProvider rootPathProvider, IFileSystemReader fileSystemReader, global::Nancy.TinyIoc.TinyIoCContainer container)
		{
            _documentStore = container.Resolve<IDocumentStore>("DocStore");
		    fsViewLocationProvider = new FileSystemViewLocationProvider(rootPathProvider, fileSystemReader);
            resourcesViewLocationProvider = new ResourceViewLocationProvider(new NSembleResourceReader(), new NSembleResourceAssemblyProvider());
		}

		public IEnumerable<ViewLocationResult> GetLocatedViews(IEnumerable<string> supportedViewExtensions)
		{
			var sb = new StringBuilder();

            // Make sure to only load saved views with supported extensions
			foreach (var s in supportedViewExtensions)
			{
				if (sb.Length > 0)
					sb.Append("|");
				sb.Append("*.");
				sb.Append(s);
			}

			ViewTemplate[] views;
			using (var session = _documentStore.OpenSession())
			{
                // It's probably safe to assume we will have no more than 1024 views, so no reason to bother with paging
				views = session.Advanced.LoadStartingWith<ViewTemplate>(Constants.RavenViewDocumentPrefix, sb.ToString(), 0, 1024);
			}

            // Read the views from the default location
		    IEnumerable<ViewLocationResult> defaultViews = fsViewLocationProvider.GetLocatedViews(supportedViewExtensions)
		                                                                         .Concat(resourcesViewLocationProvider.GetLocatedViews(supportedViewExtensions));
			if (views.Length == 0)
				return defaultViews;

            // Views are uniquely identified by their Location, Name and Extension
			var ret = new HashSet<ViewLocationResult>(from v in views
			                                          select new ViewLocationResult(
				                                          v.Location,
				                                          v.Name,
				                                          v.Extension,
				                                          () => new StringReader(v.Contents)));

			foreach (var v in defaultViews)
				ret.Add(v);

			return ret;
		}

        /// <summary>
        /// Returns an <see cref="ViewLocationResult"/> instance for all the views matching the viewName that could be located by the provider.
        /// </summary>
        /// <param name="supportedViewExtensions">An <see cref="IEnumerable{T}"/> instance, containing the view engine file extensions that is supported by the running instance of Nancy.</param>
        /// <param name="viewName">The name of the view to try and find</param>
        /// <returns>An <see cref="IEnumerable{T}"/> instance, containing <see cref="ViewLocationResult"/> instances for the located views.</returns>
        /// <remarks>If no views could be located, this method should return an empty enumerable, never <see langword="null"/>.</remarks>
        public IEnumerable<ViewLocationResult> GetLocatedViews(IEnumerable<string> supportedViewExtensions, string location, string viewName)
        {
            var allResults = this.GetLocatedViews(supportedViewExtensions);

            return allResults.Where(vlr => vlr.Location.Equals(location, StringComparison.OrdinalIgnoreCase) &&
                                           vlr.Name.Equals(viewName, StringComparison.OrdinalIgnoreCase));
        }
	}
}
