namespace RazorComponentHelpers;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

public class ComponentRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
{
    public async Task<string> RenderComponentAsync<T>(CancellationToken cancel, Dictionary<string, object?>? parameters = null) where T : IComponent
    {
        await using var htmlRenderer = new HtmlRenderer(serviceProvider, loggerFactory);
        parameters ??= [];
        parameters.Add("Cancel", cancel);

        var componentParameters = ParameterView.FromDictionary(parameters);
        var output = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await htmlRenderer.RenderComponentAsync<T>(componentParameters);
            return result.ToHtmlString();
        });
        return output;
    }

    public async Task<string> RenderFragmentAsync(RenderFragment fragment)
    {
        await using var htmlRenderer = new HtmlRenderer(serviceProvider, loggerFactory);

        var parameters = new Dictionary<string, object?>
        {
            ["Fragment"] = fragment
        };

        var componentParameters = ParameterView.FromDictionary(parameters);
        var output = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await htmlRenderer.RenderComponentAsync<RenderFragmentWrapper>(componentParameters);
            return result.ToHtmlString();
        });
        return output;
    }

    private class RenderFragmentWrapper : ComponentBase
    {
        [Parameter] public RenderFragment? Fragment { get; set; }
        [Parameter] public CancellationToken Cancel { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Fragment?.Invoke(builder);
        }
    }

}
