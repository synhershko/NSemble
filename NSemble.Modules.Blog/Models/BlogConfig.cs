using System.Collections.Generic;
using NSemble.Core.Models;

namespace NSemble.Modules.Blog.Models
{
    public class BlogConfig
    {
        public string WordPressBlogId { get; set; }
        public string WordPressAPIKey { get; set; }
        
        public string AkismetAPIKey { get; set; }
        public string AkismetDomain { get; set; }

        public List<Widget> Widgets { get; set; }
    }
}
