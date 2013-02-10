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
	}
}
