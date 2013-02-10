namespace NSemble.Core.Models
{
    public abstract class Widget
    {
        protected Widget(string name, string region)
        {
            Name = name;
            RegionName = region;
        }

        public string Name { get; set; }
        public string RegionName { get; set; }
        public abstract string ViewName { get; }
        public dynamic Content { get; set; }
    }
}
