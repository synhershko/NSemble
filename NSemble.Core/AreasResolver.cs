using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Raven.Client;

namespace NSemble.Core
{
    public sealed class AreaConfigs
    {
        public string AreaName { get; set; }
        public string ModuleName { get; set; }
        public string DocumentsPrefix { get; set; }
        public string TenantName { get; set; }
    }

    public sealed class AreasResolver
    {
	    private const string AreasDocumentName = "NSemble/Areas";
        private readonly ConcurrentDictionary<string, AreaConfigs> AreasByRoute = new ConcurrentDictionary<string, AreaConfigs>();
        private readonly ConcurrentDictionary<string, AreaConfigs> AreasByName = new ConcurrentDictionary<string, AreaConfigs>();

        private AreasResolver(){}
        public static readonly AreasResolver Instance = new AreasResolver();

		public bool HasAreas { get { return AreasByRoute.Count > 0; } }

        public void RegisterArea(string prefix, AreaConfigs configs)
        {
            if (prefix != null)
            {
                if (!prefix.StartsWith("/"))
                    throw new ArgumentException("URL prefix for area has to start with a /", "prefix");

                if (prefix.Length > 1)
                    prefix = prefix.TrimEnd(new[] {' ', '/'});

                AreasByRoute.AddOrUpdate(prefix, s => configs, (s, s1) => configs);
            }

			AreasByName.AddOrUpdate(configs.AreaName.ToLower(CultureInfo.InvariantCulture), s => configs, (s, s1) => configs);
        }

        public string AdminAreaPrefix { get; set; }

        public string ParseArea(string url, out AreaConfigs configs)
        {
			configs = GetAreaConfigsByPrefix(url);
			if (configs != null) return "/";

            var pos = url.LastIndexOf('/');
	        if (pos > 0)
	        {
		        while (pos > 0 && (configs = GetAreaConfigsByPrefix(url.Substring(0, pos))) == null)
		        {
			        pos = url.LastIndexOf('/', pos - 1);
		        }
	        }
	        if (configs != null) return url.Substring(pos);

            configs = GetAreaConfigsByPrefix("/");
            return url;
        }

        private AreaConfigs GetAreaConfigsByPrefix(string prefix)
        {
            AreaConfigs val;
            return (AreasByRoute.TryGetValue(prefix, out val)) ? val : null;
        }

        public string GetPrefixByAreaConfigs(AreaConfigs areaConfigs)
        {
            return (from ac in AreasByRoute where ac.Value == areaConfigs select ac.Key).FirstOrDefault();
        }

		public AreaConfigs GetAreaConfigsByName(string areaName)
		{
            if (string.IsNullOrWhiteSpace(areaName))
                throw new ArgumentException("Area name cannot be empty", "areaName");

			AreaConfigs val;
			return (AreasByName.TryGetValue(areaName.ToLowerInvariant(), out val)) ? val : null;
		}

		public void PersistToStore(IDocumentSession session)
		{
			session.Store(AreasByRoute, AreasDocumentName);
			session.SaveChanges();
		}

		public bool LoadFromStore(IDocumentSession session)
		{
			var d = session.Load<IDictionary<string, AreaConfigs>>(AreasDocumentName);
			if (d == null) return false;

			AreasByRoute.Clear();
			foreach (var areaConfig in d)
			{
				RegisterArea(areaConfig.Key, areaConfig.Value);
			}
			return true;
		}
    }
}