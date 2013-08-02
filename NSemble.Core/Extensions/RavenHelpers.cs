namespace NSemble.Core.Extensions
{
    public static class RavenHelpers
    {
        public static int ToIntId(this string id)
        {
            return int.Parse(id.Substring(id.LastIndexOf('/') + 1));
        }
    }
}
