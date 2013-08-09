using NSemble.Core.Models;
using Raven.Client;

namespace NSemble.Core.Widgets
{
    public class StaticContentWidget : Widget
    {
        public StaticContentWidget(string name, string region) : base(name, region)
        {
        }

        public override string ViewName
        {
            get { return "Widgets/StaticContentWidget.cshtml"; }
        }

        public string Content { get; set; }

        public override dynamic GetViewContent(IDocumentSession session)
        {
            return Content;
        }
    }
}
