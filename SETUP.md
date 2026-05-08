# Setup Guide — New Client Repository

This repository was created from the [Agentic-ALM-Template](https://github.com/mikefactorial/Agentic-ALM-Template).

## Quick Start

Run the setup script, install the plugin, then let the agent walk you through the rest:

```powershell
.\Initialize-Repo.ps1
```

This initializes the `.platform` submodule and prints plugin install instructions. After installing the plugin, say to the agent:

> "Set up this repo — it was just created from the Agentic-ALM-Template."

The `setup-client-repo` skill handles the rest interactively.

---

## Manual Steps Reference

If you prefer to configure without the agent, the sections below cover each step.

---

## Step 2: Configure the Package Deployer Project

Open `deployments/package/Deployer/PlatformPackage.csproj`. Find the `<!-- SETUP: Add one ProjectReference per solution -->` comment block and add an `<ItemGroup>` with one entry per solution that belongs in this package. `ImportOrder` controls import sequence — lower numbers first (core/base solution must have the lowest).

```xml
<ItemGroup>
  <ProjectReference Include="../../../src/solutions/acm_AcmePlatform/acm_AcmePlatform.cdsproj"
                    ReferenceOutputAssembly="false" ImportOrder="1" ImportMode="async" />
</ItemGroup>
```

That's it — no renaming required. The project uses a generic `PlatformPackage` name that works for any client.

---

## Step 3: Configure environment-config.json

Edit `deployments/settings/environment-config.json` to match your actual:
- Solution names and prefixes
- Environment slugs and URLs (integration, test, prod)
- Package groups (which solutions deploy together)

---

## Step 4: Set Up GitHub Secrets and Variables

### Repository Secrets

| Secret | Description |
|--------|-------------|
| *(none required)* | Secrets are set per GitHub Environment — see below |

### Environment Variables (per GitHub Environment)

Each deployment target environment needs these variables set in **GitHub Environments**
(Settings → Environments → create one per `slug` in `environment-config.json`):

| Variable | Description |
|----------|-------------|
| `DATAVERSE_URL` | Dataverse environment URL |
| `DATAVERSE_CLIENT_ID` | Service principal / app registration client ID |
| `AZURE_TENANT_ID` | Azure Active Directory tenant ID |

### Repository-Level Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `PR_VALIDATION_INTEGRATION_ENV` | Integration environment slug for PR validation builds | `acme-integration` |

---

## Step 5: Configure OIDC Federated Credentials

GitHub Actions authenticates to Dataverse using OIDC — no stored passwords or secrets. Every environment (including dev and integration) needs:

1. An **Azure AD app registration** (service principal) added as a Dataverse App User
2. A **federated identity credential** on that app registration, scoped to the GitHub Environment

Two roles are required (may be the same person):
- **Power Platform Admin** — `pac admin create-service-principal`
- **Azure AD App Owner** — add the federated credential via Azure CLI

### Step 5a — Create the service principal *(Power Platform Admin)*

Repeat for each environment. The output includes the **Application (Client) ID** you'll need next.

```powershell
# Install pac if needed
winget install Microsoft.PowerPlatform.CLI

pac auth create --interactive --environment <dataverse-env-url>
pac admin create-service-principal --environment <dataverse-env-url>
```

### Step 5b — Add federated credentials *(Azure AD App Owner)*

```powershell
# Install Azure CLI if needed
winget install Microsoft.AzureCLI
az login

# Run once — list all environment slugs if they share one app registration,
# or run once per registration if each environment has its own
.platform/.github/workflows/scripts/Setup-GitHubFederatedCredentials.ps1 `
    -AppRegistrationId "<client-id>" `
    -GitHubOrg "<githubOrg>" `
    -RepositoryName "<repoName>" `
    -Environments @("<env-slug-1>", "<env-slug-2>", "<env-slug-3>")
```

The script creates a federated credential for each slug with subject:
`repo:<githubOrg>/<repoName>:environment:<env-slug>`

It skips credentials that already exist and prints a created / skipped / error summary.

### Step 5c — Set client IDs in GitHub Environments

If you haven't already set `DATAVERSE_CLIENT_ID` in Step 4:

```powershell
gh variable set DATAVERSE_CLIENT_ID --env <env-slug> --repo "<org>/<repo>" --body "<client-id>"
```

### Verify

```powershell
gh workflow run test-oidc-auth.yml --repo "<org>/<repo>"
gh run watch --repo "<org>/<repo>"
```

A green run confirms the full OIDC auth chain works for that environment.

> **Need to delegate this to an admin?** Ask the Copilot agent (`"help me set up OIDC"` or `"generate admin OIDC instructions"`) and it will produce a ready-to-share hand-off document via the `setup-oidc` skill.

---

## Step 6: Commit Setup Changes and Create Develop Branch

After completing configuration, commit everything and create the `develop` branch:

```powershell
git add -A
git commit -m "chore: initial repo setup — environment-config, package project, GitHub environments"
git push origin main

# Create develop branch from main if it doesn't already exist
$developExists = git ls-remote --heads origin develop
if (-not $developExists) {
    git checkout -b develop
    git push origin develop
    git checkout main
}
```

---

## Step 7: Branch Protection

Use `gh` to configure protection rules for both branches:

```powershell
$org  = "<githubOrg>"
$repo = "<repoName>"

# main — require PR, no direct pushes, no force push
gh api --method PUT /repos/$org/$repo/branches/main/protection `
    --field required_status_checks=null `
    --field enforce_admins=false `
    --field "required_pull_request_reviews[dismiss_stale_reviews]=true" `
    --field "required_pull_request_reviews[required_approving_review_count]=1" `
    --field "restrictions=null" `
    --field allow_force_pushes=false `
    --field allow_deletions=false

# develop — require PR review, no direct pushes, no force push
gh api --method PUT /repos/$org/$repo/branches/develop/protection `
    --field required_status_checks=null `
    --field enforce_admins=false `
    --field "required_pull_request_reviews[dismiss_stale_reviews]=true" `
    --field "required_pull_request_reviews[required_approving_review_count]=1" `
    --field "restrictions=null" `
    --field allow_force_pushes=false `
    --field allow_deletions=false
```

> Source-branch enforcement for `main` (PRs from `develop`/`hotfix/*` only) is handled by `check-source-branch.yml` — the GitHub API does not support this at the branch protection level.

## Step 7: Initialize Submodules

This repo has one submodule:

| Submodule | Repo | Purpose |
|-----------|------|---------|
| `.platform` | `Agentic-ALM-Workflows` | PowerShell scripts, plugin skills, and CI |

Initialize with:

```powershell
.\Initialize-Repo.ps1
```

After init, `.platform/.github/workflows/scripts/` contains all scripts used locally and by GitHub Actions.

## Keeping Platform Assets Updated

Run the same script any time to update:

```powershell
.\Initialize-Repo.ps1
```

This updates `.platform` to the latest `Agentic-ALM-Workflows` main, then reminds you to refresh the plugin. Idempotent — safe to re-run.

After running `Initialize-Repo.ps1`, also update the plugin in VS Code to pick up any new or changed skills:

**Command Palette (`Ctrl+Shift+P`) → `Chat: Update Plugins (Force)`**

This forces VS Code to re-fetch all installed agent plugins from their sources, ensuring the locally cached skill files match the latest version.
