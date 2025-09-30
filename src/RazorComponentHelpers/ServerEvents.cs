using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace RazorComponentHelpers;

public static class ServerEvents
{
    public record Event(string Name, string Data);

    public static Event Html(string name, string html) => new(name, html.ReplaceLineEndings(""));
    public static Event Json<T>(string name, T data) => new(name, JsonSerializer.Serialize(data));
    
    public static async Task Stream(this HttpContext context, IAsyncEnumerable<Event> events, CancellationToken cancel)
    {
        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        await foreach (var e in events.WithCancellation(cancel))
        {
            await context.Response.WriteAsync($"event: {e.Name}\n", cancellationToken: cancel);
            await context.Response.WriteAsync($"data: {e.Data}\n\n", cancellationToken: cancel);
            await context.Response.Body.FlushAsync(cancel);
        }
    }
}