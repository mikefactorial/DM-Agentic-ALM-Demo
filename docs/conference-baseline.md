# Conference Baseline (Work In Progress)

This document tracks the baseline state that must be in place before the conference session.

## Scope

- Solution: dmi_AgenticALMSample
- Story: Galactic First Contact Desk
- Environments:
  - Dev: dmi-dev
  - Test: dmi-test
- Session format:
  - Manual ALM path
  - Agentic ALM path

## Baseline Components

- Dataverse table and columns for first-contact missions
- Model-driven app shell with core views/forms
- Plugin Custom API processing path
- PCF field control for mission inference display
- Environment variable definitions for runtime settings
- Curated sample data (10-20 records)

## Current Status

- [x] Feature implementation branch created: feat/GH200_BaselineSolutionBuild
- [x] Plugin solution scaffolded under src/plugins/dmi_AgenticALMSample
- [ ] Dataverse table/forms/views created in dmi-dev
- [ ] Model-driven app shell created and published
- [ ] Feature solution synced to source via sync-solution workflow
- [ ] Managed deploy validated in dmi-test
- [ ] Baseline release package generated from main

## Workflow Run Artifacts (fill during execution)

- sync-solution:
- build-deploy-solution:
- create-release-package:
- deploy-package:

## Validation Checklist

- [ ] App opens in dmi-test
- [ ] Mission records can be created/updated
- [ ] Plugin path is registered and callable
- [ ] PCF control renders expected output state
- [ ] One golden mission record demonstrates expected output delta
