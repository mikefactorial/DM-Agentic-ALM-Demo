using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Tooling.PackageDeployment.CrmPackageExtentionBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformPackage.Services
{
    /// <summary>
    /// Deployment functionality for environment variables.
    /// </summary>
    public class EnvironmentVariableService
    {
        private readonly IOrganizationService _serviceClient;
        private readonly TraceLogger _traceLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentVariableService"/> class.
        /// </summary>
        /// <param name="serviceClient">A service client authenticated as a licensed user.</param>
        /// <param name="traceLogger">The logger.</param>
        public EnvironmentVariableService(IOrganizationService serviceClient, TraceLogger traceLogger)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            _traceLogger = traceLogger ?? throw new ArgumentNullException(nameof(traceLogger));
        }

        /// <summary>
        /// Sets an environment variable on the target Dataverse environment.
        /// </summary>
        /// <param name="key">Environment variable key.</param>
        /// <param name="value">Environment variable value.</param>
        public void SetEnvironmentVariable(string key, string value)
        {
            _traceLogger.Log($"Setting {key} environment variable to {value}.");

            var definition = this.GetDefinitionByKey(key, new ColumnSet(false));
            if (definition == null)
            {
                throw new ArgumentException($"Environment variable {key} not found on target instance.");
            }

            var definitionReference = definition.ToEntityReference();
            _traceLogger.Log($"Found environment variable on target instance: {definition.Id}", TraceEventType.Verbose);

            this.UpsertEnvironmentVariableValue(value, definitionReference);
        }

        private void UpsertEnvironmentVariableValue(string value, EntityReference definitionReference)
        {
            var existingValue = this.GetValueByDefinitionId(definitionReference, new ColumnSet("value"));
            if (existingValue != null)
            {
                existingValue["value"] = value;
                _serviceClient.Update(existingValue);
            }
            else
            {
                this.SetValue(value, definitionReference);
            }
        }

        private Entity GetValueByDefinitionId(EntityReference definitionReference, ColumnSet columnSet)
        {
            var definitionQuery = new QueryExpression("environmentvariablevalue")
            {
                ColumnSet = columnSet,
                Criteria = new FilterExpression(),
            };
            definitionQuery.Criteria.AddCondition("environmentvariabledefinitionid", ConditionOperator.Equal, definitionReference.Id);

            return _serviceClient.RetrieveMultiple(definitionQuery).Entities.FirstOrDefault();
        }

        private void SetValue(string value, EntityReference definition)
        {
            var val = new Entity("environmentvariablevalue")
            {
                Attributes = new AttributeCollection
                {
                    { "value", value },
                    { "environmentvariabledefinitionid", definition },
                },
            };

            _serviceClient.Create(val);
        }

        private Entity GetDefinitionByKey(string key, ColumnSet columnSet)
        {
            var definitionQuery = new QueryExpression("environmentvariabledefinition")
            {
                ColumnSet = columnSet,
                Criteria = new FilterExpression(),
            };
            definitionQuery.Criteria.AddCondition("schemaname", ConditionOperator.Equal, key);

            return _serviceClient.RetrieveMultiple(definitionQuery).Entities.FirstOrDefault();
        }
    }
}