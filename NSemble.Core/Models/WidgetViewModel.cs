using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Raven.Client;

namespace NSemble.Core.Models
{
    public class WidgetViewModel
    {
        public WidgetViewModel(IDocumentSession session, Widget widget)
        {
            RegionName = widget.RegionName;
            ViewName = widget.ViewName;
            Content = widget.GetViewContent(session);
        }

        public string RegionName { get; set; }
        public string ViewName { get; set; }
        public dynamic Content { get; set; }
    }
}
