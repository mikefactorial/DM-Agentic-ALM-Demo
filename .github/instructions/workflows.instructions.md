---
applyTo: ".github/workflows/**"
---

# GitHub Actions Workflows & Automation

## Workflow Inventory

13 workflow files in `.github/workflows/`:

### Inner Loop (Development)

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `sync-solution.yml` | Manual dispatch | Sync solution from integration env → repo (commits to branch) |
| `sync-build-deploy-solution.yml` | Manual dispatch | Sync from dev → build → deploy (always syncs first) |
| `build-deploy-solution.yml` | Manual dispatch | Build from current branch → deploy (no sync, for feature branches) |
| `promote-solution.yml` | Manual dispatch | Promote components: dev → integration (export → import → copy) |

### Outer Loop (Build, Release, Deploy)

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `create-release-package.yml` | Push to `main` / manual | Build all 4 package groups (8 ZIPs), create GitHub Release |
| `deploy-package.yml` | Manual dispatch | Deploy a package group to one environment (`pac package deploy`) |

### Validation & Utility

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `pr-validation.yml` | Pull request | Smart change detection → build/test only what changed |
| `check-source-branch.yml` | Pull request (disabled) | Enforce: only `develop`/`hotfix/*` can merge to `main` |
| `check-feature-solution-files.yml` | Pull request to `develop`/`main` | Block feature solution folders from entering `develop` or `main` |
| `test-oidc-auth.yml` | Manual dispatch | Validate OIDC federated auth to a Dataverse environment |

## Workflow Dispatch Inputs

### sync-solution.yml

| Input | Type | Required | Default | Choices |
|-------|------|----------|---------|---------|
| `environment` | choice | yes | — | Read from `innerLoopEnvironments[].slug` in `environment-config.json` (integration envs) |
| `solution_name` | string | yes | — | — |
| `commit_message` | string | yes | — | — |
| `branch_name` | string | no | `develop` | — |
| `publish_customizations` | boolean | no | true | — |

### build-deploy-solution.yml

| Input | Type | Required | Default | Choices |
|-------|------|----------|---------|---------|
| `solution_name` | string | yes | — | — |
| `target_environments` | string | yes | — | Comma-separated slug(s) from `environments[]` in `environment-config.json` (e.g., `{envPrefix}-dev-test`) |
| `continue_on_error` | boolean | no | false | — |
| `use_upgrade` | boolean | no | false | — |

Runs on the caller's current branch — designed for feature branch → dev-test deployments without sync.

### sync-build-deploy-solution.yml

| Input | Type | Required | Default | Choices |
|-------|------|----------|---------|---------|
| `source_environment` | choice | yes | — | Read from `innerLoopEnvironments[].slug` in `environment-config.json` (dev envs) |
| `solution_name` | string | yes | — | — |
| `target_environments` | string | yes | — | Comma-separated |
| `publish_customizations` | boolean | no | true | — |
| `branch_name` | string | no | `develop` | — |
| `commit_message` | string | no | `chore: automated solution sync` | — |
| `continue_on_error` | boolean | no | false | — |
| `use_upgrade` | boolean | no | false | — |

Always syncs first (job dependency chain: sync → build → deploy).

### promote-solution.yml

| Input | Type | Required | Default | Choices |
|-------|------|----------|---------|---------|
| `source_environment` | choice | yes | — | Read from `innerLoopEnvironments[].slug` in `environment-config.json` (dev envs) |
| `target_environment` | choice | yes | — | Read from `innerLoopEnvironments[].slug` in `environment-config.json` (integration envs) |
| `source_solution_name` | string | yes | — | — |
| `target_solution_name` | string | yes | — | — |
| `sync_target_solution` | boolean | no | false (dispatch) / true (call) | — |
| `sync_commit_message` | string | no | `chore: automated solution sync after promote` | — |
| `sync_branch_name` | string | no | `develop` | — |
| `publish_customizations` | boolean | no | true | — |

Jobs: export-solution → import-and-copy → sync-target-solution (optional).

### create-release-package.yml

| Input | Type | Required | Default | Choices |
|-------|------|----------|---------|---------|
| `package_version` | string | no | auto (date-based) | — |
| `create_release` | boolean | no | true | — |
| `draft_release` | boolean | no | false | — |

Auto-triggered on push to `main`. Builds one managed + one unmanaged ZIP per package group defined in `environment-config.json`.

### deploy-package.yml

| Input | Type | Required | Default | Choices |
|-------|------|----------|---------|---------|
| `environment` | choice | yes | — | Read from `environments[].slug` in `environment-config.json` |
| `package` | choice | yes | — | Read from `packageGroups[].name` in `environment-config.json` |
| `release_tag` | string | no | latest release | — |

Uses `pac package deploy` (outer loop). Resolves solutions from `environment-config.json`.

## Two Deployment Mechanisms

| Mechanism | Script | PAC Command | Used By | Use Case |
|-----------|--------|-------------|---------|----------|
| Solution import | `Deploy-Solutions.ps1` | `pac solution import` | build-deploy-solution, sync-build-deploy-solution | Inner loop: feature → dev-test |
| Package deploy | `Deploy-Package.ps1` | `pac package deploy` | deploy-package | Outer loop: release → test/prod |

## Script Reference

All scripts in `.github/workflows/scripts/`:

### Build Scripts

| Script | Key Parameters |
|--------|---------------|
| `Build-Controls.ps1` | `-artifactsPath`, `-testResultsPath`, `-skipTests`, `-projectPaths`, `-projectFilter` |
| `Build-Plugins.ps1` | `-artifactsPath`, `-testResultsPath`, `-configuration`, `-projectPaths`, `-skipTests` |
| `Build-Solutions.ps1` | `-solutionList` (required), `-targetEnvironmentList`, `-artifactsPath`, `-configuration` |
| `Build-Package.ps1` | `-PackageVersion`, `-ArtifactsPath`, `-Configuration` |

### Deploy Scripts

| Script | Key Parameters |
|--------|---------------|
| `Deploy-Solutions.ps1` | `-solutionList`, `-targetEnvironment`, `-environmentUrl`, `-artifactsPath`, `-tenantId`, `-clientId`, `-continueOnError`, `-useUpgrade` |
| `Deploy-Package.ps1` | `-packageGroup`, `-solutions`, `-targetEnvironment`, `-environmentUrl`, `-tenantId`, `-clientId` |

### Sync/Promote Scripts

| Script | Key Parameters |
|--------|---------------|
| `Sync-Solution.ps1` | `-solutionName`, `-environmentUrl`, `-skipGitCommit`, `-branchName`, `-commitMessage` |
| `Promote-Solution.ps1` | `-sourceSolutionName`, `-targetSolutionName`, `-sourceEnvironmentUrl`, `-targetEnvironmentUrl` |
| `Copy-Components.ps1` | `-environmentUrl`, `-sourceSolutionName`, `-targetSolutionName` |

### Settings/Config Scripts

| Script | Key Parameters |
|--------|---------------|
| `Generate-DeploymentSettings.ps1` | `-solutionName`, `-targetEnvironment`, `-templatePath`, `-outputPath`, `-configPath` |
| `Validate-DeploymentSettings.ps1` | `-SolutionList`, `-TargetEnvironmentList`, `-SolutionsRoot`, `-ConfigPath` |
| `Populate-EnvironmentValues.ps1` | `-environments` |

### Detection Scripts

| Script | Key Parameters |
|--------|---------------|
| `Detect-ChangedComponents.ps1` | `-BaseBranch`, `-HeadBranch` → outputs `plugins_changed`, `controls_changed`, `control_groups` |
| `Detect-ChangedSolutions.ps1` | `-BaseBranch`, `-HeadBranch` → outputs `has_changes`, `solution_list` |

## Pipeline Hook System

14-stage extensible hook system via `Invoke-PipelineHooks.ps1`:

### Hook Stages

| Stage | When | Called By |
|-------|------|-----------|
| `pre-unpack` / `post-unpack` | Before/after solution XML unpacking | Sync-Solution.ps1 |
| `pre-unpack-canvas` / `post-unpack-canvas` | Canvas app .msapp extraction | Sync-Solution.ps1 |
| `pre-commit` / `post-commit` | Before/after git commit | Sync-Solution.ps1 |
| `pre-build` / `post-build` | Before/after solution build | Build-Solutions.ps1 |
| `pre-export` / `post-export` | Before/after solution export | Promote-Solution.ps1 |
| `pre-import` / `post-import` | Before/after solution import | Promote-Solution.ps1 |
| `pre-deploy` / `post-deploy` | Before/after deployment | Deploy-Solutions.ps1 |

### Hook Invocation Pattern

```powershell
# Source the hook manager
. ".github/workflows/scripts/Invoke-PipelineHooks.ps1"

# Invoke all hooks for a stage
$context = @{
    solutionName    = $solutionName
    environmentUrl  = $environmentUrl
    artifactsPath   = $artifactsPath
}
$success = Invoke-PipelineHooks -Stage "post-deploy" -Context $context
```

Hooks are PowerShell scripts in `.github/workflows/scripts/hooks/` named `{Stage}-{Description}.ps1`. Parameters are matched via AST parsing — only context keys matching the hook's `param()` block are passed.

### Hook Variables

Global variables/secrets are passed via environment variables:
- `HOOK_VARIABLES` — JSON string of non-secret parameters
- `HOOK_SECRETS` — JSON string of GitHub secrets

Access in hooks via:

```powershell
. "$PSScriptRoot\..\HookVariables.ps1"
$value  = Get-HookVariable -Name "KEY_NAME" -Default "default"
$secret = Get-HookSecret   -Name "SECRET_KEY"
```

### Existing Hooks

| Hook | Purpose |
|------|---------|
| `Pre-Build-Hook.ps1` | Pre-build preparation |
| `Post-Build-Hook.ps1` | Post-build processing |
| `Pre-Commit-Hook.ps1` | Pre-commit validation |
| `Post-Commit-Hook.ps1` | Post-commit processing |
| `Pre-Deploy-Hook.ps1` | Pre-deployment preparation |
| `Post-Deploy-Hook.ps1` | Post-deployment processing |
| `Post-Deploy-EnableFlowsAndProcesses.ps1` | Activate Cloud Flows & Classic Workflows |
| `Pre-Export-Hook.ps1` | Pre-export validation |
| `Post-Export-Hook.ps1` | Post-export processing |
| `Pre-Import-Hook.ps1` | Pre-import preparation |
| `Post-Import-Hook.ps1` | Post-import verification |
| `Pre-Unpack-Hook.ps1` | Pre-unpack preparation |
| `Post-Unpack-Hook.ps1` | Post-unpack processing |

### Creating New Hooks

1. Create file: `.github/workflows/scripts/hooks/{Stage}-{YourDescription}.ps1`
2. Add a `param()` block — only declare parameters you need from the stage's context
3. The hook manager auto-discovers and invokes matching hooks alphabetically
4. To disable: rename the file to break the `{Stage}-*.ps1` pattern (e.g., add `.disabled`)

## Authentication

All CI/CD uses OIDC federated credentials — no client secrets:

```powershell
pac auth create --githubFederated `
    --tenant $tenantId `
    --applicationId $clientId `
    --environment $environmentUrl
```

Requires workflow permission: `id-token: write`

Per-environment GitHub variables:
- `AZURE_TENANT_ID` (repo-level)
- `DATAVERSE_CLIENT_ID` (per-environment)
- `DATAVERSE_URL` (per-environment)

## Concurrency Control

- `build-deploy-solution.yml`: Per-solution-per-environment (no cancel-in-progress)
- `create-release-package.yml`: Per-ref (cancel-in-progress: true)
- `promote-solution.yml`: Single concurrency group (no cancellation)
- Deploy workflows use `max-parallel` to control simultaneous deployments

## Critical Rules

1. **Promotion runs locally** — run `Promote-Solution.ps1 -Phase All` then `Sync-Solution.ps1` to a sync branch, then open a PR to `develop`; the `promote-solution.yml` workflow is available but not required
2. **Hook ContinueOnError = true** by default — hooks shouldn't stop the pipeline unless critical
3. **`check-source-branch.yml`** is currently disabled — when enabled, enforces `main` ← `develop`/`hotfix/*` only
4. **`check-feature-solution-files.yml`** blocks any PR to `develop` or `main` that includes files under `src/solutions/<name>/` where `<name>` is not a `mainSolution` from `environment-config.json`. Feature solution source is a build artifact — only main solution sync PRs belong in `develop`.
5. **App tokens**: Sync and promote workflows use GitHub App tokens for git operations (not the default `GITHUB_TOKEN`)
