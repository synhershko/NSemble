using System;
using System.Linq;
using NSemble.Modules.Blog.Models;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace NSemble.Modules.Blog.Indexes
{
    public class TagCloudIndex : AbstractIndexCreationTask<BlogPost, TagCloudIndex.ReduceResult>
    {
        public class ReduceResult
        {
            public string Tag { get; set; }
            public int Count { get; set; }
            public DateTime LastPost { get; set; }
        }

        public TagCloudIndex()
        {
            Map = docs => from doc in docs
                from tag in doc.Tags
                select new {Tag = tag, TagAnalyzed = tag, Count = 1, LastPost = doc.PublishedAt};

            Reduce = results => from r in results
                group r by r.Tag.ToLowerInvariant()
                into g
                select
                    new
                    {
                        Tag = g.Key,
                        TagAnalyzed = g.Key,
                        Count = g.Sum(x => x.Count),
                        LastPost = g.Max(x => x.LastPost)
                    };

            Index("Tag", FieldIndexing.NotAnalyzed);
            Index("TagAnalyzed", FieldIndexing.Analyzed);
        }
    }
}
