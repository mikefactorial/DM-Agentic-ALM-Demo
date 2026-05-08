# src/controls

PCF (PowerApps Component Framework) TypeScript controls — custom UI components for model-driven apps and canvas apps. One subfolder per solution area.

## What Lives Here

```
src/controls/
  acm_AcmePlatform/
    MyFieldControl/
      MyFieldControl/
        index.ts               # Control implementation — implements StandardControl or VirtualControl
        ControlManifest.Input.xml  # Declares properties, resources, and data types
        package.json
        tsconfig.json
        __tests__/
      node_modules/
      package.json
```

## What PCF Controls Are

PCF controls replace or augment the default rendering of a field or dataset on a model-driven app form. They are TypeScript components that implement a Dataverse-defined interface — the platform calls `init`, `updateView`, and `destroy` at lifecycle points.

Common uses:
- Custom field editors (date pickers, rich text, signature capture, color pickers)
- Dataset grids with custom sorting, filtering, or rendering
- React components embedded on forms (using the Virtual control model)
- Read-only visualizations (charts, maps, status indicators)

## Control Types

| Type | Use for | React support |
|------|---------|---------------|
| **Standard** (`StandardControl`) | Field-level, full DOM control | Manual (mount/unmount) |
| **Virtual** (`ReactControl`) | React components, no direct DOM access | Built-in — platform manages React tree |
| **Dataset** | Custom grids and galleries | Both patterns |

## Inner Loop: Developing PCF Controls

1. **Scaffold** — `scaffold-pcf-control` skill runs `pac pcf init`, sets up the manifest and testing harness, and wires the control into the solution `.cdsproj`.
2. **Develop locally** — `npm run start` launches the test harness at `localhost:8181` with hot reload. No Dataverse connection required.
3. **Push to dev** — `pac pcf push --publisher-prefix <prefix>` compiles and pushes the control to the dev environment for real-environment testing.
4. **Add to feature solution** — after pushing, go to make.powerapps.com and manually add the PCF control to your feature solution. **PCF controls are not auto-tracked.**
5. **Build for CI** — `Build-Controls.ps1` compiles all controls for the outer-loop build. Inner-loop never uses this script.
6. **Sync** — run `sync-solution` to capture the control component entry in solution XML.

## Key Manifest Properties

- `name` — control namespace.class, must be globally unique
- `display-name-key` — localized display name
- `of-type-group` / `of-type` — what field types this control can bind to
- `property` elements — inputs from the form; declare `usage="bound"` for two-way binding
- `resx` — localization files
- `css` / `code` — compiled output paths

## Testing

- **Local harness** — `npm run start` — fastest feedback loop, no Dataverse needed
- **Jest unit tests** — `npm test` — test logic independently of the harness
- **In-environment** — add the control to a form field in the dev environment and test with real data
- The `validate-pull-request.yml` workflow runs `Build-Controls.ps1` for changed controls on PR

## Agent Commands

| Task | Say to agent |
|------|--------------|
| Create a new PCF control | `"scaffold a PCF field control"` or `"scaffold a React dataset grid"` |
| Push control to dev | run `pac pcf push --publisher-prefix <prefix>` in terminal |
| Build and validate | `"build the solution"` |
