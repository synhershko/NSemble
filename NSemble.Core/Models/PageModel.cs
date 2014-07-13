using System.Collections.Generic;

namespace NSemble.Core.Models
{
    public class PageModel
    {
        public string PrefixTitle { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public bool IsAuthenticated { get; set; }
        public string CurrentUser { get; set; }
        public List<ErrorModel> Errors { get; set; }
    }
}
