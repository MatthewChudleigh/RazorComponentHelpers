# RazorComponentHelpers

RazorComponentHelpers is a lightweight helper library that makes it easier to render Razor components outside of a running Blazor UI and to parse loosely formatted JSON payloads.

It is intended to be published as a NuGet package so it can be reused across tools that need to host components for templating, emails, or background processing scenarios.

## Features

- **Component rendering utilities** – programmatically render Razor components or inline `RenderFragment` instances into HTML strings using the standard Blazor rendering pipeline.
- **Flexible overloads** – call `RenderComponentAsync<T>(Dictionary<string, object?>? parameters = null)` for quick rendering or pass a `CancellationToken` via the overload that accepts one so dependent components can observe cancellation.
- **HTML output from fragments** – wrap anonymous render fragments without having to wire up a full component type.
- **JSON converters** – resilient `System.Text.Json` converters that accept string or numeric representations for booleans and nullable integers, simplifying integrations with inconsistent payloads.

## Getting Started

> The package has not yet been published. Once a NuGet feed is available, replace the placeholder version below with the released number.

1. Install the package:

   ```bash
   dotnet add package RazorComponentHelpers --version <version>
   ```

2. Register the helpers in your service container (if you rely on dependency injection):

   ```csharp
   builder.Services.AddScoped<ComponentRenderer>();
   ```

3. Inject and use the renderer inside a service or background job:

   ```csharp
   public class EmailTemplateService(ComponentRenderer renderer)
   {
       public Task<string> RenderSummaryAsync() =>
           renderer.RenderComponentAsync<SummaryComponent>(new Dictionary<string, object?>
           {
               ["Message"] = "Hello World"
           });

       public Task<string> RenderTemplatedAsync(CancellationToken cancel) =>
           renderer.RenderComponentAsync<SummaryComponent>(cancel, new Dictionary<string, object?>
           {
               ["Message"] = "Cancel-aware rendering"
           });
   }
   ```

4. Render inline fragments when you do not have a dedicated component:

   ```csharp
   var html = await renderer.RenderFragmentAsync(builder =>
   {
       builder.OpenElement(0, "p");
       builder.AddContent(1, "Hello from a fragment!");
       builder.CloseElement();
   });
   ```

5. Configure the JSON converters when setting up `JsonSerializerOptions`:

   ```csharp
   var options = new JsonSerializerOptions
   {
       Converters =
       {
           new StringBooleanConverter(),
           new StringNullIntConverter()
       }
   };
   ```

## Usage Notes

- `ComponentRenderer` relies on DI services made available in the host application. Ensure any components you render have their dependencies registered.
- The renderer adds a `Cancel` parameter automatically when you call the overload that accepts a `CancellationToken`, so components can opt into cancellation semantics.
- Rendering occurs on the internal Blazor dispatcher, mirroring how components run in a normal request pipeline.
- The JSON converters throw a `JsonException` if the payload cannot be interpreted as the expected type; wrap calls in try/catch if you need custom error handling.

## Continuous Delivery

A GitHub Actions workflow (`.github/workflows/publish.yml`) builds, packs, and publishes the library to NuGet when you push a Git tag that starts with `v` (for example `v1.0.0`) or trigger the workflow manually. Store your NuGet API key in the repository secrets as `NUGET_API_KEY` before running the workflow.

## Development

This repository uses central package management (`Directory.Packages.props`) so NuGet package versions are defined once for the entire solution. Add new package versions there and omit the `Version` attribute from individual project files.

Run the unit test suite with:

```bash
dotnet test
```

## Roadmap

- Publish the library to NuGet with complete metadata.
- Provide samples demonstrating background rendering and email generation scenarios.
- Add more converters or rendering helpers based on community input.

## Contributing

Issues and pull requests are welcome! Please open an issue to discuss significant proposals so we can align on scope before you invest time in an implementation.

## License

RazorComponentHelpers is distributed under the MIT License. See the [LICENSE](LICENSE) file for details.