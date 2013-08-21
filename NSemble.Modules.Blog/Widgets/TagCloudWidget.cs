using NSemble.Core.Models;
using Raven.Client;

namespace NSemble.Modules.Blog.Widgets
{
    public class TagCloudWidget : Widget
    {
        public TagCloudWidget(string name, string region) : base(name, region)
        {
        }

        public override string ViewName
        {
            get { return "TagCloudWidget.cshtml"; }
        }

        public override dynamic GetViewContent(IDocumentSession session)
        {
            return "";
        }
    }
}
