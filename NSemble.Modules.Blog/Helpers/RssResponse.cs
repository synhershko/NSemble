using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel.Syndication;
using System.Xml;
using NSemble.Core.Extensions;
using NSemble.Modules.Blog.Models;
using Nancy;

namespace NSemble.Modules.Blog.Helpers
{
    public class RssResponse : Response
    {
        private readonly BlogConfig blogConfig;
        private Uri BlogUrl { get; set; }

        public RssResponse(IEnumerable<BlogPost> model, Uri BlogUrl, BlogConfig blogConfig)
        {
            this.blogConfig = blogConfig;
            this.BlogUrl = BlogUrl;

            this.Contents = GetXmlContents(model);
            this.ContentType = "application/rss+xml";
            this.StatusCode = HttpStatusCode.OK;
        }

        private Action<Stream> GetXmlContents(IEnumerable<BlogPost> model)
        {
            var items = new List<SyndicationItem>();
            var areaRoutePrefix = BlogUrl.ToString();

            foreach (var post in model)
            {
                var postUrl = post.ToUrl(areaRoutePrefix);
                var contentString = post.CompiledContent(true).ToHtmlString();

                var item = new SyndicationItem(
                    title: post.Title,
                    content: contentString,
                    itemAlternateLink: new Uri(BlogUrl, postUrl)
                    )
                               {
                                   PublishDate = post.PublishedAt.UtcDateTime,
                                   Summary = new TextSyndicationContent(contentString, TextSyndicationContentKind.XHtml),
                                   LastUpdatedTime = post.LastEditedAt == null ? post.PublishedAt.UtcDateTime : post.LastEditedAt.Value,
                                   // TODO authors
                               };
                items.Add(item);
            }

            var feed = new SyndicationFeed(
                blogConfig.BlogTitle,
                blogConfig.BlogDescription,
                BlogUrl,
                items)
                           {
                               Copyright = new TextSyndicationContent(blogConfig.CopyrightString, TextSyndicationContentKind.Plaintext),
                               Generator = "NSemble blog module",
                           };

            var formatter = new Rss20FeedFormatter(feed);
            return stream =>
            {
                using (XmlWriter writer = XmlWriter.Create(stream))
                {
                    formatter.WriteTo(writer);
                }
            };
        }
    }
}
