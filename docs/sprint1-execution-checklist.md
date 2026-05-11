# Sprint 1 Execution Checklist

This checklist is optimized for the first implementation block (90 minutes).

## 0-10 Minutes: Feature Lane Setup

1. Confirm current branch is feat/GH200_BaselineSolutionBuild.
2. In make.powerapps.com (dmi-dev):
   - Create feature solution for baseline work.
   - Set it as preferred solution.
3. Confirm solution components are being created under preferred solution.

## 10-40 Minutes: Maker Baseline Assets

1. Create first-contact table and core fields.
2. Add choice columns for status/priority.
3. Build main and quick-create forms.
4. Create at least two views (all + triage-focused).
5. Publish customizations.

## 40-55 Minutes: Sync to Source

Dispatch workflow:
- file: .github/workflows/sync-solution.yml
- environment: dmi-dev
- solution_name: dmi_AgenticALMSample
- commit_message: chore: sync baseline sprint 1 metadata
- branch_name: feat/GH200_BaselineSolutionBuild
- publish_customizations: true
- create_pr: false

Verify:
- src/solutions/dmi_AgenticALMSample metadata changed
- deployments/settings/templates/dmi_AgenticALMSample_template.json updated if needed

## 55-75 Minutes: Managed Build + Deploy to Test

Dispatch workflow:
- file: .github/workflows/build-deploy-solution.yml
- solution_name: dmi_AgenticALMSample
- target_environments: dmi-test
- use_upgrade: false

Verify in dmi-test:
- App loads
- Table views/forms work
- Choice columns save correctly

## 75-90 Minutes: Lock Evidence + Prepare Sprint 2

1. Capture run URLs/screenshots.
2. Record blockers in conference-baseline.md.
3. Queue Sprint 2 tasks:
   - Custom API metadata
   - Plugin registration path
   - Managed identity wiring
