# deployments/settings

Deployment configuration — the single source of truth for environment topology, solution identifiers, credentials references, connection mappings, and environment variables. Everything the ALM workflows and skills need to run is derived from the files here.

## Files

| File | Purpose |
|------|---------|
| `environment-config.json` | Master project config — environments, solutions, GitHub repo, publisher, package groups |
| `environment-variables.json` | Dataverse environment variable values per deployment environment |
| `connection-mappings.json` | Connection reference mappings per deployment environment |
| `templates/` | Auto-generated deployment settings files (from `sync-solution`) — do not edit directly |

---

## environment-config.json

The central configuration file. Every skill and workflow reads from this — nothing is hardcoded.

### Key Sections

**Project identity**
```json
{
  "clientName": "Acme Corp Platform",
  "productDescription": "Power Platform solution for Acme Corp",
  "publisher": "AcmeCorp",
  "githubOrg": "AcmeCorp",
  "repoName": "AcmeCorp-Platform",
  "packageTag": "AcmePlatform",
  "trackingSystem": "azureBoards"
}
```

**Solution areas** — one entry per Dataverse solution in this repo
```json
"solutionAreas": [
  {
    "name": "AcmePlatform",
    "prefix": "acm",
    "mainSolution": "acm_AcmePlatform",
    "cdsproj": "src/solutions/acm_AcmePlatform/acm_AcmePlatform.cdsproj",
    "pluginsPath": "src/plugins/acm_AcmePlatform",
    "pluginsSln": "src/plugins/acm_AcmePlatform/AcmeCorp.AcmePlatform.Plugins.sln",
    "devEnv": "acme-dev",
    "integrationEnv": "acme-integration"
  }
]
```

**Inner loop environments** — dev and integration, used for daily development
```json
"innerLoopEnvironments": [
  { "slug": "acme-dev",         "url": "https://org-dev12345.crm.dynamics.com/" },
  { "slug": "acme-integration", "url": "https://org-int67890.crm.dynamics.com/" }
]
```

**Deployment environments** — dev-test, test, prod — targets for managed deployments
```json
"environments": [
  { "slug": "acme-dev-test", "url": "https://org-dvt11111.crm.dynamics.com/" },
  { "slug": "acme-test",     "url": "https://org-tst22222.crm.dynamics.com/" },
  { "slug": "acme-prod",     "url": "https://org-prd33333.crm.dynamics.com/" }
]
```

**Package groups** — which solutions go together in a release package, and which environments receive it
```json
"packageGroups": [
  {
    "name": "AcmePlatform",
    "solutions": ["acm_AcmePlatform"],
    "dataSolution": "acm_AcmePlatform",
    "environments": ["acme-dev-test", "acme-test", "acme-prod"]
  }
]
```

### Environment Topology

The full environment set is:

| Environment | Type | Inner/Outer | Purpose |
|-------------|------|-------------|---------|
| Dev | Unmanaged | Inner | Developer sandbox — feature solutions created and iterated here |
| Integration | Unmanaged | Inner | Assembly point — promoted feature components assembled before release |
| Dev-Test | Managed | Outer (inner loop validation) | Validates individual features as managed before integration |
| Test / UAT | Managed | Outer | User acceptance testing; receives release packages |
| Production | Managed | Outer | Live system; receives release packages after UAT sign-off |

Dev-test and integration are optional — the minimum viable topology is Dev + Test + Production.

---

## environment-variables.json

Dataverse **environment variable** values per deployment environment. Environment variables in Dataverse are solution components that store configuration values — connection strings, API keys, feature flags — that differ between environments.

```json
{
  "acme-dev-test": {
    "acm_ApiBaseUrl": "https://api.staging.example.com",
    "acm_FeatureFlag_NewUI": "false"
  },
  "acme-prod": {
    "acm_ApiBaseUrl": "https://api.example.com",
    "acm_FeatureFlag_NewUI": "true"
  }
}
```

The `Generate-DeploymentSettings.ps1` script reads this file to produce the per-environment settings files that Package Deployer uses during deployment. Values are never embedded in the solution ZIP.

---

## connection-mappings.json

Maps connection reference schema names to the connection ID that should be used in each deployment environment. Connection references are solution components — the actual connection (with credentials) lives in the environment and is referenced by ID.

```json
{
  "acme-dev-test": {
    "acm_SharedDataverse": "/providers/Microsoft.PowerApps/apis/shared_commondataservice/connections/abc123"
  },
  "acme-prod": {
    "acm_SharedDataverse": "/providers/Microsoft.PowerApps/apis/shared_commondataservice/connections/xyz789"
  }
}
```

Find connection IDs in Power Automate → Data → Connections → select a connection → the ID is in the URL.

---

## templates/

Auto-generated deployment settings templates produced during `sync-solution`. Each template maps to a deployment environment and is populated with actual values from `environment-variables.json` and `connection-mappings.json` at deploy time.

**Do not edit these files directly.** Re-run `sync-solution` to regenerate them after adding new environment variables or connection references to your solution.

---

## Adding a New Environment Variable

1. Add the schema name to `environment-variables.json` with values for each deployment environment
2. Run `sync-solution` to regenerate settings templates
3. In your dev environment, set the environment variable's current value for testing
4. On the next deployment, Package Deployer will set the correct value per environment automatically

## Adding a New Connection Reference

1. Add the schema name to `connection-mappings.json` with the connection ID per environment
2. Run `sync-solution` to regenerate settings templates
3. The `deploy-solution` skill handles injecting connection mappings during inner-loop imports
