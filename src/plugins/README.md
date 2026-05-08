# src/plugins

.NET plugin assemblies for Dataverse server-side business logic. One subfolder per solution area, each containing a Visual Studio solution (`.sln`) with one or more `.csproj` projects.

## What Lives Here

```
src/plugins/
  acm_AcmePlatform/
    AcmeCorp.AcmePlatform.Plugins.sln
    AcmeCorp.AcmePlatform.Plugins.Core/
      AcmeCorp.AcmePlatform.Plugins.Core.csproj   # SDK-style, targets net462
      PluginBase.cs                                # Base class — shared context helpers
    AcmeCorp.AcmePlatform.Plugins.SomeFeature/
      AcmeCorp.AcmePlatform.Plugins.SomeFeature.csproj
      SomePlugin.cs
```

## What Plugins Are

Plugins are C# classes that Dataverse calls synchronously or asynchronously in response to platform events (Create, Update, Delete, custom API calls, etc.). They run inside the Dataverse sandbox — no network access, no file system, no external dependencies beyond the SDK.

Common uses:
- Enforce business rules that can't be expressed with Power Automate
- Enrich records on create/update (calculate fields, auto-populate lookups)
- Validate data before it's committed
- Implement custom APIs (server-side logic for canvas apps, PCF controls, or external callers)
- Post-operation processing (fire-and-forget async work)

## Project Conventions

| Convention | Detail |
|------------|--------|
| Target framework | `net462` (Dataverse sandbox requirement) |
| Base class | `{Publisher}.Plugins.{SolutionArea}.Core.PluginBase` |
| NuGet reference | `Microsoft.PowerApps.MSBuild.Plugin` |
| Assembly signing | Required — strong-named key per project |
| No IoC / DI | Plugins are instantiated by Dataverse, not a container |

## Inner Loop: Developing Plugins

1. **Scaffold** — `scaffold-plugin` skill creates the `.csproj`, wires it into the `.sln` and `.cdsproj`, and sets up the base class structure.
2. **Build** — `dotnet build` (inner loop) compiles the assembly locally. `Build-Plugins.ps1` is outer-loop CI only.
3. **Register** — `register-plugin` skill runs `Register-Plugin.ps1` to push the binary to dev and register/update message processing steps. This replaces the Plugin Registration Tool (PRT).
4. **Iterate** — change code → `dotnet build` → `Register-Plugin.ps1` → test in dev environment. No sync required for pure code changes.
5. **Add to feature solution** — after registering, go to make.powerapps.com and manually add the plugin assembly and steps to your feature solution. **Plugins are not auto-tracked.**
6. **Sync** — run `sync-solution` to capture the plugin registration in solution XML.

## Registration Steps

Each plugin class needs one or more **steps** registered in Dataverse. Steps define:
- **Message** — which event triggers this plugin (e.g. `Create`, `Update`, `acm_MyCustomApi`)
- **Entity** — which table (leave blank for global messages)
- **Stage** — Pre-Validation, Pre-Operation (sync), or Post-Operation (sync or async)
- **Filtering attributes** — for Update steps, which columns trigger the plugin

The `Register-Plugin.ps1` script reads step definitions and keeps registrations in sync with source control.

## Testing

- Unit test with a mocked `IOrganizationService` and `ITracingService` — plugins are plain C# classes
- Integration test by triggering the event in the dev environment and inspecting results
- The `validate-pull-request.yml` workflow builds changed plugin projects automatically on PR

## Agent Commands

| Task | Say to agent |
|------|--------------|
| Create a new plugin | `"scaffold a plugin for account create"` |
| Push plugin binary to dev | `"register the plugin"` |
| Build and validate | `"build the solution"` |
