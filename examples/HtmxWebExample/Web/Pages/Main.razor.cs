using HtmxWebExample.Web.Layouts;
using RazorComponentHelpers;

namespace HtmxWebExample.Web.Pages;

public partial class Main
{
    private const string RouteMain = "/";
    private const string RouteMainExample = "/main/example";

    public static void AddEndpoints(WebApplication app)
    {
        app.MapGet(RouteMain, async (ComponentRenderer render) => await render.RenderPageAsync<Main, MainLayout>());
        app.MapGet(RouteMainExample, async (ComponentRenderer render) => await render.RenderFragmentAsync(Example("Hello World!")));
    }
}