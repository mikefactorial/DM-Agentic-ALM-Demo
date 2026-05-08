---
applyTo: "src/solutions/**"
---

# Dataverse Solution Metadata

## Solution Layout

Solutions live under `src/solutions/`, each with a `.cdsproj` and `src/` directory. The solution list is defined in `solutionAreas[]` in `deployments/settings/environment-config.json`.

| Field | Example |
|-------|---------|
| Solution folder | `{solutionPrefix}_{solutionName}/` — read from `solutionAreas[].prefix` + `solutionAreas[].name` in `environment-config.json` |
| Publisher | Read from `publisher` in `environment-config.json` |

Multi-solution repos will have one folder per solution area. Dependencies between solutions (e.g., one solution depending on another) should be documented in `environment-config.json` and `solutionAreas[x].cdsproj`.

## Directory Structure

Typical `src/` subdirectories (varies by solution):

```
src/
├── appactions/             # Custom actions
├── AppModules/             # Model-driven app definitions
├── AppModuleSiteMaps/      # App navigation
├── CanvasApps/             # Canvas app sources (.msapp extracted)
├── Connectors/             # Custom connectors
├── Controls/               # PCF control bundles (managed-layer extracts)
├── customapis/             # Custom API definitions
├── Entities/               # Table schemas, forms, views, ribbons
├── environmentvariabledefinitions/  # Environment variable schema
├── msdyn_plans/            # Power Automate Desktop plans
├── OptionSets/             # Global choice definitions
├── organizationsettings/   # Org-level settings
├── Other/                  # Solution.xml, Customizations.xml, Relationships.xml
├── PluginAssemblies/       # Plugin DLL metadata (not the DLLs themselves)
├── pluginpackages/         # Plugin package definitions
├── Roles/                  # Security roles
├── SdkMessageProcessingSteps/  # Plugin step registrations
├── WebResources/           # JavaScript, HTML, CSS, images
└── Workflows/              # Cloud flows (JSON definitions)
```

## .cdsproj Structure

Uses `AlbanianXrm.CDSProj.Sdk/1.0.9`:

```xml
<Project Sdk="AlbanianXrm.CDSProj.Sdk/1.0.9">
  <PropertyGroup>
    <SolutionRootPath>src</SolutionRootPath>
    <SolutionPackageType>Both</SolutionPackageType>  <!-- Managed + Unmanaged -->
  </PropertyGroup>
  <ItemGroup>
    <!-- ProjectReference for each plugin .csproj -->
    <!-- ProjectReference for each PCF .pcfproj -->
  </ItemGroup>
</Project>
```

`SolutionPackageType=Both` generates both managed and unmanaged ZIPs during build.

## Solution.xml

Located at `src/Other/Solution.xml`. Contains:
- `UniqueName` — Solution identifier (e.g., `{solutionPrefix}_{solutionName}` from environment-config.json)
- `Version` — Date-based: `YYYY.MM.DD.N` (e.g., `2026.03.18.5828`)
- `Managed` — `2` = unmanaged (repo always stores unmanaged)
- `Publisher` — Name, prefix, option value prefix
- `RootComponents` — List of all solution components by type

## Sync Operations

Sync exports a solution from a Dataverse environment and unpacks it to the repo:

```powershell
# Via workflow (recommended)
# Dispatch sync-solution.yml with environment + solution_name

# Via script (local)
.github/workflows/scripts/Sync-Solution.ps1 `
  -solutionName "{solutionPrefix}_{solutionName}" `
  -environmentUrl "{integrationEnvUrl}" `
  -skipGitCommit
```

> Derive `solutionName`, `solutionPrefix` from `solutionAreas[]` and `integrationEnvUrl` from `innerLoopEnvironments[]` in `environment-config.json`.

Sync triggers these hook stages:
1. `Pre-Unpack` / `Post-Unpack` — Before/after solution XML unpacking
2. `Pre-Unpack-Canvas` / `Post-Unpack-Canvas` — Canvas app `.msapp` extraction
3. `Pre-Commit` / `Post-Commit` — Before/after git commit

## Build

```powershell
.github/workflows/scripts/Build-Solutions.ps1 `
  -solutionList "{solutionPrefix}_{solutionName}" `
  -targetEnvironmentList "{devTestSlug}" `
  -artifactsPath ./out
```

> Derive values from `solutionAreas[]` and `environments[]` in `environment-config.json`.

Build triggers `Pre-Build` / `Post-Build` hooks. Output: `{solution}_{version}_managed.zip` and `{solution}_{version}.zip`.

## Deployment Settings Generation

During build, `Generate-DeploymentSettings.ps1` merges templates with per-environment config:

```
deployments/settings/templates/{solution}_template.json  (from sync)
  + deployments/settings/connection-mappings.json
  + deployments/settings/environment-variables.json
  → {solution}_{version}_{environment}_settings.json  (build artifact)
```

## Component Types and Tracking

| Component | Auto-tracked in preferred solution? |
|-----------|-------------------------------------|
| Tables (Entities) | Yes |
| Forms, Views | Yes |
| Cloud Flows (Workflows) | Yes |
| Web Resources | Yes |
| Plugin Assemblies | Yes (via PRT registration) |
| Plugin Steps | Yes (via PRT registration) |
| PCF Controls | **No — must add manually** |
| Canvas Apps | Yes |
| Security Roles | Yes |
| Environment Variables | Yes |
| Connection References | Yes |

## Critical Rules

1. **NEVER edit Solution.xml manually** — always sync from the Dataverse environment
2. **Always set preferred solution** when developing in Dataverse — components get tracked to the wrong solution otherwise
3. **Version format**: `YYYY.MM.DD.N` — auto-calculated from git tags by `Get-NextVersion.ps1`
4. **Unmanaged source**: Repository always stores unmanaged solution metadata; managed ZIPs are build artifacts
5. **Component deletion limitations**: Merged components (forms, sitemaps, security roles, global choices) cannot have sub-elements removed via deployment. Use workarounds: remove from originating solution, deprecate+replace, or hide
6. **Forward slashes** in plugin assembly paths within `PluginAssemblies/` XML files
7. **Canvas apps**: `.msapp` files are extracted to source-controllable format during sync (via canvas unpack hooks)
