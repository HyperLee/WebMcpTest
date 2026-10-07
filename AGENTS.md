# Repository Guidelines

## Project Structure & Module Organization

- `WebMcpTest/` is the ASP.NET Core Razor Pages application targeting .NET 10. `Pages/` contains `.cshtml` pages and PageModels; `Pages/Shared/` contains shared layouts and partials; `wwwroot/` contains CSS, JavaScript, and vendor assets. `Program.cs` configures startup, while `appsettings*.json` contains runtime configuration.
- `spec/` stores product and acceptance requirements. `docs/` stores templates and implementation plans.
- No dedicated automated test project is currently checked in.

## Build, Test, and Development Commands

- `dotnet restore WebMcpTest\WebMcpTest.csproj` restores packages.
- `dotnet build WebMcpTest\WebMcpTest.csproj` compiles C# and Razor.
- `dotnet run --project WebMcpTest\WebMcpTest.csproj --launch-profile https` runs locally; launch settings expose HTTPS on 7020 and HTTP on 5133.
- No test project exists; validate changes with a successful build and browser smoke tests of affected routes. Add a test project before relying on `dotnet test`.

## Coding Style & Naming Conventions

Follow `.editorconfig`: use spaces, four-space indentation for C#, and two-space indentation for JSON, XML, and project files. Keep nullable reference types enabled, use file-scoped namespaces, and use PascalCase for types, methods, and properties plus camelCase for locals and parameters. Keep page behavior in its PageModel or feature script; use descriptive kebab-case CSS classes.

## Code Documentation & Reasoning

- Add an XML `<summary>` to each major C# function or method, especially public APIs, PageModel handlers, and reusable logic. State its purpose and important input/output behavior; update it when behavior changes.
- Add comments beside non-obvious algorithms, validation, branching, and WebMCP decisions to explain the reasoning or constraints. Explain why or how the logic works; do not narrate self-explanatory lines one by one.

## Testing Guidelines

For UI work, run the application, exercise the changed route and navigation, and check the browser console for JavaScript errors. Record commands and manual checks in the pull request. If automated tests are added, use a clearly named test project and descriptive names such as `NewsPage_WhenModelIsValid_RendersArticles`.

## Commit & Pull Request Guidelines

History is sparse (`first commit`, `gitignore版本控制`) and does not establish a formal convention. Use concise, imperative subjects such as `feat: add news page`, and keep commits focused. Pull requests should state intent, affected routes/files, related spec or issue, validation results, and screenshots for visual changes.

## Security & Configuration Tips

Do not commit passwords, tokens, or environment-specific secrets to `appsettings*.json`. Use local user-secrets or environment variables, and review configuration and generated/static-asset changes before submitting a pull request.
