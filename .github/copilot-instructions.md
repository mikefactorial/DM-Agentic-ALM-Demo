# Platform — Agent Instructions

## First-Time Setup (Bootstrap)

**Check this first on every session start.** Read `deployments/settings/environment-config.json` and look for unreplaced `{{PLACEHOLDER}}` values.

If placeholders are present, this repo has not been configured yet. Guide the user through the following bootstrap before doing anything else:

### Step 1 — Initialize `.platform`

Check whether `.platform/.github/workflows/scripts/` contains files:

```powershell
Test-Path ".platform/.github/workflows/scripts"
```

If `False` or empty, run:

```powershell
.\Initialize-Repo.ps1
```

Do not proceed until `.platform` is populated.

### Step 2 — Verify required tools

```powershell
$missing = @()
if (-not (Get-Command pac    -ErrorAction SilentlyContinue)) { $missing += 'pac (Power Platform CLI) — https://aka.ms/PowerAppsCLI' }
if (-not (Get-Command gh     -ErrorAction SilentlyContinue)) { $missing += 'gh (GitHub CLI) — https://cli.github.com' }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { $missing += 'dotnet (.NET SDK) — https://dot.net' }
if ($missing) { Write-Warning "Missing tools:"; $missing | ForEach-Object { Write-Host "  $_" } }
else { Write-Host "All required tools found." -ForegroundColor Green }
```

Also verify `gh` is authenticated (`gh auth status`). If not, run `gh auth login`.

### Step 3 — Recommend agent skill plugins (optional)

Let the user know two plugins are available that automate ALM tasks via plain-English instructions. These are **recommended but not required** — the workflows, scripts, and CI/CD all work without them, and some organizations may not allow plugin installation.

Suggest installing if the user is able to:

> "Two optional plugins are available that let you describe ALM tasks in plain English instead of running scripts manually:
> 1. **`power-platform-alm`** — start features, sync solutions, deploy, promote, release. Install from Source: `https://github.com/mikefactorial/Agentic-ALM-Workflows`
> 2. **`dataverse`** — query, create, and manage Dataverse data and metadata. Install from Source: `https://github.com/microsoft/Dataverse-skills`
>
> To install: Extensions sidebar (`Ctrl+Shift+X`) → search `@agentPlugins power-platform-alm` or `@agentPlugins dataverse`. Or Command Palette → `Chat: Install Plugin From Source`.
> If already installed, run `Chat: Update Plugins (Force)` to get the latest versions.
> Skip this step if plugins are not permitted in your organization."

Do not block setup on plugin installation. Proceed to Step 4 regardless.

### Step 4 — Hand off to setup

Say:

> "Say **'set up this repo'** and I'll walk you through filling in environment-config.json, wiring GitHub environments and secrets, OIDC credentials, and branch protection."

If the `power-platform-alm` plugin is installed, the `setup-client-repo` skill will handle this automatically. If not, proceed inline using the steps in SETUP.md.

---

> **Read `deployments/settings/environment-config.json`** at the start of every session to resolve all project-specific values:
> - `clientName` — human-readable product/client name
> - `solutionAreas[].name` / `.prefix` / `.role` — solution identifiers
> - `publisher` — Dataverse publisher name
> - `githubOrg` + `repoName` — GitHub coordinates
> - `innerLoopEnvironments[]` / `environments[]` — all environment slugs and URLs
> - `trackingSystem` — `azureBoards` (default) or `github`; controls branch/commit trailer format
>
> Do not assume or hardcode any of these values — always derive them from the config file.

## Repository Overview

Power Platform ALM repository for interconnected Dataverse solutions.

Refer to `solutionAreas[]` in `deployments/settings/environment-config.json` for the full solution inventory (name, prefix, publisher, role). Multi-solution repos will have multiple entries.

**GitHub repo**: Read `githubOrg` / `repoName` from `environment-config.json`.

## Platform Scripts

PowerShell scripts used by skills and local development live in the `.platform` git submodule, which points to `mikefactorial/Agentic-ALM-Workflows`. This is the same path used by callable workflows in CI.

**Initialize or update to latest:**
```powershell
.\Initialize-Repo.ps1
```

All skill script references use `.platform/.github/workflows/scripts/`. If `.platform` is empty, run the command above.

---

## Agent Skills (Plugin)

ALM skills (start-feature, scaffold-plugin, deploy-solution, etc.) ship as the `power-platform-alm` plugin from `Agentic-ALM-Workflows`. Skills cover the full inner and outer loop. **These are recommended but not required** — all CI/CD workflows and scripts work without them.

**Install for GitHub Copilot (VS Code):**
- Extensions view (`Ctrl+Shift+X`) → search `@agentPlugins power-platform-alm` → Install
- Or: Command Palette → `Chat: Install Plugin From Source` → `https://github.com/mikefactorial/Agentic-ALM-Workflows`

Also install the **Dataverse skills** plugin for metadata, data, query, and solution operations:
- Extensions view (`Ctrl+Shift+X`) → search `@agentPlugins dataverse` → Install
- Or: Command Palette → `Chat: Install Plugin From Source` → `https://github.com/microsoft/Dataverse-skills`

**Already installed? Update both to latest:**
- Command Palette → `Chat: Update Plugins (Force)`

**Install for Claude Code (once `.platform` is initialized):**
```bash
claude --plugin-dir .platform/.github/plugins/power-platform-alm
```

After installing, describe any ALM task in plain English — the `alm-overview` router skill picks the right specialist automatically.

---

## Branching Strategy

```
main (production-ready, protected)
 ↑ PR from develop or hotfix/* only (enforced by check-source-branch.yml)
develop (integration branch)
 ↑ PR from feature branches / promote commits
feature branches: <type>/<tag>_BriefDescription (branch from develop)
hotfix branches: hotfix/<issue-number> (branch from main, merge to both main + develop)
```

- Feature branches always branch from `develop`
- Feature branch naming: `<type>/<tag>_BriefDescription`
  - `<tag>` is the normalized work item / issue tag:
    - Azure Boards: `AB12345` (strip `#`, keep `AB` prefix)
    - GitHub Issue: `GH12345` (replace `#` with `GH` prefix)
  - Types: `feat/`, `fix/`, `chore/`, `refactor/`, `docs/`, `test/`
- Hotfix branches: `hotfix/<issue-number>` from `main`
- Push to `main` triggers automatic release package build

## Commit Convention

Use [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>(<scope>): <description> <trailer>
```

- **Types**: `feat`, `fix`, `chore`, `refactor`, `docs`, `test`, `build`, `ci`
- **Scope** (optional): solution name or component area
- **Work item / issue linking** — append the appropriate trailer:
  - Azure Boards: `AB#12345`
  - GitHub Issue: `Closes #12345`
- Read `trackingSystem` from `deployments/settings/environment-config.json` (`azureBoards` or `github`) for the default

---

## ALM Model: Inner Loop and Outer Loop

### Inner Loop (Daily Development)

```
1. Branch from develop → {tag}_Description
2. Create feature solution in dev env → set as preferred solution
3. Develop & iterate in dev
4. Sync feature solution to feature branch
5. Build + deploy feature solution to dev-test
6. Test in dev-test environment
7. After validation: promote feature solution from dev → integration
8. Merge feature branch → develop (PR for code-first changes)
```

### Outer Loop (Build, Release, Deploy)

```
1. PR develop → main
2. Push to main triggers create-release-package.yml
3. Packages built, versioned, settings generated for all environments
4. GitHub Release created with artifacts
5. Manual dispatch deploy-package.yml to deploy to target environments
```

---

## Environment Topology

Read all environment slugs and URLs from `deployments/settings/environment-config.json`:

- **Inner loop** (dev + integration per solution area): `innerLoopEnvironments[]`
- **Deployment targets** (dev-test, test, prod): `environments[]`
- **Per-solution-area mapping**: `solutionAreas[x].devEnv` → dev slug; `solutionAreas[x].integrationEnv` → integration slug

Resolve the URL for any slug by finding the matching entry in `innerLoopEnvironments[]` or `environments[]` and reading `.url`.

---

## Repository Structure

```
src/
  controls/           # PCF controls
  plugins/            # .NET plugin assemblies
  solutions/          # Unpacked Dataverse solution metadata
deployments/
  settings/           # Deployment configuration
    templates/        # Auto-generated settings templates (from sync)
    connection-mappings.json
    environment-variables.json
    environment-config.json
  data/               # Configuration data for post-deploy import
.github/
  workflows/          # GitHub Actions workflow definitions
    scripts/          # PowerShell automation scripts (from PlatformWorkflows)
```

---

## Running Scripts Locally

Scripts are pulled from `Agentic-ALM-Workflows` via the two-checkout pattern in each
callable workflow. For local use, clone Agentic-ALM-Workflows and run scripts directly.

### Prerequisites

```powershell
# Read the dev URL from innerLoopEnvironments[] in environment-config.json
pac auth create --interactive --environment {devEnvUrl}
pac auth list
pac auth select --index <n>
```

---

## Critical Rules

1. **Never edit Solution.xml manually** — always sync from the Dataverse environment
2. **Always set preferred solution** when creating a feature solution in Dataverse
3. **PCF controls are NOT auto-tracked** — must be added to preferred solution manually
4. **Forward slashes** in plugin assembly file paths in solution XML (not backslashes)
5. **Settings templates are auto-generated** during sync — don't edit templates directly
6. **Date-based versioning**: `YYYY.MM.DD.N` (e.g., `2026.04.06.1`), auto-calculated from git tags
7. **Package deploy** (outer loop) vs **solution import** (inner loop) — don't mix them up
8. **Promotion runs locally** — `Promote-Solution.ps1` + `Sync-Solution.ps1` locally, then open a sync PR to `develop`
9. **dotnet build for inner loop** — `Build-Solutions.ps1` is outer-loop/CI only
10. **Always sync before deploying to dev-test** — never assume a feature is code-first only
