using Microsoft.Extensions.Logging;

namespace RazorComponentHelpers;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

public class ComponentRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
{
    public async Task<string> RenderPageAsync<TComponent, TLayout>(Dictionary<string, object?>? parameters = null)
        where TComponent : IComponent
        where TLayout : IComponent
    {
        return await InnerRenderLayoutWithBodyAsync<TComponent, TLayout>(null, parameters);
    }

    public async Task<string> RenderPageAsync<TComponent, TLayout>(CancellationToken cancel, Dictionary<string, object?>? parameters = null) 
        where TComponent : IComponent 
        where TLayout : IComponent
    {
        return await InnerRenderLayoutWithBodyAsync<TComponent, TLayout>(cancel, parameters);
    }

    public async Task<string> RenderComponentWithLayoutAsync<TComponent, TLayout>(Dictionary<string, object?>? parameters = null)
        where TComponent : IComponent
        where TLayout : IComponent
    {
        return await InnerRenderLayoutWithChildContentAsync<TComponent, TLayout>(null, parameters);
    }
    
    public async Task<string> RenderComponentWithLayoutAsync<TComponent, TLayout>(CancellationToken cancel, Dictionary<string, object?>? parameters = null)
        where TComponent : IComponent
        where TLayout : IComponent
    {
        return await InnerRenderLayoutWithChildContentAsync<TComponent, TLayout>(cancel, parameters);
    }
    
    public Task<string> RenderComponentAsync<T>(Dictionary<string, object?>? parameters = null) where T : IComponent
    {
        return InnerRenderComponentAsync<T>(null, parameters);
    }

    public Task<string> RenderComponentAsync<T>(CancellationToken cancel, Dictionary<string, object?>? parameters = null) where T : IComponent
    {
        return InnerRenderComponentAsync<T>(cancel, parameters);
    }

    private async Task<string> InnerRenderComponentAsync<T>(CancellationToken? cancel, Dictionary<string, object?>? parameters = null) where T : IComponent
    {
        await using var htmlRenderer = new HtmlRenderer(serviceProvider, loggerFactory);

        parameters ??= [];
        if (cancel != null)
        {
            parameters.Add("Cancel", cancel);
        }

        var componentParameters = ParameterView.FromDictionary(parameters);
        var output = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await htmlRenderer.RenderComponentAsync<T>(componentParameters);
            return result.ToHtmlString();
        });
        return output;
    }
    
    private async Task<string> InnerRenderLayoutWithChildContentAsync<TChildComponent, TLayout>(
        CancellationToken? cancel,
        Dictionary<string, object?>? parameters = null)
        where TChildComponent : IComponent
        where TLayout : IComponent
    {
        parameters ??= [];
        return await InnerRenderComponentAsync<LayoutView>(cancel, new Dictionary<string, object?>
        {
            ["Layout"] = typeof(TLayout),
            ["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<TChildComponent>(0);
                foreach (var kv in parameters)
                    builder.AddAttribute(1, kv.Key, kv.Value);
                builder.CloseComponent();
            })
        });
    }
    
    private async Task<string> InnerRenderLayoutWithBodyAsync<T, TLayout>(
        CancellationToken? cancel,
        Dictionary<string, object?>? parameters = null)
        where T : IComponent
        where TLayout : IComponent
    {
        await using var htmlRenderer = new HtmlRenderer(serviceProvider, loggerFactory);

        var inner = await InnerRenderComponentAsync<T>(cancel, parameters);

        // Render layout with inner component as "Body"
        var layoutParams = new Dictionary<string, object?>
        {
            ["Body"] = (RenderFragment)(builder =>
            {
                builder.AddMarkupContent(0, inner);
            })
        };

        var layoutParameterView = ParameterView.FromDictionary(layoutParams);

        return await htmlRenderer.Dispatcher.InvokeAsync(
            async () => (await htmlRenderer.RenderComponentAsync<TLayout>(layoutParameterView)).ToHtmlString()
        );
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
