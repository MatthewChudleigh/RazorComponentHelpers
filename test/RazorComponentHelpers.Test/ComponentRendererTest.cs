using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RazorComponentHelpers;

namespace RazorComponentHelpers.Test;

public class RendererTest
{
    [Fact]
    public async Task RenderComponentAsync_WithoutCancellationToken_RendersParameters()
    {
        var (renderer, provider) = CreateRenderer();
        await using (provider)
        {
            var html = await renderer.Component<TestComponent>(
                parameters: new Dictionary<string, object?> { ["Message"] = "World" })
                .ToHtmlAsync();

            Assert.Contains("Hello World", html);
            Assert.Contains("Cancel: False", html);
        }
    }

    [Fact]
    public async Task RenderComponentAsync_WithCancellationToken_AddsCancelParameter()
    {
        var (renderer, provider) = CreateRenderer();
        await using (provider)
        {
            using var tokenSource = new CancellationTokenSource();

            var html = await renderer.Component<TestComponent>(
                new Dictionary<string, object?> { ["Message"] = "World" })
                .WithCancel(tokenSource.Token)
                .ToHtmlAsync();

            Assert.Contains("Hello World", html);
            Assert.Contains("Cancel: True", html);
        }
    }

    [Fact]
    public async Task RenderFragmentAsync_RendersFragment()
    {
        var (renderer, provider) = CreateRenderer();
        await using (provider)
        {
            var html = await renderer.Fragment(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddContent(1, "Fragment");
                builder.CloseElement();
            }).ToHtmlAsync();

            Assert.Contains("<span>Fragment</span>", html, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static (Renderer Renderer, ServiceProvider Provider) CreateRenderer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        var provider = services.BuildServiceProvider();
        var renderer = new Renderer(provider, NullLoggerFactory.Instance);
        return (renderer, provider);
    }

    private sealed class TestComponent : ComponentBase
    {
        [Parameter] public string? Message { get; set; }
        [Parameter] public CancellationToken Cancel { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, $"Hello {Message}");
            builder.CloseElement();

            builder.OpenElement(2, "span");
            builder.AddContent(3, $"Cancel: {Cancel.CanBeCanceled}");
            builder.CloseElement();
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }
}