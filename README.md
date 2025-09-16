# RazorComponentHelpers

RazorComponentHelpers is a lightweight helper library that makes it easier to render Razor components outside of a running Blazor UI and to parse loosely formatted JSON payloads.

It is intended to be published as a NuGet package so it can be reused across tools that need to host components for templating, emails, or background processing scenarios.

## Features

- **Component rendering utilities** – programmatically render Razor components or inline `RenderFragment` instances into HTML strings using the standard Blazor rendering pipeline.
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
       public async Task<string> RenderAsync<TComponent>(CancellationToken cancel, Dictionary<string, object?> parameters)
           where TComponent : IComponent
       {
           return await renderer.RenderComponentAsync<TComponent>(cancel, parameters);
       }
   }
   ```

4. Configure the JSON converters when setting up `JsonSerializerOptions`:

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
- The renderer adds a `Cancel` parameter automatically so you can pass a `CancellationToken` through to components that support it.
- Rendering occurs on the internal Blazor dispatcher, mirroring how components run in a normal request pipeline.
- The JSON converters throw a `JsonException` if the payload cannot be interpreted as the expected type; wrap calls in try/catch if you need custom error handling.

## Roadmap

- Publish the library to NuGet with complete metadata.
- Provide samples demonstrating background rendering and email generation scenarios.
- Add more converters or rendering helpers based on community input.

## Contributing

Issues and pull requests are welcome! Please open an issue to discuss significant proposals so we can align on scope before you invest time in an implementation.

## License

RazorComponentHelpers is distributed under the MIT License. See the [LICENSE](LICENSE) file for details.
