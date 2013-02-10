using NSemble.Core.Models;

namespace NSemble.Core.Widgets
{
    public class StaticContentWidget : Widget
    {
        public StaticContentWidget(string name, string region) : base(name, region)
        {
        }

        public override string ViewName
        {
            get { return "Foo.cshtml"; }
        }
    }
}
