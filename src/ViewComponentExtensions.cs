using Microsoft.AspNetCore.Mvc;

namespace Hydro;

internal static class ViewComponentExtensions
{
    public static IViewComponentResult DefaultView<TModel>(this ViewComponent component, TModel model) =>
        component.View(component.GetDefaultViewPath(), model);

    public static string GetDefaultViewPath(this ViewComponent component)
    {
        var type = component.GetType();
        var assemblyName = type.Assembly.GetName().Name;

        return $"{type.FullName.Replace(assemblyName, "~").Replace(".", "/")}.cshtml";
    }
}
