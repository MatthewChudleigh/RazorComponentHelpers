using HtmxWebExample.Web.Layouts;
using RazorComponentHelpers;

namespace HtmxWebExample.Web.Pages;

public partial class Main
{
    private const string RouteMain = "/";
    private const string RouteMainExample = "/main/example";

    public static void AddEndpoints(WebApplication app)
    {
        app.MapGet(RouteMain, async (Renderer render) => await render.Component<Main>().WithLayout<MainLayout>().ToResultAsync());
        app.MapGet(RouteMainExample, async (Renderer render) => await render.Fragment(Example("Hello World!")).ToResultAsync());
    }
}