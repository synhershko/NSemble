using System;
using System.ComponentModel.DataAnnotations;
using NSemble.Core.Models;
using Raven.Imports.Newtonsoft.Json;

namespace NSemble.Modules.ContentPages.Models
{
    /// <summary>
    /// General purpose content page to be used throughout the site
    /// </summary>
    public class ContentPage : IDynamicContent, ISearchable
    {
        public ContentPage()
        {
            LastChanged = DateTimeOffset.Now;
        }

        /// <summary>
        /// Content page Id, essentially a slug
        /// </summary>
        public string Id { get; set; }

        [Required]
        public string Title { get; set; }

        public DateTimeOffset LastChanged { get; set; }

        [Required]
        //[AllowHtml]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; }

        [JsonIgnore]
        public string Slug
        {
            get
            {
	            if (Id == null)
		            return null;
	            return Id.Substring(Id.IndexOf('/') + 1);
            }
            set { }
        }

        public DynamicContentType ContentType { get; set; }

        public static string FullContentPageId(string slug)
        {
            return "contentpages/" + slug;
        }
    }
}