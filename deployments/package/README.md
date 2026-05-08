# deployments/package

The **Package Deployer project** — the outer-loop deployment unit for this repo. Wraps all Dataverse solutions and configuration data into a single versioned package that can be deployed to any environment atomically using `pac package deploy`.

## Structure

```
deployments/package/
  Package.sln                        # Visual Studio solution — one project for the full package
  Deployer/
    PlatformPackage.csproj           # Package project — references solution .cdsproj files
    PackageImportExtension.cs        # Lifecycle hooks — runs before/after each solution import
    Constants.cs                     # Setting key prefixes and XML element names
    PkgAssets/
      ImportConfig.xml               # Import order and config data file references
      manifest.ppkg.json             # Package manifest (schema version)
    Models/
      DeploymentSettings.cs          # Deserialization model for per-environment settings
      SolutionConfig.cs              # Per-solution config read from deployment settings
      ManagedIdentityConfig.cs       # Managed identity binding config
    Services/
      EnvironmentVariableService.cs  # Sets Dataverse environment variable values post-import
      ConnectionReferenceService.cs  # Wires connection references to the correct connections
      EnvironmentSettingsService.cs  # Applies general environment-level settings
      ManagedIdentityConfigService.cs # Configures managed identity bindings
      WorkflowService.cs             # Activates or deactivates workflows post-import
```

## What the Package Deployer Does

`pac package deploy` runs the package against a target environment in this sequence:

1. **Read deployment settings** — the `PackageImportExtension` loads the per-environment settings file (generated from `deployments/settings/`) to resolve environment variable values, connection reference IDs, and other settings.
2. **Import solutions** — imports each solution ZIP in `ImportOrder` sequence (lowest first). The order is defined by `ImportOrder` attributes on `<ProjectReference>` entries in `PlatformPackage.csproj`.
3. **Apply settings** — after each solution import, the extension applies:
   - Environment variable values (`EnvironmentVariableService`)
   - Connection reference mappings (`ConnectionReferenceService`)
   - Managed identity bindings (`ManagedIdentityConfigService`)
   - General environment settings (`EnvironmentSettingsService`)
4. **Activate workflows** — `WorkflowService` enables cloud flows that arrive disabled.
5. **Import config data** — imports data files listed in `ImportConfig.xml` from `deployments/data/`.

## Setup: Wiring Solutions into the Package

When setting up a new repo, open `PlatformPackage.csproj` and add one `<ProjectReference>` per solution. The `<!-- SETUP: Add one ProjectReference per solution -->` comment marks the location.

```xml
<!-- Single solution -->
<ItemGroup>
  <ProjectReference Include="../../../src/solutions/acm_AcmePlatform/acm_AcmePlatform.cdsproj"
                    ReferenceOutputAssembly="false" ImportOrder="1" ImportMode="async" />
</ItemGroup>

<!-- Multi-solution — lowest ImportOrder is imported first (core/base solution) -->
<ItemGroup>
  <ProjectReference Include="../../../src/solutions/acm_CoreSolution/acm_CoreSolution.cdsproj"
                    ReferenceOutputAssembly="false" ImportOrder="1" ImportMode="async" />
  <ProjectReference Include="../../../src/solutions/acm_AddOn/acm_AddOn.cdsproj"
                    ReferenceOutputAssembly="false" ImportOrder="2" ImportMode="async" />
</ItemGroup>
```

Paths are relative from `deployments/package/Deployer/` — repo root is `../../../`.

## Deployment Settings Format

The `PackageImportExtension` reads a settings file at deploy time. Settings keys use prefixes defined in `Constants.cs`:

| Prefix | Purpose |
|--------|---------|
| `PD_ENVVAR_{schemaName}` | Set a Dataverse environment variable value |
| `PD_CONNREF_{schemaName}` | Wire a connection reference to a connection ID |
| `PD_MANAGEDIDENTITY_{name}` | Configure a managed identity binding |
| `PD_ENVSETTING_{key}` | Apply a general environment-level setting |

These are generated automatically by `Generate-DeploymentSettings.ps1` from `environment-variables.json` and `connection-mappings.json` — you do not write them by hand.

## Selective Solution Deployment

To deploy only a subset of solutions from the package, pass a runtime setting to `pac package deploy`:

```powershell
pac package deploy `
    --package <path-to-package.zip> `
    --environment <env-url> `
    --runtimePackageSettings "target_solutions=acm_AcmePlatform"
```

Valid solution names come from `packageGroups[].solutions` in `environment-config.json`. Omit `target_solutions` to deploy all solutions in the package.

## Inner Loop vs Outer Loop

This project is **outer loop only**. Do not use `pac package deploy` during inner-loop development.

| | Inner Loop | Outer Loop |
|-|-----------|-----------|
| **Command** | `pac solution import` | `pac package deploy` |
| **Target** | Dev or Dev-Test | Test or Production |
| **Settings** | Applied by `deploy-solution` skill | Applied by this package |
| **Solutions** | Individual, unmanaged or managed | All solutions, managed, versioned |
| **Trigger** | Manual / `deploy-solution` skill | `deploy-package.yml` workflow or skill |

## Building the Package

The package is built by `Build-Package.ps1` (called by `create-release-package.yml` on push to `main`). The output is a `.ppkg` ZIP included in the GitHub Release assets. You do not build this locally during normal development.
