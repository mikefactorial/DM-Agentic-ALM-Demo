# src/solutions

Unpacked Dataverse solution metadata, tracked in source control using `.cdsproj` files. One subfolder per solution area, named `{solutionPrefix}_{solutionName}` (e.g. `acm_AcmePlatform`).

## What Lives Here

Each solution folder is created by `pac solution unpack` and contains the raw XML that Dataverse uses to define your solution:

```
src/solutions/
  acm_AcmePlatform/
    acm_AcmePlatform.cdsproj   # Build definition — references plugins and controls
    Other/
      Solution.xml             # Solution metadata (version, publisher, description)
    Entities/                  # Table definitions — columns, forms, views, charts
    Workflows/                 # Cloud flows and business rules
    CanvasApps/                # Canvas app source (unpacked)
    WebResources/              # JS, HTML, CSS, images
    Relationships/             # Table relationships
    ...                        # Other component folders added as you build
```

> **Never edit `Solution.xml` manually.** Version and component lists are managed by the ALM process — always sync from the Dataverse environment.

## How Solutions Are Managed: Feature Solutions

The ALM model uses **feature solutions** to isolate work in progress from the main integration solution. Each work item gets its own temporary Dataverse solution.

```
Main solution (acm_AcmePlatform)   ← integration branch, always releasable
  ↑ components copied in at staging
Feature solution (acm_AB1234_MyFeature)  ← developer's scratch space
  ↑ components added during development
```

### Inner Loop: Feature to Integration

1. **Start feature** — `start-feature` skill creates a feature solution in your dev environment and sets it as the preferred solution. New components go here automatically.
2. **Develop and iterate** — build and customize in the dev environment. Sync to your feature branch with the `sync-solution` skill.
3. **Test in dev-test** — deploy the feature solution (managed) to the dev-test environment with the `deploy-solution` skill. This validates the solution in a clean environment before integration.
4. **Promote to integration** — once validated, the `promote-solution` skill runs `Promote-Solution.ps1` locally to copy components from your feature solution into the main solution in the integration environment, then opens a sync PR to `develop`.
5. **PR merge** — code-first changes (plugins, controls) are reviewed and merged. Solution metadata flows via the sync PR.

### Outer Loop: Release

When `develop` is merged to `main`, `create-release-package.yml` builds versioned solution ZIPs and creates a GitHub Release. `deploy-package.yml` deploys these to test and production environments.

## Version Numbering

Versions use date-based format: `YYYY.MM.DD.N` (e.g. `2026.05.02.1`). The `Get-NextVersion.ps1` script calculates the next version from existing git tags — you never set version numbers manually.

## Source Control Rules

- Sync from the environment before every feature branch (never assume source is current)
- Do not hand-edit component XML — use the environment + sync workflow
- PCF controls and plugin assemblies are **not** auto-tracked; add them to the feature solution manually in make.powerapps.com after `pac pcf push` or `Register-Plugin.ps1`
- Settings templates under `deployments/settings/templates/` are auto-generated during sync — do not edit them directly

## Agent Commands

| Task | Say to agent |
|------|--------------|
| Sync solution from environment | `"sync the solution from dev"` |
| Start a new feature | `"start feature for AB1234"` |
| Deploy feature to dev-test | `"deploy to dev-test"` |
| Promote feature to integration | `"promote feature AB1234"` |
| Cut a release | `"create a release"` |
