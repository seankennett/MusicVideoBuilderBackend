namespace SpaWebApi.Extensions
{
    public static class ListExtensions
    {
        public static T Pop<T>(this List<T> list)
        {
            T r = list[0];
            list.RemoveAt(0);
            return r;
        }
    }
}
