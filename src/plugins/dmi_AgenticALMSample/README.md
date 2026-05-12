# dmi_AgenticALMSample Plugins

Plugin solution scaffold for conference demo implementation.

## Projects

- DynamicMinds.Plugins.AgenticALMSample.Core
  - PluginBase and shared execution context abstractions
  - EnvironmentVariableService helper for Dataverse environment variables
- DynamicMinds.Plugins.AgenticALMSample.FirstContact
  - ProcessFirstContactSignalPlugin baseline handler

## Build

From this folder:

dotnet build DynamicMinds.AgenticALMSample.Plugins.sln

## Notes

- Projects target net462 and include Microsoft.PowerApps.MSBuild.Plugin.
- Assemblies are configured to sign when snk/DynamicMinds.AgenticALMSample.snk exists.
- Sprint 2 replaces rule-based plugin logic with managed identity Azure OpenAI callout.
