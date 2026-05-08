using System;
using System.Linq;
using System.Diagnostics;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.PackageDeployment.CrmPackageExtentionBase;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Messages;


namespace PlatformPackage.Services
{
    public class ConnectionReferenceService
    {
        public const string ConnectionReferenceLogicalName = "connectionreference";

        private readonly IOrganizationService _serviceClient;
        private readonly TraceLogger _traceLogger;

        public ConnectionReferenceService(IOrganizationService serviceClient, TraceLogger traceLogger)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient)); ;
            _traceLogger = traceLogger ?? throw new ArgumentNullException(nameof(traceLogger)); ;
        }

        public void SetConnectionReferences(IDictionary<string, string> connections, string connectionOwner = null)
        {
            if (connections is null || !connections.Any())
            {
                _traceLogger.Log("No connections provided. Skipping.");
                return;
            }

            var updateRequests = this.GetConnectionReferences(connections.Keys.ToArray())
                .Select(e => new Microsoft.Xrm.Sdk.Messages.UpdateRequest
                {
                    Target = new Entity(ConnectionReferenceLogicalName)
                    {
                        Id = e.Id,
                        Attributes =
                        {
                            {
                                "connectionreferenceid",
                                e.Id
                            },
                            {
                                "connectionid",
                                connections[e.GetAttributeValue<string>("connectionreferencelogicalname").ToLower()]
                            },
                        },
                    },
                }).ToList();

            // Send the execute multiple request to update the connection references in batch
            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ContinueOnError = true,
                    ReturnResponses = true,
                },
                Requests = new OrganizationRequestCollection(),
            };
            executeMultipleRequest.Requests.AddRange(updateRequests);

            ExecuteMultipleResponse executeMultipleResponse = null;
            executeMultipleResponse = (ExecuteMultipleResponse)this._serviceClient.Execute(executeMultipleRequest);
            if (executeMultipleResponse.IsFaulted)
            {
                foreach (var response in executeMultipleResponse.Responses.Where(r => r.Fault != null).Select(r => r.Fault))
                {
                    _traceLogger.Log(response.Message);
                }
            }
        }

        private IEnumerable<Entity> GetConnectionReferences(IEnumerable<object> logicalNames)
        {
            if (logicalNames == null)
            {
                throw new ArgumentNullException(nameof(logicalNames));
            }

            // query expression to retrieve the connection references filtered by the connectionreferencelogicalname attribute
            var query = new QueryExpression(ConnectionReferenceLogicalName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression()
                {
                    Conditions =
                    {
                        new ConditionExpression
                        (
                            "connectionreferencelogicalname",
                            ConditionOperator.In,
                            logicalNames.ToArray()

                        )
                    },
                }
            };
            return _serviceClient.RetrieveMultiple(query).Entities.ToList();

        }

    }
}