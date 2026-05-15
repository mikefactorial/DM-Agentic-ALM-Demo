# Dynamic Minds Agentic ALM Demo

A reference Power Platform solution that demonstrates **agentic ALM** for code-first Dataverse components. The app itself is a space-mission signal-analysis system: incoming transmissions are analysed by Azure OpenAI (accessed via Dataverse Managed Identity — no secrets), and the results are surfaced in a custom PCF card on the form.

---

## What's in this repo

### Solution — `dmi_AgenticALMSample`

| Component type | Name | Description |
|---------------|------|-------------|
| **Table** | `dmi_FirstContact` (First Contact) | Records an incoming signal transmission. A plugin fires on Create to call Azure OpenAI and write back the inference results. |
| **Table** | `dmi_signalscenario` (Signal Scenario) | Seed / reference data that drives test scenarios. Imported via Configuration Migration Tool during package deploy. |
| **Plugin package** | `DynamicMinds.Plugins.AgenticALMSample` | NuGet-based plugin package registered in Dataverse. Contains the `Core` base library and the `FirstContact` plugin assembly. |
| **Plugin** | `ProcessFirstContactSignalPlugin` | Post-operation Create on `dmi_FirstContact`. Reads `dmi_signaltranscript`, calls Azure OpenAI Chat Completions via `IManagedIdentityService`, and writes back `dmi_intent`, `dmi_priority`, and `dmi_actions`. |
| **PCF control** | `MissionInferenceCard` (`dmi`) | TypeScript field control bound to `intent` (text), `priority` (option set, colour-coded badge), and `actions` (semicolon-separated list). Displayed on the First Contact form. |
| **Environment variables** | `dmi_AzureOpenAIEndpoint`, `dmi_AzureOpenAIDeployment`, `dmi_FirstContactPromptTemplate` | Drive Azure OpenAI connectivity and the per-environment system prompt. |

### Azure dependencies

| Resource | Details |
|----------|---------|
| Azure OpenAI | `oai-dmdemo` (`https://oai-dmdemo.openai.azure.com/`) — `gpt-4o-mini` deployment |
| Managed Identity | App registration `756aac52-b051-4a6b-ab88-be75acc59959` (`PowerPlatform-Demo`), granted **Cognitive Services OpenAI User** on the Azure OpenAI resource. No client secrets — token acquired at runtime by `IManagedIdentityService`. |

### How it works end-to-end

```
User creates a First Contact record (dmi_FirstContact)
  └─▶ ProcessFirstContactSignalPlugin (post-operation, synchronous)
        ├─ Reads dmi_signaltranscript from the Target entity
        ├─ Fetches env vars: AzureOpenAIEndpoint, AzureOpenAIDeployment, FirstContactPromptTemplate
        ├─ Acquires bearer token via IManagedIdentityService (no secrets)
        ├─ Calls Azure OpenAI Chat Completions (JSON-mode)
        └─ Writes dmi_intent, dmi_priority, dmi_actions back to the record
              └─▶ MissionInferenceCard PCF control renders the results on the form
```

---

## Documentation

| Area | File |
|------|------|
| **Feature lifecycle (start → release)** | [DEVELOPER-GUIDE.md](DEVELOPER-GUIDE.md) |
| Solution metadata and inner ALM loop | [src/solutions/README.md](src/solutions/README.md) |
| Plugin development and registration | [src/plugins/README.md](src/plugins/README.md) |
| PCF control development | [src/controls/README.md](src/controls/README.md) |
| Environment variables and connection references | [deployments/settings/README.md](deployments/settings/README.md) |
| Configuration and reference data | [deployments/data/README.md](deployments/data/README.md) |
| Package Deployer project (outer loop) | [deployments/package/README.md](deployments/package/README.md) |

## Repository Structure

```
src/
  controls/
    dmi_AgenticALMSample/
      PCF-MissionInferenceCard/   # TypeScript PCF control — MissionInferenceCard
  plugins/
    dmi_AgenticALMSample/
      DynamicMinds.Plugins.AgenticALMSample.Core/       # Base PluginBase class, shared services
      DynamicMinds.Plugins.AgenticALMSample.FirstContact/ # ProcessFirstContactSignalPlugin + AzureOpenAIService
      DynamicMinds.Plugins.AgenticALMSample.FirstContact.Tests/
  solutions/
    dmi_AgenticALMSample/         # Unpacked solution metadata (.cdsproj)
      src/
        Entities/
          dmi_FirstContact/       # First Contact table
          dmi_signalscenario/     # Signal Scenario table
        pluginpackages/           # Plugin package registration
        SdkMessageProcessingSteps/ # Plugin step registrations
        Controls/                 # PCF control registration
        environmentvariabledefinitions/
deployments/
  settings/
    environment-config.json       # Central config (environments, managed identities, packages)
    environment-variables.json    # Per-environment variable values
    connection-mappings.json      # Connection reference mappings
    templates/                    # Auto-generated deployment settings templates (do not edit)
  data/
    dmi_AgenticALMSample/
      config-data/                # Signal Scenario seed records (CMT format)
  package/
    Deployer/                     # Package Deployer project (outer-loop deploy)
.github/
  workflows/                      # Thin-caller GitHub Actions workflows
  instructions/                   # Copilot coding instructions
.platform/                        # Agentic-ALM-Workflows submodule (scripts + reusable workflows)
```

## Branching Strategy

```
main (production-ready, protected)
 ↑ PR from develop or hotfix/* only
develop (integration branch)
 ↑ PR from feature branches / promote commits
feat/GH<N>_Description   (branch from develop, e.g. feat/GH5_SpaceMissionFirstContact)
fix/GH<N>_Description    (branch from develop)
hotfix/<issue-number>     (branch from main → merge to both main + develop)
```

Commit trailer format: `Closes #<N>` (GitHub Issues tracking).

## Environments

| Slug | Role | URL |
|------|------|-----|
| `dmi-dev` | Inner loop (dev + integration) | https://<dev>.crm.dynamics.com/ |
| `dmi-test` | Outer loop — test | https://<test>.crm.dynamics.com/ |
| `dmi-prod` | Outer loop — production | https://<prod>.crm.dynamics.com/ |

## CI/CD

All workflows are **thin callers** — they delegate to
[`mikefactorial/Agentic-ALM-Workflows`](https://github.com/mikefactorial/Agentic-ALM-Workflows)
(the `.platform` submodule) and only contain `on:` triggers and `uses:` references.
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
