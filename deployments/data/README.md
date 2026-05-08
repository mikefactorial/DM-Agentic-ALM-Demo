# deployments/data

Configuration and reference data imported into each environment after solution deployment. Managed via `pac data export` / `pac data import` — never through Power Automate flows, Dataverse Web API scripts, or manual entry.

## What Lives Here

```
deployments/data/
  acm_AcmePlatform/
    data.xml            # Records exported from the source environment
    data_schema.xml     # Schema describing which entities and fields to include
```

One subfolder per solution that has configuration data, named after the solution.

## What Config Data Is For

Config data seeds or synchronizes reference records that your solution depends on — records that must exist before the solution works correctly in a fresh environment. Examples:

- Environment-specific configuration records (API endpoints, feature flags, admin settings)
- Reference/lookup data (categories, status codes, product types)
- Default workflow input records
- Admin-created records that cannot be shipped in the solution itself

This is distinct from **user data** (generated at runtime) and **transactional data** (imported separately). Config data travels with every deployment.

## How It Works

The Package Deployer imports config data automatically after solution import. The import order is controlled by `deployments/package/Deployer/PkgAssets/ImportConfig.xml` — solutions and data files are listed there in the sequence they should be applied.

## Exporting Data from Dev

After creating or updating reference records in your dev environment, export them to source control:

```powershell
# Export using a schema you've already defined
pac data export `
    --schema-file deployments/data/acm_AcmePlatform/data_schema.xml `
    --data-file   deployments/data/acm_AcmePlatform/data.xml `
    --environment <dev-environment-url>
```

Commit both files to your feature branch. The data is imported on the next deployment.

## Creating a Schema for the First Time

If no schema exists yet for a solution, use the Configuration Migration Tool (CMT) to define which entities and records to include, then export the schema. The `manage-config-data` skill can walk you through this.

## Agent Commands

| Task | Say to agent |
|------|--------------|
| Set up config data for a solution | `"set up config data for the AcmePlatform solution"` |
| Export records from dev | `"export config data from dev"` |
| Import data into an environment | `"import config data to dev-test"` |