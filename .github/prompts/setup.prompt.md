---
mode: agent
description: Bootstrap and configure a new repo created from Agentic-ALM-Template. Checks prerequisites, initializes .platform, installs the ALM plugin, then hands off to the setup-client-repo skill.
---

This repo was created from the Agentic-ALM-Template and needs first-time configuration.

Follow these steps in order:

## Step 1: Initialize .platform

Check if the submodule is ready:

```powershell
Test-Path ".platform/.github/workflows/scripts"
```

If `False` or empty, run:

```powershell
.\Initialize-Repo.ps1
```

Do not continue until `.platform` is populated.

## Step 2: Verify Required Tools

```powershell
$missing = @()
if (-not (Get-Command pac    -ErrorAction SilentlyContinue)) { $missing += 'pac (Power Platform CLI) — https://aka.ms/PowerAppsCLI' }
if (-not (Get-Command gh     -ErrorAction SilentlyContinue)) { $missing += 'gh (GitHub CLI) — https://cli.github.com' }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { $missing += 'dotnet (.NET SDK) — https://dot.net' }
if ($missing) { Write-Warning "Missing tools:"; $missing | ForEach-Object { Write-Host "  $_" } }
else { Write-Host "All required tools found." -ForegroundColor Green }
```

Also check `gh auth status`. If not authenticated, run `gh auth login`.

## Step 3: Install Agent Skill Plugins (Recommended, Not Required)

Two plugins are available that let you automate ALM tasks in plain English. They are **recommended but not required** — all CI/CD workflows and scripts work without them, and some organizations may not permit plugin installation. Skip this step if that applies.

### Plugin 1 — Power Platform ALM skills (`power-platform-alm`)

If already installed, update first: **Command Palette (`Ctrl+Shift+P`) → `Chat: Update Plugins (Force)`**

To install:
- Extensions sidebar (`Ctrl+Shift+X`) → search `@agentPlugins power-platform-alm` → Install
- Or: Command Palette → `Chat: Install Plugin From Source` → `https://github.com/mikefactorial/Agentic-ALM-Workflows`

### Plugin 2 — Dataverse skills (`dataverse`)

To install:
- Extensions sidebar (`Ctrl+Shift+X`) → search `@agentPlugins dataverse` → Install
- Or: Command Palette → `Chat: Install Plugin From Source` → `https://github.com/microsoft/Dataverse-skills`

## Step 4: Run Setup

Once the plugin is installed, say:

> "Set up this repo"

The `setup-client-repo` skill will guide you through filling in `environment-config.json`, creating GitHub environments and variables, configuring OIDC credentials, and setting up branch protection.
