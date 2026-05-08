---
applyTo: "src/plugins/**"
---

# Plugin Development

## Project Layout

Plugins are organized by solution under `src/plugins/`:

| Folder | Solution file |
|--------|--------------|
| `{solutionPrefix}_{solutionName}/` | `{publisher}.{solutionName}.Plugins.sln` |

> Derive `solutionPrefix`, `solutionName`, and `publisher` from `solutionAreas[]` and `publisher` in `environment-config.json`.

Additional solution folders will appear here for multi-solution repos. Each folder typically contains a Core library project (`{publisher}.Plugins.{solutionName}.Core/`) that provides the shared `PluginBase` class, and one or more feature plugin projects.

## Shared Core Library

The Core project (`{publisher}.Plugins.{solutionName}.Core/`) provides base classes and utilities shared across all plugin projects in this solution area:

- `PluginBase.cs` — Base class for all plugins
- `EntityExtensions.cs` — Entity helper methods
- `EnvironmentVariableService.cs` — Read environment variables from Dataverse
- `HttpClientWrapper.cs` — HTTP client for external calls
- `Tests/` — Test utilities (`PluginTestFactory.cs`, `ReflectionHelper.cs`)

All plugin projects in the same solution area reference Core. When modifying Core, check for impacts across all plugin projects.

## Build

```powershell
# Build all plugins
.github/workflows/scripts/Build-Plugins.ps1 -artifactsPath ./out -skipTests

# Build specific solution's plugins
.github/workflows/scripts/Build-Plugins.ps1 -projectPaths "src/plugins/{solutionPrefix}_{solutionName}/{publisher}.{solutionName}.Plugins.sln" -skipTests

# Build with tests
.github/workflows/scripts/Build-Plugins.ps1 -artifactsPath ./out -testResultsPath ./test-results
```

Parameters: `-artifactsPath`, `-testResultsPath`, `-configuration` (Debug/Release), `-projectPaths`, `-skipTests`

## Test

- **{SolutionName}**: MSTest v2 — `{PluginProjectName}.Tests/`
- **ChartEngine & SpecEngine**: Have `Tests/` folders excluded from build via `<Compile Remove="Tests\**" />`
- **Core**: Test utilities in `Tests/` folder (not a separate test project)

```powershell
# Run tests via dotnet
cd src/plugins/{solutionPrefix}_{solutionName}
dotnet test {publisher}.{solutionName}.Plugins.sln
```

## Inner Loop (Local Development)

```
1. Edit C# code
2. dotnet build (or Build-Plugins.ps1)
3. Push + register via Register-Plugin.ps1
4. Test in Dataverse (trigger the plugin's message)
5. Iterate
```

Plugin registrations are auto-tracked in the preferred solution.

### Register-Plugin.ps1

Replaces the Plugin Registration Tool (PRT) for pushing binaries and registering steps:

```powershell
# Push plugin package + register all steps from solution XML
.github/workflows/scripts/Register-Plugin.ps1 `
    -EnvironmentUrl "{devEnvUrl}" `
    -SolutionPath "src/solutions/{solutionPrefix}_{solutionName}" `
    -PluginName "{solutionPrefix}_{publisher}.Plugins.{solutionName}.{Name}" `
    -RegisterSteps -SolutionName "my_feature"

# Push only (no step registration)
.github/workflows/scripts/Register-Plugin.ps1 `
    -EnvironmentUrl "{devEnvUrl}" `
    -SolutionPath "src/solutions/{solutionPrefix}_{solutionName}" `
    -PluginName "{solutionPrefix}_{publisher}.Plugins.{solutionName}.{Name}"

# Register a single new step
.github/workflows/scripts/Register-Plugin.ps1 `
    -EnvironmentUrl "{devEnvUrl}" `
    -PluginType "{publisher}.Plugins.{solutionName}.{Name}.{PluginClass}" `
    -Message "Create" -PrimaryEntity "{solutionPrefix}_sample" `
    -Stage 40 -StepMode 0 -SolutionName "my_feature"

# Register custom APIs from solution XML
.github/workflows/scripts/Register-Plugin.ps1 `
    -EnvironmentUrl "{devEnvUrl}" `
    -CustomApiPath "src/solutions/{solutionPrefix}_{solutionName}/src/customapis/{solutionPrefix}_MyCustomApi"
```

> Derive `solutionPrefix`, `solutionName`, and `publisher` from `environment-config.json`. Derive `devEnvUrl` from `solutionAreas[x].devEnv` → `innerLoopEnvironments[].url`.

Parameters: `-EnvironmentUrl`, `-SolutionPath`, `-PluginName`, `-PluginFile`, `-RegisterSteps`, `-SkipPush`, `-PluginType`, `-Message`, `-PrimaryEntity`, `-Stage`, `-StepMode`, `-Rank`, `-FilteringAttributes`, `-AsyncAutoDelete`, `-PreImageAttributes`, `-PostImageAttributes`, `-CustomApiPath`, `-SolutionName`, `-TenantId`, `-ClientId`

## Project Configuration

New plugins must use **SDK-style csproj** targeting net462 with `Microsoft.PowerApps.MSBuild.Plugin`. This produces a **plugin package** (stored in `pluginpackages/` in the solution). Do NOT use old-style `ToolsVersion` csproj — that produces a plain DLL registered in `PluginAssemblies/`, which pac solution sync will rename (stripping dots from folder names) and break the build.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net462</TargetFramework>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CrmSdk.CoreAssemblies" Version="9.0.2.*" PrivateAssets="All" />
    <PackageReference Include="Microsoft.PowerApps.MSBuild.Plugin" Version="1.*" PrivateAssets="All" />
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.*" PrivateAssets="All" />
  </ItemGroup>
</Project>
```

> Any plugin project using old-style csproj is a legacy exception, not a template.

### Core Library Reference

All plugin classes must inherit from `{Publisher}.Plugins.{CoreSolutionAreaName}.Core.PluginBase` (from `solutionAreas[x].corePluginRef` in `environment-config.json`). Do NOT implement `IPlugin` directly.

For cross-solution Core references (where `corePluginRef` starts with `../../`), the Core library lives in a different solution area's plugin folder — use the path exactly as specified in config.

## Directory.Build.targets

Plugin solution folders may contain a `Directory.Build.targets` that provides:
- `GetProjectOutputPath` target for legacy (non-SDK) .csproj files
- `PowerAppsComponentType=Plugin` metadata for Plugin Registration Tool integration

## Solution References

Plugin assemblies wired into each solution `.cdsproj` are tracked in `solutionAreas[x].pluginsSln` in `deployments/settings/environment-config.json`.

## Critical Rules

1. **SDK-style csproj with `Microsoft.PowerApps.MSBuild.Plugin` required** for all new plugins — produces a plugin package in `pluginpackages/`. Old-style ToolsVersion csproj produces a DLL in `PluginAssemblies/`; pac solution sync strips dots from `PluginAssemblies/` folder names, breaking subsequent builds
2. **PluginBase required** — all plugins must inherit from `{Publisher}.Plugins.{CoreSolutionAreaName}.Core.PluginBase`. Never implement `IPlugin` directly
3. **Cross-solution Core references** — when `solutionAreas[x].corePluginRef` starts with `../../`, the Core library lives in another solution area's folder. Use the exact path from config. Do not create a private PluginBase in the dependent project
4. **Forward slashes required** in plugin assembly file paths in solution XML (not backslashes)
5. **Strong-name signing** is mandatory — all assemblies must be signed with `.snk` key files
6. **Register-Plugin.ps1** for registering assemblies/packages and message processing steps (replaces PRT)
7. **Sandbox isolation** — plugins run in Dataverse sandbox; no file system access, limited network
8. **Core library changes** affect all plugin projects — test thoroughly
9. **Plugin must exist** in the environment before using `pac plugin push` — deploy the solution first for new plugins
