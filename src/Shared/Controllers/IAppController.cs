using OnlineShopModular.Shared.Controllers;

namespace OnlineShopModular.Shared.Controllers
{
    public interface IAppController
    {
        void AddQueryString(string key, object? value) { }
        void AddQueryStrings(Dictionary<string, object?> queryString) { }
    }
}

namespace OnlineShopModular.Shared
{
    public static class IAppControllerExtensions
    {
        public static TAppController WithQuery<TAppController>(this TAppController controller, string? existingQueryString)
            where TAppController : IAppController
        {
            return controller.WithQuery(queryString: AppQueryStringCollection.Parse(existingQueryString));
        }

        public static TAppController WithQuery<TAppController>(this TAppController controller, string key, object? value)
            where TAppController : IAppController
        {
            controller.AddQueryString(key, value);
            return controller;
        }

        public static TAppController WithQuery<TAppController>(this TAppController controller, Dictionary<string, object?> queryString)
            where TAppController : IAppController
        {
            controller.AddQueryStrings(queryString);
            return controller;
        }

        public static TAppController WithQueryIf<TAppController>(this TAppController controller, bool condition, string key, object? value)
            where TAppController : IAppController
        {
            if (condition)
            {
                controller.WithQuery(key, value);
            }
            return controller;
        }
    }
}
