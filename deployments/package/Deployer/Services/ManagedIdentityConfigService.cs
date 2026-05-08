using Microsoft.Xrm.Tooling.PackageDeployment.CrmPackageExtentionBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlatformPackage.Services
{
    /// <summary>
    /// Service for configuring managed identities in Dataverse solution packages.
    /// </summary>
    public class ManagedIdentityConfigService
    {
        private readonly TraceLogger _traceLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedIdentityConfigService"/> class.
        /// </summary>
        /// <param name="traceLogger">The logger.</param>
        public ManagedIdentityConfigService(TraceLogger traceLogger)
        {
            _traceLogger = traceLogger ?? throw new ArgumentNullException(nameof(traceLogger));
        }

        /// <summary>
        /// Updates the managed identity configuration in a solution file.
        /// Unzips the solution, updates the managed identity's applicationId and tenantId in customizations.xml,
        /// and rezips the solution.
        /// </summary>
        /// <param name="solutionFilePath">Full path to the solution zip file.</param>
        /// <param name="managedIdentityName">The managed identity name to search for (matches the &lt;name&gt; element value).</param>
        /// <param name="applicationId">The new application ID to set (with or without curly braces).</param>
        /// <param name="tenantId">The new tenant ID to set (with or without curly braces).</param>
        public void UpdateManagedIdentity(string solutionFilePath, string managedIdentityName, string applicationId, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(solutionFilePath))
                throw new ArgumentNullException(nameof(solutionFilePath));

            if (!File.Exists(solutionFilePath))
                throw new FileNotFoundException($"Solution file not found: {solutionFilePath}");

            if (string.IsNullOrWhiteSpace(managedIdentityName))
                throw new ArgumentNullException(nameof(managedIdentityName));

            if (string.IsNullOrWhiteSpace(applicationId))
                throw new ArgumentNullException(nameof(applicationId));

            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            _traceLogger.Log($"Updating managed identity in solution: {solutionFilePath}");
            _traceLogger.Log($"ManagedIdentityName: {managedIdentityName}");
            _traceLogger.Log($"ApplicationId: {applicationId}");
            _traceLogger.Log($"TenantId: {tenantId}");

            // Normalize IDs (remove curly braces if present)
            string normalizedApplicationId = applicationId.Trim('{', '}');
            string normalizedTenantId = tenantId.Trim('{', '}');

            // Create a temporary directory for extraction
            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            try
            {
                // Extract the solution zip
                _traceLogger.Log($"Extracting solution to temporary directory: {tempDirectory}");
                ZipFile.ExtractToDirectory(solutionFilePath, tempDirectory);

                // Find and update customizations.xml
                string customizationsPath = Path.Combine(tempDirectory, "customizations.xml");
                if (!File.Exists(customizationsPath))
                {
                    throw new FileNotFoundException($"customizations.xml not found in solution package.");
                }

                _traceLogger.Log("Loading customizations.xml");
                XDocument customizationsDoc = XDocument.Load(customizationsPath);

                // Find the managed identity element by name
                var managedIdentityElement = customizationsDoc
                    .Descendants("managedidentities")
                    .Elements("managedidentity")
                    .FirstOrDefault(mi =>
                    {
                        var nameElement = mi.Element("name")?.Value;
                        return nameElement != null && nameElement.Equals(managedIdentityName, StringComparison.OrdinalIgnoreCase);
                    });

                if (managedIdentityElement == null)
                {
                    throw new InvalidOperationException($"Managed identity with name '{managedIdentityName}' not found in customizations.xml");
                }

                var managedIdentityId = managedIdentityElement.Attribute("managedidentityid")?.Value ?? "Unknown";
                _traceLogger.Log($"Found managed identity: {managedIdentityName} (ID: {managedIdentityId})");

                // Update applicationid element
                var applicationIdElement = managedIdentityElement.Element("applicationid");
                if (applicationIdElement != null)
                {
                    string oldValue = applicationIdElement.Value;
                    applicationIdElement.Value = $"{{{normalizedApplicationId}}}";
                    _traceLogger.Log($"Updated applicationid from {oldValue} to {applicationIdElement.Value}");
                }
                else
                {
                    _traceLogger.Log("Warning: applicationid element not found, creating new element");
                    managedIdentityElement.Add(new XElement("applicationid", $"{{{normalizedApplicationId}}}"));
                }

                // Update tenantid element
                var tenantIdElement = managedIdentityElement.Element("tenantid");
                if (tenantIdElement != null)
                {
                    string oldValue = tenantIdElement.Value;
                    tenantIdElement.Value = $"{{{normalizedTenantId}}}";
                    _traceLogger.Log($"Updated tenantid from {oldValue} to {tenantIdElement.Value}");
                }
                else
                {
                    _traceLogger.Log("Warning: tenantid element not found, creating new element");
                    managedIdentityElement.Add(new XElement("tenantid", $"{{{normalizedTenantId}}}"));
                }

                // Save the updated customizations.xml
                _traceLogger.Log("Saving updated customizations.xml");
                customizationsDoc.Save(customizationsPath);

                // Delete the original solution file
                _traceLogger.Log($"Deleting original solution file: {solutionFilePath}");
                File.Delete(solutionFilePath);

                // Rezip the solution
                _traceLogger.Log($"Creating updated solution file: {solutionFilePath}");
                ZipFile.CreateFromDirectory(tempDirectory, solutionFilePath, CompressionLevel.Optimal, false);

                _traceLogger.Log("Managed identity update completed successfully");
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDirectory))
                {
                    _traceLogger.Log($"Cleaning up temporary directory: {tempDirectory}");
                    Directory.Delete(tempDirectory, true);
                }
            }
        }
    }
}
