using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace RazorComponentHelpers;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

public class Renderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
{
    public ComponentBuilder Fragment(RenderFragment fragment)
    {
        return new ComponentBuilder(serviceProvider, loggerFactory)
        {
            Type = typeof(RenderFragmentWrapper),
            Parameters = new Dictionary<string, object?>()
            {
                {"Fragment", fragment}
            }
        };
    }

    public ComponentBuilder Component<TComponent>(Dictionary<string, object?>? parameters = null) where TComponent : IComponent
    {
        return new ComponentBuilder(serviceProvider, loggerFactory)
        {
            Type = typeof(TComponent),
            Parameters = parameters ?? []
        };
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

public class BaseComponentBuilder(IServiceProvider serviceProvider, ILoggerFactory loggerFactory) 
{
    protected IServiceProvider ServiceProvider => serviceProvider;
    protected ILoggerFactory LoggerFactory => loggerFactory;
    
    public required Type Type { get; init; }
    public required Dictionary<string, object?> Parameters { get; init; }
    
    public async Task<string> ToHtmlAsync()
    {
        await using var htmlRenderer = new HtmlRenderer(serviceProvider, loggerFactory);
        return await InnerRenderToHtmlAsync(htmlRenderer);
    }

    public async Task<IResult> ToResultAsync()
    {
        var html = await ToHtmlAsync();
        return Results.Text(html, "text/html");
    }
    
    private async Task<string> InnerRenderToHtmlAsync(HtmlRenderer htmlRenderer)
    {
        return await InnerRenderToHtmlWithParametersAsync(htmlRenderer, Type, Parameters);
    }

    private static async Task<string> InnerRenderToHtmlWithParametersAsync(HtmlRenderer htmlRenderer, Type type, Dictionary<string, object?> parameters)
    {
        var componentParameters = ParameterView.FromDictionary(parameters);
        var output = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await htmlRenderer.RenderComponentAsync(type, componentParameters);
            return result.ToHtmlString();
        });
        return output;
    }
}

public class ComponentBuilder(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    : BaseComponentBuilder(serviceProvider, loggerFactory)
{
    public ComponentBuilder WithCancel(CancellationToken cancel, string cancelParameterName = "Cancel")
    {
        var parameters = Parameters.ToDictionary(
            parameter => parameter.Key, 
            parameter => parameter.Value);
        parameters[cancelParameterName] = cancel;
        
        return new ComponentBuilder(ServiceProvider, LoggerFactory)
        {
            Type = Type,
            Parameters = parameters,
        };
    }
    
    public ComponentWithLayoutBuilder WithLayout<TLayout>()
    where TLayout : IComponent
    {
        var type = typeof(LayoutView);
        var parameters = new Dictionary<string, object?>
        {
            ["Layout"] = typeof(TLayout),
            ["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent(0, Type);
                foreach (var kv in Parameters)
                {
                    builder.AddAttribute(1, kv.Key, kv.Value);
                }
                builder.CloseComponent();
            })
        };
        
        return new ComponentWithLayoutBuilder(ServiceProvider, LoggerFactory) { Type = type, Parameters = parameters };
    }
}

public class ComponentWithLayoutBuilder(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    : BaseComponentBuilder(serviceProvider, loggerFactory);


