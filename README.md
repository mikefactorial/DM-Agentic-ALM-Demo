# {{CLIENT_NAME}} Platform Repository

{{PRODUCT_DESCRIPTION}}

---

<!-- TEMPLATE INFO — delete this section after setup is complete -->

## About This Template

This repository contains a moderately opinionated implementation of ALM for Power Platform and GitHub using workflows and Copilot agent skills. It's intended to be a starting point for Power Platform projects that use code-first components in the platform and want to leverage agentic workflows for development and ensure those agents follow best practices for ALM and development.

The template provides:
- **Agentic setup and development** — a GitHub Copilot plugin (`power-platform-alm`) with skills that automate the full ALM lifecycle in plain English: start features, sync solutions, deploy, promote, release
- **Feature solution isolation** — each work item gets its own Dataverse feature solution; components promote to integration when validated, keeping the main solution always releasable
- **OIDC authentication** — GitHub Actions authenticates to Dataverse without stored secrets using federated identity credentials on Azure AD app registrations
- **Package Deployer outer loop** — releases are versioned `.ppkg` packages that carry all solutions, deployment settings, and configuration data, deployed atomically across environments
- **Thin caller CI/CD** — all workflow logic lives in [Agentic-ALM-Workflows](https://github.com/mikefactorial/Agentic-ALM-Workflows) (the `.platform` submodule); this repo contains only `on:` triggers and `uses:` references

### Documentation

| Area | README |
|------|--------|
| **Feature lifecycle cheat sheet (start → release)** | **[DEVELOPER-GUIDE.md](DEVELOPER-GUIDE.md)** |
| Solution metadata and the inner ALM loop | [src/solutions/README.md](src/solutions/README.md) |
| Plugin development and registration | [src/plugins/README.md](src/plugins/README.md) |
| PCF control development | [src/controls/README.md](src/controls/README.md) |
| Environments, environment variables, connection references | [deployments/settings/README.md](deployments/settings/README.md) |
| Configuration and reference data | [deployments/data/README.md](deployments/data/README.md) |
| Package Deployer project (outer loop) | [deployments/package/README.md](deployments/package/README.md) |
| First-time setup | [SETUP.md](SETUP.md) |

<!-- END TEMPLATE INFO -->

---

## Quick Start

See [SETUP.md](SETUP.md) for initial configuration steps after cloning this template.

## Repository Structure

```
src/
  controls/     # PCF (PowerApps Component Framework) controls
  plugins/      # .NET plugin assemblies
  solutions/    # Unpacked Dataverse solution metadata (.cdsproj)
deployments/
  settings/     # Deployment configuration (environment-config.json, mappings, etc.)
  data/         # Configuration data for post-deploy import
.github/
  workflows/    # GitHub Actions thin-caller workflows
  instructions/ # Copilot coding instructions
```

## Branching Strategy

```
main (production-ready, protected)
 ↑ PR from develop or hotfix/* only
develop (integration branch)
 ↑ PR from feature branches / promote commits
feature/AB<N>_Description   (branch from develop)
hotfix/<issue-number>        (branch from main → merge to both main + develop)
```

## CI/CD

All workflows are **thin callers** — they delegate to
`mikefactorial/Agentic-ALM-Workflows`
and only contain `on:` triggers and `uses:` references.
Scripts and reusable jobs live in Agentic-ALM-Workflows.

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `sync-solution.yml` | Manual | Export solution from Dataverse to repo |
| `build-deploy-solution.yml` | Manual | Build → Deploy (inner loop, no sync) |
| `sync-build-deploy-solution.yml` | Manual | Sync → Build → Deploy |
| `Promote-Solution.yml` | Manual | promote dev → integration |
| `deploy-package.yml` | Manual | Deploy release package to an environment |
| `deploy-solutions.yml` | Manual / after release | Deploy individual solutions from a release |
| `create-release-package.yml` | Push to `main` / manual | Build release packages + create GitHub Release |
| `pr-validation.yml` | Pull request | Build and validate changed components |
| `check-source-branch.yml` | Pull request | Enforce branch policies |

## Credits

- [AlbanianXrm.CDSProj.Sdk](https://github.com/AlbanianXrm/CDSProj.Sdk) — SDK-style MSBuild project support for Dataverse solution projects including plugin packages and managed identities (`.cdsproj`)
