using Microsoft.Xrm.Tooling.PackageDeployment.CrmPackageExtentionBase;
using Newtonsoft.Json;
using PlatformPackage.Models;
using PlatformPackage.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace PlatformPackage
{
    /// <summary>
    /// Import package starter frame.
    /// </summary>
    [Export(typeof(IImportExtensions))]
    public class PackageImportExtension : ImportExtension
    {
        #region Metadata

        /// <summary>
        /// Folder name where package assets are located in the final output package zip.
        /// </summary>
        public override string GetImportPackageDataFolderName => "PkgAssets";

        /// <summary>
        /// Name of the Import Package to Use
        /// </summary>
        /// <param name="plural">if true, return plural version</param>
        public override string GetNameOfImport(bool plural) => "Package Deployer";

        /// <summary>
        /// Long name of the Import Package.
        /// </summary>
        public override string GetLongNameOfImport => "Package Deployer";

        /// <summary>
        /// Description of the package, used in the package selection UI
        /// </summary>
        public override string GetImportPackageDescriptionText => "Package Deployer";

        #endregion

        private EnvironmentVariableService environmentVariableService;
        private ConnectionReferenceService connectionReferenceService;
        private ManagedIdentityConfigService managedIdentityConfigService;
        private EnvironmentSettingsService environmentSettingsService;
        private WorkflowService workflowService;
        private List<SolutionConfig> targetSolutions;
        private Dictionary<string, string> environmentVariables;
        private Dictionary<string, string> connectionReferences;

        private IDictionary<string, string> ConnectionReferences => connectionReferences ?? (connectionReferences = new Dictionary<string, string>());
        private IDictionary<string, string> EnvironmentVariables => environmentVariables ?? (environmentVariables = new Dictionary<string, string>());
        private IDictionary<string, string> EnvironmentSettings => GetSettings(Constants.EnvironmentSettingsPrefix);


        protected EnvironmentVariableService EnvironmentVariableService
        {
            get
            {
                if (environmentVariableService == null)
                {
                    environmentVariableService = new EnvironmentVariableService(ServiceClient, PackageLog);
                }
                return environmentVariableService;
            }
        }

        protected ConnectionReferenceService ConnectionReferenceService
        {
            get
            {
                if (connectionReferenceService == null)
                {
                    connectionReferenceService = new ConnectionReferenceService(ServiceClient, PackageLog);
                }
                return connectionReferenceService;
            }
        }

        protected WorkflowService WorkflowService
        {
            get
            {
                if (workflowService == null)
                {
                    workflowService = new WorkflowService(ServiceClient, PackageLog);
                }
                return workflowService;
            }
        }

        protected ManagedIdentityConfigService ManagedIdentityService
        {
            get
            {
                if (managedIdentityConfigService == null)
                {
                    managedIdentityConfigService = new ManagedIdentityConfigService(PackageLog);

                }
                return managedIdentityConfigService;
            }
        }

        protected EnvironmentSettingsService EnvironmentSettingService
        {
            get
            {
                if (environmentSettingsService == null)
                {
                    environmentSettingsService = new EnvironmentSettingsService(ServiceClient, PackageLog);
                }
                return environmentSettingsService;
            }
        }

        public List<SolutionConfig> TargetSolutions
        {
            get
            {
                if (targetSolutions == null)
                {
                    targetSolutions = new List<SolutionConfig>();

                    IEnumerable<string> zipFiles = Directory.GetFiles(PackageAssetsPath).Where(x => x.EndsWith(".zip"));

                    zipFiles.ToList().ForEach(
                        x =>
                        {
                            using (ZipArchive zipArchive = new ZipArchive(new MemoryStream(File.ReadAllBytes(x))))
                            {
                                ZipArchiveEntry solutionXmlFile = zipArchive.Entries.FirstOrDefault(y => y.Name == Constants.SolutionXmlFile);
                                if (solutionXmlFile != null && solutionXmlFile != default(ZipArchiveEntry))
                                {

                                    XDocument solutionXml = XDocument.Load(solutionXmlFile.Open());
                                    string solutionUniqueName = solutionXml.Root.Descendants(Constants.SolutionUniqueNameElement).FirstOrDefault()?.Value;
                                    string solutionVersion = solutionXml.Root.Descendants(Constants.SolutionVersionElement).FirstOrDefault()?.Value;

                                    // x is already a full path from Directory.GetFiles, no need to concatenate with PackageAssetsPath
                                    targetSolutions.Add(new SolutionConfig() { ZipPath = x, UniqueName = solutionUniqueName, Version = solutionVersion });
                                }

                            }
                        });
                }

                return targetSolutions;
            }
        }

        private string PackageAssetsPath => Path.Combine(CurrentPackageLocation, GetImportPackageDataFolderName);

        /// <summary>
        /// Called to Initialize any functions in the Custom Extension.
        /// </summary>
        /// <see cref="ImportExtension.InitializeCustomExtension"/>
        public override void InitializeCustomExtension()
        {
            PackageLog.Log("[InitializeCustomExtension] running");

            // Filter ImportConfig.xml to only the requested solutions (if specified).
            // Must run before the Package Deployer reads the file for its import queue.
            PackageLog.Log("Filtering solution import list");
            FilterImportConfig();

            // Apply Managed Identity Configurations (must run before import)
            PackageLog.Log("Applying Managed Identity Configurations");
            ApplyManagedIdentityConfigurations();

            // Load deployment settings from base64-encoded runtime settings
            PackageLog.Log("Loading Deployment Settings");
            LoadDeploymentSettings();

            // Apply Environment Settings
            PackageLog.Log("Applying Environment Settings");
            EnvironmentSettingService.UpdateOrganizationSettings(EnvironmentSettings);
        }

        /// <summary>
        /// Called before the Main Import process begins, after solutions and data.
        /// </summary>
        /// <see cref="ImportExtension.BeforeImportStage"/>
        /// <returns></returns>
        public override bool BeforeImportStage()
        {
            return true;
        }

        /// <summary>
        /// Raised before the named solution is imported to allow for any configuration settings to be made to the import process
        /// </summary>
        /// <see cref="ImportExtension.PreSolutionImport"/>
        /// <param name="solutionName">name of the solution about to be imported</param>
        /// <param name="solutionOverwriteUnmanagedCustomizations">Value of this field from the solution configuration entry</param>
        /// <param name="solutionPublishWorkflowsAndActivatePlugins">Value of this field from the solution configuration entry</param>
        /// <param name="overwriteUnmanagedCustomizations">If set to true, imports the Solution with Override Customizations enabled</param>
        /// <param name="publishWorkflowsAndActivatePlugins">If set to true, attempts to auto publish workflows and activities as part of solution deployment</param>
        public override void PreSolutionImport(string solutionName, bool solutionOverwriteUnmanagedCustomizations, bool solutionPublishWorkflowsAndActivatePlugins, out bool overwriteUnmanagedCustomizations, out bool publishWorkflowsAndActivatePlugins)
        {
            base.PreSolutionImport(solutionName, solutionOverwriteUnmanagedCustomizations, solutionPublishWorkflowsAndActivatePlugins, out overwriteUnmanagedCustomizations, out publishWorkflowsAndActivatePlugins);
        }

        /// <summary>
        /// Called during a solution upgrade when both solutions, old and new, are present in the system.
        /// This function can be used to provide a means to do data transformation or upgrade while a solution update is occurring.
        /// </summary>
        /// <see cref="ImportExtension.RunSolutionUpgradeMigrationStep"/>
        /// <param name="solutionName">Name of the solution</param>
        /// <param name="oldVersion">version number of the old solution</param>
        /// <param name="newVersion">Version number of the new solution</param>
        /// <param name="oldSolutionId">Solution ID of the old solution</param>
        /// <param name="newSolutionId">Solution ID of the new solution</param>
        public override void RunSolutionUpgradeMigrationStep(string solutionName, string oldVersion, string newVersion, Guid oldSolutionId, Guid newSolutionId)
        {
            base.RunSolutionUpgradeMigrationStep(solutionName, oldVersion, newVersion, oldSolutionId, newSolutionId);
        }

        /// <summary>
        /// Called After all Import steps are complete, allowing for final customizations or tweaking of the instance.
        /// </summary>
        /// <see cref="ImportExtension.AfterPrimaryImport"/>
        /// <returns></returns>
        public override bool AfterPrimaryImport()
        {
            PackageLog.Log("[AfterPrimaryImport] running");

            // Set environment variables
            foreach (var envVar in EnvironmentVariables)
            {
                PackageLog.Log($"Setting environment variable: {envVar.Key} to {envVar.Value}");
                EnvironmentVariableService.SetEnvironmentVariable(envVar.Key, envVar.Value);
            }

            // Set connection references
            PackageLog.Log($"Setting connection references");
            ConnectionReferenceService.SetConnectionReferences(ConnectionReferences);

            PackageLog.Log($"Calling GetWorkflowStatesBySolution - PackageAssetsPath: {PackageAssetsPath}");
            var workflowStates = WorkflowService.GetWorkflowStatesFromSolutions(PackageAssetsPath);

            if (workflowStates != null && workflowStates.Count > 0)
            {
                PackageLog.Log($"Workflows found: {workflowStates.Count}");
                // Process workflow status changes using batching
                WorkflowService.ProcessWorkflowStates(workflowStates);
            }
            else
            {
                PackageLog.Log("No workflows found to process.");
            }


            return true;

        }

        /// <summary>
        /// Reads the <c>target_solutions</c> runtime setting and removes any solution entries
        /// from <c>ImportConfig.xml</c> that are not in the requested set.
        ///
        /// Pass the setting to <c>pac package deploy</c> like so:
        /// <code>
        ///   pac package deploy --package Package.zip \
        ///     --environment https://org.crm.dynamics.com \
        ///     --runtimePackageSettings "target_solutions=MySolution1,MySolution2"
        /// </code>
        ///
        /// Omit the setting (or set it to empty/all) to deploy every solution in the package.
        /// </summary>
        private void FilterImportConfig()
        {
            // Resolve the value of the target_solutions runtime setting
            string rawValue = null;
            if (this.RuntimeSettings != null)
            {
                var match = this.RuntimeSettings
                    .FirstOrDefault(s => s.Key.Equals(Constants.TargetSolutionsSettingKey, StringComparison.OrdinalIgnoreCase));
                if (match.Key != null)
                    rawValue = match.Value?.ToString();
            }

            if (string.IsNullOrWhiteSpace(rawValue) || rawValue.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                PackageLog.Log("target_solutions not set or 'all' — importing every solution in the package.");
                return;
            }

            var requestedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in rawValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                requestedNames.Add(token.Trim());
            }

            PackageLog.Log($"Filtering import to solutions: {string.Join(", ", requestedNames)}");

            // Locate and modify ImportConfig.xml
            string importConfigPath = Path.Combine(PackageAssetsPath, "ImportConfig.xml");
            if (!File.Exists(importConfigPath))
            {
                PackageLog.Log($"ImportConfig.xml not found at {importConfigPath} — skipping filter.");
                return;
            }

            var doc = XDocument.Load(importConfigPath);
            var solutionsEl = doc.Root?.Element("solutions");
            if (solutionsEl == null)
            {
                PackageLog.Log("ImportConfig.xml has no <solutions> element — skipping filter.");
                return;
            }

            var toRemove = solutionsEl
                .Elements("configsolutionfile")
                .Where(el =>
                {
                    var filename = el.Attribute("solutionpackagefilename")?.Value;
                    if (string.IsNullOrEmpty(filename)) return false;
                    var solutionName = Path.GetFileNameWithoutExtension(filename);
                    return !requestedNames.Contains(solutionName);
                })
                .ToList();

            if (toRemove.Count == 0)
            {
                PackageLog.Log("All solutions in ImportConfig.xml are within the requested set — no changes needed.");
                return;
            }

            foreach (var el in toRemove)
            {
                var name = el.Attribute("solutionpackagefilename")?.Value ?? "(unknown)";
                PackageLog.Log($"  Excluding solution from import: {name}");
                el.Remove();
            }

            doc.Save(importConfigPath);

            int remaining = solutionsEl.Elements("configsolutionfile").Count();
            PackageLog.Log($"ImportConfig.xml updated. Remaining solutions to import: {remaining}");
        }

        /// <summary>
        /// Applies managed identity configurations by deserializing base64-encoded JSON settings and updating solution files.
        /// Expected settings format: {solutionname}_managedidentities={base64_json_array}
        /// JSON structure: [{"name":"...", "applicationId":"...", "tenantId":"...", "solutionName":"..."}]
        /// </summary>
        private void ApplyManagedIdentityConfigurations()
        {
            PackageLog.Log("Looking for managed identity configurations...");

            // Get all settings that end with "_managedidentities"
            var managedIdentitySettings = GetAllManagedIdentitySettings();

            if (managedIdentitySettings == null || managedIdentitySettings.Count == 0)
            {
                PackageLog.Log("No managed identity configurations to apply.");
                return;
            }

            PackageLog.Log($"Found {managedIdentitySettings.Count} managed identity configuration setting(s)");

            int processedCount = 0;
            int skippedCount = 0;

            foreach (var setting in managedIdentitySettings)
            {
                try
                {
                    string solutionName = setting.Key;
                    string base64Json = setting.Value;

                    PackageLog.Log($"Processing managed identities for solution: {solutionName}");

                    // Decode base64 to JSON
                    byte[] bytes = Convert.FromBase64String(base64Json);
                    string json = Encoding.UTF8.GetString(bytes);

                    PackageLog.Log($"Decoded JSON: {json}");

                    // Deserialize JSON array to list of ManagedIdentityConfig
                    var managedIdentities = JsonConvert.DeserializeObject<List<ManagedIdentityConfig>>(json);

                    if (managedIdentities == null || managedIdentities.Count == 0)
                    {
                        PackageLog.Log($"No managed identities found in configuration for solution: {solutionName}");
                        continue;
                    }

                    PackageLog.Log($"Found {managedIdentities.Count} managed identity/identities for solution: {solutionName}");

                    // Process each managed identity
                    foreach (var config in managedIdentities)
                    {
                        try
                        {
                            // Validate the configuration has all required fields
                            if (string.IsNullOrWhiteSpace(config.Name))
                            {
                                PackageLog.Log($"Warning: Missing Name in managed identity config. Skipping.");
                                skippedCount++;
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(config.ApplicationId))
                            {
                                PackageLog.Log($"Warning: Missing ApplicationId for managed identity '{config.Name}'. Skipping.");
                                skippedCount++;
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(config.TenantId))
                            {
                                PackageLog.Log($"Warning: Missing TenantId for managed identity '{config.Name}'. Skipping.");
                                skippedCount++;
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(config.SolutionName))
                            {
                                PackageLog.Log($"Warning: Missing SolutionName for managed identity '{config.Name}'. Skipping.");
                                skippedCount++;
                                continue;
                            }

                            // Find the solution in the target solutions list
                            var solution = TargetSolutions.FirstOrDefault(s =>
                                s.UniqueName.Equals(config.SolutionName, StringComparison.OrdinalIgnoreCase));

                            if (solution == null)
                            {
                                PackageLog.Log($"Warning: Solution '{config.SolutionName}' not found in package for managed identity '{config.Name}'. Available solutions: {string.Join(", ", TargetSolutions.Select(s => s.UniqueName))}");
                                skippedCount++;
                                continue;
                            }

                            PackageLog.Log($"Updating managed identity '{config.Name}' in solution '{solution.UniqueName}' at path: {solution.ZipPath}");

                            // Update the managed identity in the solution using the service
                            ManagedIdentityService.UpdateManagedIdentity(solution.ZipPath, config.Name, config.ApplicationId, config.TenantId);

                            processedCount++;
                            PackageLog.Log($"Successfully updated managed identity '{config.Name}'");
                        }
                        catch (Exception ex)
                        {
                            PackageLog.Log($"Error applying managed identity configuration for '{config.Name}': {ex.Message}");
                            PackageLog.Log($"Stack trace: {ex.StackTrace}");
                            skippedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    PackageLog.Log($"Error processing managed identity setting '{setting.Key}': {ex.Message}");
                    PackageLog.Log($"Stack trace: {ex.StackTrace}");
                    skippedCount++;
                }
            }

            PackageLog.Log($"Managed identity configuration completed. Processed: {processedCount}, Skipped: {skippedCount}");
        }

        /// <summary>
        /// Loads deployment settings by deserializing base64-encoded JSON settings from runtime settings.
        /// Expected settings format: {solutionname}_deploymentsettings={base64_json}
        /// JSON structure: {"EnvironmentVariables":[{"SchemaName":"...","Value":"..."}],"ConnectionReferences":[{"Name":"...","ConnectionId":"..."}]}
        /// </summary>
        private void LoadDeploymentSettings()
        {
            PackageLog.Log("Looking for deployment settings...");

            // Get all settings that end with "_deploymentsettings"
            var deploymentSettingsCollection = GetAllDeploymentSettings();

            if (deploymentSettingsCollection == null || deploymentSettingsCollection.Count == 0)
            {
                PackageLog.Log("No deployment settings found. Using legacy GetSettings approach.");
                
                // Fall back to legacy approach for backward compatibility
                environmentVariables = GetSettings(Constants.DataverseEnvironmentVariablePrefix).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                connectionReferences = GetSettings(Constants.ConnectionReferencePrefix).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                
                PackageLog.Log($"Loaded {environmentVariables.Count} environment variables and {connectionReferences.Count} connection references from legacy settings");
                return;
            }

            PackageLog.Log($"Found {deploymentSettingsCollection.Count} deployment settings configuration(s)");

            environmentVariables = new Dictionary<string, string>();
            connectionReferences = new Dictionary<string, string>();

            foreach (var setting in deploymentSettingsCollection)
            {
                try
                {
                    string solutionName = setting.Key;
                    string base64Json = setting.Value;

                    PackageLog.Log($"Processing deployment settings for solution: {solutionName}");

                    // Decode base64 to JSON
                    byte[] bytes = Convert.FromBase64String(base64Json);
                    string json = Encoding.UTF8.GetString(bytes);

                    PackageLog.Log($"Decoded JSON: {json.Substring(0, Math.Min(200, json.Length))}...");

                    // Deserialize JSON to DeploymentSettings
                    var deploymentSettings = JsonConvert.DeserializeObject<DeploymentSettings>(json);

                    if (deploymentSettings == null)
                    {
                        PackageLog.Log($"Failed to deserialize deployment settings for solution: {solutionName}");
                        continue;
                    }

                    // Process environment variables
                    if (deploymentSettings.EnvironmentVariables != null && deploymentSettings.EnvironmentVariables.Count > 0)
                    {
                        PackageLog.Log($"Found {deploymentSettings.EnvironmentVariables.Count} environment variable(s) for solution: {solutionName}");
                        
                        foreach (var envVar in deploymentSettings.EnvironmentVariables)
                        {
                            if (string.IsNullOrWhiteSpace(envVar.SchemaName))
                            {
                                PackageLog.Log("Warning: Environment variable missing SchemaName. Skipping.");
                                continue;
                            }

                            // Use lowercase key for consistency with GetSettings pattern
                            string key = envVar.SchemaName.ToLower();
                            environmentVariables[key] = envVar.Value ?? string.Empty;
                            
                            PackageLog.Log($"  Loaded environment variable: {envVar.SchemaName}");
                        }
                    }

                    // Process connection references
                    if (deploymentSettings.ConnectionReferences != null && deploymentSettings.ConnectionReferences.Count > 0)
                    {
                        PackageLog.Log($"Found {deploymentSettings.ConnectionReferences.Count} connection reference(s) for solution: {solutionName}");
                        
                        foreach (var connRef in deploymentSettings.ConnectionReferences)
                        {
                            if (string.IsNullOrWhiteSpace(connRef.Name))
                            {
                                PackageLog.Log("Warning: Connection reference missing Name. Skipping.");
                                continue;
                            }

                            // Use lowercase key for consistency with GetSettings pattern
                            string key = connRef.Name.ToLower();
                            connectionReferences[key] = connRef.ConnectionId ?? string.Empty;
                            
                            PackageLog.Log($"  Loaded connection reference: {connRef.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    PackageLog.Log($"Error processing deployment setting '{setting.Key}': {ex.Message}");
                    PackageLog.Log($"Stack trace: {ex.StackTrace}");
                }
            }

            PackageLog.Log($"Deployment settings loaded. Total environment variables: {environmentVariables.Count}, Connection references: {connectionReferences.Count}");
        }

        /// <summary>
        /// Gets all deployment settings from runtime settings.
        /// Settings are expected to be in the format: {solutionname}_deploymentsettings={base64_json}
        /// </summary>
        /// <returns>Dictionary where key is solution name and value is base64 encoded JSON</returns>
        private IDictionary<string, string> GetAllDeploymentSettings()
        {
            var deploymentSettings = new Dictionary<string, string>();

            if (this.RuntimeSettings == null)
            {
                return deploymentSettings;
            }

            // Look for settings ending with "_deploymentsettings"
            const string suffix = "_deploymentsettings";

            foreach (var setting in this.RuntimeSettings)
            {
                if (setting.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    // Extract solution name from the key
                    string solutionName = setting.Key.Substring(0, setting.Key.Length - suffix.Length);
                    deploymentSettings[solutionName] = setting.Value.ToString();

                    PackageLog.Log($"Found deployment settings for solution: {solutionName}");
                }
            }

            return deploymentSettings;
        }

        /// <summary>
        /// Gets all managed identity settings from runtime settings.
        /// Settings are expected to be in the format: {solutionname}_managedidentities={base64_json}
        /// </summary>
        /// <returns>Dictionary where key is solution name and value is base64 encoded JSON</returns>
        private IDictionary<string, string> GetAllManagedIdentitySettings()
        {
            var managedIdentitySettings = new Dictionary<string, string>();

            if (this.RuntimeSettings == null)
            {
                return managedIdentitySettings;
            }

            // Look for settings ending with "_managedidentities"
            const string suffix = "_managedidentities";

            foreach (var setting in this.RuntimeSettings)
            {
                if (setting.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    // Extract solution name from the key
                    string solutionName = setting.Key.Substring(0, setting.Key.Length - suffix.Length);
                    managedIdentitySettings[solutionName] = setting.Value.ToString();

                    PackageLog.Log($"Found managed identity setting for solution: {solutionName}");
                }
            }

            return managedIdentitySettings;
        }

        private IDictionary<string, string> GetSettings(string prefix)
        {
            this.PackageLog.Log($"Getting {prefix} settings");

            var environmentVariables = Environment.GetEnvironmentVariables();
            var mappings = environmentVariables.Keys
                .Cast<string>()
                .Where(k => k.StartsWith($"{Constants.EnvironmentVariablePrefix}{prefix}_", StringComparison.InvariantCultureIgnoreCase))
                .ToDictionary(
                    k => k.Remove(0, Constants.EnvironmentVariablePrefix.Length + prefix.Length + 1).ToLower(),
                    v => environmentVariables[v].ToString());

            this.PackageLog.Log($"{mappings.Count} matching settings found in environment variables");

            if (this.RuntimeSettings == null)
            {
                return mappings;
            }

            var runtimeSettingMappings = this.RuntimeSettings
                .Where(s => s.Key.StartsWith($"{prefix}:"))
                .ToDictionary(kvp => kvp.Key.Remove(0, prefix.Length + 1).ToLower(), kvp => kvp.Value.ToString());

            this.PackageLog.Log($"{mappings.Count} matching settings found in runtime settings");

            foreach (var runtimeSettingsMapping in runtimeSettingMappings)
            {
                if (mappings.ContainsKey(runtimeSettingsMapping.Key))
                {
                    this.PackageLog.Log($"Overriding environment variable setting with runtime setting for {runtimeSettingsMapping.Key}.");
                }

                mappings[runtimeSettingsMapping.Key] = runtimeSettingsMapping.Value;
            }

            return mappings;
        }

    }
}
