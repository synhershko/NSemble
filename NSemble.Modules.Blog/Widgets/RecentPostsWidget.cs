using NSemble.Core.Models;

namespace NSemble.Modules.Blog.Widgets
{
    public class RecentPostsWidget : Widget
    {
        public RecentPostsWidget(string name, string region) : base(name, region)
        {
        }

        public override string ViewName
        {
            get { return "ListBlogPostsWidget.cshtml"; }
        }
    }
}
