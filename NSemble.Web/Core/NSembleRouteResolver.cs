namespace NSemble.Web.Core
{
    using System;
    using System.Linq;

    using Nancy;
    using Nancy.Helpers;

    using Nancy.Routing.Trie;
    using NSemble.Core;
    using Nancy.Responses;
    using Nancy.Routing;

	/// <summary>
	/// The default implementation for deciding if any of the available routes is a match for the incoming HTTP request.
	/// </summary>
	public class NSembleRouteResolver : IRouteResolver
	{
        private readonly INancyModuleCatalog catalog;

        private readonly INancyModuleBuilder moduleBuilder;

        private readonly IRouteCache routeCache;

        private readonly IRouteResolverTrie trie;

        public NSembleRouteResolver(INancyModuleCatalog catalog, INancyModuleBuilder moduleBuilder, IRouteCache routeCache, IRouteResolverTrie trie)
        {
            this.catalog = catalog;
            this.moduleBuilder = moduleBuilder;
            this.routeCache = routeCache;
            this.trie = trie;

            this.BuildTrie();
        }

	    private void BuildTrie()
        {
            this.trie.BuildTrie(this.routeCache);
        }

        public ResolveResult Resolve(NancyContext context)
        {
            var pathDecoded = HttpUtility.UrlDecode(context.Request.Path);

            AreaConfigs areaConfigs;

            // We are leveraging sort of a hack of Nancy's routing system. Protect from direct access to internal paths.
            if (pathDecoded.StartsWith(Constants.ResolverAreaPrefix) || pathDecoded.StartsWith(Constants.ResolverAdminAreaPrefix))
                return GetNotFoundResult(context);

            // First, try resolving the admin area. By convention, it is always "admin/areaName/path", where "admin" is configurable
            if (pathDecoded.StartsWith(AreasResolver.Instance.AdminAreaPrefix))
            {
                String areaName;
                var path = pathDecoded.Substring(AreasResolver.Instance.AdminAreaPrefix.Length);
                if (path.Length == 0)
                {
                    areaName = "home";
                    path = "/";
                }
                else
                {
                    int pos = path.IndexOf('/', 1);
                    if (pos > 1)
                    {
                        areaName = path.Substring(1, pos - 1);
                        path = path.Substring(pos);
                    }
                    else
                    {
                        areaName = path.Substring(1);
                        path = "/";
                    }
                }

                // Core admin modules take precedence over user modules
                if (!NSembleCoreAdminModule.AvailableModules.TryGetValue(areaName, out areaConfigs))
                    areaConfigs = AreasResolver.Instance.GetAreaConfigsByName(areaName);

                if (areaConfigs == null)
                    return GetNotFoundResult(context);

                context.Items.Add("AreaConfigs", areaConfigs);

                return Resolve(context, string.Concat(Constants.ResolverAdminAreaPrefix, "/", areaConfigs.ModuleName, path));
            }

            // Try resolving a redirect; this can be done only on the full URLs
            var redirect = AreasResolver.Instance.CheckRedirect(pathDecoded);
            if (redirect != null)
            {
                if (redirect.HttpStatusCode == HttpStatusCode.NotFound)
                    return new ResolveResult(new NotFoundRoute(context.Request.Method, pathDecoded), DynamicDictionary.Empty, null, null, null);

                return new ResolveResult(new Route(context.Request.Method, pathDecoded, null, o => o),
                                         DynamicDictionary.Empty,
                                         nancyContext =>
                                         new RedirectResponse(redirect.NewRoute, redirect.HttpStatusCode == HttpStatusCode.MovedPermanently ? RedirectResponse.RedirectType.Permanent : RedirectResponse.RedirectType.SeeOther),
                                         null,
                                         (nancyContext, exception) =>
                                         new RedirectResponse(redirect.NewRoute, redirect.HttpStatusCode == HttpStatusCode.MovedPermanently ? RedirectResponse.RedirectType.Permanent : RedirectResponse.RedirectType.SeeOther));
            }

            var remainingPath = AreasResolver.Instance.ParseArea(pathDecoded, out areaConfigs);
            if (areaConfigs == null)
                return GetNotFoundResult(context);

            context.Items.Add("AreaConfigs", areaConfigs);

            var newPath = string.Concat(Constants.ResolverAreaPrefix, "/", areaConfigs.ModuleName, remainingPath);
            return Resolve(context, newPath);
        }

	    private ResolveResult Resolve(NancyContext context, string pathDecoded)
	    {
	        var results = this.trie.GetMatches(GetMethod(context), pathDecoded, context);

	        if (!results.Any())
	        {
	            if (this.IsOptionsRequest(context))
	            {
	                return this.BuildOptionsResult(context);
	            }
	            return this.GetNotFoundResult(context);
	        }

	        // Sort in descending order
	        Array.Sort(results, (m1, m2) => -m1.CompareTo(m2));

	        for (var index = 0; index < results.Length; index++)
	        {
	            var matchResult = results[index];
	            if (matchResult.Condition == null || matchResult.Condition.Invoke(context))
	            {
	                return this.BuildResult(context, matchResult);
	            }
	        }

	        return this.GetNotFoundResult(context);
	    }

	    private ResolveResult BuildOptionsResult(NancyContext context)
        {
            var path = context.Request.Path;

            var options = this.trie.GetOptions(path, context);

            var optionsResult = new OptionsRoute(path, options);

            return new ResolveResult(
                            optionsResult,
                            new DynamicDictionary(), 
                            null,
                            null,
                            null);                        
        }

        private bool IsOptionsRequest(NancyContext context)
        {
            return context.Request.Method.Equals("OPTIONS", StringComparison.Ordinal);
        }

        private ResolveResult BuildResult(NancyContext context, MatchResult result)
        {
            var associatedModule = this.GetModuleFromMatchResult(context, result);
            var route = associatedModule.Routes.ElementAt(result.RouteIndex);
            var parameters = DynamicDictionary.Create(result.Parameters);

            return new ResolveResult
            {
                Route = route,
                Parameters = parameters,
                Before = associatedModule.Before,
                After = associatedModule.After,
                OnError = associatedModule.OnError
            };
        }

        private INancyModule GetModuleFromMatchResult(NancyContext context, MatchResult result)
        {
            var module = this.catalog.GetModule(result.ModuleType, context);

            return this.moduleBuilder.BuildModule(module, context);
        }

        private ResolveResult GetNotFoundResult(NancyContext context)
        {
            return new ResolveResult
            {
                Route = new NotFoundRoute(context.Request.Method, context.Request.Path),
                Parameters = DynamicDictionary.Empty,
                Before = null,
                After = null,
                OnError = null
            };
        }

        private static string GetMethod(NancyContext context)
        {
            var requestedMethod = context.Request.Method;
            
            return requestedMethod.Equals("HEAD", StringComparison.Ordinal) ? "GET" : requestedMethod;
        }
	}
}