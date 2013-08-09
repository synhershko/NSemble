using System.Linq;
using NSemble.Core.Models;
using NSemble.Modules.Blog.Models;
using Raven.Client.Linq;

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

        public override dynamic GetViewContent(Raven.Client.IDocumentSession session)
        {
            return session.Query<BlogPost>().Where(x => x.CurrentState == BlogPost.State.Public).OrderByDescending(x => x.PublishedAt).Take(10).ToArray();
        }
    }
}
