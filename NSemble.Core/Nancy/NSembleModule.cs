using System;
using System.Collections.Generic;
using System.Dynamic;
using NSemble.Core.Models;
using Nancy;
using Raven.Client;

namespace NSemble.Core.Nancy
{
    public abstract class NSembleModule : NancyModule
    {
        protected NSembleModule(string moduleName)
            : base(string.Concat(Constants.ResolverAreaPrefix, "/", moduleName))
        {
            if (string.IsNullOrWhiteSpace(moduleName))
                throw new ArgumentException("Module name cannot be empty", "moduleName");

            if (moduleName.IndexOf('/') > -1)
                throw new ArgumentException("Module name cannot contain slashes", "moduleName");

            SetupModelDefaults();
        }

        protected NSembleModule(bool forAdmin, string modulePath)
            : base(modulePath)
        {
            SetupModelDefaults();
        }

        public AreaConfigs AreaConfigs
        {
            get
            {
                object obj;
                AreaConfigs ret = null;
                if (Context.Items.TryGetValue("AreaConfigs", out obj))
                    ret = (AreaConfigs)obj;
                return ret;
            }
        }

        public string AreaRoutePrefix
        {
            get
            {
                return AreasResolver.Instance.GetPrefixByAreaConfigs(AreaConfigs);
            }
        }

        public string DocumentPrefix
        {
            get
            {
                var ac = AreaConfigs;
                if (ac != null) return ac.DocumentsPrefix ?? string.Empty;
                return string.Empty;
            }
        }

        public dynamic Model = new ExpandoObject();
        protected PageModel Page { get; set; }
        protected List<Widget> Widgets { get; set; }
        private void SetupModelDefaults()
        {
            Before += ctx =>
            {
                Page = new PageModel()
                {
                    IsAuthenticated = ctx.CurrentUser != null,
                    CurrentUser = ctx.CurrentUser != null ? ctx.CurrentUser.UserName : string.Empty,
                    PrefixTitle = "My website - ", // TODO: pull from configs
                    Errors = new List<ErrorModel>(),
                };

                Model.Page = Page;

                return null;
            };
        }

        protected virtual void LoadWidgets(IDocumentSession session)
        {
        }
    }
}