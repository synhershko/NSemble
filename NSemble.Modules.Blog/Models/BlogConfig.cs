using System;
using System.Collections.Generic;
using NSemble.Core.Models;

namespace NSemble.Modules.Blog.Models
{
    public class BlogConfig
    {
        public BlogConfig()
        {
            KeepCommentsOpenFor = TimeSpan.FromDays(30*6); // approximately six months
        }

        public string BlogTitle { get; set; }
        public string TagLine { get; set; }
        public string BlogDescription { get; set; }
        public string CopyrightString { get; set; }
        public string OwnerEmail { get; set; }

        public TimeSpan KeepCommentsOpenFor { get; set; }
        
        public string WordPressBlogId { get; set; }
        public string WordPressAPIKey { get; set; }
        
        public string AkismetAPIKey { get; set; }
        public string AkismetDomain { get; set; }

        public List<Widget> Widgets { get; set; }
    }
}
