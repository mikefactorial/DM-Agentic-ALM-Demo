# Developer Guide — Feature Lifecycle Cheat Sheet

Quick reference for the full Power Platform ALM process, from starting a feature to deploying a release.

Each step shows two paths:
- **With agent skills** — describe the task in plain English to GitHub Copilot
- **Without agent skills** — run the GitHub Actions workflow or PowerShell script directly

---

## Workflow Map

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           INNER LOOP                                    │
│                                                                         │
│  [develop] ──► branch: feat/AB12345_Description                         │
│                  │                                                      │
│                  ▼                                                      │
│           1. Start feature ──► create feature solution in dev           │
│                  │                                                      │
│                  ▼                                                      │
│           2. Build & iterate in the Dataverse dev environment           │
│                  │                                                      │
│                  ▼                                                      │
│           3. Sync feature solution ──► commits to feature branch        │
│                  │                                                      │
│                  ▼                                                      │
│           4. Build + deploy to dev-test (managed)                       │
│                  │                                                      │
│                  ▼                                                      │
│           5. Test in dev-test                                            │
│                  │                                                      │
│                  ▼                                                      │
│           6. Promote feature ──► copies components into main solution   │
│              (dev → integration, or dev → dev if no integration env)    │
│                  │                                                      │
│                  ▼                                                      │
│           7. Sync main solution ──► sync/mainSolution-AB12345 branch    │
│                  │                    │                                 │
│                  │                    └──► PR to [develop] ◄────────┐  │
│                  │                                                   │  │
│                  └── (code-first only) ──────────────────────────────┘  │
│                    8. Code PR to [develop] via Create-FeatureCodePR.ps1 │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                           OUTER LOOP                                    │
│                                                                         │
│  [develop] ──────────────────────────────────────────────────────────► │
│                                                                         │
│           9.  PR: develop → main                                        │
│                  │                                                      │
│                  ▼                                                      │
│          10. Push to main triggers create-release-package.yml           │
│              ──► versioned .ppkg built, GitHub Release created          │
│                  │                                                      │
│                  ▼                                                      │
│          11. Manual: run deploy-package.yml ──► deploy to test / prod   │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Inner Loop

### Step 1 — Start a Feature

Creates a feature branch from `develop` and a feature solution in your dev Dataverse environment.

**With agent:**
> "Start a new feature for work item AB#12345 — {brief description}"

**Without agent:**

1. Create the branch:
   ```powershell
   git checkout develop
   git pull
   git checkout -b feat/AB12345_BriefDescription
   ```

2. Create a feature solution in the [Power Apps maker portal](https://make.powerapps.com) in your dev environment:
   - Name: `{Prefix}_{SolutionName}_AB12345` (e.g. `acm_AcmePlatform_AB12345`)
   - Set it as your **preferred solution**

---

### Step 2 — Build in Dev

Work directly in the Dataverse dev environment using the maker portal. All changes go into your feature solution (set as preferred solution in the maker portal — top-right **Settings → Advanced settings**).

> PCF controls and plugins are **not auto-tracked** — add them to your preferred solution manually after building.

---

### Step 3 — Sync Feature Solution to Branch

Exports your feature solution from dev and unpacks it to `src/solutions/{featureSolution}/` on your feature branch.

**With agent:**
> "Sync my feature solution AB12345 from dev"

**Without agent — GitHub Actions:**

Run **Actions → Sync Solution** (`sync-solution.yml`) with:
| Input | Value |
|-------|-------|
| Solution name | your feature solution unique name |
| Source environment | your dev environment slug (e.g. `myapp-dev`) |
| Branch name | your feature branch (e.g. `feat/AB12345_BriefDescription`) |
| Commit message | `chore(AB12345): sync feature solution` |

**Without agent — local script:**
```powershell
.platform/.github/workflows/scripts/Sync-Solution.ps1 `
    -solutionName "{featureSolution}" `
    -environmentUrl "{devEnvUrl}" `
    -commitMessage "chore(AB12345): sync feature solution AB#12345" `
    -branchName "feat/AB12345_BriefDescription"
```

---

### Step 4 — Build and Deploy to Dev-Test

Builds any code-first components (plugins, PCF controls), packages the feature solution, and deploys it as managed to your dev-test environment. Always sync first (Step 3) before deploying.

**With agent:**
> "Deploy my feature solution AB12345 to dev-test"

**Without agent — GitHub Actions:**

Run **Actions → Sync, Build and Deploy Solution** (`sync-build-deploy-solution.yml`) with:
| Input | Value |
|-------|-------|
| Solution name | your feature solution unique name |
| Source environment | dev environment slug |
| Target environment | dev-test environment slug (e.g. `myapp-dev-test`) |
| Branch name | your feature branch |

---

### Step 5 — Test in Dev-Test

Validate the feature in the dev-test environment. When satisfied, proceed to promote.

---

### Step 6 — Promote Feature to Integration

Exports the feature solution from dev, imports it to the integration environment (or back to dev if no dedicated integration environment is configured), and copies its components into the main solution.

**With agent:**
> "Promote feature solution AB12345 to integration"

**Without agent — GitHub Actions:**

Run **Actions → Promote Solution** (`promote-solution.yml`) with:
| Input | Value |
|-------|-------|
| Source environment | dev environment slug (e.g. `myapp-dev`) |
| Target environment | integration environment slug (e.g. `myapp-integration`); use dev slug if no integration env |
| Source solution name | your feature solution unique name |
| Target solution name | main solution unique name (e.g. `acm_AcmePlatform`) |
| Sync target solution | `true` to auto-sync after promote |
| Sync branch name | `sync/acm_AcmePlatform-AB12345` |
| Sync commit message | `chore(acm_AcmePlatform): sync after promoting AB12345 to integration AB#12345` |

**Without agent — local script:**
```powershell
.platform/.github/workflows/scripts/Promote-Solution.ps1 `
    -Phase All `
    -sourceSolutionName "{featureSolution}" `
    -targetSolutionName "{mainSolution}" `
    -sourceEnvironmentUrl "{devEnvUrl}" `
    -targetEnvironmentUrl "{integrationEnvUrl}"
```

---

### Step 7 — Open Sync PR to Develop

After promote, the main solution in integration has the new components. Sync it to a branch and open a PR to `develop`.

**With agent:**
> "Open a sync PR for the main solution after promoting AB12345"

**Without agent:**

```powershell
$syncBranch = "sync/{mainSolution}-AB12345"
git checkout develop && git pull
git checkout -b $syncBranch

.platform/.github/workflows/scripts/Sync-Solution.ps1 `
    -solutionName "{mainSolution}" `
    -environmentUrl "{integrationEnvUrl}" `
    -commitMessage "chore({mainSolution}): sync after promoting AB12345 to integration AB#12345" `
    -branchName $syncBranch

git push origin $syncBranch
gh pr create --base develop --head $syncBranch `
    --title "chore({mainSolution}): sync after promoting AB12345 to integration" `
    --body "Automated sync after promoting feature AB12345."
```

> Merge this PR once reviewed. This is the only PR that should touch `src/solutions/{mainSolution}/`.

---

### Step 8 — Code-First PR to Develop (if needed)

If your feature includes plugins, PCF controls, deployment settings changes, or config data — open a separate PR from your feature branch to `develop`. The `check-feature-solution-files` guard will block the PR if it contains `src/solutions/{featureSolution}/`, so use the script that strips those folders automatically.

**With agent:**
> "Open a code PR for feature AB12345 to develop"

**Without agent:**
```powershell
.platform/.github/workflows/scripts/Create-FeatureCodePR.ps1 `
    -featureSolutionName "{featureSolution}" `
    -baseBranch "develop"
```

> **Do NOT open a PR directly from your feature branch.** The feature solution folder is a build artifact and must never enter `develop` or `main`. `Create-FeatureCodePR.ps1` strips it automatically.

---

## Outer Loop

### Step 9 — Merge Develop to Main (Release)

When `develop` is stable and ready to release, open a PR from `develop` to `main`.

```
gh pr create --base main --head develop \
    --title "release: {version} — {description}"
```

> Only `develop` and `hotfix/*` branches can merge to `main` (enforced by `check-source-branch.yml`).

---

### Step 10 — Release Package Builds Automatically

Merging to `main` triggers `create-release-package.yml` automatically. This:
- Calculates the version (`YYYY.MM.DD.N` from git tags)
- Builds all plugin assemblies and PCF controls
- Packages all solutions into a `.ppkg` Package Deployer package
- Generates deployment settings for all target environments
- Creates a GitHub Release with all artifacts attached

No manual action needed.

---

### Step 11 — Deploy to Target Environments

Deploy the release package to test or production by running the workflow manually.

**With agent:**
> "Deploy the latest release to the test environment"

**Without agent — GitHub Actions:**

Run **Actions → Deploy Package** (`deploy-package.yml`) with:
| Input | Value |
|-------|-------|
| Target environment | environment slug (e.g. `myapp-test`) |
| Release tag | the GitHub release tag (e.g. `2026.05.03.1`) |

---

## Quick Reference — Workflows

| Workflow | When to run | Manual dispatch |
|----------|-------------|-----------------|
| `sync-solution.yml` | After making changes in Dataverse | ✅ |
| `build-deploy-solution.yml` | Deploy a solution to an environment | ✅ |
| `sync-build-deploy-solution.yml` | Sync + build + deploy in one step | ✅ |
| `promote-solution.yml` | Promote feature → integration | ✅ |
| `create-release-package.yml` | Build release package | Auto (push to `main`) |
| `deploy-package.yml` | Deploy release to test/prod | ✅ |
| `validate-pull-request.yml` | Validate PR changes | Auto (PR to any branch) |
| `check-feature-solution-files.yml` | Guard develop/main from feature solution files | Auto (PR to `develop`/`main`) |
| `check-source-branch.yml` | Enforce branch merge rules | Auto (PR to `main`) |

---

## Quick Reference — Branch Naming

| Type | Pattern | Branch from | Merges to |
|------|---------|-------------|-----------|
| Feature | `feat/AB12345_Description` | `develop` | `develop` (via sync PR + code PR) |
| Bug fix | `fix/AB12345_Description` | `develop` | `develop` |
| Solution sync | `sync/{mainSolution}-AB12345` | `develop` | `develop` |
| Hotfix | `hotfix/AB12345` | `main` | `main` + `develop` |
| Chore / docs | `chore/AB12345_Description` | `develop` | `develop` |

---

## Quick Reference — Commit Trailers

| Tracking system | Trailer format | Example |
|----------------|----------------|---------|
| Azure Boards (default) | `AB#12345` | `feat(acm_AcmePlatform): add approval flow AB#12345` |
| GitHub Issues | `Closes #12345` | `feat(acm_AcmePlatform): add approval flow Closes #12345` |

Check `trackingSystem` in `deployments/settings/environment-config.json` to confirm which applies to this repo.
