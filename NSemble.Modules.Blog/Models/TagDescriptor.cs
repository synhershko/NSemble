using NSemble.Core.Models;

namespace NSemble.Modules.Blog.Models
{
    public class TagDescriptor : IDynamicContent
    {
        public string Content { get; set; }
        public string CachedRenderedContent { get; private set; }
        public DynamicContentType ContentType { get; set; }

        public SortOrder DefaultSortingOrder { get; set; }
        public int? PostsPerPage { get; set; }
    }
}
