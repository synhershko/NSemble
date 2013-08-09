using NSemble.Core.Models;
using Raven.Client;

namespace NSemble.Core.Widgets
{
    public class StaticContentWidget : Widget, IDynamicContent
    {
        public StaticContentWidget(string name, string region) : base(name, region)
        {
            ContentType = DynamicContentType.Markdown;
        }

        public override string ViewName
        {
            get { return "Widgets/StaticContentWidget.cshtml"; }
        }

        public DynamicContentType ContentType { get; set; }
        public string Content { get; set; }

        public override dynamic GetViewContent(IDocumentSession session)
        {
            return this;
        }
    }
}
