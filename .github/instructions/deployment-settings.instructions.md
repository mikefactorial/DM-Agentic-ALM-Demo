---
applyTo: "deployments/**"
---

# Deployment Settings & Configuration Data

## Two Configuration Mechanisms

| Mechanism | When Applied | What It Configures | PAC Command |
|-----------|-------------|-------------------|-------------|
| Deployment Settings | During solution import | Connection references, environment variables | `--settings-file` or `--settings` |
| Configuration Data | After deployment (post-deploy hook) | Reference/lookup table records | `pac data import` |

## Directory Structure

```
deployments/
├── settings/
│   ├── templates/                          # Auto-generated from sync (DO NOT edit)
│   │   └── {solution}_template.json        # One per solution area
│   ├── connection-mappings.json            # Connector → Connection ID per env
│   ├── environment-variables.json          # Env variable values per env
│   └── environment-config.json             # Package groups + environment list
└── data/
    └── {solution}/                         # One per solution area (if config data exists)
        ├── ConfigData.xml
        └── config-data/
            ├── data.xml
            ├── data_schema.xml
            └── [Content_Types].xml
```

---

## Deployment Settings Pipeline

### Template Files (`templates/`)

Auto-generated during solution sync. Structure:

```json
{
  "ConnectionReferences": [
    {
      "LogicalName": "{solutionPrefix}_MicrosoftDataverse{solutionPrefix}_{SolutionName}{Publisher}",
      "ConnectionId": "",
      "ConnectorId": "/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps"
    }
  ],
  "EnvironmentVariables": [
    {
      "SchemaName": "slp_RootURL",
      "Value": ""
    }
  ]
}
```

**NEVER edit template files manually** — they are regenerated on every sync. Edit the mapping files instead.

### connection-mappings.json

Maps connector types to connection IDs per environment:

```json
{
  "environments": {
    "{envPrefix}-test": {
      "/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps": "shared-commondataser-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
      "/providers/Microsoft.PowerApps/apis/shared_office365": "shared-office365-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
    },
    "{envPrefix}-prod": {
      "/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps": "...",
      "/providers/Microsoft.PowerApps/apis/shared_office365": "..."
    }
  }
}
    }
  }
}
```

Connection IDs are 32-character hex strings (no hyphens) or GUID format with hyphens. Get them via:

```powershell
pac connection list --environment https://your-org.crm.dynamics.com
```

### environment-variables.json

Per-environment values for solution environment variables:

```json
{
  "environments": {
    "{envPrefix}-test": {
      "{solutionPrefix}_RootURL": "https://{envPrefix}-test.crm.dynamics.com",
      "{solutionPrefix}_SomeVariable": "value-for-test"
    },
    "{envPrefix}-prod": {
      "{solutionPrefix}_RootURL": "https://{envPrefix}-prod.crm.dynamics.com",
      "{solutionPrefix}_SomeVariable": "value-for-prod"
    }
  },
  "metadata": {
    "{solutionPrefix}_RootURL": {
      "displayName": "Root URL",
      "schemaName": "{solutionPrefix}_RootURL",
      "type": "String",
      "description": "..."
    }
  }
}
```

Variable schema names use the solution prefix (e.g., `{solutionPrefix}_VariableName`). Add one entry per variable per environment. The `metadata` section is for documentation only and is not used by the deployment scripts.

### environment-config.json

Defines all deployment environments and package groups. Read from `environment-config.json` at runtime — do not hardcode environment counts or package group names in scripts or instructions.
```

### Build Merge Process

During solution build, `Generate-DeploymentSettings.ps1` merges template + config → per-environment settings:

```
template/{solution}_template.json
  + connection-mappings.json[environment]
  + environment-variables.json[environment]
  → {solution}_{version}_{environment}_settings.json
```

Script parameters:

```powershell
Generate-DeploymentSettings.ps1 `
    -solutionName "{solutionPrefix}_{solutionName}" `
    -targetEnvironment "{envPrefix}-test" `
    -templatePath "deployments/settings/templates/{solutionPrefix}_{solutionName}_template.json" `
    -outputPath "./artifacts/{solutionPrefix}_{solutionName}_1.0_{envPrefix}-test_settings.json" `
    -configPath "./deployments/settings"
```

> Derive `solutionPrefix`, `solutionName`, and `envPrefix` from `environment-config.json`.

### Deploy-Time Settings Application

**Solution import** (inner loop via `Deploy-Solutions.ps1`):

```powershell
pac solution import --path solution.zip --settings-file settings.json
```

**Package deploy** (outer loop via `Deploy-Package.ps1`):

Settings are transformed from JSON to pipe-delimited format:

```
key1=value1|key2=value2|key3=value3
```

Passed via: `pac package deploy --package pkg.zip --settings "key1=value1|key2=value2"`

### Validation

```powershell
Validate-DeploymentSettings.ps1 `
    -SolutionList "{solutionPrefix}_{solutionName}" `
    -TargetEnvironmentList "{envPrefix}-test"
```

> Derive values from `solutionAreas[]` and `environments[]` in `environment-config.json`.

Validates that all connection references have IDs and all environment variables have values for each solution/environment combination.

---

## Configuration Data Pipeline

### Purpose

Reference and lookup data (e.g., test method codes, report templates, instrument configs) that must be consistent across environments. Imported **after** solution deployment via post-deploy hooks.

### Structure

```
deployments/data/{solution}/
├── ConfigData.xml              # Root manifest
└── config-data/
    ├── data.xml                # Record data
    ├── data_schema.xml         # Schema definition
    └── [Content_Types].xml     # Content type metadata
```

### Export Data

```powershell
pac data export `
    --environment https://your-env.crm.dynamics.com/ `
    --schemaFile deployments/data/{solutionPrefix}_{solutionName}/config-data/data_schema.xml `
    --dataFile deployments/data/{solutionPrefix}_{solutionName}/config-data/data.xml
```

### Import Data

```powershell
pac data import `
    --environment https://your-env.crm.dynamics.com/ `
    --data deployments/data/{solutionPrefix}_{solutionName}/config-data
```

> Derive `solutionPrefix` and `solutionName` from `solutionAreas[]` in `environment-config.json`.

### Post-Deploy Hook

`Post-Deploy-Hook.ps1` automatically imports configuration data after each solution deployment. It looks for data in `deployments/data/{solution}/config-data/` and runs `pac data import` if data exists.

---

## Common Operations

### Adding a New Environment Variable

1. Add the variable definition to your solution in Dataverse (make.powerapps.com)
2. Sync the solution to the repo — this updates the template file in `templates/`
3. Add values for all target environments in `environment-variables.json`
4. Add metadata entry in the `metadata` section of `environment-variables.json`
5. Commit both the updated template and `environment-variables.json`

### Adding a New Connection Reference

1. Add the connection reference to your solution in Dataverse
2. Sync the solution — template updated automatically
3. Get connection IDs for each environment:
   ```powershell
   pac connection list --environment https://your-env.crm.dynamics.com
   ```
4. Add connector type → connection ID mapping per environment in `connection-mappings.json`
5. Commit both the updated template and `connection-mappings.json`

### Adding Configuration Data

1. Export from source environment:
   ```powershell
   pac data export --environment <url> --schemaFile <schema> --dataFile <data>
   ```
2. Place files under `deployments/data/{solution}/config-data/`
3. Test locally: `pac data import --environment <url> --data <folder>`
4. Commit alongside solution changes

### Adding a New Environment

1. Add the environment name to `environments` array in `environment-config.json`
2. Add connection mappings for the new environment in `connection-mappings.json`
3. Add environment variable values in `environment-variables.json`
4. Create the GitHub environment (repository settings) with:
   - `DATAVERSE_URL` variable
   - `DATAVERSE_CLIENT_ID` variable
   - Approval rules (if test/prod tier)
5. Update workflow choice lists if the environment should be dispatch-selectable

## Critical Rules

1. **NEVER edit template files** in `templates/` — they are auto-generated during sync
2. **Always populate all 9 environments** when adding new connection references or variables
3. **Build fails** if connection references have empty/placeholder IDs
4. **Build warns** (non-fatal) for missing environment variable values
5. **Config data import order** is alphabetical by folder name — use numbered prefixes for dependencies
6. **Connection IDs** can be GUID format or 32-char hex without hyphens — both are valid
7. **Environment prefix mapping**: Use `solutionAreas[x].prefix` to match variables to their environment slugs (e.g., `acm_` variables → `acmecorp-*` environments)
