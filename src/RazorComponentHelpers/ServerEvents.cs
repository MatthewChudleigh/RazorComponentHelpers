using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace RazorComponentHelpers;

public record SseEvent(string Name, Func<IEnumerable<string>> Data);

public static class ServerEvents
{
    public static SseEvent Data(string name, Func<IEnumerable<string>> data) => new(name, data);
    public static SseEvent Html(string name, string html) => new(name, () => [html]);
    public static SseEvent Html(string name, IEnumerable<string> html) => new(name, () => html.Select(h => h));
    public static SseEvent Json<T>(string name, T data) => new(name, () => [JsonSerializer.Serialize(data)]);
    public static SseEvent Json<T>(string name, IEnumerable<T> data) => new(name, () => data.Select(d => JsonSerializer.Serialize(d)));
    
    public static async Task Stream(this HttpContext context, IAsyncEnumerable<SseEvent> events, CancellationToken cancel)
    {
        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        await foreach (var e in events.WithCancellation(cancel))
        {
            await context.Response.WriteAsync($"event: {e.Name}\n", cancellationToken: cancel);
            foreach (var d in e.Data())
            {
                await context.Response.WriteAsync($"data: {d.ReplaceLineEndings("")}\n", cancellationToken: cancel);
            }

            await context.Response.WriteAsync($"\n", cancellationToken: cancel);
            await context.Response.Body.FlushAsync(cancel);
        }
    }
}