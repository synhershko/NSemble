using System;
using System.Web;
using NSemble.Web.Core;
using Nancy;
using Nancy.Bootstrapper;
using Raven.Client;
using Raven.Client.Document;

namespace NSemble.Core.Nancy
{
    public class NSembleBootstraper : DefaultNancyBootstrapper
    {
		protected override void ApplicationStartup(global::Nancy.TinyIoc.TinyIoCContainer container, IPipelines pipelines)
		{
            Conventions.ViewLocationConventions.Add((viewName, model, context) =>
            {
                var actualViewName = viewName;
                var tmp = viewName.LastIndexOf('/');
                if (tmp > -1) actualViewName = viewName.Substring(tmp + 1);

                if (context.ModulePath.StartsWith(Constants.ResolverAdminAreaPrefix))
                    return string.Concat("Modules", context.ModulePath.Substring(Constants.ResolverAdminAreaPrefix.Length), "/Admin/Views/", actualViewName);

                if (context.ModulePath.StartsWith(Constants.ResolverAreaPrefix))
                    return string.Concat("Modules", context.ModulePath.Substring(Constants.ResolverAreaPrefix.Length), "/Views/", actualViewName);

                if (tmp > -1)
                    return string.Concat(viewName.Substring(0, tmp), "/Views/", viewName.Substring(tmp + 1));
                return viewName;
            });

            var docStore = container.Resolve<IDocumentStore>("DocStore");

            AreasResolver.Instance.AdminAreaPrefix = "/admin";

            using (var session = docStore.OpenSession())
            {
                AreasResolver.Instance.LoadFromStore(session);
            }

            Raven.Client.Indexes.IndexCreation.CreateIndexes(typeof(NSembleBootstraper).Assembly, docStore);
		}

        protected override void ConfigureApplicationContainer(global::Nancy.TinyIoc.TinyIoCContainer container)
        {
            // TODO: support multiple doc-stores
            var docStore = new DocumentStore { ConnectionStringName = "RavenDB" };
            var conventions = docStore.Conventions;

            conventions.FindFullDocumentKeyFromNonStringIdentifier = (o, type, arg3) =>
            {
                var ret = conventions.DefaultFindFullDocumentKeyFromNonStringIdentifier(o, type, arg3);
                var areaConfigs = HttpContext.Current.Items["AreaConfigs"] as AreaConfigs;
                if (areaConfigs != null)
                    ret = areaConfigs.DocumentsPrefix + ret;

                return ret;
            };

            conventions.TransformTypeTagNameToDocumentKeyPrefix = s =>
            {
                var ret = DocumentConvention.DefaultTransformTypeTagNameToDocumentKeyPrefix(s);
                var areaConfigs = HttpContext.Current.Items["AreaConfigs"] as AreaConfigs;
                if (areaConfigs != null)
                    ret = areaConfigs.DocumentsPrefix + ret;

                return ret;
            };

            docStore.Initialize();
            container.Register<IDocumentStore>(docStore, "DocStore");

            base.ConfigureApplicationContainer(container);
        }

        protected override void ConfigureRequestContainer(global::Nancy.TinyIoc.TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);

            var docStore = container.Resolve<IDocumentStore>("DocStore");
            var session = docStore.OpenSession();
            container.Register<IDocumentSession>(session);
        }

        protected override NancyInternalConfiguration InternalConfiguration
        {
            get
            {
				return NancyInternalConfiguration
					.WithOverrides(x => { x.ViewLocationProvider = typeof (NSembleViewLocationProvider); x.RouteResolver = typeof(NSembleRouteResolver); })
                    .WithIgnoredAssembly(asm => asm.FullName.StartsWith("RavenDB", StringComparison.InvariantCulture)); // or override ConfigureApplicationContainer to set AutoRegister to false
            }
        }
    }
}