using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.PackageDeployment.CrmPackageExtentionBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformPackage.Services
{
    /// <summary>
    /// Service for configuring environment-level settings in Dataverse.
    /// Handles mapping of dictionary values to Organization entity attributes using metadata.
    /// </summary>
    public class EnvironmentSettingsService
    {
        private readonly IOrganizationService _serviceClient;
        private readonly TraceLogger _traceLogger;

        private const string OrganizationEntityName = "organization";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSettingsService"/> class.
        /// </summary>
        /// <param name="serviceClient">A service client authenticated as a licensed user.</param>
        /// <param name="traceLogger">The logger.</param>
        public EnvironmentSettingsService(IOrganizationService serviceClient, TraceLogger traceLogger)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            _traceLogger = traceLogger ?? throw new ArgumentNullException(nameof(traceLogger));
        }

        /// <summary>
        /// Updates organization settings by mapping dictionary values to the Organization entity.
        /// Uses entity metadata to ensure proper type conversion.
        /// </summary>
        /// <param name="settings">Dictionary of setting names (logical names) and their string values.</param>
        public void UpdateOrganizationSettings(IDictionary<string, string> settings)
        {
            if (settings == null || settings.Count == 0)
            {
                _traceLogger.Log("No organization settings to update.");
                return;
            }

            _traceLogger.Log($"Updating {settings.Count} organization setting(s)");

            // Retrieve organization entity metadata
            var metadata = RetrieveOrganizationMetadata();

            // Get the organization record (there's always only one)
            var organization = RetrieveOrganization();
            if (organization == null)
            {
                throw new InvalidOperationException("Unable to retrieve organization record.");
            }

            _traceLogger.Log($"Retrieved organization record: {organization.Id}");

            // Map settings to entity attributes
            var updateEntity = new Entity(OrganizationEntityName, organization.Id);
            int mappedCount = 0;
            int skippedCount = 0;

            foreach (var setting in settings)
            {
                string attributeName = setting.Key.ToLower();
                object value = setting.Value;

                try
                {
                    // Find the attribute metadata
                    var attributeMetadata = metadata.Attributes
                        .FirstOrDefault(a => a.LogicalName.Equals(attributeName, StringComparison.OrdinalIgnoreCase));

                    if (attributeMetadata == null)
                    {
                        _traceLogger.Log($"Warning: Attribute '{attributeName}' not found in organization metadata. Skipping.");
                        skippedCount++;
                        continue;
                    }

                    // Check if attribute is valid for update
                    if (attributeMetadata.IsValidForUpdate == false)
                    {
                        _traceLogger.Log($"Warning: Attribute '{attributeName}' is not valid for update. Skipping.");
                        skippedCount++;
                        continue;
                    }

                    // Map the value based on attribute type
                    var mappedValue = MapValueToAttributeType(attributeName, value, attributeMetadata);

                    if (mappedValue != null)
                    {
                        updateEntity[attributeName] = mappedValue;
                        _traceLogger.Log($"Mapped setting '{attributeName}' = {value} (Type: {attributeMetadata.AttributeType})");
                        mappedCount++;
                    }
                    else
                    {
                        _traceLogger.Log($"Warning: Unable to map value for '{attributeName}'. Skipping.");
                        skippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _traceLogger.Log($"Error mapping setting '{attributeName}': {ex.Message}");
                    skippedCount++;
                }
            }

            // Update the organization record if there are any changes
            if (updateEntity.Attributes.Count > 0)
            {
                _traceLogger.Log($"Updating organization with {mappedCount} attribute(s)");
                _serviceClient.Update(updateEntity);
                _traceLogger.Log($"Organization settings updated successfully. Mapped: {mappedCount}, Skipped: {skippedCount}");
            }
            else
            {
                _traceLogger.Log("No valid attributes to update.");
            }
        }

        /// <summary>
        /// Retrieves the organization entity metadata including all attributes.
        /// </summary>
        /// <returns>Entity metadata for the organization entity.</returns>
        private EntityMetadata RetrieveOrganizationMetadata()
        {
            _traceLogger.Log("Retrieving organization entity metadata");

            var request = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = OrganizationEntityName,
                RetrieveAsIfPublished = false
            };

            var response = (RetrieveEntityResponse)_serviceClient.Execute(request);

            _traceLogger.Log($"Retrieved metadata for {response.EntityMetadata.Attributes.Length} attributes");

            return response.EntityMetadata;
        }

        /// <summary>
        /// Retrieves the organization record.
        /// </summary>
        /// <returns>The organization entity.</returns>
        private Entity RetrieveOrganization()
        {
            _traceLogger.Log("Retrieving organization record");

            var query = new QueryExpression(OrganizationEntityName)
            {
                ColumnSet = new ColumnSet("organizationid"),
                TopCount = 1
            };

            var results = _serviceClient.RetrieveMultiple(query);

            return results.Entities.FirstOrDefault();
        }

        /// <summary>
        /// Maps a value to the appropriate type based on attribute metadata.
        /// </summary>
        /// <param name="attributeName">The attribute logical name.</param>
        /// <param name="value">The value to map.</param>
        /// <param name="metadata">The attribute metadata.</param>
        /// <returns>The mapped value, or null if mapping failed.</returns>
        private object MapValueToAttributeType(string attributeName, object value, AttributeMetadata metadata)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                switch (metadata.AttributeType)
                {
                    case AttributeTypeCode.String:
                    case AttributeTypeCode.Memo:
                        return value.ToString();

                    case AttributeTypeCode.Integer:
                        return Convert.ToInt32(value);

                    case AttributeTypeCode.BigInt:
                        return Convert.ToInt64(value);

                    case AttributeTypeCode.Decimal:
                        return Convert.ToDecimal(value);

                    case AttributeTypeCode.Double:
                        return Convert.ToDouble(value);

                    case AttributeTypeCode.Money:
                        return new Money(Convert.ToDecimal(value));

                    case AttributeTypeCode.Boolean:
                        return Convert.ToBoolean(value);

                    case AttributeTypeCode.DateTime:
                        if (value is DateTime dateValue)
                        {
                            return dateValue;
                        }
                        return DateTime.Parse(value.ToString());

                    case AttributeTypeCode.Picklist:
                    case AttributeTypeCode.State:
                    case AttributeTypeCode.Status:
                        return new OptionSetValue(Convert.ToInt32(value));

                    case AttributeTypeCode.Lookup:
                    case AttributeTypeCode.Customer:
                    case AttributeTypeCode.Owner:
                        // For lookups, expect a string in format "entityname:guid"
                        if (value is string lookupStr && lookupStr.Contains(":"))
                        {
                            var parts = lookupStr.Split(':');
                            if (parts.Length == 2 && Guid.TryParse(parts[1], out Guid lookupId))
                            {
                                return new EntityReference(parts[0], lookupId);
                            }
                        }
                        _traceLogger.Log($"Warning: Lookup value for '{attributeName}' must be in format 'entityname:guid'");
                        return null;

                    case AttributeTypeCode.Uniqueidentifier:
                        if (value is Guid guidValue)
                        {
                            return guidValue;
                        }
                        return Guid.Parse(value.ToString());

                    case AttributeTypeCode.Virtual:
                    case AttributeTypeCode.EntityName:
                    case AttributeTypeCode.ManagedProperty:
                    case AttributeTypeCode.CalendarRules:
                    case AttributeTypeCode.PartyList:
                        _traceLogger.Log($"Warning: Attribute type {metadata.AttributeType} for '{attributeName}' is not supported for direct mapping.");
                        return null;

                    default:
                        _traceLogger.Log($"Warning: Unknown attribute type {metadata.AttributeType} for '{attributeName}'");
                        return null;
                }
            }
            catch (Exception ex)
            {
                _traceLogger.Log($"Error converting value '{value}' for attribute '{attributeName}': {ex.Message}");
                return null;
            }
        }
    }
}
